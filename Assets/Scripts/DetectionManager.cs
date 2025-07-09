using UnityEngine;

public class DetectionManager : MonoBehaviour
{
    [Header("イベントチャンネル")]
    public FloatEventChannelSO detectionChannel;
    [Header("参照")]
    public ScreenEffectsController screenEffects;

    [Header("ゲージ設定")]
    public float maxDetection = 100f;
    [Tooltip("ノイズエフェクトがかかる時間（秒）")]
    public float effectDuration = 0.5f; // エフェクトの長さを共通化

    // ★★★ しきい値の変数を分かりやすく変更 ★★★
    [Header("しきい値設定")]
    [Tooltip("この値を超えるとFilmGrainが一瞬かかる")]
    public float filmGrainFlashThreshold = 30f;
    [Tooltip("この値を超えるとLensDistortionが一瞬かかり、FilmGrainが永続的にかかる")]
    public float highAlertThreshold = 70f;

    private float currentDetection = 0f;
    private bool isGameOver = false;

    // ★★★ 状態を管理するフラグを整理 ★★★
    private bool hasTriggeredGrainFlash = false;
    private bool hasTriggeredLensDistortionFlash = false;
    private bool isPersistentGrainActive = false;

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

    // ★★★ Updateメソッドのロジックを全面的に刷新 ★★★
    void Update()
    {
        if (isGameOver || screenEffects == null) return;

        // 見つかり度を時間経過で減少させる
        if (currentDetection > 0)
        {
            currentDetection -= 5f * Time.deltaTime; // 仮の減少率
        }
        currentDetection = Mathf.Clamp(currentDetection, 0, maxDetection);

        // --- 30のしきい値判定 ---
        if (currentDetection >= filmGrainFlashThreshold && !hasTriggeredGrainFlash)
        {
            hasTriggeredGrainFlash = true;
            screenEffects.FlashFilmGrain(effectDuration);
        }
        else if (currentDetection < filmGrainFlashThreshold && hasTriggeredGrainFlash)
        {
            hasTriggeredGrainFlash = false; // 下回ったら、またトリガーできるようにリセット
        }

        // --- 70のしきい値判定（一瞬のグリッチ） ---
        if (currentDetection >= highAlertThreshold && !hasTriggeredLensDistortionFlash)
        {
            hasTriggeredLensDistortionFlash = true;
            screenEffects.FlashGlitchEffect(effectDuration);
        }
        else if (currentDetection < highAlertThreshold && hasTriggeredLensDistortionFlash)
        {
            hasTriggeredLensDistortionFlash = false;
        }

        // --- 70以上の永続的なノイズ判定 ---
        if (currentDetection >= highAlertThreshold && !isPersistentGrainActive)
        {
            isPersistentGrainActive = true;
            screenEffects.SetPersistentFilmGrain(true);
        }
        else if (currentDetection < highAlertThreshold && isPersistentGrainActive)
        {
            isPersistentGrainActive = false;
            screenEffects.SetPersistentFilmGrain(false);
        }

        // --- ゲームオーバー判定 ---
        if (currentDetection >= maxDetection)
        {
            isGameOver = true;
            Debug.Log("見つかった！ゲームオーバー。");
            GameManager.Instance.SetInputEnabled(false);
            screenEffects.TriggerTvOff();
        }
    }

    public float GetCurrentDetection() { return currentDetection; }
    public float GetMaxDetection() { return maxDetection; }
}