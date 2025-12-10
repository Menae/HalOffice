using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HighlightTarget
{
    /// <summary>
    /// Inkファイルで指定する際の名前（例: curtain, item_box）。Inspectorで入力。
    /// </summary>
    [Tooltip("Inkファイルで指定する際の名前（例: curtain, item_box）")]
    public string name;

    /// <summary>
    /// 実際に表示するハイライト用のパネル。InspectorでD&D。
    /// nullチェックは呼び出し側で行う。
    /// </summary>
    [Tooltip("実際に表示するハイライト用のパネル")]
    public GameObject panel;
}

[System.Serializable]
public class TutorialVideo
{
    /// <summary>
    /// Inkファイルで指定する際の名前（例: DetectionMeter, HowToMove）。Inspectorで入力。
    /// </summary>
    [Tooltip("Inkファイルで指定する際の名前（例: DetectionMeter, HowToMove）")]
    public string name;

    /// <summary>
    /// 再生するVideoPlayerコンポーネント。InspectorでD&D。nullチェックあり。
    /// </summary>
    [Tooltip("再生するVideoPlayerコンポーネント")]
    public UnityEngine.Video.VideoPlayer player;

    /// <summary>
    /// 対応する動画パネル。InspectorでD&D。
    /// </summary>
    [Tooltip("対応する動画パネル")]
    public GameObject panel;
}

