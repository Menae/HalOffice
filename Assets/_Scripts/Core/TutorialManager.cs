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

[System.Serializable]
public class TutorialVideo
{
    [Tooltip("Inkファイルで指定する際の名前（例: DetectionMeter, HowToMove）")]
    public string name;
    [Tooltip("再生するVideoPlayerコンポーネント")]
    public UnityEngine.Video.VideoPlayer player;
    [Tooltip("対応する動画パネル")]
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
    [Tooltip("Inkタグから呼び出せる動画リスト")]
    public List<TutorialVideo> tutorialVideos;

    public bool IsPlayingEffect { get; private set; } = false;
    private bool isTutorialFinished = false;
    private bool cancelVideoLoops = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable() => ChatController.OnTagsProcessed += HandleTags;
    private void OnDisable() => ChatController.OnTagsProcessed -= HandleTags;

    private void Start() => ResetEffects();

    public void StartTutorial()
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(TutorialSequence());
    }

    private IEnumerator TutorialSequence()
    {
        Debug.Log("チュートリアルシーケンス開始。");
        if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(false);

        // チュートリアル中はドラッグ＆クリック無効化
        if (DragDropManager.Instance != null)
            DragDropManager.Instance.SetInteractionEnabled(false);

        ResetEffects();

        ChatController chat = GlobalUIManager.Instance.chatController;
        if (chat != null && tutorialInk != null)
            chat.StartConversation(tutorialInk);
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
                    StartCoroutine(ShowTutorialVideo(value)); // ←新しいメソッドに変更
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

    private IEnumerator ShowTutorialVideo(string videoName)
    {
        IsPlayingEffect = true;
        cancelVideoLoops = false;

        TutorialVideo video = tutorialVideos.Find(v => v.name == videoName);
        if (video == null || video.player == null || video.panel == null)
        {
            Debug.LogWarning($"チュートリアル動画 '{videoName}' が見つかりません。");
            IsPlayingEffect = false;
            yield break;
        }

        video.panel.SetActive(true);
        video.player.isLooping = true;

        bool firstLoopEnded = false;
        UnityEngine.Video.VideoPlayer.EventHandler handler = (vp) => { firstLoopEnded = true; };
        video.player.loopPointReached += handler;

        video.player.Prepare();
        yield return new WaitUntil(() => video.player.isPrepared);
        video.player.Play();
        yield return new WaitUntil(() => video.player.isPlaying);

        // 1周目の終了を待機
        yield return new WaitUntil(() => firstLoopEnded);

        // 会話はまだ進めないが、入力受付だけを再開する
        IsPlayingEffect = false;
        if (GameManager.Instance != null)
            GameManager.Instance.SetInputEnabled(true);

        // 自動進行はさせない
        video.player.loopPointReached -= handler;

        // reset_effects が呼ばれるまでループ再生を継続
        while (!cancelVideoLoops && video.player != null && video.player.isLooping)
        {
            yield return null;
        }

        if (video.player != null) video.player.Stop();
        video.panel.SetActive(false);
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

    private IEnumerator FinishTutorialSequence()
    {
        isTutorialFinished = true;
        ResetEffects();
        Debug.Log("チュートリアルが完全に終了しました。");

        if (DragDropManager.Instance != null)
            DragDropManager.Instance.SetInteractionEnabled(true); // 再び有効化

        if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(true);
        if (day1Manager != null) day1Manager.ShowStartGameButton();

        yield return null;
        gameObject.SetActive(false);
    }

    private void ResetEffects()
    {
        cancelVideoLoops = true;

        DeactivateAllHighlights();

        if (tutorialVideos != null)
        {
            foreach (var v in tutorialVideos)
            {
                if (v.player != null)
                {
                    v.player.isLooping = false;
                    v.player.Stop();
                }
                if (v.panel != null) v.panel.SetActive(false);
            }
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
                target.panel.SetActive(false);
        }
    }
}
