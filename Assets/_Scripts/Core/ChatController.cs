using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class BubblePrefabSet
{
    [Tooltip("発言者アイコンが左にある吹き出しのプレハブ")]
    public GameObject bubblePrefabLeft;
    [Tooltip("発言者アイコンが右にある吹き出しのプレハブ")]
    public GameObject bubblePrefabRight;
    [Tooltip("アイコンがないナレーション用の吹き出しのプレハブ")]
    public GameObject bubblePrefabSystem;
}

public class ChatController : MonoBehaviour
{
    public static ChatController Instance { get; private set; }

    public static event System.Action OnConversationFinished;
    public static event System.Action<List<string>> OnTagsProcessed;

    [Header("UI参照 (UI References)")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private ScrollRect scrollRectDefault;
    [SerializeField] private ScrollRect scrollRectSpecial;
    [SerializeField] private RectTransform contentContainerDefault;
    [SerializeField] private RectTransform contentContainerSpecial;
    [SerializeField] private GameObject choicesContainerDefault;
    [SerializeField] private GameObject choicesContainerSpecial;

    [Header("吹き出しプレハブ (Bubble Prefabs)")]
    [SerializeField] private BubblePrefabSet defaultPrefabs;
    [SerializeField] private BubblePrefabSet specialPrefabs;

    [Header("選択肢ボタン (Choice Buttons)")]
    [SerializeField] private GameObject choiceButtonPrefabDefault;
    [SerializeField] private GameObject choiceButtonPrefabSpecial;

    [Header("スキップ機能 (Skip Feature)")]
    [Tooltip("スキップボタンのGameObject")]
    [SerializeField] private GameObject skipButton;
    [Tooltip("スキップ演出時、1行ごとに待機する秒数（0で最速）")]
    [SerializeField] private float skipLineDelay = 0.05f;

    [Header("発言者プロフィール (Speaker Profiles)")]
    [SerializeField] private List<SpeakerProfile> speakerProfiles;

    [Header("サウンド設定 (Sound Settings)")]
    [Tooltip("効果音を再生するためのAudioSource")]
    public AudioSource audioSource;
    [Tooltip("チャットウィンドウが開く時の効果音")]
    public AudioClip openWindowSound;
    [Range(0f, 1f)]
    [Tooltip("ウィンドウが開く効果音の音量")]
    public float openWindowVolume = 1.0f;

    // --- 内部変数 ---
    private Dictionary<string, Sprite> speakerIconDatabase;
    private Story currentStory;

    private bool isSkipping = false;
    private bool isSkipButtonVisible = false;

    // 連打検出用の設定
    private int rapidClickCount = 0;
    private float lastClickTime = 0f;
    private const float RAPID_CLICK_THRESHOLD = 0.5f;   // 0.5秒以内のクリック
    private const int RAPID_CLICK_TARGET = 8;           // 3回連打

    [System.Serializable]
    public class SpeakerProfile
    {
        public string tag;
        public Sprite icon;
    }

    private IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();

        // 現在アクティブなレイアウトに応じて、適切なScrollRectを選択する
        ScrollRect activeScrollRect = GlobalUIManager.Instance.layoutDefault.activeSelf
                                      ? scrollRectDefault
                                      : scrollRectSpecial;

        if (activeScrollRect != null)
        {
            activeScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public bool IsConversationFinished()
    {
        return currentStory == null || !currentStory.canContinue && currentStory.currentChoices.Count == 0;
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
        InitializeDatabase();
    }

    private void Update()
    {
        // スキップボタンが既に表示されているか、会話中でなければ検出しない
        if (isSkipButtonVisible || currentStory == null || !chatPanel.activeSelf)
        {
            return;
        }

        bool skipTriggered = false;

        // 1. キー入力の検出 (スペース、ESC、エンター)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
        {
            skipTriggered = true;
        }

        // 2. 左クリック連打の検出
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastClickTime < RAPID_CLICK_THRESHOLD)
            {
                rapidClickCount++;
            }
            else
            {
                rapidClickCount = 1; // 1回目としてリセット
            }
            lastClickTime = Time.time;

            if (rapidClickCount >= RAPID_CLICK_TARGET)
            {
                skipTriggered = true;
            }
        }

        // 3. トリガーされたらボタン表示
        if (skipTriggered)
        {
            ShowSkipButton();
        }
    }

