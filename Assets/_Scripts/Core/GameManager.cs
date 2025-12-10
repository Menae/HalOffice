using System;
using System.Collections.Generic;
using UnityEngine;

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
/// Inspector経由で設定される永続データやゲーム進行状態を管理。
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// グローバルにアクセス可能な GameManager の唯一のインスタンス。
    /// 他のオブジェクトの Awake より後にアクセスする場合は存在チェックが必要。
    /// </summary>
    public static GameManager Instance { get; private set; }

    // --- Inspectorに表示される変数の宣言 ---

    [Header("評価システム (Evaluation System)")]
    [Tooltip("正しく配置されたアイテムの数")]
    /// <summary>
    /// 正しく配置されたアイテムの数。評価計算に利用する。
    /// Inspectorで初期値を設定可能。
    /// </summary>
    public int correctPlacementCount = 0;

    [Tooltip("現在の日付 (Day 1, Day 2...)")]
    /// <summary>
    /// 現在のゲーム内日数。AdvanceDay でインクリメントする。
    /// UI反映は AdvanceDay 内で行う。直接変更すると UI と整合しない可能性あり。
    /// </summary>
    public int currentDay = 1;

    [Tooltip("リザルトシーンを再生すべきかどうかのフラグ")]
    /// <summary>
    /// リザルトシーンを表示するかどうかのフラグ。シーン遷移ロジックで参照。
    /// </summary>
    public bool shouldShowResults = false;

    [Header("永続データ (Persistent Data)")]
    [Tooltip("ゲームに登場する全ての証拠リスト")]
    /// <summary>
    /// ゲーム中に存在する全ての証拠オブジェクト。ScriptableObject のリストとして Inspector で設定。
    /// ResetAllClues で状態を初期化するため、null チェックが行われる。
    /// </summary>
    public List<Clue> allCluesInGame;

    /// <summary>
    /// プレイヤーの評判スコア。解析や結果画面で使用。
    /// </summary>
    public int reputationScore = 0;

    /// <summary>
    /// 調査が直前に終了したかどうかのフラグ。結果画面やロジック分岐で参照。
    /// </summary>
    public bool justFinishedInvestigation = false;

    /// <summary>
    /// レポート用に収集された証拠の一覧。調査フェーズ中に追加される。
    /// </summary>
    public List<Clue> collectedCluesForReport;

    [Header("会話ログ")]
    /// <summary>
    /// 会話履歴ログ。会話システムや保存処理で利用。
    /// </summary>
    public List<DialogueLineData> conversationLog = new List<DialogueLineData>();

    // --- Inspectorに表示されないプロパティ ---

    // 他のスクリプトが安全にアクセスするためのパブリック「プロパティ」
    // (ヘッダーを付けられないため、Inspectorには表示されない)
    /// <summary>
    /// プレイヤー入力の有効/無効。外部から読み取り可能、設定は SetInputEnabled を通すこと。
    /// 直接書き換えると DragDropManager などとの整合性が取れない可能性あり。
    /// </summary>
    public bool isInputEnabled { get; private set; } = true;


    // --- メソッドの定義 ---

    /// <summary>
    /// Unity の初期化ライフサイクルで最初に呼ばれることがある。シングルトン初期化を行う。
    /// Awake はオブジェクト生成直後に呼ばれるため、他のオブジェクトの Awake 順序に依存する処理は注意。
    /// </summary>
    private void Awake()
    {
        // シングルトンが未設定なら初期化し、破棄されないようにする。
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ゲーム開始時に全証拠の状態を初期化。allCluesInGame が null の場合は何もしない。
            ResetAllClues();
        }
        else
        {
            // 既にインスタンスが存在する場合は重複を避けるため破棄。
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ゲーム開始時やリセット時に、全ての証拠の発見状態を未発見に戻す。
    /// allCluesInGame が null の場合は早期リターンする。個々の Clue が null の場合も安全にスキップ。
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
    /// プレイヤー入力の有効/無効を切り替える。
    /// DragDropManager や他の入力管理コンポーネントに状態を伝搬させ、一貫した入力制御を行う。
    /// null チェックを行い、対象マネージャが存在しない場合は安全に終了する。
    /// </summary>
    /// <param name="enabled">入力を有効にする場合は true、無効にする場合は false。</param>
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;

        // ドラッグ＆ドロップの制御を伝搬。DragDropManager が存在する場合のみ呼び出す。
        if (DragDropManager.Instance != null)
        {
            // DragDropManager が CursorController など他のコンポーネントへも反映する設計。
            DragDropManager.Instance.SetInteractionEnabled(enabled);
        }
    }

    /// <summary>
    /// ゲーム内の日数を 1 日進める。UI 更新は GlobalUIManager が存在する場合のみ行う。
    /// 日付の増加は副作用として UI 更新とログ出力を行う。
    /// </summary>
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
    /// リザルト画面が閉じられたタイミングで購読者に通知するイベント。
    /// リスナーはアンロード完了後の後処理を行うことを想定。
    /// </summary>
    public event Action OnResultSceneClosed;

    /// <summary>
    /// 指定したリザルトシーンを閉じる処理を開始する。
    /// ボタン押下など UI 側から呼ばれることを想定。非同期アンロードを内部で開始する。
    /// </summary>
    /// <param name="sceneName">閉じたいリザルトシーンの名前。</param>
    public void CloseResultScene(string sceneName)
    {
        StartCoroutine(UnloadResultRoutine(sceneName));
    }

    /// <summary>
    /// 非同期で指定シーンをアンロードし、アンロード完了時に OnResultSceneClosed を呼ぶ。
    /// シーン名が不正でも SceneManager 側で安全に処理されるが、呼び出し側で存在チェックを推奨。
    /// </summary>
    /// <param name="sceneName">アンロード対象のシーン名。</param>
    private System.Collections.IEnumerator UnloadResultRoutine(string sceneName)
    {
        // 指定されたシーンを非同期でアンロード（破棄）する。
        yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);

        // アンロードが完了したことを通知する（購読者がいなければ無視される）。
        Debug.Log("リザルトシーンが閉じられました。メインシーンへ制御を戻します。");
        OnResultSceneClosed?.Invoke();
    }
}