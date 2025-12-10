using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スタートボタンの動作を制御し、フェード演出とシーン遷移を管理するクラス。
/// ボタン押下時に画面を暗転させた後、指定オブジェクトをアクティブ化し、元のUIを非表示にする。
/// </summary>
[RequireComponent(typeof(Button))]
public class StartButtonController : MonoBehaviour
{
    [Header("フェード設定")]
    [Tooltip("暗転に使用する黒いImageコンポーネント")]
    public Image fadeImage;

    [Tooltip("フェードアウトにかかる時間（秒）")]
    public float fadeOutDuration = 1.0f;

    [Tooltip("画面が真っ暗な状態で待機する時間（秒）")]
    public float holdBlackDuration = 0.5f;

    [Tooltip("フェードインにかかる時間（秒）")]
    public float fadeInDuration = 1.0f;

    [Header("シーン遷移設定")]
    [Tooltip("暗転後にアクティブにするGameObject")]
    public GameObject objectToActivate;

    [Tooltip("遷移後に非表示にするUIの親オブジェクト")]
    public GameObject objectToDeactivate;

    /// <summary>
    /// objectToDeactivateから自動取得されるCanvasGroup。UI全体のフェード制御に使用。
    /// </summary>
    private CanvasGroup uiToFadeOut;

    /// <summary>
    /// このGameObjectにアタッチされているButtonコンポーネント。
    /// </summary>
    private Button startButton;

    /// <summary>
    /// 初期化処理。Buttonコンポーネントの取得と、必要なコンポーネントの検証を行う。
    /// fadeImageまたはCanvasGroupが未設定の場合はボタンを無効化する。
    /// </summary>
    private void Awake()
    {
        startButton = GetComponent<Button>();

        if (objectToDeactivate != null)
        {
            uiToFadeOut = objectToDeactivate.GetComponent<CanvasGroup>();
        }

        if (fadeImage == null || uiToFadeOut == null)
        {
            Debug.LogError("Fade Image または Object To Deactivate に Canvas Group が設定されていません！", this.gameObject);
            startButton.interactable = false;
        }
    }

    /// <summary>
    /// スタートボタンがクリックされた際に呼ばれる。
    /// ボタンが無効化されている場合は何もせず、有効な場合はフェード演出を開始する。
    /// </summary>
    public void OnStartButtonClick()
    {
        if (startButton.interactable == false) return;
        StartCoroutine(FadeAndActivateRoutine());
    }

    /// <summary>
    /// フェードアウト → オブジェクト切り替え → フェードイン の一連の演出を実行するコルーチン。
    /// 1. 画面を徐々に暗転させる
    /// 2. 暗転中に新しいオブジェクトをアクティブ化し、旧UIを透明にする
    /// 3. 指定時間暗転を維持
    /// 4. 画面を徐々に明るくする
    /// 5. 旧UIオブジェクトを非アクティブ化して完了
    /// </summary>
    private IEnumerator FadeAndActivateRoutine()
    {
        startButton.interactable = false;

        // フェードアウト実行
        fadeImage.gameObject.SetActive(true);
        Color fadeColor = fadeImage.color;
        float timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeOutDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

        // 暗転した瞬間の処理
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }
        uiToFadeOut.alpha = 0;
        uiToFadeOut.interactable = false;

        // 真っ暗な時間を維持
        yield return new WaitForSeconds(holdBlackDuration);

        // フェードイン実行
        timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1.0f - Mathf.Clamp01(timer / fadeInDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        fadeImage.gameObject.SetActive(false);

        // 最後の後処理
        // CanvasGroupのAlphaを1に戻して、次回表示される時に備える
        uiToFadeOut.alpha = 1;
        // 指定された親オブジェクトごと非アクティブにする
        objectToDeactivate.SetActive(false);
    }
}