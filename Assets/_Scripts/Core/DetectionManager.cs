using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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
        if (isGameOver) return;
        isGameOver = true;
        OnGameOver?.Invoke();

        // TVオフ演出をトリガー
        if (screenEffects != null)
        {
            screenEffects.TriggerTvOff();
        }

        // 自身でシーンをロードするのをやめ、Day1Managerに命令する
        if (day1Manager != null)
        {
            // 2秒後に調査終了処理を呼び出すコルーチンを開始
            StartCoroutine(RequestEndInvestigationAfterDelay(2.0f));
        }
    }

    private IEnumerator RequestEndInvestigationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Day1Managerに、gameOverSceneNameに遷移するように命令
        evaluationTrigger.EndDayAndEvaluate();
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

    public float GetCurrentDetection() { return currentDetection; }
    public float GetMaxDetection() { return maxDetection; }
}