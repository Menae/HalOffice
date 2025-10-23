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

    [Header("発言者プロフィール (Speaker Profiles)")]
    [SerializeField] private List<SpeakerProfile> speakerProfiles;

    private Dictionary<string, Sprite> speakerIconDatabase;
    private Story currentStory;

    [System.Serializable]
    public class SpeakerProfile
    {
        public string tag;
        public Sprite icon;
    }

    // IsConversationFinished, Awake, StartConversation, AdvanceConversation, ToggleChatWindow, InitializeDatabase, DisplayLine は変更なし

    // DisplayChoices, MakeChoice, GetPrefabFromTags, GetSpeakerIconFromTags, RebuildLog, DisplayLineWithoutScroll, ScrollToBottom は変更なし

    // ▼▼▼【修正】ForceScrollDownメソッドを修正 ▼▼▼
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

    // (変更のないメソッドは省略)
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

    public void StartConversation(TextAsset inkJsonAsset)
    {
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

        if (GameManager.Instance != null) GameManager.Instance.conversationLog.Clear();
        foreach (Transform child in contentContainerDefault) Destroy(child.gameObject);
        foreach (Transform child in contentContainerSpecial) Destroy(child.gameObject);

        currentStory = new Story(inkJsonAsset.text);
        AdvanceConversation();
    }

    public void AdvanceConversation()
    {
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