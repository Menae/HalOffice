using System;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool isInputEnabled { get; private set; } = true;

    [Header("タイマー設定")]
    [Tooltip("カウントダウンの開始時間（秒）")]
    public float totalTimeInSeconds = 300f;

    [Header("時間経過イベント")]
    [Tooltip("この残り秒数になったら、オブジェクトAを表示する")]
    public float eventA_TriggerTime = 120f;
    public GameObject imageObjectA;
    [Tooltip("この残り秒数になったら、オブジェクトBを表示する")]
    public float eventB_TriggerTime = 60f;
    public GameObject imageObjectB;
    [Tooltip("イベントで表示した画像が消えるまでの時間（秒）")]
    public float imageDisplayDuration = 5.0f;

    [Header("通知エフェクト設定")]
    [Tooltip("通知が表示される時の効果音")]
    public AudioClip notificationSound;
    [Range(0f, 1f)]
    [Tooltip("通知効果音の音量")]
    public float notificationVolume = 1.0f;

    [Header("ゲームオーバー設定")]
    //ScreenEffectsControllerへの参照
    [Tooltip("画面エフェクトを制御するコントローラー")]
    public DetectionManager detectionManager;

    private float currentTime;
    private bool eventATriggered = false;
    private bool eventBTriggered = false;
    private AudioSource audioSource; // 効果音再生用のAudioSource
    private bool isTimeUp = false; // 時間切れになったかどうかを管理

    //時間切れになったことを通知するイベント
    public static event Action OnTimeUp;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        currentTime = totalTimeInSeconds;
        if (imageObjectA != null) imageObjectA.SetActive(false);
        if (imageObjectB != null) imageObjectB.SetActive(false);
    }

    void Update()
    {
        //時間切れになったらタイマー処理を停止
        if (isTimeUp) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
        }
        else
        {
            //時間切れ処理を呼び出す
            currentTime = 0;
            isTimeUp = true; //時間切れフラグを立てて、二度と実行されないようにする
            TriggerTimesUp();
        }

        CheckTimedEvents();
    }

    private void CheckTimedEvents()
    {
        if (!eventATriggered && currentTime <= eventA_TriggerTime)
        {
            eventATriggered = true;
            TriggerNotification(imageObjectA);
        }

        if (!eventBTriggered && currentTime <= eventB_TriggerTime)
        {
            eventBTriggered = true;
            TriggerNotification(imageObjectB);
        }
    }

    private void TriggerNotification(GameObject notificationObject)
    {
        if (notificationObject == null) return;

        //効果音を再生
        if (audioSource != null && notificationSound != null)
        {
            audioSource.PlayOneShot(notificationSound, notificationVolume);
        }

        notificationObject.SetActive(true);

        Animator animator = notificationObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("fadein");
        }

        //指定秒数後にフェードアウトを開始するコルーチンを起動
        StartCoroutine(TriggerFadeOutAfterDelay(notificationObject, imageDisplayDuration));
    }

    private IEnumerator TriggerFadeOutAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        //フェードアウトアニメーションをトリガー
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("fadeout");
        }
        //メモ:フェードアウトのアニメーションの最後にオブジェクトを非表示にするかも
    }

    //時間切れの時の処理をまとめたメソッド
    private void TriggerTimesUp()
    {
        Debug.Log("時間切れ！ゲームオーバー。");
        SetInputEnabled(false);

        //イベントを放送する
        OnTimeUp?.Invoke();
    }

    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
    }

    public float GetCurrentDetection() { return currentTime; }
    public float GetMaxDetection() { return totalTimeInSeconds; }
}