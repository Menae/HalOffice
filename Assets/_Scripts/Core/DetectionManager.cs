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

    [Header("ゲージ設定")]
    public float maxDetection = 100f;
    [Tooltip("ノイズエフェクトがかかる時間（秒）")]
    public float effectDuration = 0.5f;

    [Header("しきい値設定")]
    [Tooltip("この値を超えるとFilmGrainが一瞬かかる")]
    public float filmGrainFlashThreshold = 30f;
    [Tooltip("この値を超えるとLensDistortionが一瞬かかり、FilmGrainが永続的にかかる")]
    public float highAlertThreshold = 70f;

    [Header("シーン遷移設定")]
    [Tooltip("ゲームオーバー時にロードするシーンの名前")]
    public string gameOverSceneName = "GameOverScene";

    private float currentDetection = 0f;
    private bool isGameOver = false;

    // 状態を管理するフラグ（これらは新しいScreenEffectsControllerでは不要になったため、ここからも削除）
    // private bool hasTriggeredGrainFlash = false;
    // private bool hasTriggeredLensDistortionFlash = false;
    // private bool isPersistentGrainActive = false;

    private void OnEnable()
    {
        if (detectionChannel != null) { detectionChannel.OnEventRaised += IncreaseDetection; }
        GameManager.OnTimeUp += HandleTimeUp;
    }
    private void OnDisable()
    {
        if (detectionChannel != null) { detectionChannel.OnEventRaised -= IncreaseDetection; }
        GameManager.OnTimeUp -= HandleTimeUp;
    }
    void IncreaseDetection(float amount)
    {
        if (isGameOver || (GameManager.Instance != null && !GameManager.Instance.isGameActive)) return;
        currentDetection += amount;
    }

    private void HandleTimeUp()
    {
        if (isGameOver) return;
        isGameOver = true;
        OnGameOver?.Invoke();

        if (screenEffects != null)
        {
            // ゲームオーバー時のTVオフ演出はScreenEffectsControllerの役割なので、これは残す
            screenEffects.TriggerTvOff();
        }
        StartCoroutine(LoadSceneAfterDelay(2.0f));
    }

    void Update()
    {
        if (isGameOver || (GameManager.Instance != null && !GameManager.Instance.isGameActive)) return;

        //見つかり度を時間経過で減少させる
        if (currentDetection > 0)
        {
            currentDetection -= 5f * Time.deltaTime; //仮の減少率
        }
        currentDetection = Mathf.Clamp(currentDetection, 0, maxDetection);

        // ▼▼▼ 以下のブロックを全て削除 ▼▼▼
        // 新しいScreenEffectsControllerが自律的に見つかり度を監視するため、
        // DetectionManagerがエフェクトを命令する必要はなくなった。
        /*
        //30のしきい値判定
        if (currentDetection >= filmGrainFlashThreshold && !hasTriggeredGrainFlash)
        {
            hasTriggeredGrainFlash = true;
            screenEffects.FlashFilmGrain(effectDuration);
        }
        else if (currentDetection < filmGrainFlashThreshold && hasTriggeredGrainFlash)
        {
            hasTriggeredGrainFlash = false;
        }

        //70のしきい値判定
        if (currentDetection >= highAlertThreshold && !hasTriggeredLensDistortionFlash)
        {
            hasTriggeredLensDistortionFlash = true;
            screenEffects.FlashGlitchEffect(effectDuration);
        }
        else if (currentDetection < highAlertThreshold && hasTriggeredLensDistortionFlash)
        {
            hasTriggeredLensDistortionFlash = false;
        }

        //70以上の永続的なノイズ判定
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
        */
        // ▲▲▲ ここまで削除 ▲▲▲

        //見つかり度がMAXになったか？
        if (currentDetection >= maxDetection)
        {
            HandleTimeUp();
        }
    }

    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(gameOverSceneName);
    }

    public float GetCurrentDetection() { return currentDetection; }
    public float GetMaxDetection() { return maxDetection; }
}