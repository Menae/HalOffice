using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GlobalUIManager : MonoBehaviour
{
    /// <summary>
    /// シングルトンインスタンスへの参照。遅延生成を行い、シーンに存在しない場合はResourcesからプレハブをロードして生成する。
    /// 副作用: 必要に応じてインスタンスをInstantiateする。非同期処理ではないため呼び出しタイミングに注意。
    /// </summary>
    private static GlobalUIManager _instance;

    /// <summary>
    /// グローバルにアクセス可能なインスタンス。初回アクセス時にシーン内検索→Resourcesロードの順で解決する。
    /// 呼び出しタイミング: Awakeより前にアクセスされるとプレハブ生成が行われる可能性あり。
    /// </summary>
    public static GlobalUIManager Instance
    {
        get
        {
            // インスタンスがまだ存在しない場合
            if (_instance == null)
            {
                // まずシーン内から探す
                _instance = FindObjectOfType<GlobalUIManager>();

                // それでも見つからなければ、Resourcesフォルダからプレハブをロードして生成
                if (_instance == null)
                {
                    // プレハブのパス
                    var prefab = Resources.Load<GameObject>("Prefabs/GlobalUIManager");
                    var go = Instantiate(prefab);
                    _instance = go.GetComponent<GlobalUIManager>();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// タスクバーのアイコン関連の登録エントリ。
    /// Inspectorで各フィールドにウィンドウとアイコンを割り当てる想定。
    /// </summary>
    [System.Serializable]
    public class TaskbarIconEntry
    {
        /// <summary>
        /// 管理対象のウィンドウオブジェクト（例：ChatPanel）。nullチェックを行うため必ずセットすること。
        /// </summary>
        [Tooltip("管理対象のウィンドウオブジェクト（例：ChatPanel）")]
        public GameObject appWindow;

        /// <summary>
        /// デフォルト状態のアイコン（アイコンA）。ウィンドウ非アクティブ時に表示する。
        /// </summary>
        [Tooltip("デフォルト状態のアイコン（アイコンA）")]
        public GameObject iconDefault;

        /// <summary>
        /// ウィンドウがアクティブな時に表示するアイコン（アイコンB）。
        /// </summary>
        [Tooltip("ウィンドウがアクティブな時に表示するアイコン（アイコンB）")]
        public GameObject iconActive;
    }

    [Header("コンポーネント参照")]
    /// <summary>
    /// 同じオブジェクトにアタッチされているChatController。InspectorでD&D。
    /// 副作用: AwakeでGetComponentして既にセットされていない場合に上書きされる可能性あり。
    /// </summary>
    [Tooltip("同じオブジェクトにアタッチされているChatController")]
    public ChatController chatController;

    [Header("UI要素")]
    /// <summary>
    /// シーン間で永続させたいタスクバーの親オブジェクト。表示/非表示切替に使用。
    /// </summary>
    [Tooltip("シーン間で永続させたいタスクバーの親オブジェクト")]
    public GameObject taskbarObject;

    /// <summary>
    /// デスクトップアイコンの親オブジェクト。表示/非表示切替に使用。
    /// </summary>
    [Tooltip("デスクトップアイコンの親オブジェクト")]
    public GameObject desktopIconsObject;

    [Header("時計機能 (Clock System)")]
    /// <summary>
    /// 時間を表示するTextMeshPro (HH:mm形式)。nullチェックあり。
    /// </summary>
    [Tooltip("時間を表示するTextMeshPro (HH:mm形式)")]
    public TextMeshProUGUI clockText;

    [Header("時計の動作設定")]
    /// <summary>
    /// 時計が動き出すシーンの名前（例：Day1Scene）。該当シーンでResetAndStartClockが呼ばれる。
    /// </summary>
    [Tooltip("時計が動き出すシーンの名前（例：Day1Scene）")]
    public string mainSceneName = "Day1Scene";

    /// <summary>
    /// スタート画面で固定表示する時間（例：09:00）。主にStart画面やメニューでの表示用。
    /// </summary>
    [Tooltip("スタート画面で固定表示する時間（例：09:00）")]
    public string fixedStartTimeString = "09:00";

    [Header("ゲーム内時間の進行設定")]
    /// <summary>
    /// ゲーム内での開始時間（時）。Inspectorで設定。
    /// </summary>
    [Tooltip("ゲーム内での開始時間（時）")]
    [Range(0, 23)]
    public int gameStartTimeHour = 9;

    /// <summary>
    /// ゲーム内での開始時間（分）。Inspectorで設定。
    /// </summary>
    [Tooltip("ゲーム内での開始時間（分）")]
    [Range(0, 59)]
    public int gameStartTimeMinute = 0;

    /// <summary>
    /// ゲーム内での終了時間（時）。Inspectorで設定。
    /// </summary>
    [Tooltip("ゲーム内での終了時間（時）")]
    [Range(0, 23)]
    public int gameEndTimeHour = 17;

    /// <summary>
    /// ゲーム内での終了時間（分）。Inspectorで設定。
    /// </summary>
    [Tooltip("ゲーム内での終了時間（分）")]
    [Range(0, 59)]
    public int gameEndTimeMinute = 0;

    /// <summary>
    /// 開始から終了までにかかる現実の時間（秒）。0以下の場合は時間が進まない。
    /// </summary>
    [Tooltip("開始から終了までにかかる現実の時間（秒）")]
    public float totalRealTimeDurationInSeconds = 300f;

    [Header("日付表示設定")]
    /// <summary>
    /// 現在の日数を表示するTextMeshPro（例：Day 1）。nullチェックあり。
    /// </summary>
    [Tooltip("現在の日数を表示するTextMeshPro（例：Day 1）")]
    public TextMeshProUGUI dayText;

    [Header("タスクバーアイコン設定")]
    /// <summary>
    /// 管理したいウィンドウと選択フレームのペアを登録するリスト。各要素のnullチェックをUpdateTaskbarIconsで行う。
    /// </summary>
    [Tooltip("管理したいウィンドウと選択フレームのペアをここに登録する")]
    public List<TaskbarIconEntry> taskbarIconEntries;

    [Header("表示設定")]
    /// <summary>
    /// タスクバーを非表示にしたいシーンの名前をリストに追加。OnSceneLoadedで参照。
    /// </summary>
    [Tooltip("タスクバーを非表示にしたいシーンの名前をリストに追加")]
    public List<string> scenesToHideTaskbar;

    [Header("チャットアプリのレイアウト設定")]
    /// <summary>
    /// デフォルトのレイアウトの親オブジェクト。InspectorでD&D。
    /// </summary>
    [Tooltip("デフォルトのレイアウトの親オブジェクト")]
    public GameObject layoutDefault;

    /// <summary>
    /// 特別シーン用のレイアウトの親オブジェクト。InspectorでD&D。
    /// </summary>
    [Tooltip("特別シーン用のレイアウトの親オブジェクト")]
    public GameObject layoutSpecial;

    /// <summary>
    /// 特別レイアウトを適用したいシーンの名前リスト。OnSceneLoadedで参照。
    /// </summary>
    [Tooltip("特別レイアウトを適用したいシーンの名前リスト")]
    public List<string> specialLayoutScenes;

    [Header("デモ版用設定")]
    /// <summary>
    /// デモ版の最終日に表示するImage（例：デモ終了画面）。nullチェックあり。
    /// </summary>
    [Tooltip("デモ版の最終日に表示するImage（例：デモ終了画面）")]
    public Image demoEndImage;

    /// <summary>
    /// デモ終了を表示する日数（例：2日目で終了）。GameManagerのcurrentDayと比較して表示制御。
    /// </summary>
    [Tooltip("デモ終了を表示する日数（例：2日目で終了）")]
    public int demoEndDay = 2;

    /// <summary>
    /// デモ終了画面を出すシーン名（例：LoginScene）。OnSceneLoadedで参照。
    /// </summary>
    [Tooltip("デモ終了画面を出すシーン名（例：LoginScene）")]
    public string demoEndSceneName = "LoginScene";

    // 内部処理用の変数
    private bool isClockRunning = false;
    private float currentGameTimeInMinutes;
    private float gameEndTimeInMinutes;
    private float inGameMinutesPerRealSecond;

    /// <summary>
    /// Unityイベント: GameObjectが有効化されたときに呼ばれる。シーンロードイベントの購読を開始する。
    /// 呼び出し順序: OnEnableはAwake/Startとは独立して呼ばれる可能性あり。
    /// </summary>
    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }

    /// <summary>
    /// Unityイベント: GameObjectが無効化されたときに呼ばれる。シーンロードイベントの購読を解除する。
    /// </summary>
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    /// <summary>
    /// Unityイベント: Awakeでシングルトンの確立と永続化、コンポーネント参照の初期化を行う。
    /// 呼び出しタイミング: インスタンスの初期化に使用。Startより前に実行される。
    /// 副作用: 同一シングルトンが存在する場合は自身をDestroyする。
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        chatController = GetComponent<ChatController>();

        InitializeClock();
    }

    /// <summary>
    /// 新しいシーンがロードされるたびに自動的に呼ばれる。SceneManager.sceneLoadedからトリガーされる。
    /// 役割:
    /// - タスクバーの表示/非表示切替
    /// - 時計の稼働/停止切替
    /// - デモ終了画面の表示判定
    /// </summary>
    /// <param name="scene">ロードされたシーン情報。</param>
    /// <param name="mode">ロードモード。</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // --- 1. タスクバーの表示/非表示を決定 ---
        if (scenesToHideTaskbar.Contains(scene.name))
        {
            // 両方（タスクバーとアイコン）を非表示にする
            SetDesktopUIVisibility(false);
        }
        else
        {
            // 両方（タスクバーとアイコン）を表示する
            SetDesktopUIVisibility(true);
        }

        // --- 2. 時計の稼働/停止を決定 ---
        if (scene.name == mainSceneName)
        {
            ResetAndStartClock();
        }
        else
        {
            StopClockAndShowFixedTime();
        }

        // --- 3. デモ終了処理 ---
        if (demoEndImage != null &&
            scene.name == demoEndSceneName &&
            GameManager.Instance != null &&
            GameManager.Instance.currentDay >= demoEndDay)
        {
            demoEndImage.gameObject.SetActive(true);
        }
        else if (demoEndImage != null)
        {
            demoEndImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Unityイベント: 毎フレーム呼ばれる。タスクバーアイコン・日付表示・ゲーム内時計の更新を順に行う。
    /// パフォーマンス: 重い処理を入れないこと。各更新はnullチェックを行う。
    /// </summary>
    private void Update()
    {
        UpdateTaskbarIcons();
        UpdateDayDisplay();

        UpdateGameClock();
    }

    /// <summary>
    /// 時計システムの内部変数を計算・初期化する。
    /// 実施内容:
    /// - 開始/終了時間を分単位に変換
    /// - ゲーム内分進行速度を算出（現実秒あたりのゲーム分）
    /// - 初期表示を固定時間で更新
    /// </summary>
    private void InitializeClock()
    {
        // 1. ゲーム内時間を「分」に変換
        float startTimeInMinutes = (gameStartTimeHour * 60) + gameStartTimeMinute;
        gameEndTimeInMinutes = (gameEndTimeHour * 60) + gameEndTimeMinute;

        // 2. 現在の時間を開始時間にセット (ただし最初は非稼働)
        currentGameTimeInMinutes = startTimeInMinutes;

        // 3. 進行速度を計算
        float totalGameTimeMinutes = gameEndTimeInMinutes - startTimeInMinutes;

        // ゼロ除算を避ける
        if (totalRealTimeDurationInSeconds > 0 && totalGameTimeMinutes > 0)
        {
            // (ゲーム内時間) / (現実の時間) = 1秒あたりに進むゲーム時間(分)
            inGameMinutesPerRealSecond = totalGameTimeMinutes / totalRealTimeDurationInSeconds;
        }
        else
        {
            inGameMinutesPerRealSecond = 0; // 時間が進まない
        }

        // 4. 初期表示
        isClockRunning = false; // 最初は止まっている
        UpdateClockDisplay(fixedStartTimeString); // 固定時間で表示
    }

    /// <summary>
    /// GameManagerのcurrentDayをUIに反映する。
    /// 副作用: GameManagerがnullの場合は何もしない。
    /// </summary>
    private void UpdateDayDisplay()
    {
        if (dayText == null) return;
        if (GameManager.Instance == null) return;

        int day = GameManager.Instance.currentDay;
        dayText.text = $"{day}";
    }

    /// <summary>
    /// GameManagerなど他クラスから手動で更新を呼び出したい場合のエントリ。
    /// 副作用: UpdateDayDisplayを呼ぶのみ。
    /// </summary>
    public void RefreshDayDisplay()
    {
        UpdateDayDisplay();
    }

    /// <summary>
    /// 全ての登録済みウィンドウの状態をチェックし、タスクバーアイコンの表示を更新する。
    /// 実行タイミング: 毎フレームのUpdateから呼ばれる。エントリのnullチェックを行い不完全な登録はスキップする。
    /// </summary>
    private void UpdateTaskbarIcons()
    {
        if (taskbarIconEntries == null) return;

        foreach (var entry in taskbarIconEntries)
        {
            // 登録が不完全な場合はスキップ
            if (entry.appWindow == null || entry.iconDefault == null || entry.iconActive == null)
            {
                continue;
            }

            // ウィンドウがアクティブかどうかを判定
            bool isWindowActive = entry.appWindow.activeSelf;

            // ウィンドウがアクティブなら
            if (isWindowActive)
            {
                // アイコンA（デフォルト）を非表示
                entry.iconDefault.SetActive(false);
                // アイコンB（アクティブ）を表示
                entry.iconActive.SetActive(true);
            }
            // ウィンドウが非アクティブなら（閉じているなら）
            else
            {
                // アイコンA（デフォルト）を表示
                entry.iconDefault.SetActive(true);
                // アイコンB（アクティブ）を非表示
                entry.iconActive.SetActive(false);
            }
        }
    }

    /// <summary>
    /// メインシーン開始時に時計をリセットし進行を開始する。
    /// 副作用: currentGameTimeInMinutesとisClockRunningを更新し、表示を更新する。
    /// </summary>
    public void ResetAndStartClock()
    {
        // 開始時間に戻す
        currentGameTimeInMinutes = (gameStartTimeHour * 60) + gameStartTimeMinute;
        isClockRunning = true;
        UpdateClockDisplay(); // 現在のゲーム時間で表示を更新
    }

    /// <summary>
    /// スタート画面などで時計を停止し、固定の文字列を表示する。
    /// 副作用: isClockRunningをfalseに設定し、固定テキストを表示する。
    /// </summary>
    public void StopClockAndShowFixedTime()
    {
        isClockRunning = false;
        UpdateClockDisplay(fixedStartTimeString);
    }

    /// <summary>
    /// ゲーム内時計の時間を進め、UIを更新する。Updateから毎フレーム呼ばれる。
    /// 注意: currentGameTimeInMinutesが終了時間を超えた場合に停止する。終了イベントは発行しない（コメント参照）。
    /// </summary>
    private void UpdateGameClock()
    {
        // 時計が動いていない、または既に終了時間を超えていたら何もしない
        if (!isClockRunning || currentGameTimeInMinutes >= gameEndTimeInMinutes)
        {
            if (isClockRunning && currentGameTimeInMinutes >= gameEndTimeInMinutes)
            {
                // 終了時間に達した瞬間に一度だけ停止処理
                isClockRunning = false;
                // ここで時間切れイベントを発行することも可能
                // Debug.Log("時計が終了時間に達しました。");
            }
            return;
        }

        // 1. 現実の時間(Time.deltaTime)を使ってゲーム内時間を進める
        currentGameTimeInMinutes += inGameMinutesPerRealSecond * Time.deltaTime;

        // 2. 終了時間を超えないように制限
        if (currentGameTimeInMinutes > gameEndTimeInMinutes)
        {
            currentGameTimeInMinutes = gameEndTimeInMinutes;
        }

        // 3. UI表示を更新
        UpdateClockDisplay();
    }

    /// <summary>
    /// 実行中のゲーム内時間に基づいて時計のUIを更新する。
    /// フォーマット: "HH:mm"。clockTextがnullの場合は何もしない。
    /// </summary>
    private void UpdateClockDisplay()
    {
        if (clockText == null) return;

        // floatの「分」を整数の「時」と「分」に変換
        int totalMinutes = Mathf.FloorToInt(currentGameTimeInMinutes);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        // "HH:mm" 形式 (09:05 のように0埋め) で表示
        clockText.text = string.Format("{0:00}:{1:00}", hours, minutes);
    }

    /// <summary>
    /// 時計のUIに指定した固定文字列を表示する。
    /// 副作用: clockTextがnullでない場合にテキストを書き換える。
    /// </summary>
    /// <param name="timeString">表示する文字列 (例: "09:00")</param>
    private void UpdateClockDisplay(string timeString)
    {
        if (clockText == null) return;
        clockText.text = timeString;
    }

    /// <summary>
    /// デスクトップUI（タスクバーとデスクトップアイコン）の表示/非表示を切り替える。
    /// 副作用: 指定オブジェクトがnullでない場合にSetActiveを呼ぶ。
    /// </summary>
    /// <param name="isVisible">表示する場合はtrue、非表示はfalse。</param>
    public void SetDesktopUIVisibility(bool isVisible)
    {
        if (taskbarObject != null) { taskbarObject.SetActive(isVisible); }
        if (desktopIconsObject != null) { desktopIconsObject.SetActive(isVisible); }
    }
}