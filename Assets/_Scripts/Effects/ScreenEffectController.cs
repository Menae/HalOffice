using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// 画面のグリッチやTVオン/オフ演出、関連効果音を統合して管理するコントローラ。
/// DetectionManager の検知値を監視し、30%/70%のしきい値で短時間および永続的な画面効果を発生させる。
/// </summary>
public class ScreenEffectsController : MonoBehaviour
{
    [Header("コンポーネント参照")]
    [Tooltip("シーン内にあるGlitchControllerをアサイン")]
    /// <summary>
    /// グリッチ描画を行うコンポーネント。InspectorでD&D。
    /// null時は描画制御をスキップするが、呼び出し箇所でnullチェックあり。
    /// </summary>
    public GlitchController glitchController;

    [Tooltip("シーン内にあるDetectionManagerをアサイン")]
    /// <summary>
    /// 検知ゲージ管理クラス。InspectorでD&D。
    /// 現在の検知値を参照してエフェクト発火判定を行う。null時は早期returnで処理停止。
    /// </summary>
    public DetectionManager detectionManager;

    [Tooltip("TVオフ演出用のUIパネル")]
    /// <summary>
    /// TVオフ用のUIパネル。InspectorでD&D。アニメータがあればトリガーを送信して演出を再生。
    /// null時はUI演出をスキップする。
    /// </summary>
    public GameObject tvOffEffectPanel;

    [Header("ゲーム終了時の制御")]
    [Tooltip("TVオフ演出時に無効化（非表示）にするNPCオブジェクト")]
    /// <summary>
    /// TVオフ演出に合わせて無効化するNPCオブジェクト。InspectorでD&D。null時は無処理。
    /// </summary>
    public GameObject npcObjectToDisable;

    [Header("サウンド設定")]
    [Tooltip("エフェクトと同時に再生する効果音")]
    /// <summary>
    /// グリッチ発生時に再生する効果音。null時は音を鳴らさない。
    /// </summary>
    public AudioClip glitchSoundClip;

    [Range(0f, 1f)]
    [Tooltip("効果音の音量スケール")]
    /// <summary>
    /// グリッチ効果音の再生ボリュームスケール（0〜1）。
    /// </summary>
    public float glitchVolumeScale = 1.0f;

    [Tooltip("TVがオフになる時の効果音")]
    /// <summary>
    /// TVオフ時に再生する効果音。null時は音を鳴らさない。
    /// </summary>
    public AudioClip tvOffSoundClip;

    [Range(0f, 1f)]
    [Tooltip("TVオフ効果音の音量スケール")]
    /// <summary>
    /// TVオフ効果音の再生ボリュームスケール（0〜1）。
    /// </summary>
    public float tvOffVolumeScale = 1.0f;

    // Inspectorで参照するコンポーネントは Start 内で取得・検証する。
    private AudioSource audioSource;
    private Animator tvOffAnimator;

    // トリガーフラグ：しきい値を超えた瞬間の発火を一度だけ行うためのガード。
    private bool hasTriggeredEffect30 = false;
    private bool hasTriggeredEffect70 = false;

    // TVオフや外的要因でエフェクトを一時停止するためのフラグ。
    private bool areEffectsSuspended = false;

    // 30%用の短時間コルーチン管理。
    private Coroutine effect30Coroutine;

    /// <summary>
    /// Unity Start。シーンロード直後の初期化処理を実行。
    /// 呼び出しタイミング: MonoBehaviour.Start（Awakeの後、最初のフレームの直前）。
    /// 初期化内容: AudioSource取得、必須参照の検証、グリッチ値の初期化、TVパネルの非表示化。
    /// 注意: GlitchController / DetectionManager が未設定だとこのコンポーネントを無効化する。
    /// </summary>
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

        // 再プレイ等に備えて一時停止フラグをリセット。
        areEffectsSuspended = false;
    }

    /// <summary>
    /// Unity Update。毎フレームの検知値監視とエフェクト制御を行う。
    /// 呼び出しタイミング: 毎フレーム。DetectionManager から現在値を取得してしきい値判定を行う。
    /// 注意: areEffectsSuspended が true の間はエフェクト制御を行わない。
    /// </summary>
    private void Update()
    {
        if (glitchController == null || detectionManager == null) return;
        if (areEffectsSuspended) return;

        float currentDetection = detectionManager.GetCurrentDetection();

        // しきい値を超えた瞬間のトリガー処理（瞬間音や一時的な演出）
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

        // フラグのリセット（閾値を下回ったら再発火可能にする）
        if (currentDetection < 70f) hasTriggeredEffect70 = false;
        if (currentDetection < 30f) hasTriggeredEffect30 = false;

        // 永続的なエフェクト処理：短時間コルーチン実行中はここでの永続適用を行わない
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

    /// <summary>
    /// 30%到達時に一時的に強めのグリッチを発生させるコルーチン。
    /// 動作: 効果音再生 → 一時的にノイズ/強度を上げる → 後続の検知値が70未満なら元に戻す。
    /// </summary>
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

    /// <summary>
    /// 外部から呼び出して短時間のグリッチバーストを発生させるコルーチン。
    /// 呼び出し例: イベントに合わせて一瞬の視覚強調を行いたいとき。
    /// 注意: duration が短すぎると視覚的効果が見えにくい。
    /// </summary>
    /// <param name="duration">グリッチの持続時間（秒）。</param>
    public IEnumerator TriggerGlitchBurstRoutine(float duration)
    {
        if (audioSource != null && glitchSoundClip != null)
        {
            audioSource.PlayOneShot(glitchSoundClip, glitchVolumeScale);
        }

        if (glitchController != null)
        {
            glitchController.noiseAmount = 80f;
            glitchController.glitchStrength = 750f;
        }

        yield return new WaitForSeconds(duration);

        if (glitchController != null)
        {
            glitchController.noiseAmount = 0f;
            glitchController.glitchStrength = 0f;
        }
    }

    /// <summary>
    /// TVオフ演出を開始する。検知の増加を強制停止し、音やUIアニメーションを再生、必要なオブジェクトを無効化する。
    /// 呼び出し例: ゲーム終了処理から一連の演出を開始する際に使用。
    /// 注意: detectionManager が null でない場合は検知停止を試みる。tvOffEffectPanel や npcObjectToDisable が null の場合はそれぞれの処理をスキップ。
    /// </summary>
    public void TriggerTvOff()
    {
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

        if (npcObjectToDisable != null)
        {
            npcObjectToDisable.SetActive(false);
            Debug.Log("TVオフ演出に伴い、NPCを無効化しました。");
        }
    }

    /// <summary>
    /// TVオン状態に戻す。TVオフ演出パネルを非表示化することでUIを復帰させる。
    /// 注意: tvOffEffectPanel が null の場合は何もしない。
    /// </summary>
    public void TriggerTvOn()
    {
        if (tvOffEffectPanel != null)
        {
            tvOffEffectPanel.SetActive(false);
        }
    }
}