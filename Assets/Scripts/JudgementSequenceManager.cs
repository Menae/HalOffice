using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; // シーン遷移に必要

public class JudgementSequenceManager : MonoBehaviour
{
    // 状態を管理するためのenum（列挙型）
    private enum JudgementState { WaitingForStamp, Stamped, Submitted }
    private JudgementState currentState = JudgementState.WaitingForStamp;

    [Header("UIオブジェクト参照")]
    [Tooltip("調査書全体のゲームオブジェクト")]
    public GameObject investigationDocument;
    [Tooltip("送信ボタン")]
    public Button submitButton;
    [Tooltip("閉じるボタン")]
    public Button closeButton;

    [Header("結果表示設定")]
    [Tooltip("結果を表示するImageコンポーネント")]
    public Image resultImage;
    [Tooltip("承認（緑スタンプ）の時に表示するスプライト")]
    public Sprite approveSprite;
    [Tooltip("却下（赤スタンプ）の時に表示するスプライト")]
    public Sprite rejectSprite;

    [Header("フェードと遅延の設定")]
    [Tooltip("画面の暗転に使う黒いImage")]
    public Image fadePanel;
    [Tooltip("調査書が消えてから暗転するまでの待機時間")]
    public float delayBeforeFadeOut = 2.0f;
    [Tooltip("結果が表示されている時間")]
    public float resultDisplayDuration = 3.0f;
    [Tooltip("フェードにかかる時間")]
    public float fadeDuration = 1.0f;
    [Tooltip("最後の暗転後、シーン遷移するまでの待機時間")]
    public float delayAfterFinalFade = 1.5f;

    [Header("サウンド設定")]
    public AudioSource audioSource;
    public AudioClip submitSound;

    [Header("シーン設定")]
    [Tooltip("最後に戻るタイトルシーンの名前")]
    public string titleSceneName = "TitleScene";

    // どのスタンプが押されたかを記憶する変数
    private bool stampedResultIsApprove;

    void Start()
    {
        // 初期状態ではボタンを無効化し、結果画像を隠す
        submitButton.interactable = false;
        closeButton.interactable = false;
        resultImage.gameObject.SetActive(false);
        if (fadePanel != null) fadePanel.gameObject.SetActive(false);

        // ボタンにクリック時の処理を登録
        submitButton.onClick.AddListener(OnSubmitClicked);
        closeButton.onClick.AddListener(OnCloseClicked);
    }

    // JudgementZoneから呼び出されるメソッド
    public void OnStampApplied(bool isApprove)
    {
        // スタンプがまだ押されていない状態の時だけ処理
        if (currentState != JudgementState.WaitingForStamp) return;

        stampedResultIsApprove = isApprove; // 押されたスタンプの種類を記憶
        currentState = JudgementState.Stamped; // 状態を「スタンプ済み」に更新
        submitButton.interactable = true; // 送信ボタンを有効化
    }

    // 送信ボタンが押された時の処理
    private void OnSubmitClicked()
    {
        if (currentState != JudgementState.Stamped) return;

        if (audioSource != null && submitSound != null)
        {
            audioSource.PlayOneShot(submitSound); // 効果音を再生
        }
        currentState = JudgementState.Submitted; // 状態を「送信済み」に更新
        submitButton.interactable = false; // 送信ボタンを無効化
        closeButton.interactable = true; // 閉じるボタンを有効化
    }

    // 閉じるボタンが押された時の処理
    private void OnCloseClicked()
    {
        if (currentState != JudgementState.Submitted) return;
        closeButton.interactable = false;
        // 一連の終了演出を開始
        StartCoroutine(EndSequenceRoutine());
    }

    // 終了演出のシーケンス
    private IEnumerator EndSequenceRoutine()
    {
        // 調査書を非表示
        investigationDocument.SetActive(false);

        // 数秒待つ
        yield return new WaitForSeconds(delayBeforeFadeOut);

        // 画面を暗転させる
        yield return StartCoroutine(Fade(Color.black));

        // 1秒待つ
        yield return new WaitForSeconds(1.0f);

        // 結果のスプライトを設定し、表示
        resultImage.sprite = stampedResultIsApprove ? approveSprite : rejectSprite;
        resultImage.gameObject.SetActive(true);

        // 画面を元に戻す（フェードイン）
        yield return StartCoroutine(Fade(Color.clear));

        // 数秒間、結果を表示
        yield return new WaitForSeconds(resultDisplayDuration);

        // 再び画面を暗転させる
        yield return StartCoroutine(Fade(Color.black));

        // 指定した時間、待機する
        yield return new WaitForSeconds(delayAfterFinalFade);

        // タイトルシーンに戻る
        SceneManager.LoadScene(titleSceneName);
    }

    // フェード処理の汎用コルーチン
    private IEnumerator Fade(Color targetColor)
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        float timer = 0f;
        Color startColor = fadePanel.color;

        while (timer < fadeDuration)
        {
            fadePanel.color = Color.Lerp(startColor, targetColor, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        fadePanel.color = targetColor;
    }
}