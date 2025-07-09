using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(AudioSource))]
public class ScreenEffectsController : MonoBehaviour
{
    [Header("エフェクト設定")]
    [Tooltip("各種エフェクトを制御するためのGlobal Volume")]
    public Volume postProcessVolume;
    [Tooltip("TVオフ演出用のUIパネル")]
    public GameObject tvOffEffectPanel;

    [Header("サウンド設定")]
    [Tooltip("グリッチエフェクトと同時に再生する効果音")]
    public AudioClip glitchSoundClip;
    [Range(0f, 1f)]
    [Tooltip("グリッチ効果音の音量スケール")]
    public float glitchVolumeScale = 1.0f;
    [Tooltip("TVがオフになる時の効果音")]
    public AudioClip tvOffSoundClip;
    [Range(0f, 1f)]
    [Tooltip("TVオフ効果音の音量スケール")]
    public float tvOffVolumeScale = 1.0f;

    private Coroutine glitchCoroutine;
    private Coroutine filmGrainCoroutine;

    private LensDistortion lensDistortionEffect;
    private FilmGrain filmGrainEffect;
    private AudioSource audioSource;
    private Animator tvOffAnimator;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out lensDistortionEffect);
            postProcessVolume.profile.TryGet(out filmGrainEffect);
        }

        // 開始時はすべてのエフェクトをオフに
        if (lensDistortionEffect != null)
        {
            lensDistortionEffect.intensity.value = 0f;
            lensDistortionEffect.scale.value = 1f;
        }
        if (filmGrainEffect != null)
        {
            filmGrainEffect.intensity.value = 0f;
        }

        if (tvOffEffectPanel != null)
        {
            tvOffEffectPanel.SetActive(false);
            tvOffAnimator = tvOffEffectPanel.GetComponent<Animator>();
        }
    }

    // 「LensDistortion」を一時的に再生するメソッド
    public void FlashGlitchEffect(float duration)
    {
        if (glitchCoroutine != null) StopCoroutine(glitchCoroutine);
        glitchCoroutine = StartCoroutine(FlashLensDistortionRoutine(duration));
    }

    // 「FilmGrain」を一時的に再生するメソッド
    public void FlashFilmGrain(float duration)
    {
        if (filmGrainCoroutine != null) StopCoroutine(filmGrainCoroutine);
        filmGrainCoroutine = StartCoroutine(FlashFilmGrainRoutine(duration));
    }

    // 「FilmGrain」を永続的に再生/停止するメソッド
    public void SetPersistentFilmGrain(bool isActive)
    {
        if (filmGrainEffect != null)
        {
            filmGrainEffect.intensity.value = isActive ? 1f : 0f;
        }
    }

    // LensDistortionを再生するコルーチン
    private IEnumerator FlashLensDistortionRoutine(float duration)
    {
        if (lensDistortionEffect == null) yield break;
        if (audioSource != null && glitchSoundClip != null)
        {
            audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
        }
        lensDistortionEffect.intensity.value = 1f;
        lensDistortionEffect.scale.value = 0.3f;
        yield return new WaitForSeconds(duration);
        lensDistortionEffect.intensity.value = 0f;
        lensDistortionEffect.scale.value = 1f;
    }

    // FilmGrainを再生するコルーチン
    private IEnumerator FlashFilmGrainRoutine(float duration)
    {
        if (filmGrainEffect == null) yield break;
        filmGrainEffect.intensity.value = 1f;
        if (audioSource != null && glitchSoundClip != null)
        {
            audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
        }
        yield return new WaitForSeconds(duration);
        filmGrainEffect.intensity.value = 0f;
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