using System;
using UnityEngine;

public class DetectionManager : MonoBehaviour
{
    public static event Action OnGameOver;

    [Header("イベントチャンネル")]
    public FloatEventChannelSO detectionChannel;

    [Header("参照")]
    public ScreenEffectsController screenEffects;
    [Tooltip("シーンのタイマーを管理するDay1Manager")]
    public Day1Manager day1Manager;
    public EvaluationTrigger evaluationTrigger;

    [Header("ゲージ設定")]
    public float maxDetection = 100f;

    [Header("シーン遷移設定")]
    public string gameOverSceneName = "GameOverScene";

    private float currentDetection = 0f;
    private bool isGameOver = false;

    private void OnEnable()
    {
        if (detectionChannel != null) { detectionChannel.OnEventRaised += IncreaseDetection; }
        Day1Manager.OnTimeUp += HandleTimeUp;
    }
    private void OnDisable()
    {
        if (detectionChannel != null) { detectionChannel.OnEventRaised -= IncreaseDetection; }
        Day1Manager.OnTimeUp -= HandleTimeUp;
    }
    void IncreaseDetection(float amount)
    {
        if (isGameOver || (day1Manager != null && !day1Manager.isGameActive)) return;
        currentDetection += amount;
    }

    private void HandleTimeUp()
    {
        // isGameOverフラグは、二重呼び出しを防ぐために残す
        if (isGameOver) return;
        isGameOver = true;
        OnGameOver?.Invoke();

        // 既に修正済みの「完璧な」ゲームオーバー処理を即座に呼び出す
        if (evaluationTrigger != null)
        {
            evaluationTrigger.EndDayAndEvaluate();
        }
        else
        {
            Debug.LogError("EvaluationTriggerがDetectionManagerに設定されていません！");
        }
    }

    void Update()
    {
        if (isGameOver || (day1Manager != null && !day1Manager.isGameActive)) return;

        if (currentDetection > 0)
        {
            currentDetection -= 5f * Time.deltaTime;
        }
        currentDetection = Mathf.Clamp(currentDetection, 0, maxDetection);

        if (currentDetection >= maxDetection)
        {
            HandleTimeUp();
        }
    }

    // 外部から強制的に検知を停止させるメソッド
    public void ForceStopDetection()
    {
        // これをtrueにすることで、UpdateやIncreaseDetectionが即座に動かなくなります
        isGameOver = true;
        Debug.Log("DetectionManager: 強制停止しました。");
    }

    public float GetCurrentDetection() { return currentDetection; }
    public float GetMaxDetection() { return maxDetection; }
}