using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ムービー演出で操作するアクター（キャラやオブジェクト）の定義
/// </summary>
[System.Serializable]
public class MovieActor
{
    [Tooltip("Inkタグで呼び出すための名前 (例: Guard)")]
    public string actorName;
    [Tooltip("動かす対象のTransform")]
    public RectTransform actorTransform;
    [Tooltip("操作する対象のAnimator")]
    public Animator actorAnimator;
}

[System.Serializable]
public class MovieLocation
{
    [Tooltip("Inkタグで呼び出すための名前 (例: Point_A)")]
    public string locationName;
    [Tooltip("移動先のTransform")]
    public RectTransform locationTransform;
}

public class StartupSequenceManager : MonoBehaviour
{
    [Header("フェード設定")]
    public Image fadeImage;

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
    public GameObject osBootPhase;
    public GameObject openingMoviePhase;
    public GameObject titlePhase;
    public GameObject loginPhase;
    public DesktopManager desktopManager;

    [Header("スキップ設定")]
    [Tooltip("起動時のOSブート演出（osBootPhase）をスキップします")]
    public bool skipOsBootPhase = false;

    [Header("タイトル演出")]
    public Animator titleLogoAnimator;
    [Tooltip("タイトルロゴのアニメーション再生時間(秒)")]
    public float titleAnimDuration = 3.0f;

    [Header("オープニング演出")]
    [Tooltip("オープニングで再生する会話を管理するDialogueManager")]
    public DialogueManager openingDialogueManager;
    [Tooltip("オープニングで再生する会話のInkファイル(任意)")]
    public TextAsset openingChatInk;

    [Header("オープニングムービー演出 (Actors)")]
    [Tooltip("ムービー内で操作するキャラクター/オブジェクトのリスト")]
    public List<MovieActor> movieActors;

    [Header("オープニングムービー演出 (Locations)")]
    [Tooltip("ムービー内で使用する移動先のリスト")]
    public List<MovieLocation> movieLocations;

    [Header("オープニングムービー演出 (Settings)")]
    [Tooltip("キャラクターの移動速度")]
    public float movieMoveSpeed = 2.0f;

    [Header("オープニング操作UI")]
    [Tooltip("会話終了後に表示するパネル（電源ボタンを含むルート）")]
    public GameObject openingProceedPanel;

    [Tooltip("次フェーズへ進むための電源ボタン")]
    public Button openingProceedButton;
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
    public AudioSource audioSource;
    [Tooltip("タイトルロゴ表示時に再生する効果音")]
    public AudioClip titleLogoSound;
    [Range(0f, 1f)]
    [Tooltip("タイトルロゴ効果音の音量")]
    public float titleLogoVolume = 1.0f;

    private bool openingProceedClicked = false;
    private bool openingProceedRoutineRunning = false;

    // DialogueManagerの会話終了イベントを購読するための処理
    private void OnEnable()
    {
        DialogueManager.OnDialogueFinished += OnOpeningChatFinished;
        DialogueManager.OnTagsProcessed += HandleMovieTags;

        if (openingProceedButton != null)
            openingProceedButton.onClick.AddListener(OnOpeningProceedClicked);
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueFinished -= OnOpeningChatFinished;
        DialogueManager.OnTagsProcessed -= HandleMovieTags;

        if (openingProceedButton != null)
            openingProceedButton.onClick.RemoveListener(OnOpeningProceedClicked);
    }



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
    /// ムービー中のInkタグを処理する (OnTagsProcessed から呼ばれる)
    /// </summary>
    private void HandleMovieTags(List<string> tags)
    {
        // DialogueManagerに「クリック進行」を一時停止させる
        openingDialogueManager.SetIsPlayingEffect(true);

        StartCoroutine(ProcessTagsRoutine(tags));
    }

