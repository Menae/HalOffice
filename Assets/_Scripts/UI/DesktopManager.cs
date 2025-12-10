using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ゲームのスタート画面（ログイン画面、デスクトップ画面）のUI遷移と、
/// デスクトップ上の各アプリケーションの挙動を一元管理するクラス。
/// </summary>
public class DesktopManager : MonoBehaviour
{
    // --- Inspectorで設定する項目 ---

    [Header("ログインシーケンス設定")]
    [Tooltip("ログイン画面の「はじめる」ボタン")]
    /// <summary>
    /// ログイン画面の「はじめる」ボタン。InspectorでD&D。Startでクリックリスナを登録。
    /// </summary>
    public Button startButton;

    [Tooltip("ログイン画面全体を管理する親オブジェクトのCanvas Group")]
    /// <summary>
    /// ログイン画面をまとめるCanvasGroup。表示/非表示やインタラクティブ制御に使用。
    /// </summary>
    public CanvasGroup loginScreenCanvasGroup;

    [Tooltip("ログイン後に表示するデスクトップ画面の親オブジェクト")]
    /// <summary>
    /// デスクトップ画面のルートGameObject。ログイン完了時に有効化する。
    /// </summary>
    public GameObject desktopScreen;

    [Tooltip("アプリケーションを終了するためのボタン")]
    /// <summary>
    /// アプリ終了用ボタン。Editor/実機で挙動が異なるためStartでリスナを登録。
    /// </summary>
    public Button exitButton;

    [Header("通知設定")]
    [Tooltip("通知アプリアイコンの右上に表示するバッジ")]
    /// <summary>
    /// 通知バッジのGameObject。表示/非表示で通知状態を表現。
    /// </summary>
    public GameObject notificationBadge;

    [Tooltip("デスクトップ表示後、通知が表示されるまでの待機時間(秒)")]
    /// <summary>
    /// デスクトップ表示後に通知を表示するまでの待機時間（秒）。Inspectorで調整可能。
    /// </summary>
    public float notificationDelay = 3.0f;

    [Tooltip("通知が表示される時に再生する効果音")]
    /// <summary>
    /// 通知再生時のAudioClip。AudioSourceが割り当てられている場合に再生する。
    /// </summary>
    public AudioClip notificationSound;

    [Range(0f, 1f)]
    [Tooltip("通知効果音の音量")]
    /// <summary>
    /// 通知効果音の音量（0.0〜1.0）。
    /// </summary>
    public float notificationVolume = 0.8f;

    [Tooltip("効果音を再生するためのAudioSourceコンポーネント")]
    /// <summary>
    /// 効果音再生に使用するAudioSource。nullの場合は音を鳴らさない。
    /// </summary>
    public AudioSource audioSource;

    [Header("ハイライト演出設定")]
    [Tooltip("Inkタグから呼び出せるハイライト対象リスト")]
    /// <summary>
    /// ハイライト表示対象のリスト。各要素は名前で参照される。
    /// </summary>
    public List<HighlightTarget> highlightTargets;

    [Header("デスクトップ設定")]
    [Tooltip("デスクトップのアプリ情報をまとめたリスト")]
    /// <summary>
    /// デスクトップ上のアプリ情報リスト。各要素でアイコンやウィンドウを管理。
    /// </summary>
    public List<AppEntry> desktopApps;

    [Tooltip("アプリアイコンをホバーした時に表示する背景のImage")]
    /// <summary>
    /// アイコンホバー時に表示するフレームImage。Startで非表示にして親を保持。
    /// </summary>
    public Image iconHoverFrame;

    [Header("チュートリアル会話設定")]
    [Tooltip("シーン内のChatController")]
    /// <summary>
    /// シーン内のChatController参照。nullチェックあり。チュートリアル開始に使用。
    /// </summary>
    public ChatController chatController;

    [Tooltip("最初に再生する会話のInkファイル（JSONアセット）")]
    /// <summary>
    /// 最初に再生するチュートリアル用のInk JSONアセット。nullの場合は再生しない。
    /// </summary>
    public TextAsset tutorialChatInk;

