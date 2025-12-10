using System.Collections; // ▼▼▼ 追加 ▼▼▼
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ▼▼▼ 追加 ▼▼▼

public class MySceneManager : MonoBehaviour
{

    // シングルトンインスタンス
    public static MySceneManager Instance { get; private set; }

    [Header("フェード設定")]
    [Tooltip("フェード演出に使用するUIのImageコンポーネント")]
    public Image fadeImage;

    [Tooltip("フェードにかかる時間（秒）")]
    public float fadeDuration = 1.0f;

    private void Awake()
    {
        // シングルトンパターンの実装
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // シーンをまたいで存在させる
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初期状態ではフェード用の画像は透明で非表示にしておく
        if (fadeImage != null)
        {
            fadeImage.color = Color.clear;
            fadeImage.gameObject.SetActive(false);
        }
    }


    public void LoadScene(string sceneName)
    {
        // フェード用のImageが設定されていればフェードアウトを開始する
        if (fadeImage != null)
        {
            StartCoroutine(FadeOutAndLoadScene(sceneName));
        }
        else
        {
            // 設定されていなければ、即座にシーンをロードする
            Debug.LogWarning("Fade Imageが設定されていません。フェードアウトなしでシーンをロードします。");
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        // フェード用の画像をアクティブにする
        fadeImage.gameObject.SetActive(true);

        float timer = 0f;
        Color startColor = Color.clear;
        Color endColor = Color.black;

        // 指定した時間をかけて徐々に黒くする
        while (timer < fadeDuration)
        {
            fadeImage.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null; // 1フレーム待つ
        }

        // 完全に黒くしてからシーンをロード
        fadeImage.color = endColor;
        SceneManager.LoadScene(sceneName);
    }
}