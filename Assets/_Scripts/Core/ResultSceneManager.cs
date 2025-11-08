using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class ResultSceneManager : MonoBehaviour
{
    [System.Serializable]
    public class ResultEntry
    {
        [Tooltip("この演出が適用される最低スコア")]
        public int minScore;
        [Tooltip("再生する天秤アニメーションのトリガー名")]
        public string scaleAnimationTrigger;
        [Tooltip("再生する評価ダイアログのInkファイル")]
        public TextAsset resultInkFile;
    }

    [Header("デバッグ設定")]
    [Tooltip("trueの場合、シーンを直接再生してもダミースコアで動作します")]
    public bool enableDebugMode = false;
    [Tooltip("デバッグモード時に使用する仮のスコア")]
    public int debugScore = 3;

    [Header("リザルトUI設定")]
    [Tooltip("最初に起動アニメーションを実行するUIパネル")]
    public GameObject evaluationUIPanel;
    [Tooltip("UIパネル起動時に発火させるアニメータートリガー名")]
    public string bootAnimationTrigger = "BOOT";
    [Tooltip("シーン開始後、UIパネルを起動するまでの待機時間")]
    public float initialDelay = 1.0f;

    [Header("演出設定")]
    [Tooltip("スコアに応じた演出のリスト。スコアが低い順に並べてください。")]
    public List<ResultEntry> resultEntries;
    [Tooltip("演出の合間に挟む待機時間")]
    public float delayBetweenAnimations = 1.0f;

    [Header("参照")]
    public Animator scaleAnimator;
    public Animator tvOffAnimator;
    public string loginSceneName;

    [Header("サウンド設定")]
    [Tooltip("TVオフ演出時に再生する効果音")]
    public AudioClip tvOffSound;
    [Range(0f, 1f)]
    [Tooltip("TVオフ効果音の音量")]
    public float tvOffVolume = 1.0f;
    [Tooltip("効果音を再生するためのAudioSource")]
    public AudioSource audioSource;

    void Start()
    {
        // 最初にUIパネルを非表示にしておく
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
            // 元々の警告
            Debug.LogWarning("GameManager経由ではないため、リザルトをスキップします。");
        }
    }

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

        // 1. シーンが読み込まれたら、1秒待機
        yield return new WaitForSeconds(initialDelay);

        // 2. 評価アプリのUIパネルを有効化
        if (evaluationUIPanel != null)
        {
            evaluationUIPanel.SetActive(true);

            // 3. UIパネルのアニメーターを取得し "BOOT" トリガーを発火
            Animator uiAnimator = evaluationUIPanel.GetComponent<Animator>();
            if (uiAnimator != null && !string.IsNullOrEmpty(bootAnimationTrigger))
            {
                uiAnimator.SetTrigger(bootAnimationTrigger);
            }
        }

        // 4. あとは今まで通り (天秤アニメーションの前の待機)
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

        GameManager.Instance.currentDay++;
        Debug.Log($"Day {GameManager.Instance.currentDay} に進みます。");

        if (tvOffAnimator != null)
        {
            if (audioSource != null && tvOffSound != null)
            {
                audioSource.PlayOneShot(tvOffSound, tvOffVolume);
            }

            tvOffAnimator.SetTrigger("TVOFF");
            yield return new WaitForSeconds(2.0f);
        }

        SceneManager.LoadScene(loginSceneName);
    }
}