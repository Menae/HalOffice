using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// リザルトシーンの演出制御を管理するコンポーネント。
/// シーン開始後に評価UIや天秤アニメーション、ダイアログを順序通りに再生する。
/// GameManager のフラグに依存して実行可否を判断。コルーチンで演出シーケンスを制御。
/// </summary>
public class ResultSceneManager : MonoBehaviour
{
    /// <summary>
    /// スコアに応じた演出情報のエントリ。
    /// Inspectorで各項目を設定。スコアが閾値以上ならこのエントリが適用される。
    /// </summary>
    [System.Serializable]
    public class ResultEntry
    {
        /// <summary>
        /// この演出が適用される最低スコア。スコアがこの値以上なら選択対象となる。
        /// </summary>
        [Tooltip("この演出が適用される最低スコア")]
        public int minScore;

        /// <summary>
        /// 再生する天秤アニメーションのトリガー名。Animator に設定されたトリガー名を指定。
        /// </summary>
        [Tooltip("再生する天秤アニメーションのトリガー名")]
        public string scaleAnimationTrigger;

        /// <summary>
        /// 再生する評価ダイアログのInkファイル。null ならダイアログ再生をスキップ。
        /// </summary>
        [Tooltip("再生する評価ダイアログのInkファイル")]
        public TextAsset resultInkFile;
    }

    [Header("デバッグ設定")]
    /// <summary>
    /// シーンを直接再生した際にダミーデータで動作させるフラグ。エディタのみ有効。
    /// </summary>
    [Tooltip("trueの場合、シーンを直接再生してもダミースコアで動作します")]
    public bool enableDebugMode = false;

    /// <summary>
    /// デバッグモードで使用する仮のスコア。Inspectorで設定。
    /// </summary>
    [Tooltip("デバッグモード時に使用する仮のスコア")]
    public int debugScore = 3;

    [Header("リザルトUI設定")]
    /// <summary>
    /// 最初に起動アニメーションを実行するUIパネル。InspectorでD&D。
    /// Startで一旦非アクティブ化してから所定のタイミングで有効化する。
    /// </summary>
    [Tooltip("最初に起動アニメーションを実行するUIパネル")]
    public GameObject evaluationUIPanel;

    /// <summary>
    /// UIパネル起動時に発火させるアニメータートリガー名。Animator のトリガー名を指定。
    /// </summary>
    [Tooltip("UIパネル起動時に発火させるアニメータートリガー名")]
    public string bootAnimationTrigger = "BOOT";

    /// <summary>
    /// シーン開始後、UIパネルを起動するまでの待機時間（秒）。
    /// </summary>
    [Tooltip("シーン開始後、UIパネルを起動するまでの待機時間")]
    public float initialDelay = 1.0f;

    [Header("演出設定")]
    /// <summary>
    /// スコアに応じた演出のリスト。スコアが低い順に並べること。
    /// </summary>
    [Tooltip("スコアに応じた演出のリスト。スコアが低い順に並べてください。")]
    public List<ResultEntry> resultEntries;

    /// <summary>
    /// 演出の合間に挟む待機時間（秒）。
    /// </summary>
    [Tooltip("演出の合間に挟む待機時間")]
    public float delayBetweenAnimations = 1.0f;

    [Header("参照")]
    /// <summary>
    /// 天秤の Animator。スケール演出のトリガーを発火するために使用。
    /// null の場合は天秤演出をスキップ。
    /// </summary>
    public Animator scaleAnimator;

    /// <summary>
    /// TVオフ（暗転）演出用の Animator。TVOFF トリガーを発火。
    /// null の場合は暗転演出をスキップ。
    /// </summary>
    public Animator tvOffAnimator;

    /// <summary>
    /// 演出終了後に戻るシーン名。GameManager がない場合にロードする。
    /// </summary>
    public string loginSceneName;

    [Header("サウンド設定")]
    /// <summary>
    /// TVオフ演出時に再生する効果音。null なら効果音を再生しない。
    /// </summary>
    [Tooltip("TVオフ演出時に再生する効果音")]
    public AudioClip tvOffSound;