    [Header("デバッグ設定")]
    [Tooltip("チェックを入れると、エディタ実行時に毎回チュートリアルが再生されます")]
    /// <summary>
    /// エディタ実行時にチュートリアルを強制表示するフラグ。ビルド時は無視可能。
    /// </summary>
    public bool forceShowTutorialInEditor = false;

    /// <summary>
    /// デスクトップ上の各アプリのUI要素をセットで管理するためのクラス。
    /// </summary>
    [System.Serializable]
    public class AppEntry
    {
        [Tooltip("Inspector上でアプリを識別するための名前")]
        /// <summary>
        /// Inspector上で識別する名前。ハードコードされた参照やログ出力に使用可能。
        /// </summary>
        public string appName;

        [Tooltip("アプリを起動するためのアイコンボタン")]
        /// <summary>
        /// アプリアイコンのButton。Startでクリックリスナを登録。
        /// </summary>
        public Button appIconButton;

        [Tooltip("アイコンクリック時に表示されるウィンドウパネル")]
        /// <summary>
        /// アイコンクリックで有効化するウィンドウのGameObject。nullチェックあり。
        /// </summary>
        public GameObject appPanel;

        [Tooltip("ウィンドウを閉じるためのボタン（複数可）")]
        /// <summary>
        /// ウィンドウを閉じるためのボタン一覧。各ボタンにリスナを追加してパネルを閉じる。
        /// </summary>
        public List<Button> closeButtons;

        [Header("オプション設定")]
        [Tooltip("アイコンクリック時にパネルと一緒にアクティブになるImage（任意）")]
        /// <summary>
        /// クリック時に同時にアクティブ化するImage。任意でnull可。
        /// </summary>
        public Image imageToActivate;

        [Header("シーン遷移設定（任意）")]
        [Tooltip("このボタンを押すと指定したシーンに遷移する（任意）")]
        /// <summary>
        /// シーン遷移をトリガーするボタン。シーン名が未設定の場合は無効。
        /// </summary>
        public Button sceneLoadButton;

        [Tooltip("遷移先のシーン名")]
        /// <summary>
        /// 遷移先のシーン名。空文字の場合は遷移しない。
        /// </summary>
        public string sceneNameToLoad;
    }

    // --- 内部処理用の変数 ---
    private bool isFading = false;
    private bool hasClearedFirstNotification = false;
    private Transform iconHoverFrameOriginalParent;
    private Coroutine currentEffectCoroutine = null;

    #region Unity Lifecycle Methods

    /// <summary>
    /// コンポーネントが有効化されたタイミングで呼ばれる。イベント購読を登録。
    /// ChatControllerの会話完了やタグ処理イベントを受け取るための登録を行う。
    /// </summary>
    private void OnEnable()
    {
        ChatController.OnConversationFinished += HandleTutorialFinished;
        ChatController.OnTagsProcessed += HandleTags;
    }

    /// <summary>
    /// コンポーネントが無効化されるタイミングで呼ばれる。イベント購読を解除。
    /// イベント登録が残らないように必ず解除する。
    /// </summary>
    private void OnDisable()
    {
        ChatController.OnConversationFinished -= HandleTutorialFinished;
        ChatController.OnTagsProcessed -= HandleTags;
    }

