using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// タイトル画面の機能を管理するクラス。
/// 起動時のロゴ演出、シーン遷移、アプリケーション終了、および設定パネル（音量調節）の表示切り替えを制御する。
/// </summary>
public class TitleManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("「はじめから」ボタンで遷移する先のシーン名")]
    public string gameSceneName = "P1&P2";

    [Header("Intro Sequence")]
    [Tooltip("最初に表示されるタイトルロゴのオブジェクト（Imageなど）")]
    public GameObject titleLogoObject;
    [Tooltip("ロゴの後に表示されるメッセージオブジェクト（TMPなど。「画面をクリック」等）")]
    public GameObject pressAnyKeyObject;
    [Tooltip("ロゴが表示されてからメッセージが表示され、入力受付開始になるまでの時間（秒）")]
    public float introDelayDuration = 2.0f;
    [Tooltip("フェードイン・フェードアウトにかかる時間（秒）")]
    public float fadeDuration = 1.0f;

    [Header("UI Panels")]
    [Tooltip("タイトル画面のメインパネル（はじめから・終了ボタン等が含まれる親オブジェクト）")]
    public GameObject titlePanel;

    [Tooltip("音量設定等のサブパネル（スライダーや戻るボタンが含まれる親オブジェクト）")]
    public GameObject settingsPanel;

    [Header("UI References (Volume)")]
    [Tooltip("マスター音量スライダー")]
    public Slider masterVolumeSlider;
    [Tooltip("BGM音量スライダー")]
    public Slider bgmVolumeSlider;
    [Tooltip("SE音量スライダー")]
    public Slider seVolumeSlider;

    // 音量保存用のキー
    private const string KEY_MASTER_VOL = "MasterVolume";
    private const string KEY_BGM_VOL = "BGMVolume";
    private const string KEY_SE_VOL = "SEVolume";

    // 内部ステート管理用
    private bool isWaitingForInput = false;

    private void Start()
    {
        // 音量設定を読み込み反映
        LoadVolumeSettings();

        // イントロ演出を開始
        StartCoroutine(StartIntroSequence());
    }

    /// <summary>
    /// 毎フレーム入力監視を行う。
    /// イントロ演出完了後に画面がクリックされたらメインパネルへの遷移を開始する。
    /// </summary>
    private void Update()
    {
        if (isWaitingForInput && Input.GetMouseButtonDown(0))
        {
            // クリックされたら入力待ちを解除し、フェードアウト遷移シーケンスを開始
            isWaitingForInput = false;
            StartCoroutine(TransitionToTitlePanelSequence());
        }
    }

    // --- イントロ演出ロジック ---

    /// <summary>
    /// ロゴフェードイン → 待機 → メッセージフェードイン の順序で処理を行うコルーチン。
    /// </summary>
    private IEnumerator StartIntroSequence()
    {
        // 初期化：UIパネル系を全て非表示にする
        if (titlePanel != null) titlePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // イントロ用オブジェクトを透明状態で初期化（CanvasGroupのalphaを0にする）
        InitializeForFade(titleLogoObject, 0f);
        InitializeForFade(pressAnyKeyObject, 0f);

        // 1. タイトルロゴのフェードイン
        yield return StartCoroutine(FadeCanvasGroup(titleLogoObject, 1f, fadeDuration));

        // 2. 指定時間待機（ロゴが表示されている状態）
        yield return new WaitForSeconds(introDelayDuration);

        // 3. PressAnyKeyメッセージのフェードイン
        yield return StartCoroutine(FadeCanvasGroup(pressAnyKeyObject, 1f, fadeDuration));

        // フェードイン完了後、入力待ちフラグを立てる
        isWaitingForInput = true;
    }

    // --- ボタン操作用メソッド ---

    /// <summary>
    /// 「はじめから」ボタン押下時。ゲーム本編へ遷移する。
    /// </summary>
    public void OnClickStart()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// 「音量（設定）」ボタン押下時。設定パネルを開く。
    /// </summary>
    public void OnClickOpenSettings()
    {
        ShowSettingsPanel();
    }

    /// <summary>
    /// 設定パネル内の「戻る」ボタン押下時。タイトル画面に戻る。
    /// </summary>
    public void OnClickCloseSettings()
    {
        ShowTitlePanel();
    }

    /// <summary>
    /// 「終了」ボタン押下時。アプリを終了する。
    /// </summary>
    public void OnClickExit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- 内部ロジック ---

    private void ShowTitlePanel()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void ShowSettingsPanel()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // --- 音量設定 ---

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(KEY_MASTER_VOL, value);
    }

    public void OnBGMVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_BGM_VOL, value);
        if (BGMManager.Instance != null)
        {
            var source = BGMManager.Instance.GetComponent<AudioSource>();
            if (source != null) source.volume = value;
        }
    }

    public void OnSEVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(KEY_SE_VOL, value);
    }

    private void LoadVolumeSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1.0f);
        float bgmVol = PlayerPrefs.GetFloat(KEY_BGM_VOL, 1.0f);
        float seVol = PlayerPrefs.GetFloat(KEY_SE_VOL, 1.0f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgmVol;
        if (seVolumeSlider != null) seVolumeSlider.value = seVol;

        OnMasterVolumeChanged(masterVol);
        OnBGMVolumeChanged(bgmVol);
        OnSEVolumeChanged(seVol);
    }

    /// <summary>
    /// 対象オブジェクトのCanvasGroupのアルファ値を指定時間かけて変更するコルーチン。
    /// </summary>
    private IEnumerator FadeCanvasGroup(GameObject target, float targetAlpha, float duration)
    {
        if (target == null) yield break;

        // CanvasGroupを取得。なければ自動的に追加する。
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

        float startAlpha = cg.alpha;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, timeElapsed / duration);
            yield return null;
        }

        cg.alpha = targetAlpha; // 最終値を確実に設定
    }

    /// <summary>
    /// 指定されたオブジェクトをアクティブにし、CanvasGroupの初期アルファ値を設定する。
    /// </summary>
    private void InitializeForFade(GameObject target, float initialAlpha)
    {
        if (target != null)
        {
            target.SetActive(true);
            CanvasGroup cg = target.GetComponent<CanvasGroup>();
            if (cg == null) cg = target.AddComponent<CanvasGroup>();
            cg.alpha = initialAlpha;
        }
    }

    /// <summary>
    /// イントロ終了時のフェードアウト遷移シーケンス。
    /// OnIntroFinished の代わりに呼び出される。
    /// </summary>
    private IEnumerator TransitionToTitlePanelSequence()
    {
        // ロゴとPressKeyを同時にフェードアウト開始
        Coroutine fadeOutLogo = StartCoroutine(FadeCanvasGroup(titleLogoObject, 0f, fadeDuration));
        Coroutine fadeOutPressKey = StartCoroutine(FadeCanvasGroup(pressAnyKeyObject, 0f, fadeDuration));

        // 両方のフェードアウト完了を待つ
        yield return fadeOutLogo;
        yield return fadeOutPressKey;

        // 完全に隠れたら非アクティブにする
        if (titleLogoObject != null) titleLogoObject.SetActive(false);
        if (pressAnyKeyObject != null) pressAnyKeyObject.SetActive(false);

        // メインパネルを表示
        ShowTitlePanel();
    }
}