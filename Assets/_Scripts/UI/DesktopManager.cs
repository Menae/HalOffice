using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // シーン遷移に必要

public class DesktopManager : MonoBehaviour
{
    // === Inspectorで設定する項目 ===
    [Header("ログインシーケンス設定")]
    [Tooltip("ログイン画面の「はじめる」ボタン")]
    public Button startButton;
    [Tooltip("画面遷移に使う暗転用のImage")]
    public Image fadeImage;
    [Tooltip("ログイン画面全体を管理する親オブジェクトのCanvas Group")]
    public CanvasGroup loginScreenCanvasGroup;
    [Tooltip("ログイン後に表示するデスクトップ画面の親オブジェクト")]
    public GameObject desktopScreen;
    [Tooltip("フェードにかかる時間（秒）")]
    public float fadeDuration = 1.0f;
    [Tooltip("アプリケーションを終了するためのボタン")]
    public Button exitButton;

    [Header("通知設定")]
    [Tooltip("通知アプリの右上の赤い丸")]
    public GameObject notificationBadge;
    [Tooltip("デスクトップ表示後、通知が来るまでの時間（秒）")]
    public float notificationDelay = 3.0f;
    [Tooltip("通知が来た時に再生する効果音")]
    public AudioClip notificationSound;
    [Range(0f, 1f)]
    [Tooltip("通知効果音の音量")]
    public float notificationVolume = 0.8f;
    [Tooltip("効果音を再生するためのAudioSourceコンポーネント")]
    public AudioSource audioSource;

    [Header("デスクトップ設定")]
    [Tooltip("デスクトップのアプリ情報をまとめたリスト")]
    public List<AppEntry> desktopApps;
    [Tooltip("アイコンをホバーした時に表示する白い枠のImage")]
    public Image iconHoverFrame;

    [System.Serializable]
    public class AppEntry
    {
        public string appName;
        public Button appIconButton;
        public GameObject appPanel;
        public List<Button> closeButtons;

        [Header("オプション設定")]
        [Tooltip("アイコンクリック時にパネルと一緒にアクティブになるImage（任意）")]
        public Image imageToActivate;
        
        // ▼▼▼ 以下を復活 ▼▼▼
        [Header("シーン遷移設定（任意）")]
        [Tooltip("このボタンを押すと指定したシーンに遷移する（任意）")]
        public Button sceneLoadButton;
        [Tooltip("遷移先のシーン名")]
        public string sceneNameToLoad;
    }

    // === 内部処理用の変数 ===
    private bool isFading = false;
    private bool hasClearedFirstNotification = false;

    void Start()
    {
        // --- 初期化処理 ---
        loginScreenCanvasGroup.gameObject.SetActive(true);
        desktopScreen.SetActive(false);

        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
            fadeImage.gameObject.SetActive(false);
        }
        if (notificationBadge != null)
        {
            notificationBadge.SetActive(false);
        }
        if (iconHoverFrame != null)
        {
            iconHoverFrame.gameObject.SetActive(false);
        }

