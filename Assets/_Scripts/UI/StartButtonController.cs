using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    // このCanvasGroupはobjectToDeactivateから自動で取得する
    private CanvasGroup uiToFadeOut;
    private Button startButton;

    private void Awake()
    {
        startButton = GetComponent<Button>();

        // objectToDeactivateからCanvasGroupを自動で取得
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

    public void OnStartButtonClick()
    {
        if (startButton.interactable == false) return;
        StartCoroutine(FadeAndActivateRoutine());
    }

    private IEnumerator FadeAndActivateRoutine()
    {
        startButton.interactable = false;

        // --- 1. フェードアウト実行 ---
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

        // --- 2. 暗転した瞬間の処理 ---
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }
        uiToFadeOut.alpha = 0;
        uiToFadeOut.interactable = false;

        // --- 3. 真っ暗な時間を維持 ---
        yield return new WaitForSeconds(holdBlackDuration);

        // --- 4. フェードイン実行 ---
        timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1.0f - Mathf.Clamp01(timer / fadeInDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        fadeImage.gameObject.SetActive(false);

        // --- 5. 最後の後処理 ---
        // CanvasGroupのAlphaを1に戻して、次回表示される時に備える
        uiToFadeOut.alpha = 1;
        // 指定された親オブジェクトごと非アクティブにする
        objectToDeactivate.SetActive(false);
    }
}