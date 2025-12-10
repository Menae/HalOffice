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

/// <summary>
/// チャット会話の制御を行うシングルトンコンポーネント。
/// 会話の開始、進行、スキップ、選択肢表示、ログ再構築を担当する。
/// Unityイベント順序: <see cref="Awake"/> でシングルトン初期化、外部から <see cref="StartConversation"/> を呼んで開始する想定。
/// </summary>
public class ChatController : MonoBehaviour
{
    /// <summary>
    /// グローバルアクセス用インスタンス。
    /// Awakeで初期化。複数インスタンスが存在する場合、後続は破棄。
    /// </summary>
    public static ChatController Instance { get; private set; }

    /// <summary>
    /// 会話が完全に終了したときに発行されるイベント（選択肢や継続がない状態）。
    /// </summary>
    public static event System.Action OnConversationFinished;

    /// <summary>
    /// Inkのタグを処理した直後に発行されるイベント。タグ一覧を受け取る。
    /// </summary>
    public static event System.Action<List<string>> OnTagsProcessed;

    [Header("UI参照 (UI References)")]
    [SerializeField] private GameObject chatPanel; // InspectorでD&D: チャットUIのルート
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
    [SerializeField] private List<SpeakerProfile> speakerProfiles; // InspectorでD&D: タグに対応するアイコンを登録

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
    private const int RAPID_CLICK_TARGET = 8;           // 8回連打でスキップ提示

    [System.Serializable]
    public class SpeakerProfile
    {
        public string tag;
        public Sprite icon;
    }

    /// <summary>
    /// フレーム末にスクロールを最下部に移動するためのコルーチン。
    /// レイアウトによって使用する <see cref="ScrollRect"/> を選択し、垂直位置を 0 に設定する。
    /// </summary>
    private IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();

        var manager = GlobalUIManager.Instance;
        if (manager == null) yield break;

        ScrollRect activeScrollRect = manager.layoutDefault.activeSelf ? scrollRectDefault : scrollRectSpecial;

        if (activeScrollRect != null)
        {
            activeScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 現在のストーリーが終了状態かを判定する。
    /// currentStory が null または継続不能かつ選択肢がない場合に true を返す。
    /// </summary>
    public bool IsConversationFinished()
    {
        return currentStory == null || (!currentStory.canContinue && currentStory.currentChoices.Count == 0);
    }

    /// <summary>
    /// シングルトンの初期化とデータベース構築を行う。
    /// Awake順序に依存するため、他コンポーネントの Awake より後でアクセスされることがある点に注意。
    /// </summary>
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
        InitializeDatabase();
    }

    /// <summary>
    /// 毎フレームの入力監視処理。
    /// 会話中かつスキップボタン未表示のときにキー入力／連打検出でスキップボタンを表示する。
    /// 副作用: スキップボタンの表示状態を変更する。
    /// </summary>
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
    /// スキップボタンを表示に切り替える。skipButton が null の場合は何もしない。
    /// UIの副作用あり: skipButton を SetActive(true)、内部フラグを更新。
    /// </summary>
    private void ShowSkipButton()
    {
        if (skipButton == null) return;
        skipButton.SetActive(true);
        isSkipButtonVisible = true;
    }

    /// <summary>
    /// UIのスキップボタンから呼ばれる。スキップシーケンスを開始する。
    /// 既にスキップ中またはストーリー未設定の場合は無視する。
    /// 副作用: 選択肢を閉じ、Tutorial のエフェクトを停止し、コルーチンを開始する。
    /// </summary>
    public void StartSkipSequence()
    {
        if (isSkipping || currentStory == null) return;

        isSkipping = true;
        if (skipButton != null) skipButton.SetActive(false);
        isSkipButtonVisible = false;

        // 選択肢が表示されていれば強制的に閉じる
        if (choicesContainerDefault != null) choicesContainerDefault.SetActive(false);
        if (choicesContainerSpecial != null) choicesContainerSpecial.SetActive(false);

        // 現在再生中のエフェクト(動画/ハイライト)を強制停止する
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ForceStopAllEffects();
        }

