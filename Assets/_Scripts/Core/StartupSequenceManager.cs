using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StartupSequenceManager : MonoBehaviour
{
    [Header("フェード設定")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    [Header("管理対象オブジェクト")]
    public GameObject titlePhase;
    public GameObject openingPhase;
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

    // DialogueManagerの会話終了イベントを購読するための処理
    private void OnEnable() { DialogueManager.OnDialogueFinished += OnOpeningChatFinished; }
    private void OnDisable() { DialogueManager.OnDialogueFinished -= OnOpeningChatFinished; }

    void Start()
    {
        // ゲーム開始時に、まずタスクバーを非表示にする
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.SetDesktopUIVisibility(false);
        }

        titlePhase.SetActive(false);
        openingPhase.SetActive(false);
        loginPhase.SetActive(false);
        if (desktopManager != null) desktopManager.gameObject.SetActive(false);

        StartCoroutine(MainSequence());
    }

    private IEnumerator MainSequence()
    {
        // --- 1. タイトル画面フェーズ ---
        fadeImage.color = Color.black;
        titlePhase.SetActive(true);
        yield return StartCoroutine(Fade(Color.clear));

        if (titleLogoAnimator != null) titleLogoAnimator.SetBool("TitleBoot", true);
        yield return new WaitForSeconds(titleAnimDuration);
        if (titleLogoAnimator != null) titleLogoAnimator.SetBool("TitleBoot", false);

        yield return StartCoroutine(Fade(Color.black));
        titlePhase.SetActive(false);

        // --- 2. オープニングフェーズ ---
        // ▼▼▼ この部分を修正 ▼▼▼
        // openingChatInkがInspectorで設定されている場合のみ、オープニングを実行
        if (openingDialogueManager != null && openingChatInk != null)
        {
            openingPhase.SetActive(true);
            yield return StartCoroutine(Fade(Color.clear));

            openingDialogueManager.EnterDialogueMode(openingChatInk);
            // "dialogueIsPlaying"がfalseになる(=会話が終わる)まで待機
            yield return new WaitUntil(() => openingDialogueManager.dialogueIsPlaying == false);

            yield return StartCoroutine(Fade(Color.black));
            openingPhase.SetActive(false);
        }
        else
        {
            // 設定されていない場合は、警告をログに出してスキップする
            Debug.LogWarning("オープニングの会話ファイルが設定されていないため、オープニングフェーズをスキップします。");
        }
        // ▲▲▲ ここまで ▲▲▲

        // --- 3. ログイン画面フェーズ ---
        // (この部分は変更なし)
        loginPhase.SetActive(true);
        if (desktopManager != null)
        {
            desktopManager.gameObject.SetActive(true);
            desktopManager.InitializeForSequence();
        }

        yield return StartCoroutine(Fade(Color.clear));

        if (desktopManager != null)
        {
            desktopManager.TakeOverControl();
        }

        Debug.Log("スタートアップシーケンス完了。DesktopManagerに処理を移行します。");

        this.enabled = false;
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