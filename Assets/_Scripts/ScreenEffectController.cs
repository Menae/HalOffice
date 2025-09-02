using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ScreenEffectsController : MonoBehaviour
{
    [Header("コンポーネント参照")]
    [Tooltip("シーン内にあるGlitchControllerをアサイン")]
    public GlitchController glitchController;
    [Tooltip("シーン内にあるDetectionManagerをアサイン")]
    public DetectionManager detectionManager;
    [Tooltip("TVオフ演出用のUIパネル")]
    public GameObject tvOffEffectPanel;

    [Header("サウンド設定")]
    [Tooltip("エフェクトと同時に再生する効果音")]
    public AudioClip glitchSoundClip;
    [Range(0f, 1f)]
    [Tooltip("効果音の音量スケール")]
    public float glitchVolumeScale = 1.0f;
    [Tooltip("TVがオフになる時の効果音")]
    public AudioClip tvOffSoundClip;
    [Range(0f, 1f)]
    [Tooltip("TVオフ効果音の音量スケール")]
    public float tvOffVolumeScale = 1.0f;

    // 内部で状態を管理するためのフラグ
    private AudioSource audioSource;
    private Animator tvOffAnimator;
    private bool hasTriggeredEffect30 = false;
    private bool hasTriggeredEffect70Sound = false;
    private Coroutine effect30Coroutine;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 必須コンポーネントが設定されているか確認
        if (glitchController == null || detectionManager == null)
        {
            Debug.LogError("Glitch Controller または Detection Manager がアサインされていません！このコンポーネントは動作を停止します。", this.gameObject);
            this.enabled = false;
            return;
        }

        // 開始時にエフェクトを確実にオフにする
        glitchController.noiseAmount = 0f;
        glitchController.glitchStrength = 0f;

        if (tvOffEffectPanel != null)
        {
            tvOffEffectPanel.SetActive(false);
            tvOffAnimator = tvOffEffectPanel.GetComponent<Animator>();
        }
    }

    private void Update()
    {
        // 参照がなければ何もしない
        if (glitchController == null || detectionManager == null) return;

        float currentDetection = detectionManager.GetCurrentDetection();

        // 70以上の時の処理 (最優先)
        if (currentDetection >= 70f)
        {
            // 常にエフェクトを最大にする
            glitchController.noiseAmount = 100f;
            glitchController.glitchStrength = 200f;

            // 70に達した瞬間に一度だけ効果音を鳴らす
            if (!hasTriggeredEffect70Sound)
            {
                audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
                hasTriggeredEffect70Sound = true;
            }
        }
        // 70未満の時の処理
        else
        {
            // 70のフラグをリセット
            hasTriggeredEffect70Sound = false;

            // 30に達した瞬間の処理
            if (currentDetection >= 30f && !hasTriggeredEffect30)
            {
                hasTriggeredEffect30 = true;
                if (effect30Coroutine != null) StopCoroutine(effect30Coroutine);
                effect30Coroutine = StartCoroutine(Effect30Routine());
            }
            // 30未満に落ちた時の処理
            else if (currentDetection < 30f)
            {
                hasTriggeredEffect30 = false;
                // 永続エフェクトがオフ、かつ30のコルーチンも動いていないなら、エフェクトを確実にオフにする
                if (effect30Coroutine == null)
                {
                    glitchController.noiseAmount = 0f;
                    glitchController.glitchStrength = 0f;
                }
            }
        }
    }

    // 見つかり度30の時に1秒間だけエフェクトを再生するコルーチン
    private IEnumerator Effect30Routine()
    {
        audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
        glitchController.noiseAmount = 30f;
        glitchController.glitchStrength = 200f;

        yield return new WaitForSeconds(1f);

        // 1秒後、もし70以上の永続エフェクトが発動していなければ、エフェクトをオフに戻す
        if (detectionManager.GetCurrentDetection() < 70f)
        {
            glitchController.noiseAmount = 0f;
            glitchController.glitchStrength = 0f;
        }

        effect30Coroutine = null;
    }

    // TVオフ演出のメソッド (これは外部から呼ばれる可能性があるので残す)
    public void TriggerTvOff()
    {
        if (audioSource != null && tvOffSoundClip != null)
        {
            audioSource.PlayOneShot(tvOffSoundClip, tvOffVolumeScale);
        }
        if (tvOffEffectPanel != null && tvOffAnimator != null)
        {
            tvOffEffectPanel.SetActive(true);
            tvOffAnimator.SetTrigger("TVOFF");
        }
    }
}