    /// <summary>
    /// タグの処理を非同期で行うコルーチン
    /// </summary>
    private IEnumerator ProcessTagsRoutine(List<string> tags)
    {
        foreach (string tag in tags)
        {
            // タグをコロン(:)で分割
            string[] parts = tag.Split(':');
            if (parts.Length == 0) continue;

            string key = parts[0].Trim();

            switch (key)
            {
                // Ink形式: #move_char: ActorName, LocationName
                case "move_char":
                    if (parts.Length < 3)
                    {
                        Debug.LogWarning($"Move tag format error: {tag}. Expected '#move_char: ActorName, LocationName'");
                        continue;
                    }
                    string actorToMove = parts[1].Trim();
                    string locationName = parts[2].Trim();
                    yield return StartCoroutine(MoveCharacterRoutine(actorToMove, locationName));
                    break;

                // Ink形式: #play_anim: ActorName, TriggerName, Duration
                case "play_anim":
                    if (parts.Length < 4)
                    {
                        Debug.LogWarning($"Anim tag format error: {tag}. Expected '#play_anim: ActorName, TriggerName, Duration'");
                        continue;
                    }
                    string actorToAnimate = parts[1].Trim();
                    string triggerName = parts[2].Trim();
                    if (float.TryParse(parts[3].Trim(), out float animDuration))
                    {
                        yield return StartCoroutine(PlayAnimationRoutine(actorToAnimate, triggerName, animDuration));
                    }
                    break;

                // Ink形式: #wait: 1.5
                case "wait":
                    if (parts.Length < 2)
                    {
                        Debug.LogWarning($"Wait tag format error: {tag}. Expected '#wait: Duration'");
                        continue;
                    }
                    if (float.TryParse(parts[1].Trim(), out float waitTime))
                    {
                        yield return new WaitForSeconds(waitTime);
                    }
                    break;
            }
        }

        // 全てのタグ処理が完了したので、DialogueManagerの待機を解除する
        openingDialogueManager.SetIsPlayingEffect(false);
    }

    /// <summary>
    /// リストからアクターを探し、目的地まで移動させる (Canvas UI版)
    /// </summary>
    private IEnumerator MoveCharacterRoutine(string actorName, string locationName)
    {
        // 1. リストから名前でアクターと場所を探す
        MovieActor actor = movieActors.Find(a => a.actorName == actorName);
        MovieLocation location = movieLocations.Find(l => l.locationName == locationName);

        // 2. 対象の存在チェック
        if (actor == null || actor.actorTransform == null || actor.actorAnimator == null)
        {
            Debug.LogError($"MoveCharacterRoutine: Actor '{actorName}' or its components not found in list.");
            yield break; // 処理を中断
        }
        if (location == null || location.locationTransform == null)
        {
            Debug.LogError($"MoveCharacterRoutine: Location '{locationName}' not found in list.");
            yield break; // 処理を中断
        }

        RectTransform character = actor.actorTransform;
        Animator charAnimator = actor.actorAnimator;
        // 目的地（location）の RectTransform から anchoredPosition (2D座標) を取得
        Vector2 destination = location.locationTransform.anchoredPosition;

        // 3. 移動処理
        Debug.Log($"{actorName}が{locationName}へ移動開始。");
        charAnimator.SetBool("IsWalking", true); // "IsWalking" はAnimator側のパラメータ名

        // 目的の2D座標に着くまで、毎フレーム少しずつ動かす
        while (Vector2.Distance(character.anchoredPosition, destination) > 0.1f)
        {
            character.anchoredPosition = Vector2.MoveTowards(
                character.anchoredPosition,
                destination,
                movieMoveSpeed * Time.deltaTime // ◀ UIの場合は速度を 200 や 300 など大きくする必要があるかも
            );
            yield return null; // 1フレーム待つ
        }

        // 4. 到着処理
        character.anchoredPosition = destination; // 座標をジャストに合わせる
        charAnimator.SetBool("IsWalking", false); // 歩きアニメを停止
        Debug.Log($"{actorName}が{locationName}へ移動完了。");
    }

    /// <summary>
    /// リストからアクターを探し、指定されたアニメ（トリガー）を再生し、指定秒数待機する
    /// </summary>
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

    // DialogueManagerから会話終了イベントを受け取った時に呼ばれる (現在は未使用)
    private void OnOpeningChatFinished(TextAsset finishedInk)
    {
        // 念のため、終了したのがオープニングの会話か確認
        if (finishedInk == openingChatInk)
        {
            // 別の方法で会話終了を検知する場合、ここでフラグを立てるなどの処理が可能
        }
    }

    // フェード処理（所要時間を明示指定する版）
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

    // 互換用：従来のシグネチャ（fadeDuration を使う）
    public IEnumerator Fade(Color targetColor)
    {
        // 既存呼び出しはそのままでOK
        return Fade(targetColor, fadeDuration);
    }

    private void OnOpeningProceedClicked()
    {
        if (openingProceedRoutineRunning) return;
        StartCoroutine(ProceedClickRoutine());
    }

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
}