    /// <summary>
    /// 初期化処理。UnityのStartイベントで呼ばれる。Inspectorの参照チェックとボタンリスナ登録を行う。
    /// ホバーフレームの親を保存して非表示にする処理を実行。
    /// </summary>
    void Start()
    {
        // ホバーフレームの元の親を保存し、非表示にしておく
        if (iconHoverFrame != null)
        {
            iconHoverFrameOriginalParent = iconHoverFrame.transform.parent;
            iconHoverFrame.gameObject.SetActive(false);
        }

        // ボタンのクリックイベントをスクリプトから登録
        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(StartLoginSequence);
        }
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitApplication);
        }

        // デスクトップアプリの初期設定
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
                    app.appIconButton.onClick.AddListener(() =>
                    {
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
                        button.onClick.AddListener(() =>
                        {
                            if (panelToToggle != null) panelToToggle.SetActive(false);
                            if (imageToToggle != null) imageToToggle.gameObject.SetActive(false);
                        });
                    }
                }
            }

            if (app.sceneLoadButton != null && !string.IsNullOrEmpty(app.sceneNameToLoad))
            {
                string sceneName = app.sceneNameToLoad;
                app.sceneLoadButton.onClick.AddListener(() =>
                {
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

    /// <summary>
    /// シーケンス初期化用の設定を行う。ログイン画面を有効化し、デスクトップと通知を非表示にする。
    /// 外部からシーケンス制御時に呼び出す想定。
    /// </summary>
    public void InitializeForSequence()
    {
        if (loginScreenCanvasGroup != null) loginScreenCanvasGroup.gameObject.SetActive(true);
        if (desktopScreen != null) desktopScreen.SetActive(false);
        if (notificationBadge != null) notificationBadge.SetActive(false);
    }

    /// <summary>
    /// シーケンスの制御を引き継ぎ、はじめるボタンを操作可能にする。
    /// 外部シーケンスからの呼び出しを想定。
    /// </summary>
    public void TakeOverControl()
    {
        if (startButton != null) startButton.interactable = true;
    }

    /// <summary>
    /// ログインからデスクトップへの遷移を開始する。多重呼び出し防止のためフェード中は無視する。
    /// ボタンのクリックハンドラから呼び出される。
    /// </summary>
    public void StartLoginSequence()
    {
        if (isFading) return;
        StartCoroutine(LoginToDesktopRoutine());
    }

    /// <summary>
    /// アプリケーションを終了する。Editor実行時はPlayモードを停止し、ビルド版ではApplication.Quitを呼ぶ。
    /// </summary>
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

    /// <summary>
    /// 最初の（通知）アプリアイコンがクリックされた時の処理。関連するパネルとイメージを表示し、
    /// 通知バッジが表示中であればそれを消す。
    /// </summary>
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

    /// <summary>
    /// ログイン画面からデスクトップへ遷移するコルーチン。フェード処理を待機して画面切り替えとチュートリアル開始を行う。
    /// StartupSequenceManagerのFade処理に依存するため、nullチェックを行う。
    /// </summary>
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

        yield return new WaitForSeconds(1f); // 少し待機

        // PlayerPrefsのチェックを削除し、常にチュートリアルを実行するように変更
        if (ChatController.Instance != null && tutorialChatInk != null)
        {
            ChatController.Instance.StartConversation(tutorialChatInk);
            // PlayerPrefs.SetInt("HasSeenTutorial", 1); // チュートリアルを見たことを記録する処理を削除
        }
        // (ShowNotificationRoutineは HandleTutorialFinished から呼び出されます)
    }

    /// <summary>
    /// チュートリアル再生終了イベントのハンドラ。通知表示コルーチンを開始する。
    /// ChatControllerのイベントから呼び出される。
    /// </summary>
    private void HandleTutorialFinished()
    {
        StartCoroutine(ShowNotificationRoutine());
    }

    /// <summary>
    /// 通知表示を遅延させた後、バッジ表示と効果音再生、通知アプリアイコンを有効化するコルーチン。
    /// audioSourceやnotificationSoundがnullの場合は音を再生しない。
    /// </summary>
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

    /// <summary>
    /// シーン遷移をフェード付きで行うコルーチン。既にフェード中なら中断する。
    /// StartupSequenceManagerのFadeに依存するためnullチェックを行う。
    /// </summary>
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

    /// <summary>
    /// ボタンに対してポインターのEnter/Exitイベントを登録する。EventTriggerが無ければ追加する。
    /// ホバー時にフレームを表示し、離脱時に非表示にする。
    /// </summary>
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

    /// <summary>
    /// 指定したアイコン上にホバーフレームを配置して表示する。RectTransformを親に合わせてストレッチし、描画順序を調整する。
    /// iconHoverFrameがnullの場合は何もしない。
    /// </summary>
    private void ShowHoverFrame(Transform iconTransform)
    {
        if (iconHoverFrame == null) return;

        // 1. 親をホバー対象のアイコン自身に変更する
        iconHoverFrame.transform.SetParent(iconTransform, false);

        // 2. RectTransformを親（アイコン）に合わせてストレッチさせる
        RectTransform frameRect = iconHoverFrame.rectTransform;
        frameRect.anchorMin = Vector2.zero;     // (0, 0)
        frameRect.anchorMax = Vector2.one;      // (1, 1)
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = Vector2.zero;     // サイズ差分なし
        frameRect.anchoredPosition = Vector2.zero; // 位置も中央

        // 3. 描画順序を一番後ろ（アイコンの背景）にする
        iconHoverFrame.transform.SetAsFirstSibling();

        // 4. 表示する
        iconHoverFrame.gameObject.SetActive(true);
    }

    /// <summary>
    /// ホバーフレームを非表示にして親を元に戻す。iconHoverFrameがnullの場合は何もしない。
    /// </summary>
    private void HideHoverFrame()
    {
        if (iconHoverFrame == null) return;

        // 1. 非表示にする
        iconHoverFrame.gameObject.SetActive(false);

        // 2. 親を元の場所（DesktopManagerなど）に戻す
        if (iconHoverFrameOriginalParent != null)
        {
            iconHoverFrame.transform.SetParent(iconHoverFrameOriginalParent, false);
        }
    }

    /// <summary>
    /// ChatControllerから渡されたタグリストを解析して処理を実行する。
    /// フォーマットは "key:value" を想定。対応外のキーは無視する。
    /// </summary>
    private void HandleTags(List<string> tags)
    {
        foreach (string tag in tags)
        {
            string[] parts = tag.Split(':');
            if (parts.Length == 0) continue;

            string key = parts[0].Trim();
            string value = parts.Length > 1 ? parts[1].Trim() : "";

            switch (key)
            {
                case "highlight":
                    if (currentEffectCoroutine != null) StopCoroutine(currentEffectCoroutine);
                    currentEffectCoroutine = StartCoroutine(ProcessHighlightTag(value));
                    break;

                case "highlight_off":
                    if (currentEffectCoroutine != null) StopCoroutine(currentEffectCoroutine);
                    DeactivateAllHighlights();
                    break;
            }
        }
    }

    /// <summary>
    /// ハイライトタグの処理コルーチン。カンマ区切りで複数ターゲットに対応し、必要なものだけ有効化する。
    /// 終了時にcurrentEffectCoroutineをクリアする。
    /// </summary>
    private IEnumerator ProcessHighlightTag(string highlightData)
    {
        // まず全て消す
        DeactivateAllHighlights();

        // カンマ区切りで複数のハイライトに対応（例: #highlight: Right, Left）
        string[] targetsToShow = highlightData.Split(',');
        foreach (string targetName in targetsToShow)
        {
            ActivateHighlight(targetName.Trim());
        }

        // 即座に処理を終える（待機が必要な場合はここで調整）
        yield return null;
        currentEffectCoroutine = null;
    }

    /// <summary>
    /// 名前に一致するハイライトターゲットを検索して有効化する。見つからない場合は警告を出力する。
    /// highlightTargetsがnullの場合は何もしない。
    /// </summary>
    private void ActivateHighlight(string name)
    {
        // リストから名前に一致するものを探して表示
        HighlightTarget target = highlightTargets.Find(ht => ht.name == name);
        if (target != null && target.panel != null)
        {
            target.panel.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"DesktopManager: ハイライトターゲット '{name}' が見つかりません。設定を確認してください。");
        }
    }

    /// <summary>
    /// 登録されている全てのハイライトパネルを非表示にする。highlightTargetsがnullの場合は何もしない。
    /// </summary>
    private void DeactivateAllHighlights()
    {
        if (highlightTargets == null) return;
        foreach (var target in highlightTargets)
        {
            if (target.panel != null)
                target.panel.SetActive(false);
        }
    }

    #endregion
}