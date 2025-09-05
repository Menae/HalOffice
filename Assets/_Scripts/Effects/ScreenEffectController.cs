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
    private bool hasTriggeredEffect70 = false; // 70に到達したかのフラグ
    private Coroutine effect30Coroutine;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (glitchController == null || detectionManager == null)
        {
            Debug.LogError("Glitch Controller または Detection Manager がアサインされていません！このコンポーネントは動作を停止します。", this.gameObject);
            this.enabled = false;
            return;
        }

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
        if (glitchController == null || detectionManager == null) return;

        float currentDetection = detectionManager.GetCurrentDetection();

        // --- 1. しきい値を超えた瞬間のトリガー処理 ---
        
        // 70に達した瞬間の処理
        if (currentDetection >= 70f && !hasTriggeredEffect70)
        {
            hasTriggeredEffect70 = true;
            audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
        }
        // 30に達した瞬間の処理
        else if (currentDetection >= 30f && !hasTriggeredEffect30)
        {
            hasTriggeredEffect30 = true;
            if (effect30Coroutine != null) StopCoroutine(effect30Coroutine);
            effect30Coroutine = StartCoroutine(Effect30Routine());
        }

        // --- 2. フラグのリセット処理 ---
        if (currentDetection < 70f) hasTriggeredEffect70 = false;
        if (currentDetection < 30f) hasTriggeredEffect30 = false;


        // --- 3. 状態に応じた永続的なエフェクト処理 ---

        // 30のコルーチンが動いている間は、コルーチンがエフェクトを管理する
        if (effect30Coroutine != null) return;

        // 70以上の時は永続的にエフェクトをかける
        if (currentDetection >= 70f)
        {
            glitchController.noiseAmount = 30f;
            glitchController.glitchStrength = 200f;
        }
        // それ以外の時はエフェクトをオフにする
        else
        {
            glitchController.noiseAmount = 0f;
            glitchController.glitchStrength = 0f;
        }
    }

    // 見つかり度30の時に0.5秒間だけエフェクトを再生するコルーチン
    private IEnumerator Effect30Routine()
    {
        // 30に達した時の効果音はこちらで再生
        audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);

        glitchController.noiseAmount = 30f;
        glitchController.glitchStrength = 200f;

        yield return new WaitForSeconds(0.5f);

        // 0.5秒後、もし70以上の永続エフェクトが発動していなければ、エフェクトをオフに戻す
        if (detectionManager.GetCurrentDetection() < 70f)
        {
            glitchController.noiseAmount = 0f;
            glitchController.glitchStrength = 0f;
        }

        effect30Coroutine = null;
    }

    // TVオフ演出のメソッド
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