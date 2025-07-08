using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(AudioSource))]
public class ScreenEffectsController : MonoBehaviour
{
    [Header("エフェクト設定")]
    [Tooltip("ノイズエフェクトを制御するためのGlobal Volume")]
    public Volume postProcessVolume;
    [Tooltip("TVオフ演出用のUIパネル")]
    public GameObject tvOffEffectPanel;

    [Header("サウンド設定")]
    [Tooltip("グリッチエフェクトと同時に再生する効果音")]
    public AudioClip glitchSoundClip;
    [Range(0f, 1f)]
    [Tooltip("グリッチ効果音の音量スケール")]
    public float glitchVolumeScale = 1.0f;
    // ★★★ 追加 ★★★
    [Tooltip("TVがオフになる時の効果音")]
    public AudioClip tvOffSoundClip;
    [Range(0f, 1f)]
    [Tooltip("TVオフ効果音の音量スケール")]
    public float tvOffVolumeScale = 1.0f;

    private Coroutine noiseCoroutine;
    private LensDistortion lensDistortionEffect;
    private AudioSource audioSource;
    private Animator tvOffAnimator;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out lensDistortionEffect);
        }

        if (lensDistortionEffect != null)
        {
            lensDistortionEffect.intensity.value = 0f;
            lensDistortionEffect.scale.value = 1f;
        }

        if (tvOffEffectPanel != null)
        {
            tvOffEffectPanel.SetActive(false);
            tvOffAnimator = tvOffEffectPanel.GetComponent<Animator>();
        }
    }

    public void FlashGlitchEffect(float duration)
    {
        if (noiseCoroutine != null)
        {
            StopCoroutine(noiseCoroutine);
        }
        noiseCoroutine = StartCoroutine(FlashEffectRoutine(duration));
    }

    private IEnumerator FlashEffectRoutine(float duration)
    {
        if (lensDistortionEffect == null) yield break;

        if (audioSource != null && glitchSoundClip != null)
        {
            audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
        }

        lensDistortionEffect.intensity.value = 1f;
        lensDistortionEffect.scale.value = 0.1f;

        yield return new WaitForSeconds(duration);

        lensDistortionEffect.intensity.value = 0f;
        lensDistortionEffect.scale.value = 1f;
    }

    public void TriggerTvOff()
    {
        // ★★★ 追加 ★★★
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