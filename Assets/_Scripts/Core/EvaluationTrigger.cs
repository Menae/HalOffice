using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム終了判定とエンディング演出の起動を管理するトリガークラス。
/// 評価を集計し、結果に応じた演出（Best/Good/Normal/Bad）を順次再生した後、次シーンへ遷移する。
/// </summary>
public class EvaluationTrigger : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("シーン内のObjectSlotManager")]
    /// <summary>
    /// スロット配置情報を管理するマネージャ。InspectorでD&D必須。
    /// nullの場合、評価処理は中断される。
    /// </summary>
    public ObjectSlotManager objectSlotManager;

    [Tooltip("スクリーンエフェクトを制御するコントローラ")]
    /// <summary>
    /// TVオン/オフやグリッチなどの画面エフェクトを制御するコンポーネント。
    /// nullの場合は一部演出（サウンドやエフェクト）がスキップされる。
    /// </summary>
    public ScreenEffectsController screenEffectsController;

    [Header("設定")]
    [Tooltip("遷移先のリザルトシーン名")]
    /// <summary>
    /// リザルトシーンの名前。Inspectorで設定する。
    /// 空文字や誤入力だとシーンロードに失敗する可能性あり。
    /// </summary>
    public string resultSceneName;

    [Tooltip("TVオフ演出の再生時間（秒）")]
    /// <summary>
    /// TVオフ演出後に待機する秒数。
    /// 演出時間の調整用。
    /// </summary>
    public float tvOffDelay = 2.0f;

    [Header("フェード設定")]
    [Tooltip("フェードアウトに使用する黒い画像（Canvas上のImage）")]
    /// <summary>
    /// フェードアウト時に使用する黒画像（Canvas上のImage）。InspectorでD&D。
    /// nullの場合はフェード処理をスキップして即遷移する。
    /// </summary>
    public UnityEngine.UI.Image fadeOutImage;

    [Tooltip("フェードアウトにかかる時間（秒）")]
    /// <summary>
    /// フェードアウトにかける時間（秒）。
    /// 0以下の値は瞬時フェード扱いとなる。
    /// </summary>
    public float fadeDuration = 2.0f;

    [Header("エンディング演出 (Endings)")]
    [Tooltip("【最高】本を読み始める演出（旧Goodを昇格）")]
    /// <summary>
    /// Bestエンド用の親オブジェクト。InspectorでD&D。
    /// 表示/非表示で演出のオンオフを行う。
    /// </summary>
    public GameObject endingBestRoot; // 元 endingBookRoot

    [Tooltip("【良好】新しいGood演出（これから作るもの）")]
    /// <summary>
    /// Goodエンド用の親オブジェクト。InspectorでD&D。
    /// 表示/非表示で演出のオンオフを行う。
    /// </summary>
    public GameObject endingGoodRoot; // ★新規追加

    [Tooltip("【普通】初期化される演出")]
    /// <summary>
    /// Normalエンド用の親オブジェクト。InspectorでD&D。
    /// </summary>
    public GameObject endingInitRoot;

    [Tooltip("【最悪】神の手によって潰される演出")]
    /// <summary>
    /// Badエンド用の親オブジェクト。InspectorでD&D。
    /// </summary>
    public GameObject endingCrushedRoot;

    [Header("評価基準 (Thresholds)")]
    [Tooltip("【最高】本読みエンドになる最低スコア")]
    /// <summary>
    /// Bestエンド判定に必要な最低スコア。
    /// 閾値はInspectorで調整可能。
    /// </summary>
    public int scoreThresholdForBest = 10;

    [Tooltip("【良好】新Goodエンドになる最低スコア")]
    /// <summary>
    /// Goodエンド判定に必要な最低スコア。
    /// Best未満かつこの閾値以上でGood判定。
    /// </summary>
    public int scoreThresholdForGood = 7;

    [Tooltip("【最悪】神の手エンドになるスコア以下")]
    /// <summary>
    /// Badエンド判定の閾値。閾値以下でBad判定。
    /// </summary>
    public int scoreThresholdForBad = 3;

    [Header("Bad End 設定")]
    /// <summary>
    /// Badエンド用の表示テキスト（Inkファイル）。nullチェックあり。
    /// </summary>
    public TextAsset badEndInk;

    /// <summary>
    /// 「神の手」アニメータ。アニメーション開始に使用。
    /// </summary>
    public Animator handAnimator;

    /// <summary>
    /// 破壊されるNPC用アニメータ。nullチェックあり。
    /// </summary>
    public Animator crushedNpcAnimator;

    public AudioClip alarmSound;
    [Range(0f, 1f)] public float alarmVolume = 1.0f;
    public AudioClip crushSound;
    [Range(0f, 1f)] public float crushVolume = 1.0f;

    /// <summary>
    /// 手のアニメーション完了まで待機する秒数。
    /// </summary>
    public float handAnimationDuration = 2.0f;

    [Header("Normal End 設定")]
    public TextAsset normalEndSystemInk;
    public TextAsset normalEndWakeUpInk;
    public Animator normalEndAnimator;
    public ResultSpeechBubbleController normalEndBubbleController;
    public AudioClip initSound;
    [Range(0f, 1f)] public float initVolume = 1.0f;

    /// <summary>
    /// Normalエンドの倒れる（初期化）演出の待機時間。
    /// </summary>
    public float initDuration = 3.0f;

    [Header("Good End 設定 (New)")]
    public TextAsset goodEndInk;
    public Animator goodEndAnimator;
    public ResultSpeechBubbleController goodEndBubbleController;

    [Header("Best End 設定 (Book)")]
    public TextAsset bestEndInk;
    public ResultSpeechBubbleController bestEndBubbleController;
    public Animator bestEndAnimator;

    [Tooltip("読み込み演出を表示するパネル")]
    /// <summary>
    /// Bestエンドの読み込み表示用パネル。InspectorでD&D。
    /// nullの場合は読み込み表示をスキップする。
    /// </summary>
    public GameObject bestEndLoadingPanel;

    [Tooltip("進捗を表示するスライダー")]
    /// <summary>
    /// 読み込み進捗表示用スライダー。0〜1で扱う。
    /// </summary>
    public UnityEngine.UI.Slider bestEndLoadingSlider;

    [Tooltip("読み込みにかかる時間（秒）")]
    public float bestEndLoadingDuration = 2.0f;
    public AudioClip metamorphoseSound;
    [Range(0f, 1f)] public float metamorphoseVolume = 1.0f;

    [Header("遷移設定")]
    /// <summary>
    /// 次のシーン名。デイ進行後にこのシーンへ遷移する。
    /// InspectorでD&Dまたは文字列を設定する。
    /// </summary>
    public string nextSceneName = "P1&P2";

    /// <summary>
    /// ボタンのOnClickイベントなどから呼び出す公開メソッド。
    /// スロット配置を集計しスコアを算出、GameManagerへ反映してリザルト表示フローを開始する。
    /// 副作用: GameManagerのフィールドを書き換える。
    /// </summary>
    public void EndDayAndEvaluate()
    {
        if (objectSlotManager == null)
        {
            Debug.LogError("ObjectSlotManagerが設定されていません！");
            return;
        }

        int score = 0;
        foreach (var slot in objectSlotManager.objectSlots)
        {
            if (slot.isCorrectWhenEmpty)
            {
                if (!slot.IsOccupied())
                    score++;
            }
            else
            {
                if (slot.IsOccupied() && slot.currentObject.itemData.itemType == slot.correctItemType)
                    score++;
            }
        }

        // スコアをGameManagerへ反映（副作用）
        GameManager.Instance.correctPlacementCount = score;
        GameManager.Instance.shouldShowResults = true;

        Debug.Log($"評価が完了。スコア: {score}。TVオフ演出を再生してリザルトシーンへ遷移します。");

        // タスクバー等のUIを非表示にする試み（ある場合のみ）
        if (GlobalUIManager.Instance != null)
            GlobalUIManager.Instance.SetDesktopUIVisibility(false);

        // TVオフ演出とシーン遷移はコルーチンで順次処理
        if (screenEffectsController != null)
        {
            StartCoroutine(DelayedSceneTransition());
        }
        else
        {
            // 参照が無ければ即座にリザルトシーンを加算ロード
            SceneManager.LoadScene(resultSceneName, LoadSceneMode.Additive);
        }
    }

    /// <summary>
    /// TVオフ演出 → リザルトシーンを加算ロード → リザルトが閉じられるのを待ち、
    /// 結果に応じたポストリザルト演出を開始するコルーチン。
    /// </summary>
    private IEnumerator DelayedSceneTransition()
    {
        // TVオフ演出のトリガー送出
        yield return null;
        if (screenEffectsController != null)
        {
            screenEffectsController.TriggerTvOff();
        }
        yield return new WaitForSeconds(tvOffDelay);

        // リザルトシーンを加算ロード（現在シーンはそのまま残す）
        yield return SceneManager.LoadSceneAsync(resultSceneName, LoadSceneMode.Additive);

        // 背景にあるメインシーンの入力をブロック
        if (GameManager.Instance != null)
            GameManager.Instance.SetInputEnabled(false);

        // リザルトシーンが閉じられるのを待機
        bool resultClosed = false;
        System.Action closeHandler = () => { resultClosed = true; };

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnResultSceneClosed += closeHandler;
            yield return new WaitUntil(() => resultClosed);
            GameManager.Instance.OnResultSceneClosed -= closeHandler;
        }
        else
        {
            // フェイルセーフ: GameManagerが無い場合は待たず進行
            Debug.LogError("GameManager not found. Skipping wait.");
        }

        // リザルト終了後にポスト処理を開始
        Debug.Log("リザルト終了。スコアに応じたエンディング演出を開始します。");
        StartCoroutine(PlayPostResultSequence());
    }

    /// <summary>
    /// スコアに応じたエンディング演出（Best/Good/Bad/Normal）を順次再生し、
    /// 最後にフェードアウトして次シーンへ遷移するコルーチン。
    /// </summary>
    private IEnumerator PlayPostResultSequence()
    {
        // 1. 演出開始前の準備
        if (screenEffectsController != null)
            screenEffectsController.TriggerTvOn();

        // TVオン処理の演出が終わるまで短時間待機
        yield return new WaitForSeconds(0.5f);

        // 演出中は入力を無効化してプレイヤー操作を防止
        if (GameManager.Instance != null)
            GameManager.Instance.SetInputEnabled(false);

        // 現在のスコアを取得（GameManagerに保存されている値を参照）
        int score = 0;
        if (GameManager.Instance != null)
            score = GameManager.Instance.correctPlacementCount;

        // 2. エンディング分岐処理
        GameObject targetEnding = null;

        // ケース1: BESTエンディング
        if (score >= scoreThresholdForBest)
        {
            Debug.Log($"Score: {score} -> BEST ENDING (Book)");

            targetEnding = endingBestRoot;
            if (targetEnding != null)
                targetEnding.SetActive(true);

            if (bestEndLoadingPanel != null && bestEndLoadingSlider != null)
            {
                bestEndLoadingPanel.SetActive(true);
                bestEndLoadingSlider.value = 0f;

                float loadTimer = 0f;
                while (loadTimer < bestEndLoadingDuration)
                {
                    loadTimer += Time.deltaTime;
                    bestEndLoadingSlider.value = Mathf.Clamp01(loadTimer / bestEndLoadingDuration);
                    yield return null;
                }

                bestEndLoadingSlider.value = 1.0f;
                yield return new WaitForSeconds(0.5f);

                if (bestEndAnimator != null)
                    bestEndAnimator.SetTrigger("Metamorphose");

                yield return new WaitForSeconds(1.0f);
                bestEndLoadingPanel.SetActive(false);
            }

            if (screenEffectsController != null && metamorphoseSound != null)
                screenEffectsController.GetComponent<AudioSource>().PlayOneShot(metamorphoseSound, metamorphoseVolume);

            // NPCのセリフ表示（吹き出し）
            if (bestEndBubbleController != null && bestEndInk != null)
            {
                yield return StartCoroutine(bestEndBubbleController.PlaySpeechSequence(bestEndInk, 3.0f));
            }

            // 本を読むアニメーション開始
            if (bestEndAnimator != null)
                bestEndAnimator.SetTrigger("Book");

            // 読書の余韻待機
            yield return new WaitForSeconds(4.0f);
        }
        // ケース2: GOODエンディング
        else if (score >= scoreThresholdForGood)
        {
            Debug.Log($"Score: {score} -> GOOD ENDING (New)");

            if (goodEndAnimator != null)
                goodEndAnimator.SetTrigger("Metamorphose");

            targetEnding = endingGoodRoot;
            if (targetEnding != null)
                targetEnding.SetActive(true);

            yield return new WaitForSeconds(1.0f);

            if (goodEndBubbleController != null && goodEndInk != null)
            {
                yield return StartCoroutine(goodEndBubbleController.PlaySpeechSequence(goodEndInk, 2.0f));
            }
            else
            {
                // セットされていない場合の保険的待機
                yield return new WaitForSeconds(3.0f);
            }
        }
        // ケース3: BADエンディング
        else if (score <= scoreThresholdForBad)
        {
            Debug.Log($"Score: {score} -> BAD ENDING (Crushed)");

            targetEnding = endingCrushedRoot;
            if (targetEnding != null)
                targetEnding.SetActive(true);

            // 警告ダイアログ表示（Ink再生）
            if (badEndInk != null)
            {
                var dm = DialogueManager.GetInstance();
                dm.EnterDialogueMode(badEndInk);

                // テキスト表示完了を待つ
                yield return new WaitUntil(() => dm.canContinueToNextLine);

                // 読み終わりの余韻
                yield return new WaitForSeconds(2.0f);

                // 強制的にダイアログを進めて閉じる
                dm.AdvanceDialogue();
                yield return new WaitUntil(() => dm.dialogueIsPlaying == false);
            }

            yield return new WaitForSeconds(1f);

            // 警報音再生
            if (screenEffectsController != null && alarmSound != null)
                screenEffectsController.GetComponent<AudioSource>().PlayOneShot(alarmSound, alarmVolume);

            yield return new WaitForSeconds(1.5f);

            // 神の手演出と破壊音・NPC死亡アニメ
            if (handAnimator != null)
            {
                handAnimator.SetTrigger("Kill");
                yield return new WaitForSeconds(0.1f);
                if (crushSound != null)
                {
                    if (screenEffectsController != null)
                        screenEffectsController.GetComponent<AudioSource>().PlayOneShot(crushSound, crushVolume);
                    if (crushedNpcAnimator != null)
                        crushedNpcAnimator.SetTrigger("Die");
                }
            }

            // 手のアニメ完了待ち
            yield return new WaitForSeconds(handAnimationDuration);
        }
        // ケース4: NORMALエンディング
        else
        {
            Debug.Log($"Score: {score} -> NORMAL ENDING (Initialize)");

            targetEnding = endingInitRoot;
            if (targetEnding != null)
                targetEnding.SetActive(true);

            // システムメッセージ再生
            if (normalEndSystemInk != null)
            {
                var dm = DialogueManager.GetInstance();
                dm.EnterDialogueMode(normalEndSystemInk);
                yield return new WaitForSeconds(4.0f);
                dm.AdvanceDialogue();
                yield return new WaitUntil(() => dm.dialogueIsPlaying == false);
            }

            yield return new WaitForSeconds(0.5f);

            // 初期化音とグリッチ演出
            if (screenEffectsController != null)
            {
                if (initSound != null)
                    screenEffectsController.GetComponent<AudioSource>().PlayOneShot(initSound, initVolume);

                StartCoroutine(screenEffectsController.TriggerGlitchBurstRoutine(0.5f));
            }

            if (normalEndAnimator != null)
                normalEndAnimator.SetTrigger("Init");

            // 倒れている時間を待機
            yield return new WaitForSeconds(initDuration);

            // NPCの吹き出しで補足説明（記憶喪失示唆）
            if (normalEndBubbleController != null && normalEndWakeUpInk != null)
            {
                yield return StartCoroutine(normalEndBubbleController.PlaySpeechSequence(normalEndWakeUpInk, 3.0f));
            }
        }

        // 3. 終了処理とシーン遷移
        Debug.Log("フェードアウト開始...");

        if (fadeOutImage != null)
        {
            fadeOutImage.gameObject.SetActive(true);
            fadeOutImage.color = new Color(0, 0, 0, 0);

            float timer = 0f;
            while (timer < fadeDuration)
            {
                float alpha = timer / fadeDuration;
                fadeOutImage.color = new Color(0, 0, 0, alpha);
                timer += Time.deltaTime;
                yield return null;
            }

            fadeOutImage.color = new Color(0, 0, 0, 1);
        }

        Debug.Log("全シーケンス終了。遷移します。");

        // 日付を進める（GameManagerの処理を呼び出す）
        if (GameManager.Instance != null)
            GameManager.Instance.AdvanceDay();

        // 次シーンへ遷移
        SceneManager.LoadScene(nextSceneName);
    }
}