    /// <summary>
    /// スキップボタンを表示状態にする
    /// </summary>
    private void ShowSkipButton()
    {
        if (skipButton == null) return;
        skipButton.SetActive(true);
        isSkipButtonVisible = true;
    }

    /// <summary>
    /// 【UIから呼び出す】スキップボタンのOnClickに設定する
    /// </summary>
    public void StartSkipSequence()
    {
        if (isSkipping || currentStory == null) return;

        isSkipping = true;
        if (skipButton != null) skipButton.SetActive(false); // ボタンは再度非表示に
        isSkipButtonVisible = false; // 検出も停止

        // 選択肢が表示されていれば、強制的に閉じる
        if (choicesContainerDefault != null) choicesContainerDefault.SetActive(false);
        if (choicesContainerSpecial != null) choicesContainerSpecial.SetActive(false);

        // 現在再生中のエフェクト(動画/ハイライト)を強制停止する
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ForceStopAllEffects();
        }

        // スキップ処理本体を開始
        StartCoroutine(SkipRoutine());
    }

    private IEnumerator SkipRoutine()
    {
        // スキップモード中、かつストーリーが続く限りループ
        while (isSkipping && currentStory.canContinue)
        {
            // 途中で選択肢が出てきたら、スキップは強制停止する
            if (currentStory.currentChoices.Count > 0)
            {
                isSkipping = false;
                break; // ループを抜ける
            }

            // AdvanceConversation (簡易版) を実行
            string currentLine = currentStory.Continue();
            List<string> currentTags = new List<string>(currentStory.currentTags);

            // ログには追加しておく
            if (GameManager.Instance != null)
            {
                GameManager.Instance.conversationLog.Add(new DialogueLineData { text = currentLine, tags = currentTags });
            }

            // スキップ中もUI表示(DisplayLine)は行う
            if (!string.IsNullOrWhiteSpace(currentLine))
            {
                DisplayLine(currentLine, currentTags);
            }

            // タグ処理は呼び出さない

            // #skip_targetタグがあるかチェック
            if (currentTags.Contains("skip_target"))
            {
                Debug.Log("スキップターゲットに到達。スキップを終了します。");
                isSkipping = false; // スキップモード終了
                break; // ループを抜ける
            }

            // インスペクタで設定した秒数だけ待機する
            if (skipLineDelay > 0)
            {
                yield return new WaitForSeconds(skipLineDelay);
            }
            else
            {
                // 0秒が設定されている場合は、1フレーム待機
                yield return null;
            }
        }

        // ループが完了 (skip_targetに着いたか、選択肢/終端に到達した)
        isSkipping = false;

        // 最後の処理（選択肢の表示、または会話終了処理）を呼ぶ
        AdvanceConversation();
    }

    public void StartConversation(TextAsset inkJsonAsset)
    {
        if (audioSource != null && openWindowSound != null)
        {
            audioSource.PlayOneShot(openWindowSound, openWindowVolume);
        }

        GlobalUIManager manager = GlobalUIManager.Instance;
        if (manager == null || chatPanel == null || manager.layoutDefault == null || manager.layoutSpecial == null)
        {
            Debug.LogError("ChatControllerの起動に必要な参照がGlobalUIManagerに設定されていません。");
            return;
        }

        chatPanel.SetActive(true);
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (manager.specialLayoutScenes.Contains(currentSceneName))
        {
            manager.layoutDefault.SetActive(false);
            manager.layoutSpecial.SetActive(true);
        }
        else
        {
            manager.layoutDefault.SetActive(true);
            manager.layoutSpecial.SetActive(false);
        }

        // スキップフラグをリセット
        isSkipping = false;
        isSkipButtonVisible = false;
        rapidClickCount = 0;
        if (skipButton != null) skipButton.SetActive(false);

        if (GameManager.Instance != null) GameManager.Instance.conversationLog.Clear();
        foreach (Transform child in contentContainerDefault) Destroy(child.gameObject);
        foreach (Transform child in contentContainerSpecial) Destroy(child.gameObject);

        currentStory = new Story(inkJsonAsset.text);
        AdvanceConversation();
    }

    public void AdvanceConversation()
    {
        // スキップモード中は、このメソッドは何もしない (SkipRoutineが全権を握る)
        if (isSkipping) return;

        // 通常のエフェクト待機
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsPlayingEffect)
        {
            return;
        }

        GameObject activeChoicesContainer = GlobalUIManager.Instance.layoutDefault.activeSelf
                                            ? choicesContainerDefault
                                            : choicesContainerSpecial;
        if (activeChoicesContainer != null && activeChoicesContainer.activeInHierarchy) return;

        if (currentStory == null)
        {
            if (chatPanel != null) chatPanel.SetActive(false);
            return;
        }

        if (currentStory.canContinue)
        {
            string currentLine = currentStory.Continue();
            List<string> currentTags = new List<string>(currentStory.currentTags);

            if (currentTags.Count > 0)
            {
                OnTagsProcessed?.Invoke(currentTags);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.conversationLog.Add(new DialogueLineData { text = currentLine, tags = currentTags });
            }

            if (!string.IsNullOrWhiteSpace(currentLine))
            {
                DisplayLine(currentLine, currentTags);
            }
        }

        if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else if (!currentStory.canContinue)
        {
            // 会話終了時もフラグをリセット
            isSkipping = false;
            isSkipButtonVisible = false;
            rapidClickCount = 0;
            if (skipButton != null) skipButton.SetActive(false);

            OnConversationFinished?.Invoke();
            if (chatPanel != null) chatPanel.SetActive(false);
            currentStory = null;
        }
    }

    public void ToggleChatWindow()
    {
        GlobalUIManager manager = GlobalUIManager.Instance;
        if (manager == null || chatPanel == null || manager.layoutDefault == null || manager.layoutSpecial == null) return;

        bool willBeActive = !chatPanel.activeSelf;
        chatPanel.SetActive(willBeActive);

        if (willBeActive && audioSource != null && openWindowSound != null)
        {
            audioSource.PlayOneShot(openWindowSound, openWindowVolume);
        }

        if (willBeActive)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (manager.specialLayoutScenes.Contains(currentSceneName))
            {
                manager.layoutDefault.SetActive(false);
                manager.layoutSpecial.SetActive(true);
            }
            else
            {
                manager.layoutDefault.SetActive(true);
                manager.layoutSpecial.SetActive(false);
            }
            RebuildLog();
        }
    }

    private void InitializeDatabase()
    {
        if (speakerIconDatabase != null) return;

        speakerIconDatabase = new Dictionary<string, Sprite>();
        foreach (var profile in speakerProfiles)
        {
            if (!string.IsNullOrEmpty(profile.tag) && profile.icon != null)
            {
                speakerIconDatabase[profile.tag] = profile.icon;
            }
        }
    }

    private void DisplayLine(string text, List<string> tags)
    {
        GameObject prefabToUse = GetPrefabFromTags(tags);
        Sprite iconToUse = GetSpeakerIconFromTags(tags);

        RectTransform activeContainer = GlobalUIManager.Instance.layoutDefault.activeSelf ? contentContainerDefault : contentContainerSpecial;

        GameObject newBubbleObject = Instantiate(prefabToUse, activeContainer);

        DialogueBubble bubble = newBubbleObject.GetComponent<DialogueBubble>();
        if (bubble == null)
        {
            Debug.LogError("プレハブにDialogueBubbleスクリプトがアタッチされていません！", newBubbleObject);
            return;
        }

        if (bubble.textComponent != null) bubble.textComponent.text = text;

        if (bubble.iconImage != null)
        {
            if (iconToUse != null)
            {
                bubble.iconImage.sprite = iconToUse;
                bubble.iconImage.gameObject.SetActive(true);
            }
            else
            {
                bubble.iconImage.gameObject.SetActive(false);
            }
        }
        ScrollToBottom();
    }

    private void DisplayChoices()
    {
        foreach (Transform child in choicesContainerDefault.transform) Destroy(child.gameObject);
        foreach (Transform child in choicesContainerSpecial.transform) Destroy(child.gameObject);
        choicesContainerDefault.SetActive(false);
        choicesContainerSpecial.SetActive(false);

        GameObject activeContainer = GlobalUIManager.Instance.layoutDefault.activeSelf
                                     ? choicesContainerDefault
                                     : choicesContainerSpecial;
        GameObject prefabToUse = GlobalUIManager.Instance.layoutDefault.activeSelf
                                 ? choiceButtonPrefabDefault
                                 : choiceButtonPrefabSpecial;

        if (prefabToUse == null)
        {
            Debug.LogError("使用する選択肢ボタンのプレハブが設定されていません！");
            return;
        }

        foreach (Choice choice in currentStory.currentChoices)
        {
            GameObject choiceButtonObject = Instantiate(prefabToUse, activeContainer.transform);
            TextMeshProUGUI buttonText = choiceButtonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.text;
            }
            Button button = choiceButtonObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => MakeChoice(choice.index));
            }
        }
        activeContainer.SetActive(true);
    }

    private void MakeChoice(int choiceIndex)
    {
        if (choicesContainerDefault != null) choicesContainerDefault.SetActive(false);
        if (choicesContainerSpecial != null) choicesContainerSpecial.SetActive(false);

        currentStory.ChooseChoiceIndex(choiceIndex);
        AdvanceConversation();
    }

    private GameObject GetPrefabFromTags(List<string> tags)
    {
        BubblePrefabSet activePrefabSet = GlobalUIManager.Instance.layoutDefault.activeSelf
                                          ? defaultPrefabs
                                          : specialPrefabs;
        if (tags != null)
        {
            foreach (string tag in tags)
            {
                if (tag.StartsWith("prefab:"))
                {
                    string prefabType = tag.Substring("prefab:".Length).Trim().ToLower();
                    switch (prefabType)
                    {
                        case "left": return activePrefabSet.bubblePrefabLeft;
                        case "right": return activePrefabSet.bubblePrefabRight;
                        case "system": return activePrefabSet.bubblePrefabSystem;
                    }
                }
            }
        }
        return activePrefabSet.bubblePrefabLeft;
    }

    private Sprite GetSpeakerIconFromTags(List<string> tags)
    {
        if (tags == null) return null;

        foreach (string tag in tags)
        {
            if (tag.StartsWith("speaker:"))
            {
                string speakerKey = tag.Substring("speaker:".Length).Trim();
                if (speakerIconDatabase.ContainsKey(speakerKey))
                {
                    return speakerIconDatabase[speakerKey];
                }
            }
            if (speakerIconDatabase.ContainsKey(tag))
            {
                return speakerIconDatabase[tag];
            }
        }
        return null;
    }

    private void RebuildLog()
    {
        foreach (Transform child in contentContainerDefault) Destroy(child.gameObject);
        foreach (Transform child in contentContainerSpecial) Destroy(child.gameObject);

        if (GameManager.Instance == null) return;

        foreach (var logEntry in GameManager.Instance.conversationLog)
        {
            DisplayLineWithoutScroll(logEntry.text, logEntry.tags);
        }
    }

    private void DisplayLineWithoutScroll(string text, List<string> tags)
    {
        GameObject prefabToUse = GetPrefabFromTags(tags);
        Sprite iconToUse = GetSpeakerIconFromTags(tags);

        RectTransform activeContainer = GlobalUIManager.Instance.layoutDefault.activeSelf ? contentContainerDefault : contentContainerSpecial;

        GameObject newBubbleObject = Instantiate(prefabToUse, activeContainer);
        DialogueBubble bubble = newBubbleObject.GetComponent<DialogueBubble>();
        if (bubble == null) return;

        if (bubble.textComponent != null) bubble.textComponent.text = text;
        if (bubble.iconImage != null)
        {
            if (iconToUse != null)
            {
                bubble.iconImage.sprite = iconToUse;
                bubble.iconImage.gameObject.SetActive(true);
            }
            else
            {
                bubble.iconImage.gameObject.SetActive(false);
            }
        }
    }

    private void ScrollToBottom()
    {
        StartCoroutine(ForceScrollDown());
    }
}