/// <summary>
/// チュートリアルの演出と進行を管理するシングルトンコンポーネント。
/// チャットのタグ処理に応じてハイライト表示、動画再生、操作練習ウィンドウ表示を行う。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    /// <summary>
    /// グローバルアクセス用インスタンス。Awakeで設定。
    /// </summary>
    public static TutorialManager Instance { get; private set; }

    [Header("参照")]
    /// <summary>
    /// デイ1進行管理クラス。InspectorでD&D。nullの場合は一部処理をスキップ。
    /// </summary>
    public Day1Manager day1Manager;

    [Header("チュートリアル設定")]
    /// <summary>
    /// チュートリアル用Inkファイル。InspectorでD&D。nullなら対話開始しない。
    /// </summary>
    public TextAsset tutorialInk;

    [Header("演出用オブジェクト")]
    /// <summary>
    /// ハイライト対象リスト。Inspectorで設定。
    /// </summary>
    public List<HighlightTarget> highlightTargets;

    /// <summary>
    /// Inkタグから呼び出せる動画リスト。各要素はVideoPlayerとパネルを持つ。Inspectorで設定。
    /// </summary>
    [Tooltip("Inkタグから呼び出せる動画リスト")]
    public List<TutorialVideo> tutorialVideos;

    [Header("チュートリアル専用ウィンドウ")]
    /// <summary>
    /// ドラッグ練習用のウィンドウ（パネル）。InspectorでD&D。
    /// </summary>
    [Tooltip("ドラッグ練習用のウィンドウ（パネル）")]
    public GameObject dragPracticeWindow;

    /// <summary>
    /// ドラッグ練習用のダミーアイテム（リセット用）。InspectorでD&D。
    /// </summary>
    [Tooltip("ドラッグ練習用のダミーアイテム（リセット用）")]
    public UIDraggable dummyDragItem;

    /// <summary>
    /// ゴミ捨て練習用のウィンドウ（パネル）。InspectorでD&D。
    /// </summary>
    [Tooltip("ゴミ捨て練習用のウィンドウ（パネル）")]
    public GameObject trashPracticeWindow;

    /// <summary>
    /// ゴミ捨て練習用のダミーアイテム（リセット用）。InspectorでD&D。
    /// </summary>
    [Tooltip("ゴミ捨て練習用のダミーアイテム（リセット用）")]
    public UIDraggable dummyTrashItem;

    /// <summary>
    /// 現在エフェクト（ハイライト/動画の初回再生など）を再生中かどうかを示すフラグ。
    /// ChatControllerの進行制御に使用。読み取りのみ外部公開。
    /// </summary>
    public bool IsPlayingEffect { get; private set; } = false;

    private bool isTutorialFinished = false;
    private bool cancelVideoLoops = false;
    private Coroutine currentEffectCoroutine = null;
    private bool isWaitingForPlacement = false;
    private bool isWaitingForTrash = false;

    /// <summary>
    /// Unity Awake。シングルトンの初期化を行う。早期実行が必要な初期化時に呼ばれる。
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Unity OnEnable。ChatControllerのタグ処理イベントに登録。
    /// DragDropManagerが存在する場合は操作完了イベントを登録。
    /// </summary>
    private void OnEnable()
    {
        ChatController.OnTagsProcessed += HandleTags;

        if (DragDropManager.Instance != null)
        {
            DragDropManager.Instance.OnItemPlaced += OnUserPlacedItem;
            DragDropManager.Instance.OnItemTrashed += OnUserTrashedItem;
        }
    }

    /// <summary>
    /// Unity OnDisable。イベント登録を解除して参照リークを防止。
    /// </summary>
    private void OnDisable()
    {
        ChatController.OnTagsProcessed -= HandleTags;

        if (DragDropManager.Instance != null)
        {
            DragDropManager.Instance.OnItemPlaced -= OnUserPlacedItem;
            DragDropManager.Instance.OnItemTrashed -= OnUserTrashedItem;
        }
    }

    /// <summary>
    /// Unity Start。最初にエフェクト状態をリセットする。
    /// 呼び出しタイミング: Awakeの後、最初のフレームの直前。
    /// </summary>
    private void Start() => ResetEffects();

    /// <summary>
    /// チュートリアル開始を要求。オブジェクトが有効な階層にある場合にシーケンスを開始する。
    /// </summary>
    public void StartTutorial()
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(TutorialSequence());
    }

    /// <summary>
    /// チュートリアルシーケンスの開始処理。入力を一時的に無効化し、Ink会話を開始する。
    /// </summary>
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

    /// <summary>
    /// ChatControllerから渡されるタグ列を処理して対応する演出を開始する。
    /// タグ形式: key:value または key。valueはカンマ区切りで複数指定可能。
    /// </summary>
    /// <param name="tags">処理対象のタグリスト。</param>
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

    /// <summary>
    /// 指定された動画を再生し、1周目終了後に入力を解放する。
    /// その後は reset_effects が呼ばれるまでループ再生を継続する。
    /// </summary>
    /// <param name="videoName">再生する動画の名前（tutorialVideosで一致するname）。</param>
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

        // 会話はまだ進めないが、入力受付だけを再開
        IsPlayingEffect = false;
        currentEffectCoroutine = null; // 待機が終了したので参照をクリア

        if (GameManager.Instance != null)
            GameManager.Instance.SetInputEnabled(true);

        // 自動進行はしないためハンドラを解除
        video.player.loopPointReached -= handler;

        // reset_effects が呼ばれるまでループ再生を継続
        while (!cancelVideoLoops && video.player != null && video.player.isLooping)
        {
            yield return null;
        }

        if (video.player != null) video.player.Stop();
        video.panel.SetActive(false);
    }

    /// <summary>
    /// ハイライトタグを処理して指定ターゲットを表示する。表示後に短時間待機して終了。
    /// </summary>
    /// <param name="highlightData">カンマ区切りのターゲット名リスト。</param>
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

    /// <summary>
    /// チュートリアル完了の最終処理。内部フラグを立て、開始ボタンを表示してこのオブジェクトを無効化する。
    /// </summary>
    private IEnumerator FinishTutorialSequence()
    {
        isTutorialFinished = true;
        ResetEffects();
        Debug.Log("チュートリアルが完全に終了しました。");

        if (day1Manager != null) day1Manager.ShowStartGameButton();

        yield return null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// ユーザーの操作待ちを開始する。ドラッグ配置またはゴミ捨てのいずれかを待つ。
    /// ウィンドウ表示とダミーアイテムの初期化を行う。
    /// </summary>
    /// <param name="waitForPlacement">配置を待つ場合はtrue。</param>
    /// <param name="waitForTrash">ゴミ捨てを待つ場合はtrue。</param>
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

    /// <summary>
    /// ダミーアイテムの状態をリセットするヘルパー。
    /// UIDraggable.ResetStateを呼ぶ。nullチェックなし（呼び出し側で保証）。
    /// </summary>
    /// <param name="item">リセット対象のUIDraggable。</param>
    private void ResetDummyItem(UIDraggable item)
    {
        item.ResetState();
    }

    /// <summary>
    /// アイテム配置成功時に呼ばれるコールバック。配置待ち中のみ完了処理を実行。
    /// DragDropManagerのOnItemPlacedに登録される。
    /// </summary>
    private void OnUserPlacedItem()
    {
        if (isWaitingForPlacement)
        {
            Debug.Log("チュートリアル: 配置成功を確認");
            CompleteInteractionStep();
        }
    }

    /// <summary>
    /// ゴミ捨て成功時に呼ばれるコールバック。ゴミ捨て待ち中のみ完了処理を実行。
    /// DragDropManagerのOnItemTrashedに登録される。
    /// </summary>
    private void OnUserTrashedItem()
    {
        if (isWaitingForTrash)
        {
            Debug.Log("チュートリアル: ゴミ捨て成功を確認");
            CompleteInteractionStep();
        }
    }

    /// <summary>
    /// 操作練習ステップの完了処理。ウィンドウを閉じ、操作受付を無効化し、会話を進める。
    /// </summary>
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

    /// <summary>
    /// すべての演出状態をリセットする。動画ループ停止、ハイライト非表示、ウィンドウ非表示を行う。
    /// 呼び出し後、動画のループ再生は停止する。
    /// </summary>
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

    /// <summary>
    /// 指定された名前のハイライトターゲットを有効化する。見つからない場合は警告を出力。
    /// </summary>
    /// <param name="name">ハイライトターゲットの名前。</param>
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

    /// <summary>
    /// すべてのハイライトを非表示にする。nullチェックあり。
    /// </summary>
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
    /// ChatControllerから呼び出す。現在再生中の全てのチュートリアルエフェクトを強制的に停止し、待機状態を解除する。
    /// コルーチンを停止し、動画とハイライト、ウィンドウを閉じる。
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