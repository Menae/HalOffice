using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームのスタート画面（ログイン画面、デスクトップ画面）のUI遷移と、
/// デスクトップ上の各アプリケーションの挙動を一元管理するクラス。
/// </summary>
public class DesktopManager : MonoBehaviour
{
    // --- Inspectorで設定する項目 ---

    [Header("ログインシーケンス設定")]
    [Tooltip("ログイン画面の「はじめる」ボタン")]
    public Button startButton;
    [Tooltip("ログイン画面全体を管理する親オブジェクトのCanvas Group")]
    public CanvasGroup loginScreenCanvasGroup;
    [Tooltip("ログイン後に表示するデスクトップ画面の親オブジェクト")]
    public GameObject desktopScreen;
    [Tooltip("アプリケーションを終了するためのボタン")]
    public Button exitButton;

    [Header("通知設定")]
    [Tooltip("通知アプリアイコンの右上に表示するバッジ")]
    public GameObject notificationBadge;
    [Tooltip("デスクトップ表示後、通知が表示されるまでの待機時間(秒)")]
    public float notificationDelay = 3.0f;
    [Tooltip("通知が表示される時に再生する効果音")]
    public AudioClip notificationSound;
    [Range(0f, 1f)]
    [Tooltip("通知効果音の音量")]
    public float notificationVolume = 0.8f;
    [Tooltip("効果音を再生するためのAudioSourceコンポーネント")]
    public AudioSource audioSource;

    [Header("デスクトップ設定")]
    [Tooltip("デスクトップのアプリ情報をまとめたリスト")]
    public List<AppEntry> desktopApps;
    [Tooltip("アプリアイコンをホバーした時に表示する背景のImage")]
    public Image iconHoverFrame;

    [Header("チュートリアル会話設定")]
    [Tooltip("シーン内のChatController")]
    public ChatController chatController;
    [Tooltip("最初に再生する会話のInkファイル（JSONアセット）")]
    public TextAsset tutorialChatInk;

    [Header("デバッグ設定")]
    [Tooltip("チェックを入れると、エディタ実行時に毎回チュートリアルが再生されます")]
    public bool forceShowTutorialInEditor = false;

    /// <summary>
    /// デスクトップ上の各アプリのUI要素をセットで管理するためのクラス。
    /// </summary>
    [System.Serializable]
    public class AppEntry
    {
        [Tooltip("Inspector上でアプリを識別するための名前")]
        public string appName;
        [Tooltip("アプリを起動するためのアイコンボタン")]
        public Button appIconButton;
        [Tooltip("アイコンクリック時に表示されるウィンドウパネル")]
        public GameObject appPanel;
        [Tooltip("ウィンドウを閉じるためのボタン（複数可）")]
        public List<Button> closeButtons;

        [Header("オプション設定")]
        [Tooltip("アイコンクリック時にパネルと一緒にアクティブになるImage（任意）")]
        public Image imageToActivate;

        [Header("シーン遷移設定（任意）")]
        [Tooltip("このボタンを押すと指定したシーンに遷移する（任意）")]
        public Button sceneLoadButton;
        [Tooltip("遷移先のシーン名")]
        public string sceneNameToLoad;
    }

    // --- 内部処理用の変数 ---
    private bool isFading = false;
    private bool hasClearedFirstNotification = false;

    #region Unity Lifecycle Methods

    private void OnEnable()
    {
        ChatController.OnConversationFinished += HandleTutorialFinished;
    }

    private void OnDisable()
    {
        ChatController.OnConversationFinished -= HandleTutorialFinished;
    }