        StartCoroutine(SkipRoutine());
    }

    /// <summary>
    /// スキップ処理のコルーチン。
    /// スキップ中はストーリーを進め、タグやログの一部を扱いつつ、skip_targetタグに到達または選択肢出現で停止する。
    /// 副作用: DisplayLine を呼ぶため UI を生成する。最後に <see cref="AdvanceConversation"/> を呼ぶ。
    /// </summary>
    private IEnumerator SkipRoutine()
    {
        while (isSkipping && currentStory.canContinue)
        {
            // 途中で選択肢が出てきたら、スキップは強制停止する
            if (currentStory.currentChoices.Count > 0)
            {
                isSkipping = false;
                break;
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

            // タグ処理は呼び出さない（スキップ中はイベントを発行しない設計）

            // #skip_targetタグがあるかチェック
            if (currentTags.Contains("skip_target"))
            {
                Debug.Log("スキップターゲットに到達。スキップを終了します。");
                isSkipping = false;
                break;
            }

            // インスペクタで設定した秒数だけ待機する（0なら次フレーム）
            if (skipLineDelay > 0)
            {
                yield return new WaitForSeconds(skipLineDelay);
            }
            else
            {
                yield return null;
            }
        }

        isSkipping = false;

        // 最後の処理（選択肢の表示、または会話終了処理）を呼ぶ
        AdvanceConversation();
    }

    /// <summary>
    /// 会話を Ink JSON から開始する。
    /// 引数: Ink のテキストアセット。サウンドを再生し、UIレイアウトを選択、ログ初期化、既存バブル削除、Storyを生成して進行を開始する。
    /// 入力の前提: GlobalUIManager の参照が正しく設定されていること。
    /// </summary>
    /// <param name="inkJsonAsset">InkのJSONを含むTextAsset（Inspectorでアタッチしたアセット）。</param>
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

        // スキップ関連のフラグとUIをリセット
        isSkipping = false;
        isSkipButtonVisible = false;
        rapidClickCount = 0;
        if (skipButton != null) skipButton.SetActive(false);

        if (GameManager.Instance != null) GameManager.Instance.conversationLog.Clear();

        if (contentContainerDefault != null)
        {
            foreach (Transform child in contentContainerDefault) Destroy(child.gameObject);
        }
        if (contentContainerSpecial != null)
        {
            foreach (Transform child in contentContainerSpecial) Destroy(child.gameObject);
        }

        currentStory = new Story(inkJsonAsset.text);
        AdvanceConversation();
    }

    /// <summary>
    /// 会話を一段階進める。スキップ中やエフェクト再生中は何もしない。
    /// currentStory.canContinue が true の場合に Continue() を呼び、タグを発行して表示を行う。
    /// 選択肢が出現したら <see cref="DisplayChoices"/> を呼ぶ。会話終了時はイベントを発行してUIを閉じる。
    /// </summary>
    public void AdvanceConversation()
    {
        // スキップモード中は、このメソッドは何もしない (SkipRoutineが全権を握る)
        if (isSkipping) return;

        // エフェクト再生中は待機
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsPlayingEffect)
        {
            return;
        }

        var manager = GlobalUIManager.Instance;
        if (manager == null) return;

        GameObject activeChoicesContainer = manager.layoutDefault.activeSelf ? choicesContainerDefault : choicesContainerSpecial;
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
            // 会話終了時のクリーンアップ
            isSkipping = false;
            isSkipButtonVisible = false;
            rapidClickCount = 0;
            if (skipButton != null) skipButton.SetActive(false);

            OnConversationFinished?.Invoke();
            if (chatPanel != null) chatPanel.SetActive(false);
            currentStory = null;
        }
    }

    /// <summary>
    /// チャットウィンドウの表示/非表示をトグルする。
    /// 表示時はレイアウトを選択して過去ログを再構築する。音が設定されていれば再生する。
    /// </summary>
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

    /// <summary>
    /// SpeakerProfileリストから辞書を構築する。すでに構築済みなら何もしない。
    /// Inspectorの設定が不足している場合は登録をスキップする。
    /// </summary>
    private void InitializeDatabase()
    {
        if (speakerIconDatabase != null) return;

        speakerIconDatabase = new Dictionary<string, Sprite>();
        if (speakerProfiles == null) return;

        foreach (var profile in speakerProfiles)
        {
            if (!string.IsNullOrEmpty(profile.tag) && profile.icon != null)
            {
                speakerIconDatabase[profile.tag] = profile.icon;
            }
        }
    }

    /// <summary>
    /// テキストとタグを受け取り、適切な吹き出しプレハブを生成して表示する。
    /// 表示先コンテナは現在のレイアウトに応じて選択する。生成後に自動でスクロールを最下部へ移動する。
    /// 副作用: UIオブジェクト生成および GameManager のログ追加。
    /// </summary>
    /// <param name="text">表示するテキスト（空白は無視しないが空文字は生成しない。）</param>
    /// <param name="tags">Inkから渡されたタグ群（null可）。</param>
    private void DisplayLine(string text, List<string> tags)
    {
        GameObject prefabToUse = GetPrefabFromTags(tags);
        Sprite iconToUse = GetSpeakerIconFromTags(tags);

        var manager = GlobalUIManager.Instance;
        if (manager == null) return;

        RectTransform activeContainer = manager.layoutDefault.activeSelf ? contentContainerDefault : contentContainerSpecial;
        if (activeContainer == null) return;

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

    /// <summary>
    /// 現在のストーリーの選択肢をUIに生成して表示する。
    /// 既存の選択肢はすべて破棄してから生成する。ボタンのクリックで <see cref="MakeChoice(int)"/> を呼ぶ。
    /// 注意: choice.index をローカル変数に格納してクロージャ問題を回避。
    /// </summary>
    private void DisplayChoices()
    {
        if (choicesContainerDefault != null)
        {
            foreach (Transform child in choicesContainerDefault.transform) Destroy(child.gameObject);
            choicesContainerDefault.SetActive(false);
        }
        if (choicesContainerSpecial != null)
        {
            foreach (Transform child in choicesContainerSpecial.transform) Destroy(child.gameObject);
            choicesContainerSpecial.SetActive(false);
        }

        var manager = GlobalUIManager.Instance;
        if (manager == null) return;

        GameObject activeContainer = manager.layoutDefault.activeSelf ? choicesContainerDefault : choicesContainerSpecial;
        GameObject prefabToUse = manager.layoutDefault.activeSelf ? choiceButtonPrefabDefault : choiceButtonPrefabSpecial;

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
                int idx = choice.index; // foreachのクロージャ対策
                button.onClick.AddListener(() => MakeChoice(idx));
            }
        }
        if (activeContainer != null) activeContainer.SetActive(true);
    }

    /// <summary>
    /// 選択肢を選んだときに呼ばれる。選択肢UIを閉じ、Inkにインデックスを渡して会話を進める。
    /// 副作用: currentStory に対する ChooseChoiceIndex を呼び出す。
    /// </summary>
    /// <param name="choiceIndex">選択肢のインデックス</param>
    private void MakeChoice(int choiceIndex)
    {
        if (choicesContainerDefault != null) choicesContainerDefault.SetActive(false);
        if (choicesContainerSpecial != null) choicesContainerSpecial.SetActive(false);

        currentStory.ChooseChoiceIndex(choiceIndex);
        AdvanceConversation();
    }

    /// <summary>
    /// タグから使用する吹き出しプレハブを決定する。
    /// "prefab:left/right/system" の形式を優先して解釈する。該当タグがなければ左プレハブを返す。
    /// </summary>
    /// <param name="tags">Inkタグ一覧（null可）</param>
    /// <returns>利用する吹き出しプレハブ（nullになる可能性あり）</returns>
    private GameObject GetPrefabFromTags(List<string> tags)
    {
        var manager = GlobalUIManager.Instance;
        BubblePrefabSet activePrefabSet = manager.layoutDefault.activeSelf ? defaultPrefabs : specialPrefabs;

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

    /// <summary>
    /// タグから発言者アイコンを決定する。
    /// "speaker:キー" を優先して解釈。キーが見つからなければタグそのものをキーとして辞書を検索する。
    /// 見つからない場合は null を返す。
    /// </summary>
    private Sprite GetSpeakerIconFromTags(List<string> tags)
    {
        if (tags == null) return null;
        if (speakerIconDatabase == null) InitializeDatabase();

        foreach (string tag in tags)
        {
            if (tag.StartsWith("speaker:"))
            {
                string speakerKey = tag.Substring("speaker:".Length).Trim();
                if (speakerIconDatabase != null && speakerIconDatabase.ContainsKey(speakerKey))
                {
                    return speakerIconDatabase[speakerKey];
                }
            }
            if (speakerIconDatabase != null && speakerIconDatabase.ContainsKey(tag))
            {
                return speakerIconDatabase[tag];
            }
        }
        return null;
    }

    /// <summary>
    /// 過去の会話ログからUIを再構築する。
    /// 既存バブルを削除した上で GameManager の conversationLog を辿って再生成する。
    /// </summary>
    private void RebuildLog()
    {
        if (contentContainerDefault != null)
        {
            foreach (Transform child in contentContainerDefault) Destroy(child.gameObject);
        }
        if (contentContainerSpecial != null)
        {
            foreach (Transform child in contentContainerSpecial) Destroy(child.gameObject);
        }

        if (GameManager.Instance == null) return;

        foreach (var logEntry in GameManager.Instance.conversationLog)
        {
            DisplayLineWithoutScroll(logEntry.text, logEntry.tags);
        }
    }

    /// <summary>
    /// スクロール移動を行わずにログ用の吹き出しを生成する。RebuildLogから使用する。
    /// 副作用: UIオブジェクトを生成するがスクロールは動かさない。
    /// </summary>
    /// <param name="text">表示するテキスト</param>
    /// <param name="tags">タグ一覧（null可）</param>
    private void DisplayLineWithoutScroll(string text, List<string> tags)
    {
        GameObject prefabToUse = GetPrefabFromTags(tags);
        Sprite iconToUse = GetSpeakerIconFromTags(tags);

        var manager = GlobalUIManager.Instance;
        if (manager == null) return;

        RectTransform activeContainer = manager.layoutDefault.activeSelf ? contentContainerDefault : contentContainerSpecial;
        if (activeContainer == null) return;

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

    /// <summary>
    /// スクロールを最下部に移動する処理をコルーチンで呼ぶ。UI生成直後に呼ぶことを想定。
    /// </summary>
    private void ScrollToBottom()
    {
        StartCoroutine(ForceScrollDown());
    }
}