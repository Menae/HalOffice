using System;
using UnityEngine;

/// <summary>
/// シーン内の「検知ゲージ」を管理するマネージャー。
/// プレイヤーの行動やイベントによる検知増減を集約し、最大値到達時にゲーム終了処理を開始する。
/// </summary>
/// <remarks>
/// - Inspectorでの参照設定必須フィールドあり（nullチェックは一部のみ）。  
/// - Unityのライフサイクル関数（OnEnable/OnDisable/Update）で動作。OnEnableはオブジェクト有効化時に呼ばれるため、
///   他オブジェクトの初期化順序に依存する場合はInspectorでの参照設定を確認すること。  
/// - OnGameOverはゲージ満タンまたは外部強制停止で発火。多重発火防止のため内部フラグでガード済み。
/// </remarks>
public class DetectionManager : MonoBehaviour
{
    /// <summary>
    /// ゲーム終了を通知するイベント。検知が最大に達するか強制停止されたときに発火。
    /// 購読側はシーン遷移やUI無効化等の後処理を行う想定。
    /// </summary>
    public static event Action OnGameOver;

    [Header("イベントチャンネル")]
    /// <summary>
    /// 外部から検知値を送るためのイベントチャンネル（Inspectorでアサイン）。
    /// nullの場合は外部通知を受け取らない。
    /// </summary>
    public FloatEventChannelSO detectionChannel;

    [Header("参照")]
    /// <summary>
    /// 画面エフェクト制御（InspectorでD&D）。30%/70%トリガーなどで利用される想定。
    /// nullでも基本動作は継続するが、視覚効果は発生しない。
    /// </summary>
    public ScreenEffectsController screenEffects;

    /// <summary>
    /// シーンのタイマーを管理するDay1Manager（InspectorでD&D）。
    /// 非nullならばゲームがアクティブでない間は検知増減を無視する。
    /// </summary>
    [Tooltip("シーンのタイマーを管理するDay1Manager")]
    public Day1Manager day1Manager;

    /// <summary>
    /// 評価処理・シーン遷移を実行するトリガー（InspectorでD&D）。
    /// ゲーム終了時に必須。未設定の場合はエラーログ出力。
    /// </summary>
    public EvaluationTrigger evaluationTrigger;

    [Header("ゲージ設定")]
    /// <summary>
    /// 検知ゲージの最大値（Inspectorで設定）。ゲージがこの値以上でゲームオーバー処理を開始。
    /// </summary>
    public float maxDetection = 100f;

    /// <summary>
    /// 毎秒減衰する検知量（Inspectorで調整可能）。UpdateでTime.deltaTimeを乗じて減算。
    /// デフォルトは5.0f。
    /// </summary>
    [Tooltip("1秒あたりの検知減少量")]
    public float detectionDecayRate = 5f;

    [Header("シーン遷移設定")]
    /// <summary>
    /// ゲームオーバー時に遷移するシーン名（Inspectorで設定）。空文字や誤設定は遷移失敗の原因。
    /// </summary>
    public string gameOverSceneName = "GameOverScene";

    // 内部状態
    private float currentDetection = 0f;
    private bool isGameOver = false;

    /// <summary>
    /// Unity OnEnable。オブジェクトが有効化されたタイミングでイベント購読を開始。
    /// 注意: detectionChannelやDay1Managerが未設定だと一部購読はスキップされる。
    /// </summary>
    private void OnEnable()
    {
        if (detectionChannel != null)
        {
            detectionChannel.OnEventRaised += IncreaseDetection;
        }
        Day1Manager.OnTimeUp += HandleTimeUp;
    }

    /// <summary>
    /// Unity OnDisable。オブジェクトが無効化されるタイミングでイベント購読を解除。
    /// 購読解除忘れによるメモリリークや意図しないコールバックを防止する。
    /// </summary>
    private void OnDisable()
    {
        if (detectionChannel != null)
        {
            detectionChannel.OnEventRaised -= IncreaseDetection;
        }
        Day1Manager.OnTimeUp -= HandleTimeUp;
    }

    /// <summary>
    /// 検知値を増加させる。
    /// </summary>
    /// <param name="amount">増加させる量。負値・NaNは無視。</param>
    /// <remarks>
    /// - isGameOverフラグがtrue、またはDay1Managerが存在してゲームがアクティブでない場合は無視。  
    /// - 外部からのイベント（FloatEventChannelSO）で呼ばれる想定。即時にcurrentDetectionへ加算。
    /// </remarks>
    void IncreaseDetection(float amount)
    {
        if (isGameOver) return;
        if (day1Manager != null && !day1Manager.isGameActive) return;
        if (float.IsNaN(amount) || amount <= 0f) return;

        currentDetection += amount;
        currentDetection = Mathf.Clamp(currentDetection, 0f, maxDetection);
    }

    /// <summary>
    /// タイムアップまたはゲージ満タン時に呼ばれる終了処理の入口。
    /// 多重呼び出し防止のため内部フラグでガード済み。
    /// </summary>
    private void HandleTimeUp()
    {
        if (isGameOver) return;
        isGameOver = true;
        OnGameOver?.Invoke();

        if (evaluationTrigger != null)
        {
            evaluationTrigger.EndDayAndEvaluate();
        }
        else
        {
            Debug.LogError("EvaluationTriggerがDetectionManagerに設定されていません！");
        }
    }

    /// <summary>
    /// Unity Update。毎フレームの検知減衰と満タン判定を実行。
    /// 呼び出しタイミング: 毎フレーム。isGameOverまたはゲーム非アクティブ時は処理を行わない。
    /// </summary>
    private void Update()
    {
        if (isGameOver) return;
        if (day1Manager != null && !day1Manager.isGameActive) return;

        if (currentDetection > 0f)
        {
            currentDetection -= detectionDecayRate * Time.deltaTime;
        }
        currentDetection = Mathf.Clamp(currentDetection, 0f, maxDetection);

        if (currentDetection >= maxDetection)
        {
            HandleTimeUp();
        }
    }

    /// <summary>
    /// 外部から検知処理を強制停止する。
    /// 副作用: isGameOverをtrueに設定し、以降のUpdate/IncreaseDetectionによる増減を停止。
    /// </summary>
    public void ForceStopDetection()
    {
        isGameOver = true;
        Debug.Log("DetectionManager: 強制停止しました。");
    }

    /// <summary>
    /// 現在の検知値を取得する。
    /// </summary>
    /// <returns>現在の検知値（0〜maxDetection）</returns>
    public float GetCurrentDetection() => currentDetection;

    /// <summary>
    /// 設定された検知の最大値を取得する。
    /// </summary>
    /// <returns>検知ゲージの最大値</returns>
    public float GetMaxDetection() => maxDetection;
}