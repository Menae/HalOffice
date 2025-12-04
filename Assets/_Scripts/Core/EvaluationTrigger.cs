using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class EvaluationTrigger : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("シーン内のObjectSlotManager")]
    public ObjectSlotManager objectSlotManager;

    [Tooltip("スクリーンエフェクトを制御するコントローラ")]
    public ScreenEffectsController screenEffectsController;

    [Header("設定")]
    [Tooltip("遷移先のリザルトシーン名")]
    public string resultSceneName;

    [Tooltip("TVオフ演出の再生時間（秒）")]
    public float tvOffDelay = 2.0f;

    [Header("フェード設定")]
    [Tooltip("フェードアウトに使用する黒い画像（Canvas上のImage）")]
    public UnityEngine.UI.Image fadeOutImage;
    [Tooltip("フェードアウトにかかる時間（秒）")]
    public float fadeDuration = 2.0f;

    [Header("エンディング演出 (Endings)")]
    [Tooltip("【最高】本を読み始める演出（旧Goodを昇格）")]
    public GameObject endingBestRoot; // 元 endingBookRoot

    [Tooltip("【良好】新しいGood演出（これから作るもの）")]
    public GameObject endingGoodRoot; // ★新規追加

    [Tooltip("【普通】初期化される演出")]
    public GameObject endingInitRoot;

    [Tooltip("【最悪】神の手によって潰される演出")]
    public GameObject endingCrushedRoot;

    [Header("評価基準 (Thresholds)")]
    [Tooltip("【最高】本読みエンドになる最低スコア")]
    public int scoreThresholdForBest = 10;
    [Tooltip("【良好】新Goodエンドになる最低スコア")]
    public int scoreThresholdForGood = 7;
    [Tooltip("【最悪】神の手エンドになるスコア以下")]
    public int scoreThresholdForBad = 3;

    [Header("Bad End 設定")]
    public TextAsset badEndInk;
    public Animator handAnimator;
    public Animator crushedNpcAnimator;
    public AudioClip alarmSound;
    [Range(0f, 1f)] public float alarmVolume = 1.0f;
    public AudioClip crushSound;
    [Range(0f, 1f)] public float crushVolume = 1.0f;
    public float handAnimationDuration = 2.0f;

    [Header("Normal End 設定")]
    public TextAsset normalEndSystemInk;
    public TextAsset normalEndWakeUpInk;
    public Animator normalEndAnimator;
    public ResultSpeechBubbleController normalEndBubbleController;
    public AudioClip initSound;
    [Range(0f, 1f)] public float initVolume = 1.0f;
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
    public GameObject bestEndLoadingPanel;
    [Tooltip("進捗を表示するスライダー")]
    public UnityEngine.UI.Slider bestEndLoadingSlider;
    [Tooltip("読み込みにかかる時間（秒）")]
    public float bestEndLoadingDuration = 2.0f;
    public AudioClip metamorphoseSound;
    [Range(0f, 1f)] public float metamorphoseVolume = 1.0f;

    [Header("遷移設定")]
    public string nextSceneName = "P1&P2";

    /// <summary>
    /// ボタンのOnClickイベントなどから呼び出すための公開メソッド
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

        GameManager.Instance.correctPlacementCount = score;
        GameManager.Instance.shouldShowResults = true;

        Debug.Log($"評価が完了。スコア: {score}。TVオフ演出を再生してリザルトシーンへ遷移します。");

        // 1. まずタスクバーを消す（予約）
        if (GlobalUIManager.Instance != null) GlobalUIManager.Instance.SetDesktopUIVisibility(false);

        // TVオフ演出とシーン遷移をコルーチンに任せる
        if (screenEffectsController != null)
        {
            StartCoroutine(DelayedSceneTransition()); // 遅延でシーン遷移
        }
        else
        {
            // 参照が無ければ即座に遷移
            SceneManager.LoadScene(resultSceneName, LoadSceneMode.Additive);
        }
    }

    /// <summary>
    /// TVオフ演出 → リザルト加算ロード → 待機 → スコアに応じた演出分岐
    /// </summary>
    private IEnumerator DelayedSceneTransition()
    {
        // 1. 既存のTVオフ演出
        yield return null;
        if (screenEffectsController != null)
        {
            screenEffectsController.TriggerTvOff();
        }
        yield return new WaitForSeconds(tvOffDelay);

        // 2. リザルトシーンを「加算ロード(Additive)」する
        // これにより、現在の部屋（メインシーン）は裏側にそのまま残ります
        yield return SceneManager.LoadSceneAsync(resultSceneName, LoadSceneMode.Additive);

        // 裏にあるメインシーンを操作できないように入力をブロック
        if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(false);

        // 3. リザルトシーンが閉じるのを待つ
        bool resultClosed = false;
        System.Action closeHandler = () => { resultClosed = true; };

        // GameManagerのイベントを購読して待機
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnResultSceneClosed += closeHandler;
            yield return new WaitUntil(() => resultClosed);
            GameManager.Instance.OnResultSceneClosed -= closeHandler;
        }
        else
        {
            // フェイルセーフ：GameManagerがいない場合は待たずに進む（エラー回避）
            Debug.LogError("GameManager not found. Skipping wait.");
        }

        // 4. リザルトが消えたので、結果に応じた演出を開始する
        Debug.Log("リザルト終了。スコアに応じたエンディング演出を開始します。");
        StartCoroutine(PlayPostResultSequence());
    }

    /// <summary>
    /// スコアに応じたエンディング演出（Best/Good/Bad/Normal）を再生し、
    /// 最後に画面をフェードアウトさせて次のシーンへ遷移させるコルーチン。
    /// </summary>
    private IEnumerator PlayPostResultSequence()
    {
        // ---------------------------------------------------------
        // 1. 演出開始前の準備
        // ---------------------------------------------------------

        // 画面が暗転したままになっているため、まずは明るく戻して演出が見えるようにする
        if (screenEffectsController != null)
        {
            screenEffectsController.TriggerTvOn();
        }

        // 画面が明るくなる演出（フェードなど）が終わるのを少し待つ
        yield return new WaitForSeconds(0.5f);

        // 演出中にプレイヤーが余計な操作をしないよう、入力を無効化する
        if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(false);

        // 現在のスコアを取得する
        int score = 0;
        if (GameManager.Instance != null) score = GameManager.Instance.correctPlacementCount;

        // ---------------------------------------------------------
        // 2. エンディング演出の分岐再生
        // ---------------------------------------------------------
        GameObject targetEnding = null;

        // --- ケース1: 最高のエンディング (BEST ENDING) ---
        // 条件: スコアが「最高」の閾値以上であること
        if (score >= scoreThresholdForBest)
        {
            Debug.Log($"Score: {score} -> BEST ENDING (Book)");

            // 演出用の親オブジェクトを表示
            targetEnding = endingBestRoot;
            if (targetEnding != null) targetEnding.SetActive(true);

            if (bestEndLoadingPanel != null && bestEndLoadingSlider != null)
            {
                // パネルを表示し、数値をリセット
                bestEndLoadingPanel.SetActive(true);
                bestEndLoadingSlider.value = 0f;

                // 0% -> 100% (0.0 -> 1.0) へアニメーション
                float loadTimer = 0f;
                while (loadTimer < bestEndLoadingDuration)
                {
                    loadTimer += Time.deltaTime;
                    // 0〜1の範囲で値を更新
                    bestEndLoadingSlider.value = Mathf.Clamp01(loadTimer / bestEndLoadingDuration);
                    yield return null;
                }

                // 確実に100%にする
                bestEndLoadingSlider.value = 1.0f;

                // 100%になった後、一拍置く（0.5秒）
                yield return new WaitForSeconds(0.5f);

                if (bestEndAnimator != null)
                {
                    bestEndAnimator.SetTrigger("Metamorphose");
                }

                yield return new WaitForSeconds(1.0f);

                // パネルを非表示にする
                bestEndLoadingPanel.SetActive(false);
            }

            if (screenEffectsController != null && metamorphoseSound != null)
                screenEffectsController.GetComponent<AudioSource>().PlayOneShot(metamorphoseSound, metamorphoseVolume);

            // A. NPCのセリフ（吹き出し）を表示
            if (bestEndBubbleController != null && bestEndInk != null)
            {
                // 吹き出しを表示し、3秒間待機する
                yield return StartCoroutine(bestEndBubbleController.PlaySpeechSequence(bestEndInk, 3.0f));
            }

            // B. 本を読むアニメーションを開始
            if (bestEndAnimator != null)
            {
                bestEndAnimator.SetTrigger("Book");
            }

            // 読書している姿を見せるための余韻（4秒）
            yield return new WaitForSeconds(4.0f);
        }

        // --- ケース2: 良好なエンディング (GOOD ENDING) ---
        // 条件: 最高ではないが、「良好」の閾値以上であること
        else if (score >= scoreThresholdForGood)
        {
            Debug.Log($"Score: {score} -> GOOD ENDING (New)");

            if (goodEndAnimator != null)
            {
                goodEndAnimator.SetTrigger("Metamorphose");
            }

            targetEnding = endingGoodRoot;
            if (targetEnding != null) targetEnding.SetActive(true);

            yield return new WaitForSeconds(1.0f);

            // A. NPCのセリフ（吹き出し）を表示
            if (goodEndBubbleController != null && goodEndInk != null)
            {
                // Inkファイルを再生し、読み終わってから2秒間待機する
                yield return StartCoroutine(goodEndBubbleController.PlaySpeechSequence(goodEndInk, 2.0f));
            }
            else
            {
                // 設定がない場合の保険として少し待つ
                yield return new WaitForSeconds(3.0f);
            }
        }

        // --- ケース3: 最悪のエンディング (BAD ENDING) ---
        // 条件: スコアが「最悪」の閾値以下であること
        else if (score <= scoreThresholdForBad)
        {
            Debug.Log($"Score: {score} -> BAD ENDING (Crushed)");

            targetEnding = endingCrushedRoot;
            if (targetEnding != null) targetEnding.SetActive(true);

            // A. 警告メッセージ（ダイアログ）の表示
            if (badEndInk != null)
            {
                var dm = DialogueManager.GetInstance();
                dm.EnterDialogueMode(badEndInk);

                // 文字が表示されきるのを待つ
                yield return new WaitUntil(() => dm.canContinueToNextLine);

                // 読み終わる余韻（2秒）
                yield return new WaitForSeconds(2.0f);

                // プレイヤーのクリックを待たず、強制的に会話を進めて閉じる
                dm.AdvanceDialogue();
                yield return new WaitUntil(() => dm.dialogueIsPlaying == false);
            }

            yield return new WaitForSeconds(1f);

            // B. 警報音の再生
            if (screenEffectsController != null && alarmSound != null)
                screenEffectsController.GetComponent<AudioSource>().PlayOneShot(alarmSound, alarmVolume);

            yield return new WaitForSeconds(1.5f);

            // C. 「神の手」による破壊演出
            if (handAnimator != null)
            {
                handAnimator.SetTrigger("Kill"); // 手が下りてくるアニメーション

                // 手が接触するタイミングに合わせて破壊音と破壊アニメを再生
                yield return new WaitForSeconds(0.1f);
                if (crushSound != null)
                {
                    screenEffectsController.GetComponent<AudioSource>().PlayOneShot(crushSound, crushVolume);
                    if (crushedNpcAnimator != null) crushedNpcAnimator.SetTrigger("Die");
                }
            }
            // アニメーションが完了するまで待機
            yield return new WaitForSeconds(handAnimationDuration);
        }

        // --- ケース4: 普通のエンディング (NORMAL ENDING) ---
        else
        {
            Debug.Log($"Score: {score} -> NORMAL ENDING (Initialize)");

            targetEnding = endingInitRoot;
            if (targetEnding != null) targetEnding.SetActive(true);

            // A. システムメッセージ再生
            if (normalEndSystemInk != null)
            {
                var dm = DialogueManager.GetInstance();
                dm.EnterDialogueMode(normalEndSystemInk);
                yield return new WaitForSeconds(4.0f);
                dm.AdvanceDialogue();
                yield return new WaitUntil(() => dm.dialogueIsPlaying == false);
            }

            yield return new WaitForSeconds(0.5f);

            // B. 初期化音 ＆ 倒れる演出 ＆ グリッチ演出
            if (screenEffectsController != null)
            {
                // SE再生
                if (initSound != null)
                    screenEffectsController.GetComponent<AudioSource>().PlayOneShot(initSound, initVolume);

                // 一瞬だけグリッチさせる（0.5秒間）
                StartCoroutine(screenEffectsController.TriggerGlitchBurstRoutine(0.5f));
            }

            if (normalEndAnimator != null) normalEndAnimator.SetTrigger("Init");

            // 倒れている間の待機
            yield return new WaitForSeconds(initDuration);

            // D. NPCのセリフ（記憶喪失を示唆する吹き出し）
            if (normalEndBubbleController != null && normalEndWakeUpInk != null)
            {
                yield return StartCoroutine(normalEndBubbleController.PlaySpeechSequence(normalEndWakeUpInk, 3.0f));
            }
        }

        // ---------------------------------------------------------
        // 3. 終了処理とシーン遷移
        // ---------------------------------------------------------

        Debug.Log("フェードアウト開始...");

        // 画面を徐々に暗くする（フェードアウト）
        if (fadeOutImage != null)
        {
            fadeOutImage.gameObject.SetActive(true);
            fadeOutImage.color = new Color(0, 0, 0, 0); // 透明からスタート

            float timer = 0f;
            while (timer < fadeDuration)
            {
                float alpha = timer / fadeDuration;
                fadeOutImage.color = new Color(0, 0, 0, alpha); // 徐々に黒く
                timer += Time.deltaTime;
                yield return null;
            }
            fadeOutImage.color = new Color(0, 0, 0, 1); // 完全に真っ黒にする
        }

        Debug.Log("全シーケンス終了。遷移します。");

        // 日付を進める（Day1 -> Day2）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceDay();
        }

        // 次のシーン（タイトルや次の日のシーンなど）へロード
        SceneManager.LoadScene(nextSceneName);
    }
}