using UnityEngine;

public class DetectionManager : MonoBehaviour
{
    [Header("イベントチャンネル")]
    public FloatEventChannelSO detectionChannel;
    [Header("参照")]
    public ScreenEffectsController screenEffects;
    [Header("ゲージ設定")]
    public float maxDetection = 100f;
    public float noiseEffectThreshold = 50f;
    public float noiseEffectDuration = 1.0f;
    [Tooltip("見つかり度が1秒間に減少する量")]
    public float detectionDecayRate = 5f;

    private float currentDetection = 0f;
    private bool hasTriggeredNoise = false;
    private bool isGameOver = false;

    private void OnEnable()
    {
        if (detectionChannel != null) { detectionChannel.OnEventRaised += IncreaseDetection; }
    }
    private void OnDisable()
    {
        if (detectionChannel != null) { detectionChannel.OnEventRaised -= IncreaseDetection; }
    }
    void IncreaseDetection(float amount)
    {
        if (isGameOver) return;
        currentDetection += amount;
    }

    void Update()
    {
        if (isGameOver || screenEffects == null) return;
        // 見つかり度を時間経過で減少させる
        if (currentDetection > 0)
        {
            currentDetection -= detectionDecayRate * Time.deltaTime;
        }
        // 0未満にならないように、また100を超えないように値を丸める
        currentDetection = Mathf.Clamp(currentDetection, 0, maxDetection);

        if (isGameOver || screenEffects == null) return;

        if (currentDetection >= noiseEffectThreshold && !hasTriggeredNoise)
        {
            hasTriggeredNoise = true;
            screenEffects.FlashGlitchEffect(noiseEffectDuration);
        }
        else if (currentDetection < noiseEffectThreshold && hasTriggeredNoise)
        {
            hasTriggeredNoise = false;
        }

        if (currentDetection >= maxDetection)
        {
            isGameOver = true;
            Debug.Log("見つかった！ゲームオーバー。");
            // ★★★ 変更：GameManagerに入力停止を命令 ★★★
            GameManager.Instance.SetInputEnabled(false);
            screenEffects.TriggerTvOff();
        }
    }

    public float GetCurrentDetection() { return currentDetection; }
    public float GetMaxDetection() { return maxDetection; }
}