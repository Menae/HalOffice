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
    [Tooltip("【バッド】神の手によって潰される演出のルートオブジェクト")]
    public GameObject endingCrushedRoot;
    [Tooltip("【中立】初期化される演出のルートオブジェクト")]
    public GameObject endingInitRoot;
    [Tooltip("【グッド】会話後に本を読み始める演出のルートオブジェクト")]
    public GameObject endingBookRoot;

    [Header("評価基準")]
    [Tooltip("グッド演出（本を読む）が発生する最低スコア")]
    public int scoreThresholdForGood = 8;
    [Tooltip("バッド演出（潰される）が発生してしまうスコア以下")]
    public int scoreThresholdForBad = 3;

    [Header("Bad End 設定")]
    [Tooltip("再生するダイアログのInkファイル")]
    public TextAsset badEndInk;
    [Tooltip("神の手のアニメーター（Ending_Bad_Crushedの子要素に配置）")]
    public Animator handAnimator;
    [Tooltip("破壊されるNPCのアニメーター")]
    public Animator crushedNpcAnimator;
    [Tooltip("警報音のSE")]
    public AudioClip alarmSound;
    [Range(0f, 1f)]
    public float alarmVolume = 1.0f;
    [Tooltip("破壊音（潰した時の音）")]
    public AudioClip crushSound;
    [Range(0f, 1f)]
    public float crushVolume = 1.0f;
    [Tooltip("神の手アニメーションの再生待ち時間（秒）")]
    public float handAnimationDuration = 2.0f;

    [Header("Good End 設定")]
    [Tooltip("会話用のInkファイル")]
    public TextAsset goodEndInk;
    [Tooltip("吹き出し制御スクリプト（Ending_Good_Book内のNPCにアタッチしたもの）")]
    public ResultSpeechBubbleController goodEndBubbleController;
    [Tooltip("本を読むアニメーター")]
    public Animator bookReadAnimator;

    [Header("遷移設定")]
    [Tooltip("全ての演出終了後に遷移するシーン名")]
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
    /// スコアに応じて4パターンの演出を分岐再生し、フェードアウトして遷移する
    /// </summary>
    private IEnumerator PlayPostResultSequence()
    {
        // 1. まず画面を明るく戻す
        if (screenEffectsController != null)
        {
            screenEffectsController.TriggerTvOn();
        }

        // パネルが消える一瞬の間
        yield return new WaitForSeconds(0.5f);

        // 入力は無効化しておく
        if (GameManager.Instance != null) GameManager.Instance.SetInputEnabled(false);

        int score = 0;
        if (GameManager.Instance != null) score = GameManager.Instance.correctPlacementCount;

        GameObject targetEnding = null;

        // --- 演出分岐 ---

        // 1. BAD ENDING (Crushed)
        if (score <= scoreThresholdForBad)
        {
            Debug.Log($"Score: {score} -> BAD ENDING (Crushed)");
            targetEnding = endingCrushedRoot;

            // ルートオブジェクトを表示
            if (targetEnding != null) targetEnding.SetActive(true);

            // A. ダイアログ再生
            if (badEndInk != null)
            {
                var dm = DialogueManager.GetInstance();
                dm.EnterDialogueMode(badEndInk);

                // 文字が出終わるまで待つ
                yield return new WaitUntil(() => dm.canContinueToNextLine);

                // 読み終わる余韻（2秒）
                yield return new WaitForSeconds(2.0f);

                // クリックの代わりにコードから進める
                dm.AdvanceDialogue();

                // 終了待ち
                yield return new WaitUntil(() => dm.dialogueIsPlaying == false);
            }

            yield return new WaitForSeconds(1f);

            // B. 警報音再生
            if (screenEffectsController != null && alarmSound != null)
            {
                screenEffectsController.GetComponent<AudioSource>().PlayOneShot(alarmSound, alarmVolume);
            }

            yield return new WaitForSeconds(1.5f);

            // C. 神の手アニメーション再生
            if (handAnimator != null)
            {
                handAnimator.SetTrigger("Kill");

                yield return new WaitForSeconds(0.2f);
                if (crushSound != null)
                {
                    screenEffectsController.GetComponent<AudioSource>().PlayOneShot(crushSound, crushVolume);

                    crushedNpcAnimator.SetTrigger("Die");
                }
            }

            // アニメーションが終わるまで待機
            yield return new WaitForSeconds(handAnimationDuration);
        }
        // メソッド内の分岐（Good End）
        else if (score >= scoreThresholdForGood)
        {
            Debug.Log($"Score: {score} -> GOOD ENDING (Book)");
            targetEnding = endingBookRoot;
            if (targetEnding != null) targetEnding.SetActive(true);

            // 1. 吹き出し会話を再生（待機する）
            if (goodEndBubbleController != null && goodEndInk != null)
            {
                // 3秒間表示したままにする
                yield return StartCoroutine(goodEndBubbleController.PlaySpeechSequence(goodEndInk, 3.0f));
            }

            // 2. 本を読むアニメーション開始
            if (bookReadAnimator != null)
            {
                bookReadAnimator.SetTrigger("Book");
            }

            // 読書シーンを少し見せる
            yield return new WaitForSeconds(4.0f);
        }
        // 3. NORMAL ENDING A
        else if (endingInitRoot != null)
        {
            // ... (そのまま)
            targetEnding = endingInitRoot;
            if (targetEnding != null) targetEnding.SetActive(true);
            yield return new WaitForSeconds(4.0f);
        }
        // 4. NORMAL ENDING B
        else
        {
            // ... (そのまま)
            targetEnding = null;
            yield return new WaitForSeconds(2.0f);
        }

        // --- フェードアウト処理 ---
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceDay();
        }

        SceneManager.LoadScene(nextSceneName);
    }
}