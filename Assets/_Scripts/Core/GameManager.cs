using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLineData
{
    public string text;
    public List<string> tags;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // --- Inspectorに表示される変数の宣言 ---

    [Header("評価システム (Evaluation System)")]
    [Tooltip("正しく配置されたアイテムの数")]
    public int correctPlacementCount = 0;

    [Tooltip("現在の日付 (Day 1, Day 2...)")]
    public int currentDay = 1;

    [Tooltip("リザルトシーンを再生すべきかどうかのフラグ")]
    public bool shouldShowResults = false;

    [Header("永続データ (Persistent Data)")]
    [Tooltip("ゲームに登場する全ての証拠リスト")]
    public List<Clue> allCluesInGame;
    public int reputationScore = 0;
    public bool justFinishedInvestigation = false;
    public List<Clue> collectedCluesForReport;

    [Header("会話ログ")]
    public List<DialogueLineData> conversationLog = new List<DialogueLineData>();

    // --- Inspectorに表示されないプロパティ ---

    // 他のスクリプトが安全にアクセスするためのパブリック「プロパティ」
    // (ヘッダーを付けられないため、Inspectorには表示されない)
    public bool isInputEnabled { get; private set; } = true;


    // --- メソッドの定義 ---

    private void Awake()
    {
        // 他のシーンから来たGameManagerがまだいないか？
        if (Instance == null)
        {
            // いなければ、自分が最初のインスタンスになる
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // GameManager独自の初期化処理
            ResetAllClues();
        }
        else
        {
            // 既に存在している場合は、このオブジェクトは不要なので破壊する
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ゲーム開始時などに、全ての証拠の状態を未発見に戻す
    /// </summary>
    public void ResetAllClues()
    {
        if (allCluesInGame == null) return;
        foreach (var clue in allCluesInGame)
        {
            if (clue != null) clue.ResetStatus();
        }
        Debug.Log("全ての証拠の状態をリセットしました。");
    }

    /// <summary>
    /// プレイヤーの入力を有効/無効にする
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;

        // ドラッグ＆ドロップの制御
        if (DragDropManager.Instance != null)
        {
            // ここで DragDropManager に伝えれば、
            // DragDropManager が自動的に CursorController にも伝えてくれます
            DragDropManager.Instance.SetInteractionEnabled(enabled);
        }
    }

    public void AdvanceDay()
    {
        currentDay++;
        Debug.Log($"現在は Day {currentDay} です。");

        // UI更新（存在すれば）
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.RefreshDayDisplay();
        }
    }

    /// <summary>
    /// リザルト画面が閉じられた（アンロードされた）タイミングで発火するイベント
    /// </summary>
    public event Action OnResultSceneClosed;

    /// <summary>
    /// リザルトシーン側から呼び出す。「閉じるボタン」を押した時に実行する。
    /// </summary>
    /// <param name="sceneName">閉じたいリザルトシーンの名前</param>
    public void CloseResultScene(string sceneName)
    {
        StartCoroutine(UnloadResultRoutine(sceneName));
    }

    private System.Collections.IEnumerator UnloadResultRoutine(string sceneName)
    {
        // 指定されたシーンを非同期でアンロード（破棄）する
        yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);

        // アンロードが完了したことを通知する
        Debug.Log("リザルトシーンが閉じられました。メインシーンへ制御を戻します。");
        OnResultSceneClosed?.Invoke();
    }

}