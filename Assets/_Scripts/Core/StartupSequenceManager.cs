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
    public float fadeDuration = 1.5f;

    [Header("管理対象オブジェクト")]
    public GameObject osBootPhase;
    public GameObject openingMoviePhase;
    public GameObject titlePhase;
    public GameObject loginPhase;
    public DesktopManager desktopManager;

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

    [Header("サウンド設定")]
    [Tooltip("効果音を再生するためのAudioSource")]
    public AudioSource audioSource;
    [Tooltip("タイトルロゴ表示時に再生する効果音")]
    public AudioClip titleLogoSound;
    [Range(0f, 1f)]
    [Tooltip("タイトルロゴ効果音の音量")]
    public float titleLogoVolume = 1.0f;

    // DialogueManagerの会話終了イベントを購読するための処理
    private void OnEnable()
    {
        DialogueManager.OnDialogueFinished += OnOpeningChatFinished;
        DialogueManager.OnTagsProcessed += HandleMovieTags;
    }
    private void OnDisable()
    {
        DialogueManager.OnDialogueFinished -= OnOpeningChatFinished;
        DialogueManager.OnTagsProcessed -= HandleMovieTags;
    }

    void Start()
    {
        // Day2以降はシーケンスを再生しないようにするブロック節
        if (GameManager.Instance != null && GameManager.Instance.currentDay >= 2)
        {
            Debug.Log($"Day {GameManager.Instance.currentDay}のため、スタートアップシーケンスをスキップします。");
            // このオブジェクト自体を非表示にして、後続の処理（MainSequenceコルーチン）をすべて中断する
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

        StartCoroutine(MainSequence());
    }

    private IEnumerator MainSequence()
    {
        // --- 0. 初期フェードイン ---
        fadeImage.color = Color.black;

        // --- 1. OS起動画面フェーズ (新規追加) ---
        osBootPhase.SetActive(true);
        yield return StartCoroutine(Fade(Color.clear));

        // ここにOS起動画面のアニメーションやSEの処理を追加
        // 例：Animator osAnimator = osBootPhase.GetComponent<Animator>();
        // 例：osAnimator.SetTrigger("Boot");
        yield return new WaitForSeconds(3.0f); // 仮に3秒待機

        yield return StartCoroutine(Fade(Color.black));
        osBootPhase.SetActive(false);

        // --- 2. オープニングムービーフェーズ ---
        // このフェーズでキャラ移動, クリックで会話進行を行います
        if (openingDialogueManager != null && openingChatInk != null)
        {
            openingMoviePhase.SetActive(true); // ムービー用の親オブジェクトをアクティブに
            yield return StartCoroutine(Fade(Color.clear));

            // DialogueManager にタグ処理（キャラ移動や待機）をハンドリングさせる
            // (※DialogueManager側が OnTagsProcessed と IsPlayingEffect に対応している必要あり)
            // DialogueManager.OnTagsProcessed += HandleMovieTags; // タグ処理を購読 (ステップ4で解説)

            openingDialogueManager.EnterDialogueMode(openingChatInk);

            // 会話の終了（クリックで進行し、最後まで到達）を待つ
            yield return new WaitUntil(() => openingDialogueManager.dialogueIsPlaying == false);

            // DialogueManager.OnTagsProcessed -= HandleMovieTags; // 購読解除 (ステップ4で解説)

            yield return StartCoroutine(Fade(Color.black));
            openingMoviePhase.SetActive(false);
        }
        else
        {
            Debug.LogWarning("オープニングの会話ファイルが設定されていないため、オープニングフェーズをスキップします。");
        }

        // --- 3. タイトル画面フェーズ (既存のタイトルフェーズを移動) ---
        titlePhase.SetActive(true);
        yield return StartCoroutine(Fade(Color.clear));

        if (titleLogoAnimator != null) titleLogoAnimator.SetBool("TitleBoot", true);

        // SE再生処理
        yield return new WaitForSeconds(0.5f);
        if (audioSource != null && titleLogoSound != null)
        {
            audioSource.PlayOneShot(titleLogoSound, titleLogoVolume);
        }

        yield return new WaitForSeconds(titleAnimDuration);
        if (titleLogoAnimator != null) titleLogoAnimator.SetBool("TitleBoot", false);

        yield return StartCoroutine(Fade(Color.black));
        titlePhase.SetActive(false);

        // --- 4. ログイン画面フェーズ (既存のログインフェーズを移動) ---

        yield return new WaitForSeconds(0.5f); //

        loginPhase.SetActive(true);
        if (desktopManager != null)
        {
            desktopManager.gameObject.SetActive(true);
            desktopManager.InitializeForSequence();
        }

        yield return StartCoroutine(Fade(Color.clear));
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

    // フェード処理のヘルパーメソッド (publicに変更済み)
    public IEnumerator Fade(Color targetColor)
    {
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        Color startColor = fadeImage.color;

        while (timer < fadeDuration)
        {
            fadeImage.color = Color.Lerp(startColor, targetColor, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = targetColor;

        if (targetColor == Color.clear)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
}