    void Start()
    {
        // --- ボタンのクリックイベントをスクリプトから登録 ---
        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(StartLoginSequence);
        }
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitApplication);
        }

        // --- デスクトップアプリの初期設定 ---
        for (int i = 0; i < desktopApps.Count; i++)
        {
            AppEntry app = desktopApps[i];
            GameObject panelToToggle = app.appPanel;
            Image imageToToggle = app.imageToActivate;

            if (app.appIconButton != null)
            {
                if (i == 0) // Element 0 (通知アプリ) の場合
                {
                    app.appIconButton.onClick.AddListener(OnFirstAppIconClick);

                    // 開始時は通知アプリアイコンをクリックできないようにする
                    app.appIconButton.interactable = false;
                }
                else // それ以外のアプリ
                {
                    app.appIconButton.onClick.AddListener(() => {
                        if (panelToToggle != null) panelToToggle.SetActive(true);
                        if (imageToToggle != null) imageToToggle.gameObject.SetActive(true);
                    });
                }
                SetupHoverEvents(app.appIconButton);
            }

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

            if (app.sceneLoadButton != null && !string.IsNullOrEmpty(app.sceneNameToLoad))
            {
                string sceneName = app.sceneNameToLoad;
                app.sceneLoadButton.onClick.AddListener(() => {
                    var sequenceManager = FindObjectOfType<StartupSequenceManager>();
                    if (sequenceManager != null)
                    {
                        StartCoroutine(LoadSceneRoutine(sceneName, sequenceManager));
                    }
                });
            }

            if (panelToToggle != null) panelToToggle.SetActive(false);
            if (imageToToggle != null) imageToToggle.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Public Methods

    public void InitializeForSequence()
    {
        if (loginScreenCanvasGroup != null) loginScreenCanvasGroup.gameObject.SetActive(true);
        if (desktopScreen != null) desktopScreen.SetActive(false);
        if (notificationBadge != null) notificationBadge.SetActive(false);
    }

    public void TakeOverControl()
    {
        if (startButton != null) startButton.interactable = true;
    }

    public void StartLoginSequence()
    {
        if (isFading) return;
        StartCoroutine(LoginToDesktopRoutine());
    }

    public void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Private Methods & Coroutines

    private void OnFirstAppIconClick()
    {
        if (desktopApps.Count > 0)
        {
            AppEntry firstApp = desktopApps[0];
            if (firstApp.appPanel != null) firstApp.appPanel.SetActive(true);
            if (firstApp.imageToActivate != null) firstApp.imageToActivate.gameObject.SetActive(true);
        }

        if (!hasClearedFirstNotification && notificationBadge != null && notificationBadge.activeSelf)
        {
            notificationBadge.SetActive(false);
            hasClearedFirstNotification = true;
        }
    }

    private IEnumerator LoginToDesktopRoutine()
    {
        isFading = true;

        var sequenceManager = FindObjectOfType<StartupSequenceManager>();
        if (sequenceManager != null)
        {
            yield return StartCoroutine(sequenceManager.Fade(Color.black));
        }

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

        // デスクトップ画面に切り替わったので、タスクバーを表示する
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.SetDesktopUIVisibility(true);
        }

        if (sequenceManager != null)
        {
            yield return StartCoroutine(sequenceManager.Fade(Color.clear));
        }

        isFading = false;

        bool shouldShowTutorial = (PlayerPrefs.GetInt("HasSeenTutorial", 0) == 0);
#if UNITY_EDITOR
        if (forceShowTutorialInEditor) { shouldShowTutorial = true; }
#endif

        if (shouldShowTutorial)
        {
            // シングルトン経由でChatControllerを呼び出す
            if (ChatController.Instance != null && tutorialChatInk != null)
            {
                ChatController.Instance.StartConversation(tutorialChatInk);
                PlayerPrefs.SetInt("HasSeenTutorial", 1);
            }
        }
        else
        {
            StartCoroutine(ShowNotificationRoutine());
        }
    }

    private void HandleTutorialFinished()
    {
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

        // 通知が表示されたので、アイコンをクリック可能にする
        if (desktopApps.Count > 0 && desktopApps[0].appIconButton != null)
        {
            desktopApps[0].appIconButton.interactable = true;
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneName, StartupSequenceManager sequenceManager)
    {
        if (isFading) yield break;
        isFading = true;

        if (sequenceManager != null)
        {
            yield return StartCoroutine(sequenceManager.Fade(Color.black));
        }

        SceneManager.LoadScene(sceneName);
    }

    private void SetupHoverEvents(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((eventData) => { ShowHoverFrame(button.transform); });
        trigger.triggers.Add(entryEnter);
        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((eventData) => { HideHoverFrame(); });
        trigger.triggers.Add(entryExit);
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

    #endregion
}