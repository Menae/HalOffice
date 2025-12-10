using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ムービー演出で操作するアクター（キャラやオブジェクト）の定義。actorName は Ink タグと紐付ける識別子。
/// </summary>
/// <remarks>
/// actorTransform と actorAnimator は Inspector で割り当てること。どちらかが null の場合、当該アクターに対する演出は無視される。
/// </remarks>
[System.Serializable]
public class MovieActor
{
    [Tooltip("Inkタグで呼び出すための名前 (例: Guard)")]
    public string actorName;

    [Tooltip("動かす対象のTransform")]
    public RectTransform actorTransform; // InspectorでD&D

    [Tooltip("操作する対象のAnimator")]
    public Animator actorAnimator; // InspectorでD&D
}

/// <summary>
/// ムービー内での移動先定義。locationName は Ink タグで参照する識別子。
/// </summary>
/// <remarks>
/// locationTransform は Inspector で配置すること。null の場合、移動命令は無視される。
/// </remarks>
[System.Serializable]
public class MovieLocation
{
    [Tooltip("Inkタグで呼び出すための名前 (例: Point_A)")]
    public string locationName;

    [Tooltip("移動先のTransform")]
    public RectTransform locationTransform; // InspectorでD&D
}

/// <summary>
/// 起動時の一連の演出（OSブート→オープニング→タイトル→ログイン）を管理するコンポーネント。
/// </summary>
/// <remarks>
/// Start でシーケンスを開始する。GameManager.Instance.currentDay が 2 以上の場合はシーケンスをスキップしてオブジェクトを無効化する。
/// 各フェーズで UI や AudioSource を操作するため、該当コンポーネントは Inspector で割り当てること。
/// </remarks>
public class StartupSequenceManager : MonoBehaviour
{
    [Header("フェード設定")]
    public Image fadeImage; // InspectorでD&D: フェードに使用する Image

    [Header("フェード時間（秒）")]
    [Tooltip("OSブート画面へ“開く”フェード（黒→透明）")]
    public float fadeToOsBoot = 1.0f;
    [Tooltip("OSブート画面を“閉じる”フェード（透明→黒）")]
    public float fadeFromOsBoot = 1.0f;
    [Tooltip("オープニングへ“開く”フェード（黒→透明）")]
    public float fadeToOpening = 1.0f;
    [Tooltip("オープニングを“閉じる”フェード（透明→黒）")]
    public float fadeFromOpening = 1.0f;
    [Tooltip("タイトルへ“開く”フェード（黒→透明）")]
    public float fadeToTitle = 1.0f;
    [Tooltip("タイトルを“閉じる”フェード（透明→黒）")]
    public float fadeFromTitle = 1.0f;
    [Tooltip("ログインへ“開く”フェード（黒→透明）")]
    public float fadeToLogin = 1.0f;

    // 互換用：既存の fadeDuration は残しておく（他所で参照している可能性に備える）
    [Tooltip("（互換）未指定時のデフォルトフェード時間。上の個別値が0以下ならこの値を使います。")]
    public float fadeDuration = 1.5f;

    [Header("管理対象オブジェクト")]
    public GameObject osBootPhase; // InspectorでD&D
    public GameObject openingMoviePhase; // InspectorでD&D
    public GameObject titlePhase; // InspectorでD&D
    public GameObject loginPhase; // InspectorでD&D
    public DesktopManager desktopManager; // InspectorでD&D

    [Header("スキップ設定")]
    [Tooltip("起動時のOSブート演出（osBootPhase）をスキップします")]
    public bool skipOsBootPhase = false;

    [Header("タイトル演出")]
    public Animator titleLogoAnimator; // InspectorでD&D
    [Tooltip("タイトルロゴのアニメーション再生時間(秒)")]
    public float titleAnimDuration = 3.0f;

    [Header("オープニング演出")]
    [Tooltip("オープニングで再生する会話を管理するDialogueManager")]
    public DialogueManager openingDialogueManager; // InspectorでD&D
    [Tooltip("オープニングで再生する会話のInkファイル(任意)")]
    public TextAsset openingChatInk; // InspectorでD&D

