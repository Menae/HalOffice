using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GlobalUIManager : MonoBehaviour
{
    private static GlobalUIManager _instance;
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

    [System.Serializable]
    public class TaskbarIconEntry
    {
        [Tooltip("管理対象のウィンドウオブジェクト（例：ChatPanel）")]
        public GameObject appWindow;

        [Tooltip("デフォルト状態のアイコン（アイコンA）")]
        public GameObject iconDefault;
        [Tooltip("ウィンドウがアクティブな時に表示するアイコン（アイコンB）")]
        public GameObject iconActive;
    }

    [Header("コンポーネント参照")]
    [Tooltip("同じオブジェクトにアタッチされているChatController")]
    public ChatController chatController;

    [Header("UI要素")]
    [Tooltip("シーン間で永続させたいタスクバーの親オブジェクト")]
    public GameObject taskbarObject;
    [Tooltip("デスクトップアイコンの親オブジェクト")]
    public GameObject desktopIconsObject;
    public GameObject chatAppPanel;

    [Header("日付表示設定")]
    [Tooltip("現在の日数を表示するTextMeshPro（例：Day 1）")]
    public TextMeshProUGUI dayText;

    [Header("タスクバーアイコン設定")]
    [Tooltip("管理したいウィンドウと選択フレームのペアをここに登録する")]
    public List<TaskbarIconEntry> taskbarIconEntries;


    [Header("表示設定")]
    [Tooltip("タスクバーを非表示にしたいシーンの名前をリストに追加")]
    public List<string> scenesToHideTaskbar;

    [Header("チャットアプリのレイアウト設定")]
    [Tooltip("デフォルトのレイアウトの親オブジェクト")]
    public GameObject layoutDefault; // Inspectorで「Chat-Layout1」をセット
    [Tooltip("特別シーン用のレイアウトの親オブジェクト")]
    public GameObject layoutSpecial; // Inspectorで「Chat-Layout2」をセット
    [Tooltip("特別レイアウトを適用したいシーンの名前リスト")]
    public List<string> specialLayoutScenes;

    [Header("デモ版用設定")]
    [Tooltip("デモ版の最終日に表示するImage（例：デモ終了画面）")]
    public Image demoEndImage;
    [Tooltip("デモ終了を表示する日数（例：2日目で終了）")]
    public int demoEndDay = 2;
    [Tooltip("デモ終了画面を出すシーン名（例：LoginScene）")]
    public string demoEndSceneName = "LoginScene";

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        // 自分と同じGameObjectにアタッチされているChatControllerを自動で取得
        chatController = GetComponent<ChatController>();
    }

    /// <summary>
    /// 新しいシーンがロードされるたびに自動的に呼ばれるメソッド
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // --- 1. タスクバーの表示/非表示を決定（この処理は残す）---
        if (scenesToHideTaskbar.Contains(scene.name))
        {
            taskbarObject.SetActive(false);
        }
        else
        {
            taskbarObject.SetActive(true);
        }

        if (scenesToHideTaskbar.Contains(scene.name))
        {
            taskbarObject.SetActive(false);
        }
        else
        {
            taskbarObject.SetActive(true);
        }

        // デモ終了処理
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

    private void Update()
    {
        UpdateTaskbarIcons();
        UpdateDayDisplay();
    }

    /// <summary>
    /// GameManagerのcurrentDayをUIに反映する
    /// </summary>
    private void UpdateDayDisplay()
    {
        if (dayText == null) return;
        if (GameManager.Instance == null) return;

        int day = GameManager.Instance.currentDay;
        dayText.text = $"{day}";
    }

    /// <summary>
    /// GameManagerなど他クラスから手動で更新を呼び出したい場合
    /// </summary>
    public void RefreshDayDisplay()
    {
        UpdateDayDisplay();
    }

    /// <summary>
    /// 全ての登録済みウィンドウの状態をチェックし、アイコンの表示を更新する
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

    public void SetDesktopUIVisibility(bool isVisible)
    {
        if (taskbarObject != null) { taskbarObject.SetActive(isVisible); }
        if (desktopIconsObject != null) { desktopIconsObject.SetActive(isVisible); }
    }
}