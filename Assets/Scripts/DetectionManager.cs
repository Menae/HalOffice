using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DetectionManager : MonoBehaviour
{
    [Header("イベントチャンネル")]
    public FloatEventChannelSO detectionChannel;
    [Header("参照")]
    public ScreenEffectsController screenEffects;

    [Header("ゲージ設定")]
    public float maxDetection = 100f;
    [Tooltip("ノイズエフェクトがかかる時間（秒）")]
    public float effectDuration = 0.5f; //エフェクトの長さを共通化

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

    //状態を管理するフラグ
    private bool hasTriggeredGrainFlash = false;
    private bool hasTriggeredLensDistortionFlash = false;
    private bool isPersistentGrainActive = false;

    private void OnEnable()
    {
        if (detectionChannel != null) { detectionChannel.OnEventRaised += IncreaseDetection; }
        //GameManagerからの放送を受信する登録
        GameManager.OnTimeUp += HandleTimeUp;
    }
    private void OnDisable()
    {
        if (detectionChannel != null) { detectionChannel.OnEventRaised -= IncreaseDetection; }
        //受信登録を解除する(作法)
        GameManager.OnTimeUp -= HandleTimeUp;
    }
    void IncreaseDetection(float amount)
    {
        if (isGameOver) return;
        currentDetection += amount;
    }

    //時間切れの放送を受け取った時に実行するメソッド
    private void HandleTimeUp()
    {
        //ゲームオーバー処理
        if (isGameOver) return;
        isGameOver = true;

        if (screenEffects != null)
        {
            screenEffects.TriggerTvOff();
        }
        StartCoroutine(LoadSceneAfterDelay(2.0f));
    }

    void Update()
    {
        if (isGameOver || screenEffects == null) return;

        //見つかり度を時間経過で減少させる
        if (currentDetection > 0)
        {
            currentDetection -= 5f * Time.deltaTime; //仮の減少率
        }
        currentDetection = Mathf.Clamp(currentDetection, 0, maxDetection);

        //30のしきい値判定
        if (currentDetection >= filmGrainFlashThreshold && !hasTriggeredGrainFlash)
        {
            hasTriggeredGrainFlash = true;
            screenEffects.FlashFilmGrain(effectDuration);
        }
        else if (currentDetection < filmGrainFlashThreshold && hasTriggeredGrainFlash)
        {
            hasTriggeredGrainFlash = false; //下回ったら、またトリガーできるようにリセット
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
    }

    //指定秒数待ってからシーンをロード
    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        //指定された秒数だけ待機
        yield return new WaitForSeconds(delay);

        //次に行くシーンを指定1
        SceneManager.LoadScene(gameOverSceneName);
    }

    public float GetCurrentDetection() { return currentDetection; }
    public float GetMaxDetection() { return maxDetection; }
}