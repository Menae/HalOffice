using UnityEngine;
using System.Collections.Generic;
using System;

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
    }
}