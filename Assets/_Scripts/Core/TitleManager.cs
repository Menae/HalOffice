using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// タイトル画面の機能を管理するクラス。
/// シーン遷移、アプリケーション終了、および設定パネル（音量調節）の表示切り替えを制御する。
/// </summary>
public class TitleManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("「はじめから」ボタンで遷移する先のシーン名")]
    public string gameSceneName = "P1&P2";

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

    private void Start()
    {
        // 音量設定を読み込み反映
        LoadVolumeSettings();

        // 初期表示の設定：タイトルを表示し、設定パネルを隠す
        ShowTitlePanel();
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
}