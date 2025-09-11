using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;

/// <summary>
/// Inkストーリーを再生し、会話UI（吹き出し、選択肢など）を管理するコントローラー
/// </summary>
public class ChatController : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("会話ログをスクロールさせるためのScroll Rect")]
    [SerializeField] private ScrollRect scrollRect;
    [Tooltip("吹き出しが生成される親オブジェクト(ScrollViewのContent)")]
    [SerializeField] private RectTransform contentContainer;
    [Tooltip("会話を進めるための、画面全体を覆う透明なボタン")]
    [SerializeField] private Button advanceButton;
    [Tooltip("選択肢を並べる親オブジェクト")]
    [SerializeField] private GameObject choicesContainer;

    [Header("吹き出しプレハブ")]
    [Tooltip("発言者アイコンが左にある吹き出しのプレハブ")]
    [SerializeField] private GameObject bubblePrefabLeft;
    [Tooltip("発言者アイコンが右にある吹き出しのプレハブ")]
    [SerializeField] private GameObject bubblePrefabRight;
    [Tooltip("アイコンがないナレーション用の吹き出しプレハブ")]
    [SerializeField] private GameObject bubblePrefabSystem;
    [Tooltip("選択肢ボタンのプレハブ")]
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("発言者プロフィール")]
    [Tooltip("Inkのタグと、それに対応するアイコン画像を登録するリスト")]
    [SerializeField] private List<SpeakerProfile> speakerProfiles;

    // 発言者のタグとアイコン画像を紐づけるための内部データベース
    private Dictionary<string, Sprite> speakerIconDatabase;
    private Story currentStory;

    /// <summary>
    /// 発言者のタグとアイコン画像をセットで管理するためのクラス
    /// </summary>
    [System.Serializable]
    public class SpeakerProfile
    {
        [Tooltip("Inkファイルで使うタグ名(例: boss_normal, player_happy)")]
        public string tag;
        [Tooltip("上記タグに対応するキャラクターのアイコン画像")]
        public Sprite icon;
    }

    private void Awake()
    {
        InitializeDatabase();

        if (advanceButton != null)
        {
            advanceButton.onClick.AddListener(AdvanceConversation);
        }
    }

    /// <summary>
    /// SpeakerProfilesリストを元に、アイコン検索用のデータベースを初期化する
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
    /// 新しい会話を開始するためのメソッド。DesktopManagerなどから呼び出す。
    /// </summary>
    public void StartConversation(TextAsset inkJsonAsset)
    {
        this.gameObject.SetActive(true);

        if (speakerIconDatabase == null)
        {
            InitializeDatabase();
        }

        // 以前の会話ログを全て削除
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        currentStory = new Story(inkJsonAsset.text);
        AdvanceConversation();
    }

    /// <summary>
    /// 会話を一行進める、または会話を終了する
    /// </summary>
    public void AdvanceConversation()
    {
        // プレイヤーが選択肢を選んでいる最中は、会話を進めない
        if (choicesContainer.activeInHierarchy) return;

        // これ以上会話が続かない場合は、ChatPanel自身を非アクティブにして終了
        if (currentStory != null && !currentStory.canContinue)
        {
            this.gameObject.SetActive(false);
            return;
        }

        if (currentStory == null) return;

        // Inkから次の行の情報を取得
        string currentLine = currentStory.Continue();
        List<string> currentTags = currentStory.currentTags;

        // 新しい吹き出しを表示
        DisplayLine(currentLine, currentTags);

        // もしこの後に選択肢があれば、それを表示する
        if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
    }

    /// <summary>
    /// 受け取った情報から、適切な吹き出しを生成して表示する
    /// </summary>
    private void DisplayLine(string text, List<string> tags)
    {
        GameObject prefabToUse = GetPrefabFromTags(tags);
        Sprite iconToUse = GetSpeakerIconFromTags(tags);

        GameObject newBubbleObject = Instantiate(prefabToUse, contentContainer);

        DialogueBubble bubble = newBubbleObject.GetComponent<DialogueBubble>();
        if (bubble == null)
        {
            Debug.LogError("プレハブにDialogueBubbleスクリプトがアタッチされていません!", newBubbleObject);
            return;
        }

        // 仲介役を通じて、安全かつ高速に各部品にアクセスする
        if (bubble.textComponent != null)
        {
            bubble.textComponent.text = text;
        }

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
    /// プレイヤーに選択肢を提示する
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
    /// プレイヤーが選択肢を選んだ時の処理
    /// </summary>
    private void MakeChoice(int choiceIndex)
    {
        currentStory.ChooseChoiceIndex(choiceIndex);
        choicesContainer.SetActive(false);
        AdvanceConversation();
    }

    /// <summary>
    /// Inkタグを解析し、使用すべきプレハブを返す
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
        return bubblePrefabLeft; // デフォルトは左吹き出し
    }

    /// <summary>
    /// Inkタグを解析し、使用すべきアイコン画像を返す
    /// </summary>
    private Sprite GetSpeakerIconFromTags(List<string> tags)
    {
        if (tags == null) return null;

        foreach (string tag in tags)
        {
            // speaker: タグの場合、":"以降の部分だけをキーとして使う
            if (tag.StartsWith("speaker:"))
            {
                string speakerKey = tag.Substring("speaker:".Length).Trim();
                if (speakerIconDatabase.ContainsKey(speakerKey))
                {
                    return speakerIconDatabase[speakerKey];
                }
            }
            // 短いタグにも対応
            if (speakerIconDatabase.ContainsKey(tag))
            {
                return speakerIconDatabase[tag];
            }
        }
        return null;
    }

    private void ScrollToBottom()
    {
        StartCoroutine(ForceScrollDown());
    }

    private IEnumerator ForceScrollDown()
    {
        // レイアウトグループの計算が終わるのを待ってからスクロール位置を正しく設定する
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}