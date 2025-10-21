using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Inkストーリーを再生し、会話UI(吹き出し、選択肢など)の生成と管理を行うシングルトンクラス。
/// ゲーム内の様々なトリガーから呼び出され、指定された会話劇を演出します。
/// </summary>
public class ChatController : MonoBehaviour
{
    // --- シングルトンインスタンス ---
    public static ChatController Instance { get; private set; }

    /// <summary>
    /// 会話が完全に終了したことを外部のシステムに通知するための静的イベント。
    /// </summary>
    public static event System.Action OnConversationFinished;


    // --- Inspectorで設定する項目 ---

    [Header("UI参照 (UI References)")]
    [Tooltip("表示/非表示を切り替えたいチャットパネル本体")]
    [SerializeField] private GameObject chatPanel;
    [Tooltip("会話ログをスクロールさせるためのScroll Rectコンポーネント")]
    [SerializeField] private ScrollRect scrollRect;
    [Tooltip("吹き出しが生成される親オブジェクト(ScrollViewのContent)")]
    [SerializeField] private RectTransform contentContainer;
    [Tooltip("選択肢のボタンが生成される親オブジェクト")]
    [SerializeField] private GameObject choicesContainer;

    [Header("吹き出しプレハブ (Bubble Prefabs)")]
    [Tooltip("発言者アイコンが左にある吹き出しのプレハブ")]
    [SerializeField] private GameObject bubblePrefabLeft;
    [Tooltip("発言者アイコンが右にある吹き出しのプレハブ")]
    [SerializeField] private GameObject bubblePrefabRight;
    [Tooltip("アイコンがないナレーション用の吹き出しプレハブ")]
    [SerializeField] private GameObject bubblePrefabSystem;
    [Tooltip("選択肢ボタンのプレハブ")]
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("発言者プロフィール (Speaker Profiles)")]
    [Tooltip("Inkのタグと、それに対応するアイコン画像を登録するリスト")]
    [SerializeField] private List<SpeakerProfile> speakerProfiles;


    // --- 内部処理用の変数 ---
    private Dictionary<string, Sprite> speakerIconDatabase;
    private Story currentStory;

    /// <summary>
    /// Inspector上で発言者のタグとアイコン画像をセットで管理するためのクラス。
    /// </summary>
    [System.Serializable]
    public class SpeakerProfile
    {
        [Tooltip("Inkファイルで使うタグ名(例: Boss, Player_Happy)")]
        public string tag;
        [Tooltip("上記タグに対応するキャラクターのアイコン画像")]
        public Sprite icon;
    }

    #region Unity Lifecycle Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
        InitializeDatabase();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 新しい会話を開始する。DesktopManagerなど外部のトリガーから呼び出す。
    /// </summary>
    /// <param name="inkJsonAsset">再生するInkストーリーのJSONアセット</param>
    public void StartConversation(TextAsset inkJsonAsset)
    {
        // GlobalUIManagerに必要な参照が揃っているか確認
        GlobalUIManager manager = GlobalUIManager.Instance;
        if (manager == null || chatPanel == null || manager.layoutDefault == null || manager.layoutSpecial == null)
        {
            Debug.LogError("ChatControllerの起動に必要な参照がGlobalUIManagerに設定されていません。");
            return;
        }

        // ウィンドウを開き、適切なレイアウトを選択する
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

        // ログなどを初期化して会話を開始
        if (GameManager.Instance != null) GameManager.Instance.conversationLog.Clear();
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        currentStory = new Story(inkJsonAsset.text);
        AdvanceConversation();
    }

    /// <summary>
    /// 会話を一行進める。UIのクリックイベント(EventTrigger)から呼び出す。
    /// </summary>
    public void AdvanceConversation()
    {
        if (choicesContainer.activeInHierarchy) return;

        if (currentStory != null && !currentStory.canContinue)
        {
            OnConversationFinished?.Invoke();
            if (chatPanel != null) chatPanel.SetActive(false); // 修正点：nullチェックを追加
            currentStory = null;
            return;
        }

        if (currentStory == null)
        {
            chatPanel.SetActive(false); // パネルを閉じる
            return;                     // 処理を終了する
        }

        string currentLine = currentStory.Continue();
        List<string> currentTags = new List<string>(currentStory.currentTags);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.conversationLog.Add(new DialogueLineData { text = currentLine, tags = currentTags });
        }

        DisplayLine(currentLine, currentTags);

        if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
    }

    /// <summary>
    /// チャットウィンドウの表示/非表示を切り替える
    /// </summary>
    public void ToggleChatWindow()
    {
        // GlobalUIManagerに必要な参照が揃っているか確認
        GlobalUIManager manager = GlobalUIManager.Instance;
        if (manager == null || chatPanel == null || manager.layoutDefault == null || manager.layoutSpecial == null) return;

        // ウィンドウの表示/非表示を切り替える
        bool willBeActive = !chatPanel.activeSelf;
        chatPanel.SetActive(willBeActive);

        // もしウィンドウを表示状態にしたなら
        if (willBeActive)
        {
            // 適切なレイアウトを選択
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

            // ログを再構築
            RebuildLog();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// InspectorのSpeakerProfilesリストを元に、アイコン検索用のデータベースを構築する。
    /// </summary>
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

    /// <summary>
    /// 受け取った情報から、適切な吹き出しを生成して表示する。
    /// </summary>
    private void DisplayLine(string text, List<string> tags)
    {
        GameObject prefabToUse = GetPrefabFromTags(tags);
        Sprite iconToUse = GetSpeakerIconFromTags(tags);

        GameObject newBubbleObject = Instantiate(prefabToUse, contentContainer);

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
    /// Inkストーリーに選択肢がある場合に、UIボタンを生成して表示する。
    /// </summary>
    private void DisplayChoices()
    {
        // 以前の選択肢が残っていれば削除
        foreach (Transform child in choicesContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // 選択肢の数だけボタンを生成
        foreach (Choice choice in currentStory.currentChoices)
        {
            GameObject choiceButtonObject = Instantiate(choiceButtonPrefab, choicesContainer.transform);

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

        choicesContainer.SetActive(true);
    }

    /// <summary>
    /// プレイヤーが選択肢ボタンをクリックした時の処理。
    /// </summary>
    private void MakeChoice(int choiceIndex)
    {
        currentStory.ChooseChoiceIndex(choiceIndex);
        choicesContainer.SetActive(false);
        AdvanceConversation(); // 選択後の会話に進む
    }

    /// <summary>
    /// Inkタグを解析し、使用すべきプレハブを返す。
    /// </summary>
    private GameObject GetPrefabFromTags(List<string> tags)
    {
        if (tags != null)
        {
            foreach (string tag in tags)
            {
                if (tag.StartsWith("prefab:"))
                {
                    string prefabType = tag.Substring("prefab:".Length).Trim().ToLower();
                    switch (prefabType)
                    {
                        case "left": return bubblePrefabLeft;
                        case "right": return bubblePrefabRight;
                        case "system": return bubblePrefabSystem;
                    }
                }
            }
        }
        return bubblePrefabLeft; // タグが見つからない場合のデフォルト
    }

    /// <summary>
    /// Inkタグを解析し、使用すべきアイコン画像を返す。
    /// </summary>
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
            // "speaker:"接頭辞なしの短いタグにも対応
            if (speakerIconDatabase.ContainsKey(tag))
            {
                return speakerIconDatabase[tag];
            }
        }
        return null; // 対応するアイコンが見つからなければnull
    }

    /// <summary>
    /// GameManagerに保存されたログを元に会話を再生成する
    /// </summary>
    private void RebuildLog()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        if (GameManager.Instance == null) return;
        
        foreach (var logEntry in GameManager.Instance.conversationLog)
        {
            DisplayLine(logEntry.text, logEntry.tags);
        }
    }

    /// <summary>
    /// RebuildLog専用。スクロール処理を伴わないDisplayLine。
    /// </summary>
    private void DisplayLineWithoutScroll(string text, List<string> tags)
    {
        GameObject prefabToUse = GetPrefabFromTags(tags);
        Sprite iconToUse = GetSpeakerIconFromTags(tags);

        GameObject newBubbleObject = Instantiate(prefabToUse, contentContainer);
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

    #endregion

    #region Coroutines

    private void ScrollToBottom()
    {
        StartCoroutine(ForceScrollDown());
    }

    private IEnumerator ForceScrollDown()
    {
        // レイアウトグループの計算が完了するのを待つため、フレームの終わりに実行
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    #endregion
}