    /// <summary>
    /// TVオフ効果音の音量（0.0〜1.0）。
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("TVオフ効果音の音量")]
    public float tvOffVolume = 1.0f;

    /// <summary>
    /// 効果音を再生するための AudioSource。null の場合は再生を行わない。
    /// InspectorでAudioSourceをアタッチ。
    /// </summary>
    [Tooltip("効果音を再生するためのAudioSource")]
    public AudioSource audioSource;

    /// <summary>
    /// Unityイベント: シーン読み込み後に最初に呼ばれる。初期化処理と演出開始判定を行う。
    /// GameManager のフラグに応じて ResultSequence を開始。デバッグモード時はエディタ上でダミースコアを使用。
    /// </summary>
    void Start()
    {
        // 最初にUIパネルを非表示にしておく（UI起動はコルーチンで制御）
        if (evaluationUIPanel != null)
        {
            evaluationUIPanel.SetActive(false);
        }

        // --- 通常のゲームフロー ---
        if (GameManager.Instance != null && GameManager.Instance.shouldShowResults)
        {
            GameManager.Instance.shouldShowResults = false;
            int score = GameManager.Instance.correctPlacementCount;
            StartCoroutine(ResultSequence(score));
        }
        // --- デバッグ用のフロー---
        else if (Application.isEditor && enableDebugMode)
        {
            Debug.LogWarning($"デバッグモードでリザルトを開始します。ダミースコア: {debugScore}");
            StartCoroutine(ResultSequence(debugScore));
        }
        // --- どちらにも当てはまらない場合 ---
        else
        {
            // GameManager 経由でない場合はリザルトを表示せず戻る
            Debug.LogWarning("GameManager経由ではないため、リザルトをスキップします。");
        }
    }

    /// <summary>
    /// 指定したスコアに基づいてリザルト演出を順次実行するコルーチン。
    /// 選ばれた ResultEntry に則り、UI起動→天秤→ダイアログ→暗転→シーン遷移の順で進行。
    /// </summary>
    /// <param name="score">演出選択に使用するスコア。GameManager から取得した値またはデバッグスコア。</param>
    /// <returns>IEnumerable コルーチン用の IEnumerator。</returns>
    private IEnumerator ResultSequence(int score)
    {
        ResultEntry entryToPlay = null;
        foreach (var entry in resultEntries)
        {
            if (score >= entry.minScore)
            {
                entryToPlay = entry;
            }
        }

        if (entryToPlay == null)
        {
            Debug.LogError("スコアに対応する演出が見つかりません！");
            yield break;
        }

        // 1. シーンが読み込まれたら指定秒待機
        yield return new WaitForSeconds(initialDelay);

        // 2. 評価UIパネルを有効化して起動アニメーションを発火
        if (evaluationUIPanel != null)
        {
            evaluationUIPanel.SetActive(true);

            // UI に Animator がアタッチされている場合に BOOT トリガーを発火
            Animator uiAnimator = evaluationUIPanel.GetComponent<Animator>();
            if (uiAnimator != null && !string.IsNullOrEmpty(bootAnimationTrigger))
            {
                uiAnimator.SetTrigger(bootAnimationTrigger);
            }
        }

        // 3. 天秤アニメーションの準備待機
        yield return new WaitForSeconds(delayBetweenAnimations);

        if (scaleAnimator != null && !string.IsNullOrEmpty(entryToPlay.scaleAnimationTrigger))
        {
            scaleAnimator.SetTrigger(entryToPlay.scaleAnimationTrigger);
            yield return new WaitForSeconds(3.0f);
        }

        yield return new WaitForSeconds(delayBetweenAnimations);

        if (entryToPlay.resultInkFile != null)
        {
            DialogueManager.GetInstance().EnterDialogueMode(entryToPlay.resultInkFile);
            yield return new WaitUntil(() => DialogueManager.GetInstance().dialogueIsPlaying == false);
        }

        // 日付を進める処理（currentDay++）は削除済み。
        Debug.Log("リザルト演出終了。メインシーンへ戻ります。");

        if (tvOffAnimator != null)
        {
            if (GlobalUIManager.Instance != null) GlobalUIManager.Instance.SetDesktopUIVisibility(false);

            if (audioSource != null && tvOffSound != null)
            {
                audioSource.PlayOneShot(tvOffSound, tvOffVolume);
            }

            // TVオフ（暗転）演出を発火
            tvOffAnimator.SetTrigger("TVOFF");

            // 暗転が完了するまで待機
            yield return new WaitForSeconds(2.0f);
        }

        // 所属するシーン名を取得して GameManager に返却、なければ指定シーンへロード
        string currentSceneName = this.gameObject.scene.name;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CloseResultScene(currentSceneName);
        }
        else
        {
            SceneManager.LoadScene(loginSceneName);
        }
    }
}