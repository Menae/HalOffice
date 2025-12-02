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

    [Header("ゲーム終了時の制御")]
    [Tooltip("TVオフ演出時に無効化（非表示）にするNPCオブジェクト")]
    public GameObject npcObjectToDisable;

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
    private bool hasTriggeredEffect70 = false;
    private bool areEffectsSuspended = false;
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

        // 初期化時にフラグをリセット（再プレイ時などのため）
        areEffectsSuspended = false;
    }

    private void Update()
    {
        if (glitchController == null || detectionManager == null) return;

        if (areEffectsSuspended) return;

        float currentDetection = detectionManager.GetCurrentDetection();

        // --- 1. しきい値を超えた瞬間のトリガー処理 ---
        if (currentDetection >= 70f && !hasTriggeredEffect70)
        {
            hasTriggeredEffect70 = true;
            audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
        }
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
        if (effect30Coroutine != null) return;

        if (currentDetection >= 70f)
        {
            glitchController.noiseAmount = 30f;
            glitchController.glitchStrength = 200f;
        }
        else
        {
            glitchController.noiseAmount = 0f;
            glitchController.glitchStrength = 0f;
        }
    }

    private IEnumerator Effect30Routine()
    {
        audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
        glitchController.noiseAmount = 30f;
        glitchController.glitchStrength = 200f;

        yield return new WaitForSeconds(0.5f);

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

        // TVが消える瞬間に、見つかり度の上昇を完全に止める
        if (detectionManager != null)
        {
            detectionManager.ForceStopDetection();
        }

        areEffectsSuspended = true;

        if (glitchController != null)
        {
            glitchController.noiseAmount = 0f;
            glitchController.glitchStrength = 0f;
        }

        if (audioSource != null && tvOffSoundClip != null)
        {
            audioSource.PlayOneShot(tvOffSoundClip, tvOffVolumeScale);
        }
        if (tvOffEffectPanel != null && tvOffAnimator != null)
        {
            tvOffEffectPanel.SetActive(true);
            tvOffAnimator.SetTrigger("TVOFF");
        }

        // NPCを無効化する処理
        if (npcObjectToDisable != null)
        {
            npcObjectToDisable.SetActive(false);
            Debug.Log("TVオフ演出に伴い、NPCを無効化しました。");
        }
    }

    public void TriggerTvOn()
    {
        if (tvOffEffectPanel != null)
        {
            tvOffEffectPanel.SetActive(false);
        }
    }
}