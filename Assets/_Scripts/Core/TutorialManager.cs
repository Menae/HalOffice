using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("チュートリアル専用ウィンドウ")]
    [Tooltip("ドラッグ練習用のウィンドウ（パネル）")]
    public GameObject dragPracticeWindow;
    [Tooltip("ドラッグ練習用のダミーアイテム（リセット用）")]
    public UIDraggable dummyDragItem;
    [Tooltip("ゴミ捨て練習用のウィンドウ（パネル）")]
    public GameObject trashPracticeWindow;
    [Tooltip("ゴミ捨て練習用のダミーアイテム（リセット用）")]
    public UIDraggable dummyTrashItem;

    public bool IsPlayingEffect { get; private set; } = false;
    private bool isTutorialFinished = false;
    private bool cancelVideoLoops = false;
    private Coroutine currentEffectCoroutine = null;
    private bool isWaitingForPlacement = false;
    private bool isWaitingForTrash = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        ChatController.OnTagsProcessed += HandleTags;

        // ▼▼▼ 追加 ▼▼▼
        if (DragDropManager.Instance != null)
        {
            DragDropManager.Instance.OnItemPlaced += OnUserPlacedItem;
            DragDropManager.Instance.OnItemTrashed += OnUserTrashedItem;
        }
    }

    private void OnDisable()
    {
        ChatController.OnTagsProcessed -= HandleTags;

        // ▼▼▼ 追加 ▼▼▼
        if (DragDropManager.Instance != null)
        {
            DragDropManager.Instance.OnItemPlaced -= OnUserPlacedItem;
            DragDropManager.Instance.OnItemTrashed -= OnUserTrashedItem;
        }
    }

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
                    if (currentEffectCoroutine != null) StopCoroutine(currentEffectCoroutine);
                    currentEffectCoroutine = StartCoroutine(ProcessHighlightTag(value));
                    break;
                case "highlight_off":
                    DeactivateAllHighlights();
                    break;
                case "show_gif":
                    if (currentEffectCoroutine != null) StopCoroutine(currentEffectCoroutine);
                    currentEffectCoroutine = StartCoroutine(ShowTutorialVideo(value));
                    break;
                case "wait_for_drag":
                    StartCoroutine(PrepareInteractionWait(true, false));
                    break;
                case "wait_for_trash":
                    StartCoroutine(PrepareInteractionWait(false, true));
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
        currentEffectCoroutine = null; // 待機が終了したので参照をクリア

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
        currentEffectCoroutine = null; // 待機が終了したので参照をクリア
    }

    private IEnumerator FinishTutorialSequence()
    {
        isTutorialFinished = true;
        ResetEffects();
        Debug.Log("チュートリアルが完全に終了しました。");

        if (day1Manager != null) day1Manager.ShowStartGameButton();

        yield return null;
        gameObject.SetActive(false);
    }

    // ユーザーの操作待ちを開始する
    private IEnumerator PrepareInteractionWait(bool waitForPlacement, bool waitForTrash)
    {
        Debug.Log($"操作練習開始: Placement={waitForPlacement}, Trash={waitForTrash}");

        IsPlayingEffect = true;
        isWaitingForPlacement = waitForPlacement;
        isWaitingForTrash = waitForTrash;

        // 入力を許可
        if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(true);
        if (DragDropManager.Instance != null) DragDropManager.Instance.SetInteractionEnabled(true);

        // 専用ウィンドウの表示処理
        if (waitForPlacement)
        {
            if (dragPracticeWindow != null)
            {
                dragPracticeWindow.SetActive(true);
                // ダミーアイテムを初期状態（使用可能）に戻す
                if (dummyDragItem != null) ResetDummyItem(dummyDragItem);
            }
        }
        else if (waitForTrash)
        {
            if (trashPracticeWindow != null)
            {
                trashPracticeWindow.SetActive(true);
                // ダミーアイテムを初期状態に戻す
                if (dummyTrashItem != null) ResetDummyItem(dummyTrashItem);
            }
        }

        yield return null;
    }

    // ダミーアイテムの状態をリセットするヘルパーメソッド
    private void ResetDummyItem(UIDraggable item)
    {
        item.ResetState();
    }

    // アイテム配置成功時に呼ばれる
    private void OnUserPlacedItem()
    {
        if (isWaitingForPlacement)
        {
            Debug.Log("チュートリアル: 配置成功を確認");
            CompleteInteractionStep();
        }
    }

    // ゴミ捨て成功時に呼ばれる
    private void OnUserTrashedItem()
    {
        if (isWaitingForTrash)
        {
            Debug.Log("チュートリアル: ゴミ捨て成功を確認");
            CompleteInteractionStep();
        }
    }

    // ステップ完了処理
    private void CompleteInteractionStep()
    {
        // ウィンドウを閉じる
        if (dragPracticeWindow != null) dragPracticeWindow.SetActive(false);
        if (trashPracticeWindow != null) trashPracticeWindow.SetActive(false);

        isWaitingForPlacement = false;
        isWaitingForTrash = false;

        // 操作を再び無効化
        if (DragDropManager.Instance != null)
            DragDropManager.Instance.SetInteractionEnabled(false);

        IsPlayingEffect = false;

        if (ChatController.Instance != null)
        {
            ChatController.Instance.AdvanceConversation();
        }
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

        if (dragPracticeWindow != null) dragPracticeWindow.SetActive(false);
        if (trashPracticeWindow != null) trashPracticeWindow.SetActive(false);
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

    /// <summary>
    /// ChatControllerから呼び出す
    /// 現在再生中の全てのチュートリアルエフェクト（ハイライト、動画など）を
    /// 強制的に停止し、待機状態を解除する。
    /// </summary>
    public void ForceStopAllEffects()
    {
        Debug.Log("強制スキップ：エフェクトを強制停止します。");

        // 1. 待機させているコルーチン（ハイライトや動画の1周目）が
        //    動いていれば、即座に強制停止する
        if (currentEffectCoroutine != null)
        {
            StopCoroutine(currentEffectCoroutine);
            currentEffectCoroutine = null;
        }

        // 2. 既存の ResetEffects() を呼び出す
        //    これにより、動画のバックグラウンドループが停止し、
        //    全てのハイライトと動画パネルが非表示になる
        ResetEffects();

        // 3. ChatControllerの待機を即座に解除する
        //    (IsPlayingEffectがtrueのまま停止した場合に備える)
        IsPlayingEffect = false;
    }
}
