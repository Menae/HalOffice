using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class DialogueLineData
{
    /// <summary>
    /// 会話の本文テキスト。表示やログ保存に使用する。
    /// </summary>
    public string text;

    /// <summary>
    /// 会話に付与されたタグ一覧。検索やフィルタリングに使用する。
    /// </summary>
    public List<string> tags;
}

/// <summary>
/// ゲーム全体の状態を単一インスタンスで管理するシングルトン。
/// Awakeで自身をシングルトンとして初期化し、シーン間で破棄しない。
/// Inspector経由で設定される永続データやゲーム進行状態を管理する。
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// グローバルにアクセス可能な GameManager の唯一のインスタンス。
    /// 他のオブジェクトの Awake より後にアクセスする場合は存在チェックが必要である。
    /// </summary>
    public static GameManager Instance { get; private set; }

    // --- Inspectorに表示される変数の宣言 ---

    [Header("評価システム (Evaluation System)")]
    [Tooltip("正しく配置されたアイテムの数")]
    /// <summary>
    /// 正しく配置されたアイテムの数。評価計算に利用する。
    /// Inspectorで初期値を設定可能である。
    /// </summary>
    public int correctPlacementCount = 0;

    [Tooltip("現在の日付 (Day 1, Day 2...)")]
    /// <summary>
    /// 現在のゲーム内日数。AdvanceDay でインクリメントする。
    /// UI反映は AdvanceDay 内で行う。
    /// </summary>
    public int currentDay = 1;

    [Tooltip("リザルトシーンを再生すべきかどうかのフラグ")]
    /// <summary>
    /// リザルトシーンを表示するかどうかのフラグ。シーン遷移ロジックで参照する。
    /// </summary>
    public bool shouldShowResults = false;

    [Header("永続データ (Persistent Data)")]
    [Tooltip("ゲームに登場する全ての証拠リスト")]
    /// <summary>
    /// ゲーム中に存在する全ての証拠オブジェクト。
    /// ResetAllClues で状態を初期化するため、null チェックが行われる。
    /// </summary>
    public List<Clue> allCluesInGame;

    /// <summary>
    /// プレイヤーの評判スコア。解析や結果画面で使用する。
    /// </summary>
    public int reputationScore = 0;

    /// <summary>
    /// 調査が直前に終了したかどうかのフラグ。結果画面やロジック分岐で参照する。
    /// </summary>
    public bool justFinishedInvestigation = false;

    /// <summary>
    /// レポート作成用として、調査フェーズ中に収集された証拠のリスト。
    /// </summary>
    public List<Clue> collectedCluesForReport = new List<Clue>();

    [Header("会話ログ")]
    /// <summary>
    /// 会話履歴ログ。会話システムや保存処理で利用する。
    /// </summary>
    public List<DialogueLineData> conversationLog = new List<DialogueLineData>();

    [Header("ゲーム状態 (Game State)")]
    [Tooltip("業務（ゲーム本編）が開始されているかどうか")]
    /// <summary>
    /// 業務が開始されているかどうかのフラグ。
    /// falseの間は時間経過を停止し、操作を受け付けない。
    /// </summary>
    public bool isWorkStarted = false;

    // --- Inspectorに表示されないプロパティ ---

    /// <summary>
    /// プレイヤー入力の有効/無効状態。
    /// 外部からの読み取りは可能だが、変更は SetInputEnabled を介して行う。
    /// </summary>
    public bool isInputEnabled { get; private set; } = true;


    // --- メソッドの定義 ---

    /// <summary>
    /// Unity の初期化ライフサイクルで最初に呼ばれる。シングルトン初期化を行う。
    /// </summary>
    private void Awake()
    {
        // シングルトンが未設定なら初期化し、破棄されないようにする
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ゲーム開始時に全証拠の状態を初期化
            ResetAllClues();
        }
        else
        {
            // 既にインスタンスが存在する場合は重複を避けるため破棄する
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 毎フレーム呼び出される更新処理。
    /// 業務未開始時のグローバルな制御等が必要な場合に使用する。
    /// </summary>
    private void Update()
    {
        // 業務が開始されていない場合は処理を中断する
        if (!isWorkStarted) return;
    }

    /// <summary>
    /// ゲーム開始時やリセット時に、全ての証拠の発見状態を未発見に戻す。
    /// </summary>
    public void ResetAllClues()
    {
        if (allCluesInGame == null) return;

        foreach (var clue in allCluesInGame)
        {
            if (clue != null) clue.ResetStatus();
        }

        Debug.Log("全ての証拠の状態をリセットした。");
    }

    /// <summary>
    /// プレイヤー入力の有効/無効を切り替える。
    /// DragDropManager や他の入力管理コンポーネントに状態を伝搬させる。
    /// </summary>
    /// <param name="enabled">入力を有効にする場合は true、無効にする場合は false。</param>
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;

        // ドラッグ＆ドロップの制御を伝搬。DragDropManager が存在する場合のみ呼び出す。
        if (DragDropManager.Instance != null)
        {
            DragDropManager.Instance.SetInteractionEnabled(enabled);
        }
    }

    /// <summary>
    /// 業務開始ボタン等から呼び出されるメソッド。
    /// 業務開始フラグを立て、入力操作を許可する。
    /// </summary>
    public void StartWork()
    {
        isWorkStarted = true;

        // 業務開始に伴い、操作入力を許可する
        SetInputEnabled(true);

        Debug.Log("業務開始：時計と操作を有効化した。");
    }

    /// <summary>
    /// ゲーム内の日数を 1 日進める。UI 更新は GlobalUIManager が存在する場合のみ行う。
    /// </summary>
    public void AdvanceDay()
    {
        currentDay++;
        Debug.Log($"現在は Day {currentDay} である。");

        // UI更新（存在すれば）
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.RefreshDayDisplay();
        }
    }

    /// <summary>
    /// リザルト画面が閉じられたタイミングで購読者に通知するイベント。
    /// </summary>
    public event Action OnResultSceneClosed;

    /// <summary>
    /// 指定したリザルトシーンを閉じる処理を開始する。
    /// </summary>
    /// <param name="sceneName">閉じたいリザルトシーンの名前。</param>
    public void CloseResultScene(string sceneName)
    {
        StartCoroutine(UnloadResultRoutine(sceneName));
    }

    /// <summary>
    /// 非同期で指定シーンをアンロードし、アンロード完了時に OnResultSceneClosed を呼ぶ。
    /// </summary>
    private IEnumerator UnloadResultRoutine(string sceneName)
    {
        yield return SceneManager.UnloadSceneAsync(sceneName);

        Debug.Log("リザルトシーンが閉じられた。メインシーンへ制御を戻す。");
        OnResultSceneClosed?.Invoke();
    }
}