    [Header("オープニングムービー演出 (Actors)")]
    [Tooltip("ムービー内で操作するキャラクター/オブジェクトのリスト")]
    public List<MovieActor> movieActors; // InspectorでD&D

    [Header("オープニングムービー演出 (Locations)")]
    [Tooltip("ムービー内で使用する移動先のリスト")]
    public List<MovieLocation> movieLocations; // InspectorでD&D

    [Header("オープニングムービー演出 (Settings)")]
    [Tooltip("キャラクターの移動速度")]
    public float movieMoveSpeed = 2.0f;

    [Header("オープニング操作UI")]
    [Tooltip("会話終了後に表示するパネル（電源ボタンを含むルート）")]
    public GameObject openingProceedPanel; // InspectorでD&D

    [Tooltip("次フェーズへ進むための電源ボタン")]
    public Button openingProceedButton; // InspectorでD&D

    [Header("電源ボタンサウンド")]
    [Tooltip("ボタン押下時に再生するSE")]
    public AudioClip openingProceedSE;

    [Range(0f, 1f)]
    [Tooltip("ボタンSEの音量")]
    public float openingProceedSEVolume = 1f;

    [Tooltip("ボタン押下からフェード開始までの待機秒数")]
    public float openingProceedDelay = 0.4f;

    [Tooltip("押下直後にボタンを無効化して二重クリックを防止します")]
    public bool disableProceedButtonOnClick = true;

    [Header("サウンド設定")]
    [Tooltip("効果音を再生するためのAudioSource")]
    public AudioSource audioSource; // InspectorでD&D

    [Tooltip("タイトルロゴ表示時に再生する効果音")]
    public AudioClip titleLogoSound;

    [Range(0f, 1f)]
    [Tooltip("タイトルロゴ効果音の音量")]
    public float titleLogoVolume = 1.0f;

    [Header("オープニング専用オーディオ")]
    [Tooltip("OP中に流すBGM")]
    public AudioClip openingBgm;
    [Range(0f, 1f)] public float openingBgmVolume = 0.5f;
    [Tooltip("OP中に流す環境音（ガヤ音）")]
    public AudioClip openingAmbience;
    [Range(0f, 1f)] public float openingAmbienceVolume = 0.5f;
    [Tooltip("オーディオのフェードアウト時間（0なら即停止）")]
    public float audioFadeDuration = 1.0f;

    private AudioSource opBgmSource;
    private AudioSource opAmbienceSource;
    private bool openingProceedClicked = false;
    private bool openingProceedRoutineRunning = false;
    private MovieActor currentSpeakingActor;

    /// <summary>
    /// コンポーネント有効化時に呼ばれる。イベント購読とオープニング用 AudioSource の追加を行う。
    /// </summary>
    /// <remarks>
    /// DialogueManager のイベントにリスナを登録する。GameObject に BGM と環境音用の AudioSource を追加し、ループ設定を行う。
    /// openingProceedButton が割り当てられている場合はクリックリスナを追加する。
    /// </remarks>
    private void OnEnable()
    {
        DialogueManager.OnDialogueFinished += OnOpeningChatFinished;
        DialogueManager.OnTagsProcessed += HandleMovieTags;

        // 行の表示完了を検知してアニメを止める
        DialogueManager.OnLineFinishDisplaying += StopSpeakingAnimation;

        // BGM用と環境音用に2つのスピーカー(AudioSource)を動的に追加する
        opBgmSource = gameObject.AddComponent<AudioSource>();
        opBgmSource.loop = true;
        opBgmSource.playOnAwake = false;

        opAmbienceSource = gameObject.AddComponent<AudioSource>();
        opAmbienceSource.loop = true;
        opAmbienceSource.playOnAwake = false;

        if (openingProceedButton != null)
            openingProceedButton.onClick.AddListener(OnOpeningProceedClicked);
    }

