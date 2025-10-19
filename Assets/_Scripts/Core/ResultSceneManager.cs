// ファイル名: ResultSceneManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic; // Listを使用するために必要

public class ResultSceneManager : MonoBehaviour
{
    // Inspectorで結果ごとの演出を設定するためのデータ構造
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

    [Header("演出設定")]
    [Tooltip("スコアに応じた演出のリスト。スコアが低い順に並べてください。")]
    public List<ResultEntry> resultEntries;
    [Tooltip("演出の合間に挟む待機時間")]
    public float delayBetweenAnimations = 1.0f;

    [Header("参照")]
    public Animator scaleAnimator;
    public Animator tvOffAnimator;
    public string loginSceneName; // 仮のシーン名

    void Start()
    {
        // GameManagerにリザルト表示フラグが立っている場合のみ実行
        if (GameManager.Instance != null && GameManager.Instance.shouldShowResults)
        {
            // フラグを消費して、再読み込み時に実行されないようにする
            GameManager.Instance.shouldShowResults = false;

            // GameManagerからスコアを取得して演出を開始
            int score = GameManager.Instance.correctPlacementCount;
            StartCoroutine(ResultSequence(score));
        }
        else
        {
            // 直接このシーンを再生した場合などは、何もしない（またはデバッグ用の処理）
            Debug.LogWarning("GameManager経由ではないため、リザルトをスキップします。");
        }
    }

    private IEnumerator ResultSequence(int score)
    {
        // 1. スコアに応じた演出データを決定する
        ResultEntry entryToPlay = null;
        // スコアが低い順にリストを並べている前提で、適切なものを探す
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

        yield return new WaitForSeconds(delayBetweenAnimations);

        // 2. 天秤のアニメーションを再生
        if (scaleAnimator != null && !string.IsNullOrEmpty(entryToPlay.scaleAnimationTrigger))
        {
            scaleAnimator.SetTrigger(entryToPlay.scaleAnimationTrigger);
            // アニメーションの長さに応じて待機（ここでは仮に3秒待つ）
            yield return new WaitForSeconds(3.0f);
        }

        yield return new WaitForSeconds(delayBetweenAnimations);

        // 3. 評価ダイアログを再生
        if (entryToPlay.resultInkFile != null)
        {
            DialogueManager.GetInstance().EnterDialogueMode(entryToPlay.resultInkFile);
            // 会話が終わるまで待機
            yield return new WaitUntil(() => DialogueManager.GetInstance().dialogueIsPlaying == false);
        }

        // 4. Dayを更新し、TVオフアニメを再生
        GameManager.Instance.currentDay++;
        Debug.Log($"Day {GameManager.Instance.currentDay} に進みます。");

        if (tvOffAnimator != null)
        {
            tvOffAnimator.SetTrigger("TVOFF");
            // アニメーションが終わるまで待機（仮に2秒）
            yield return new WaitForSeconds(2.0f);
        }

        // 5. ログイン画面に遷移
        SceneManager.LoadScene(loginSceneName);
    }
}