using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance { get; private set; }

    [System.Serializable]
    public class TaskbarIconEntry
    {
        [Tooltip("管理対象のウィンドウオブジェクト（例：ChatPanel）")]
        public GameObject appWindow;
        [Tooltip("上記ウィンドウがアクティブな時に表示する選択フレーム")]
        public GameObject selectionFrame;
    }

    [Header("UI要素")]
    [Tooltip("シーン間で永続させたいタスクバーの親オブジェクト")]
    public GameObject taskbarObject;
    [Tooltip("デスクトップアイコンの親オブジェクト")]
    public GameObject desktopIconsObject;
    public GameObject chatAppPanel;

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

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
    }

    private void Update()
    {
        UpdateTaskbarIcons();
    }

    /// <summary>
    /// 全ての登録済みウィンドウの状態をチェックし、選択フレームの表示を更新する
    /// </summary>
    private void UpdateTaskbarIcons()
    {
        if (taskbarIconEntries == null) return;

        foreach (var entry in taskbarIconEntries)
        {
            if (entry.appWindow != null && entry.selectionFrame != null)
            {
                // ウィンドウがアクティブかどうかを判定し、
                // それに合わせて選択フレームの表示/非表示を切り替える
                bool isWindowActive = entry.appWindow.activeSelf;
                entry.selectionFrame.SetActive(isWindowActive);
            }
        }
    }

    public void SetDesktopUIVisibility(bool isVisible)
    {
        if (taskbarObject != null) { taskbarObject.SetActive(isVisible); }
        if (desktopIconsObject != null) { desktopIconsObject.SetActive(isVisible); }
    }
}