    /// <summary>
    /// コンポーネント無効化時に呼ばれる。イベント購読解除と UI リスナの削除を行う。
    /// </summary>
    /// <remarks>
    /// 登録したイベントハンドラとボタンリスナを解除する。解除漏れは参照が残ってメモリ問題を引き起こす可能性があるため必ず解除する。
    /// </remarks>
    private void OnDisable()
    {
        DialogueManager.OnDialogueFinished -= OnOpeningChatFinished;
        DialogueManager.OnTagsProcessed -= HandleMovieTags;

        DialogueManager.OnLineFinishDisplaying -= StopSpeakingAnimation;

        if (openingProceedButton != null)
            openingProceedButton.onClick.RemoveListener(OnOpeningProceedClicked);
    }

    /// <summary>
    /// Unity の Start。シーケンス再生の前準備と条件判定を行い、メインコルーチンを開始する。
    /// </summary>
    /// <remarks>
    /// Start は Awake の後、最初のフレーム前に呼ばれる。GameManager の状態を参照して再生可否を判定する。
    /// 各フェーズで必要な GameObject や UI を初期状態にセットする。
    /// </remarks>
    void Start()
    {
        // Day2以降はシーケンスを再生しない
        if (GameManager.Instance != null && GameManager.Instance.currentDay >= 2)
        {
            Debug.Log($"Day {GameManager.Instance.currentDay}のため、スタートアップシーケンスをスキップします。");
            gameObject.SetActive(false);
            return;
        }

        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.SetDesktopUIVisibility(false);
        }

        osBootPhase.SetActive(false);
        openingMoviePhase.SetActive(false);
        titlePhase.SetActive(false);
        loginPhase.SetActive(false);
        if (desktopManager != null) desktopManager.gameObject.SetActive(false);

        // パネルは初期非表示、押下フラグもリセット
        if (openingProceedPanel != null) openingProceedPanel.SetActive(false);
        openingProceedClicked = false;
        openingProceedRoutineRunning = false;
        if (openingProceedButton != null) openingProceedButton.interactable = true;

