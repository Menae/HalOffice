using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class HighlightTarget
{
    [Tooltip("Inkファイルで指定する際の名前（例: curtain, item_box）")]
    public string name;
    [Tooltip("実際に表示するハイライト用のパネル")]
    public GameObject panel;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("参照")]
    public Day1Manager day1Manager;

    [Header("チュートリアル設定")]
    public TextAsset tutorialInk;

    [Header("演出用オブジェクト")]
    public List<HighlightTarget> highlightTargets;
    public UnityEngine.Video.VideoPlayer videoPlayer;
    public GameObject videoPanel;

    public bool IsPlayingEffect { get; private set; } = false;
    private bool isTutorialFinished = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void OnEnable()
    {
        ChatController.OnTagsProcessed += HandleTags;
    }

    private void OnDisable()
    {
        ChatController.OnTagsProcessed -= HandleTags;
    }

    private void Start()
    {
        ResetEffects();
    }

    public void StartTutorial()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(TutorialSequence());
        }
    }

    private IEnumerator TutorialSequence()
    {
        Debug.Log("チュートリアルシーケンス開始。");
        if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(false);

        ResetEffects();

        ChatController chat = GlobalUIManager.Instance.chatController;

        if (chat != null && tutorialInk != null)
        {
            chat.StartConversation(tutorialInk);
        }
        else
        {
            Debug.LogError("ChatControllerまたはTutorialInkが見つかりません！");
            if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(true);
            if (day1Manager != null) day1Manager.StartGame();
            gameObject.SetActive(false);
        }
        yield return null;
    }

    private void HandleTags(List<string> tags)
    {
        if (isTutorialFinished) return;

        foreach (string tag in tags)
        {
            string[] parts = tag.Split(':');
            if (parts.Length == 0) continue;

            string key = parts[0].Trim();
            string value = parts.Length > 1 ? parts[1].Trim() : "";

            switch (key)
            {
                case "highlight":
                    StartCoroutine(ProcessHighlightTag(value));
                    break;
                case "highlight_off":
                    DeactivateAllHighlights();
                    break;
                case "show_gif":
                    StartCoroutine(ShowGif(value));
                    break;
                case "tutorial_end":
                    StartCoroutine(FinishTutorialSequence());
                    break;
                case "reset_effects":
                    ResetEffects();
                    break;
            }
        }
    }

    private IEnumerator FinishTutorialSequence()
    {
        isTutorialFinished = true;
        ResetEffects();

        Debug.Log("チュートリアルが完全に終了しました。");
        if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(true);
        if (day1Manager != null) day1Manager.StartGame();

        Debug.Log("ゲームを開始します。");
        yield return null;
        gameObject.SetActive(false);
    }

    private IEnumerator ShowGif(string gifName)
    {
        IsPlayingEffect = true;
        Debug.Log($"{gifName} の動画を再生します。");

        if (videoPlayer != null && videoPanel != null)
        {
            videoPanel.SetActive(true);
            videoPlayer.Play();
            StartCoroutine(HideVideoWhenFinished());
        }

        yield return new WaitForSeconds(1.0f);
        IsPlayingEffect = false;
    }

    private IEnumerator HideVideoWhenFinished()
    {
        // isPlayingがfalseになる（＝再生が終わる）まで待機
        yield return new WaitForSeconds(0.1f);
        yield return new WaitUntil(() => videoPlayer != null && !videoPlayer.isPlaying);

        if (videoPanel != null)
        {
            videoPanel.SetActive(false);
        }
    }

    private IEnumerator ProcessHighlightTag(string highlightData)
    {
        IsPlayingEffect = true;
        DeactivateAllHighlights();
        string[] targetsToShow = highlightData.Split(',');
        foreach (string targetName in targetsToShow)
        {
            ActivateHighlight(targetName.Trim());
        }
        yield return new WaitForSeconds(1.0f);
        IsPlayingEffect = false;
    }

    /// <summary>
    /// 全ての演出用オブジェクトを非表示にするリセット処理
    /// </summary>
    private void ResetEffects()
    {
        DeactivateAllHighlights();
        if (videoPanel != null)
        {
            videoPanel.SetActive(false);
        }
    }

    private void ActivateHighlight(string name)
    {
        HighlightTarget target = highlightTargets.Find(ht => ht.name == name);
        if (target != null && target.panel != null)
        {
            target.panel.SetActive(true);
            Debug.Log($"ハイライト表示: {name}");
        }
        else
        {
            Debug.LogWarning($"ハイライトターゲット '{name}' が見つかりません。");
        }
    }

    private void DeactivateAllHighlights()
    {
        if (highlightTargets == null) return;
        foreach (var target in highlightTargets)
        {
            if (target.panel != null)
            {
                target.panel.SetActive(false);
            }
        }
    }
}