        // --- ボタンのクリックイベントをスクリプトから登録 ---
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartLoginSequence);
        }
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitApplication);
        }

        for (int i = 0; i < desktopApps.Count; i++)
        {
            AppEntry app = desktopApps[i];
            GameObject panelToToggle = app.appPanel;
            Image imageToToggle = app.imageToActivate;

            // --- アイコンボタンのクリックイベント設定 ---
            if (app.appIconButton != null)
            {
                if (i == 0)
                {
                    app.appIconButton.onClick.AddListener(OnFirstAppIconClick);
                }
                else
                {
                    app.appIconButton.onClick.AddListener(() => {
                        if (panelToToggle != null) panelToToggle.SetActive(true);
                        if (imageToToggle != null) imageToToggle.gameObject.SetActive(true);
                    });
                }

                // --- ホバーイベントの動的設定 ---
                EventTrigger trigger = app.appIconButton.gameObject.GetComponent<EventTrigger>() ?? app.appIconButton.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entryEnter.callback.AddListener((eventData) => { ShowHoverFrame(app.appIconButton.transform); });
                trigger.triggers.Add(entryEnter);
                EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((eventData) => { HideHoverFrame(); });
                trigger.triggers.Add(entryExit);
            }

            // --- 閉じるボタンのクリックイベント設定 ---
            if (app.closeButtons != null && app.closeButtons.Count > 0)
            {
                foreach (Button button in app.closeButtons)
                {
                    if (button != null)
                    {
                        button.onClick.AddListener(() => {
                            if (panelToToggle != null) panelToToggle.SetActive(false);
                            if (imageToToggle != null) imageToToggle.gameObject.SetActive(false);
                        });
                    }
                }
            }

            // ▼▼▼ 以下を復活 ▼▼▼
            // --- シーン遷移ボタンのクリックイベント設定 ---
            if (app.sceneLoadButton != null && !string.IsNullOrEmpty(app.sceneNameToLoad))
            {
                string sceneName = app.sceneNameToLoad;
                app.sceneLoadButton.onClick.AddListener(() => StartCoroutine(LoadSceneRoutine(sceneName)));
            }
            // ▲▲▲ ここまで ▲▲▲

            // --- 開始時の非表示設定 ---
            if (panelToToggle != null)
            {
                panelToToggle.SetActive(false);
            }
            if (imageToToggle != null)
            {
                imageToToggle.gameObject.SetActive(false);
            }
        }
    }

    private void ShowHoverFrame(Transform iconTransform)
    {
        if (iconHoverFrame == null) return;
        iconHoverFrame.transform.SetParent(iconTransform.parent, false);
        iconHoverFrame.rectTransform.position = iconTransform.position;
        iconHoverFrame.rectTransform.sizeDelta = iconTransform.GetComponent<RectTransform>().sizeDelta;
        iconHoverFrame.transform.SetAsFirstSibling();
        iconHoverFrame.gameObject.SetActive(true);
    }

    private void HideHoverFrame()
    {
        if (iconHoverFrame == null) return;
        iconHoverFrame.gameObject.SetActive(false);
    }
    
    private void OnFirstAppIconClick()
    {
        if (desktopApps.Count > 0)
        {
            AppEntry firstApp = desktopApps[0];
            if (firstApp.appPanel != null)
            {
                firstApp.appPanel.SetActive(true);
            }
            if (firstApp.imageToActivate != null)
            {
                firstApp.imageToActivate.gameObject.SetActive(true);
            }
        }
        
        if (!hasClearedFirstNotification && notificationBadge != null && notificationBadge.activeSelf)
        {
            notificationBadge.SetActive(false);
            hasClearedFirstNotification = true;
        }
    }
    
    public void StartLoginSequence()
    {
        if (isFading) return;
        StartCoroutine(LoginRoutine());
    }
    
    public void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoginRoutine()
    {
        isFading = true;
        
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, timer / fadeDuration);
            yield return null;
        }
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1f);

        if (loginScreenCanvasGroup != null)
        {
            loginScreenCanvasGroup.alpha = 0;
            loginScreenCanvasGroup.interactable = false;
            loginScreenCanvasGroup.gameObject.SetActive(false);
        }
        if (desktopScreen != null)
        {
            desktopScreen.SetActive(true);
        }

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 1f - (timer / fadeDuration));
            yield return null;
        }
        fadeImage.gameObject.SetActive(false);

        isFading = false;
        
        StartCoroutine(ShowNotificationRoutine());
    }
    
    private IEnumerator ShowNotificationRoutine()
    {
        yield return new WaitForSeconds(notificationDelay);
        if (notificationBadge != null)
        {
            notificationBadge.SetActive(true);
        }
        if (audioSource != null && notificationSound != null)
        {
            audioSource.PlayOneShot(notificationSound, notificationVolume);
        }
    }

    // ▼▼▼ 以下を復活 ▼▼▼
    /// <summary>
    /// 指定されたシーンにフェードアウトして遷移するコルーチン
    /// </summary>
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (isFading) yield break;
        isFading = true;

        // 1. フェードアウト
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, timer / fadeDuration);
            yield return null;
        }

        // 2. シーンをロード
        SceneManager.LoadScene(sceneName);
    }
}