        StartCoroutine(MainSequence());
    }

    /// <summary>
    /// 起動シーケンスの主要フローを順次実行するコルーチン。各フェーズを直列に再生する。
    /// </summary>
    /// <remarks>
    /// フェードや会話再生、タイトル演出、ログイン画面への遷移を管理する。処理中に UI や AudioSource の状態を変更する。
    /// openingDialogueManager や fadeImage が null の場合は一部処理がスキップされる可能性があるため、Inspector の設定を確認すること。
    /// </remarks>
    private IEnumerator MainSequence()
    {
        // 常に最初は黒にしておく
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black;
        }

        // --- 1. OS起動画面フェーズ ---
        if (!skipOsBootPhase)
        {
            // 表示して開く（黒→透明）
            if (osBootPhase != null) osBootPhase.SetActive(true);
            yield return StartCoroutine(Fade(Color.clear, fadeToOsBoot));

            // ブート演出（必要ならインスペクタ化してもOK）
            yield return new WaitForSeconds(3.0f);

            // 閉じる（透明→黒）→ 非表示
            yield return StartCoroutine(Fade(Color.black, fadeFromOsBoot));
            if (osBootPhase != null) osBootPhase.SetActive(false);
        }
        else
        {
            // スキップ時はOS画面を確実に消す
            if (osBootPhase != null) osBootPhase.SetActive(false);
            // ここでは黒のまま。次のフェーズで黒→透明する。
        }

        yield return new WaitForSeconds(1f);

        // --- 2. オープニングムービーフェーズ ---
        if (openingDialogueManager != null && openingChatInk != null)
        {
            if (openingMoviePhase != null) openingMoviePhase.SetActive(true);

            // フェードイン開始前に音を鳴らす
            PlayOpeningAudio();

            // 黒→透明でオープニングへ
            yield return StartCoroutine(Fade(Color.clear, fadeToOpening));

            openingDialogueManager.EnterDialogueMode(openingChatInk);
            yield return new WaitUntil(() => openingDialogueManager.dialogueIsPlaying == false);

            // 会話終了 → パネル表示 → クリック待ち
            openingProceedClicked = false;
            openingProceedRoutineRunning = false;
            if (openingProceedPanel != null) openingProceedPanel.SetActive(true);
            if (openingProceedButton != null) openingProceedButton.interactable = true;

            yield return new WaitUntil(() => openingProceedClicked);

            if (openingProceedPanel != null) openingProceedPanel.SetActive(false);

            // 画面フェードアウトと同時に音もフェードアウト開始
            // Coroutineを並列で走らせることで、画面が暗くなるのと同時に音が消えていく
            StartCoroutine(FadeOutOpeningAudio(audioFadeDuration));

            // 透明→黒で次へ
            yield return StartCoroutine(Fade(Color.black, fadeFromOpening));
            if (openingMoviePhase != null) openingMoviePhase.SetActive(false);
        }
        else
        {
            Debug.LogWarning("オープニングの会話ファイルが設定されていないため、オープニングフェーズをスキップします。");
        }

        // --- 3. タイトル画面フェーズ ---
        if (titlePhase != null) titlePhase.SetActive(true);
        yield return StartCoroutine(Fade(Color.clear, fadeToTitle));   // 黒→透明

        if (titleLogoAnimator != null) titleLogoAnimator.SetBool("TitleBoot", true);

        yield return new WaitForSeconds(0.5f);
        if (audioSource != null && titleLogoSound != null)
        {
            audioSource.PlayOneShot(titleLogoSound, titleLogoVolume);
        }

        yield return new WaitForSeconds(titleAnimDuration);
        if (titleLogoAnimator != null) titleLogoAnimator.SetBool("TitleBoot", false);

        yield return StartCoroutine(Fade(Color.black, fadeFromTitle)); // 透明→黒
        if (titlePhase != null) titlePhase.SetActive(false);

        // --- 4. ログイン画面フェーズ ---
        yield return new WaitForSeconds(0.5f);

        if (loginPhase != null) loginPhase.SetActive(true);
        if (desktopManager != null)
        {
            desktopManager.gameObject.SetActive(true);
            desktopManager.InitializeForSequence();
        }

        yield return StartCoroutine(Fade(Color.clear, fadeToLogin));   // 黒→透明
        BGMManager.Instance.TriggerBGMPlayback();

        if (desktopManager != null)
        {
            desktopManager.TakeOverControl();
        }

        Debug.Log("スタートアップシーケンス完了。DesktopManagerに処理を移行します。");
        this.enabled = false;
    }

    /// <summary>
    /// Ink のタグ一覧を解析し、話者切替と演出トリガーの検出を行う。
    /// </summary>
    /// <remarks>
    /// 'speaker: 名前' を検出した場合は該当アクターの発話アニメを開始する。
    /// 'move_char'、'play_anim'、'wait' のいずれかを検出した場合は openingDialogueManager の再生状態を待機中にし、ProcessTagsRoutine を開始する。
    /// tags が null または空の場合は何も行わない。openingDialogueManager が未設定だと呼び出し時に例外が発生するため、Inspector での設定を確認すること。
    /// </remarks>
    private void HandleMovieTags(List<string> tags)
    {
        // 既存の処理（DialogueManagerを待機させる）
        bool hasWaitEffect = false;

        foreach (string tag in tags)
        {
            string[] parts = tag.Split(':');
            string key = parts[0].Trim();

            // 話者タグの処理
            if (key == "speaker" && parts.Length > 1)
            {
                string speakerName = parts[1].Trim();
                StartSpeakingAnimation(speakerName);
            }

            // 既存のタグ（move_charなど）が含まれているかチェック
            if (key == "move_char" || key == "play_anim" || key == "wait")
            {
                hasWaitEffect = true;
            }
        }

        // 演出系タグがある場合のみコルーチンを開始
        if (hasWaitEffect)
        {
            openingDialogueManager.SetIsPlayingEffect(true);
            StartCoroutine(ProcessTagsRoutine(tags));
        }
    }

    /// <summary>
    /// タグの処理を非同期で行うコルーチン（並列処理対応）。
    /// </summary>
    /// <remarks>
    /// 各タグに応じたコルーチンを可能な限り並列で開始し、それらが全て完了するまで待機する。
    /// 不正なフォーマットのタグは警告を出力してスキップする。処理完了後に openingDialogueManager の再生待機状態を解除する。
    /// </remarks>
    private IEnumerator ProcessTagsRoutine(List<string> tags)
    {
        // 実行したコルーチン（移動やアニメ）を監視するためのリスト
        List<Coroutine> activeCoroutines = new List<Coroutine>();

        foreach (string tag in tags)
        {
            string[] parts = tag.Split(':');
            if (parts.Length == 0) continue;

            string key = parts[0].Trim();

            switch (key)
            {
                // Ink形式: #move_char: ActorName, LocationName
                case "move_char":
                    if (parts.Length < 3)
                    {
                        Debug.LogWarning($"Move tag format error: {tag}");
                        continue;
                    }
                    string actorToMove = parts[1].Trim();
                    string locationName = parts[2].Trim();

                    // yield returnせず、リストに追加して即座に次の処理へ進む
                    activeCoroutines.Add(StartCoroutine(MoveCharacterRoutine(actorToMove, locationName)));
                    break;

                // Ink形式: #play_anim: ActorName, TriggerName, Duration
                case "play_anim":
                    if (parts.Length < 4)
                    {
                        Debug.LogWarning($"Anim tag format error: {tag}");
                        continue;
                    }
                    string actorToAnimate = parts[1].Trim();
                    string triggerName = parts[2].Trim();
                    if (float.TryParse(parts[3].Trim(), out float animDuration))
                    {
                        // ここも並列化
                        activeCoroutines.Add(StartCoroutine(PlayAnimationRoutine(actorToAnimate, triggerName, animDuration)));
                    }
                    break;

                // Ink形式: #wait: 1.5
                case "wait":
                    if (parts.Length < 2) continue;
                    if (float.TryParse(parts[1].Trim(), out float waitTime))
                    {
                        yield return new WaitForSeconds(waitTime);
                    }
                    break;
            }
        }

        // 全ての命令を出し終えた後で、走っているコルーチンが全部終わるのを待つ
        foreach (var c in activeCoroutines)
        {
            if (c != null) yield return c;
        }

        // 全てのタグ処理が完了したので、DialogueManagerの待機を解除する
        openingDialogueManager.SetIsPlayingEffect(false);
    }

    /// <summary>
    /// 指定のアクターをリストから検索し、目的地まで X 軸方向のみ移動するコルーチン。
    /// </summary>
    /// <remarks>
    /// 目標位置到達判定は threshold ピクセル以内とする。到着時に Animator の IsWalking を false に設定する。
    /// actor または location に必要な参照が欠けている場合は即時終了するため、呼び出し前に Inspector 設定を確認すること。
    /// </remarks>
    private IEnumerator MoveCharacterRoutine(string actorName, string locationName)
    {
        // 1. 検索とチェック
        MovieActor actor = movieActors.Find(a => a.actorName == actorName);
        MovieLocation location = movieLocations.Find(l => l.locationName == locationName);

        if (actor == null || actor.actorTransform == null || actor.actorAnimator == null) yield break;
        if (location == null || location.locationTransform == null) yield break;

        RectTransform character = actor.actorTransform;
        Animator charAnimator = actor.actorAnimator;

        // 目標のX座標を取得（Yは現在の高さを維持）
        float targetX = location.locationTransform.anchoredPosition.x;
        // 到着とみなす距離（ピクセル）
        float threshold = 1.0f;

        Debug.Log($"{actorName}が{locationName}へ移動開始(Xのみ)。");
        charAnimator.SetBool("IsWalking", true);

        // X座標の差が閾値より大きい間はループする
        while (Mathf.Abs(character.anchoredPosition.x - targetX) > threshold)
        {
            // X座標だけ動かすターゲットを作成
            Vector2 currentPos = character.anchoredPosition;
            Vector2 targetPos = new Vector2(targetX, currentPos.y);

            character.anchoredPosition = Vector2.MoveTowards(
                currentPos,
                targetPos,
                movieMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        // 到着処理（ズレを補正して停止）
        Vector2 finalPos = character.anchoredPosition;
        finalPos.x = targetX;
        character.anchoredPosition = finalPos;

        charAnimator.SetBool("IsWalking", false);
        Debug.Log($"{actorName}が{locationName}へ移動完了。");
    }

    /// <summary>
    /// リストからアクターを探し、指定されたアニメ（トリガー）を再生し、指定秒数待機するコルーチン。
    /// </summary>
    /// <remarks>
    /// Animator が見つからない場合はエラーを出力して即時終了する。指定した duration の間だけ待機する。
    /// </remarks>
    private IEnumerator PlayAnimationRoutine(string actorName, string triggerName, float duration)
    {
        // 1. リストから名前でアクターを探す
        MovieActor actor = movieActors.Find(a => a.actorName == actorName);

        // 2. 対象の存在チェック
        if (actor == null || actor.actorAnimator == null)
        {
            Debug.LogError($"PlayAnimationRoutine: Actor '{actorName}' or its Animator not found in list.");
            yield break;
        }

        // 3. アニメーション再生
        Debug.Log($"{actorName}がアニメーション'{triggerName}'を再生 (Duration: {duration}s)。");
        actor.actorAnimator.SetTrigger(triggerName); // Animatorのトリガーを発火

        // 4. 指定された時間だけ待機する
        yield return new WaitForSeconds(duration);
    }

    /// <summary>
    /// 指定されたアクターの Speak アニメーションを開始する。
    /// </summary>
    /// <remarks>
    /// 既に別のアクターが喋っている場合は先にそのアニメを停止してから切り替える。currentSpeakingActor を更新する。
    /// actor が見つからない場合は何もしない。
    /// </remarks>
    private void StartSpeakingAnimation(string actorName)
    {
        // 前に喋っていた人がいれば、その人の口を閉じる
        StopSpeakingAnimation();

        // 新しい話者を探す
        MovieActor actor = movieActors.Find(a => a.actorName == actorName);
        if (actor != null && actor.actorAnimator != null)
        {
            actor.actorAnimator.SetBool("Speak", true);
            currentSpeakingActor = actor;
        }
    }

    /// <summary>
    /// 現在喋っているアクターの Speak アニメーションを停止する。
    /// </summary>
    /// <remarks>
    /// currentSpeakingActor が null でない場合は Speak フラグを false にし、参照をクリアする。
    /// </remarks>
    private void StopSpeakingAnimation()
    {
        if (currentSpeakingActor != null && currentSpeakingActor.actorAnimator != null)
        {
            currentSpeakingActor.actorAnimator.SetBool("Speak", false);
        }
        currentSpeakingActor = null;
    }

    /// <summary>
    /// DialogueManager から会話終了イベントを受け取った時に呼ばれる。
    /// </summary>
    /// <remarks>
    /// finishedInk が openingChatInk と一致するか確認するためのフックポイントを提供する。現在は追加処理を行わない。
    /// </remarks>
    private void OnOpeningChatFinished(TextAsset finishedInk)
    {
        // 念のため、終了したのがオープニングの会話か確認
        if (finishedInk == openingChatInk)
        {
            // 別の方法で会話終了を検知する場合、ここでフラグを立てるなどの処理が可能
        }
    }

    /// <summary>
    /// フェード処理（所要時間を明示指定する版）。targetColor へ線形補間する。
    /// </summary>
    /// <remarks>
    /// duration が 0 以下の場合は fadeDuration をフォールバックとして使用する。透明化完了時に fadeImage を非表示にする。
    /// fadeImage が null の場合は何も行わない。
    /// </remarks>
    public IEnumerator Fade(Color targetColor, float duration)
    {
        // フォールバック（0以下が入っても破綻しないように）
        float d = (duration > 0f) ? duration : Mathf.Max(0.0001f, fadeDuration);

        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        Color startColor = fadeImage.color;

        while (timer < d)
        {
            fadeImage.color = Color.Lerp(startColor, targetColor, timer / d);
            timer += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = targetColor;

        // 透明になったらフェード用オブジェクトを隠す
        if (targetColor == Color.clear)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 互換用フェードシグネチャ。既存呼び出しと互換性を保つため fadeDuration を使用して Fade を呼ぶ。
    /// </summary>
    public IEnumerator Fade(Color targetColor)
    {
        // 既存呼び出しはそのままでOK
        return Fade(targetColor, fadeDuration);
    }

    /// <summary>
    /// 開始ボタンのクリックイベントハンドラ。二重クリック抑止のためコルーチンを起動する。
    /// </summary>
    /// <remarks>
    /// openingProceedRoutineRunning が true の場合は何もしない。ボタン押下処理は ProceedClickRoutine に委譲する。
    /// </remarks>
    private void OnOpeningProceedClicked()
    {
        if (openingProceedRoutineRunning) return;
        StartCoroutine(ProceedClickRoutine());
    }

    /// <summary>
    /// 電源ボタン押下時の処理を行うコルーチン。ボタン無効化と SE 再生、遅延待機を行う。
    /// </summary>
    /// <remarks>
    /// 指定の待機後に openingProceedClicked を true にして MainSequence 側の待機を解除する。
    /// audioSource が未割り当ての場合は Camera.main を使って PlayClipAtPoint で再生を試みる。
    /// </remarks>
    private IEnumerator ProceedClickRoutine()
    {
        openingProceedRoutineRunning = true;

        if (disableProceedButtonOnClick && openingProceedButton != null)
            openingProceedButton.interactable = false;

        // SE再生（audioSource が無ければ PlayClipAtPoint でフォールバック）
        if (audioSource != null && openingProceedSE != null)
        {
            audioSource.PlayOneShot(openingProceedSE, openingProceedSEVolume);
        }
        else if (openingProceedSE != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(openingProceedSE, Camera.main.transform.position, openingProceedSEVolume);
        }

        if (openingProceedDelay > 0f)
            yield return new WaitForSeconds(openingProceedDelay);

        // ここでMainSequence側の待機が解除され、フェードへ進む
        openingProceedClicked = true;
        openingProceedRoutineRunning = false;
    }

    /// <summary>
    /// オープニング専用の BGM と環境音を再生する。
    /// </summary>
    /// <remarks>
    /// openingBgm / openingAmbience が設定されている場合に、それぞれの AudioSource にクリップをセットして再生する。
    /// AudioSource が未作成の場合は OnEnable で生成される想定。
    /// </remarks>
    private void PlayOpeningAudio()
    {
        if (openingBgm != null)
        {
            opBgmSource.clip = openingBgm;
            opBgmSource.volume = openingBgmVolume;
            opBgmSource.Play();
        }
        if (openingAmbience != null)
        {
            opAmbienceSource.clip = openingAmbience;
            opAmbienceSource.volume = openingAmbienceVolume;
            opAmbienceSource.Play();
        }
    }

    /// <summary>
    /// オープニング音源を徐々にフェードアウトして停止するコルーチン。
    /// </summary>
    /// <remarks>
    /// duration が 0 の場合は速やかに停止する。フェード終了後に音量を元の値に戻す。
    /// opBgmSource / opAmbienceSource が null の場合は NullReferenceException になるため、事前に生成されていることを前提とする。
    /// </remarks>
    private IEnumerator FadeOutOpeningAudio(float duration)
    {
        float startBgmVol = opBgmSource.volume;
        float startAmbienceVol = opAmbienceSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 音量を徐々に下げる
            opBgmSource.volume = Mathf.Lerp(startBgmVol, 0f, t);
            opAmbienceSource.volume = Mathf.Lerp(startAmbienceVol, 0f, t);

            yield return null;
        }

        // 完全に停止
        opBgmSource.Stop();
        opAmbienceSource.Stop();
        opBgmSource.volume = startBgmVol; // 一応戻しておく
        opAmbienceSource.volume = startAmbienceVol;
    }
}