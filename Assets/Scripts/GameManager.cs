using System;
using UnityEngine;
using UnityEngine.UI; // UIコンポーネントを使うために必要
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isInputEnabled { get; private set; } = true;

    // ▼▼▼ 追加 ▼▼▼
    [Header("ゲーム開始設定")]
    [Tooltip("操作説明などを表示するパネル")]
    public GameObject instructionPanel;
    [Tooltip("画面をフェードインさせるための黒い画像")]
    public Image screenFadeImage;
    [Tooltip("フェードインにかかる時間（秒）")]
    public float fadeInDuration = 2.0f;

    // ゲームがアクティブかどうかを管理するフラグ
    public bool isGameActive { get; private set; } = false;
    // ▲▲▲ ここまで ▲▲▲

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

    [Header("通知エフェクト設定")]
    [Tooltip("通知が表示される時の効果音")]
    public AudioClip notificationSound;
    [Range(0f, 1f)]
    [Tooltip("通知効果音の音量")]
    public float notificationVolume = 1.0f;

    // ... (他の変数はそのまま) ...
    private float currentTime;
    private bool eventATriggered = false;
    private bool eventBTriggered = false;
    private AudioSource audioSource;
    private bool isTimeUp = false;

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

        // ▼▼▼ 修正 ▼▼▼
        // ゲーム開始時の初期化処理
        isGameActive = false; // ゲームを非アクティブ状態で開始
        if (instructionPanel != null) instructionPanel.SetActive(true);
        if (screenFadeImage != null)
        {
            screenFadeImage.gameObject.SetActive(true);
            screenFadeImage.color = Color.black; // 完全な黒から開始
            StartCoroutine(SceneStartSequence()); // フェードイン処理を開始
        }
        else
        {
            // フェードがない場合は、即座にゲームを開始する
            StartGame();
        }
    }


    void Update()
    {
        // ▼▼▼ 修正 ▼▼▼
        // ゲームがアクティブになるまでタイマーを進めない
        if (!isGameActive || isTimeUp) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
        }
        else
        {
            currentTime = 0;
            isTimeUp = true;
            TriggerTimesUp();
        }
        CheckTimedEvents();
    }

    private IEnumerator SceneStartSequence()
    {
        // フェードイン処理
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            // 徐々に透明にする
            screenFadeImage.color = Color.Lerp(Color.black, Color.clear, timer / fadeInDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        screenFadeImage.color = Color.clear; // 完全に透明にする
        screenFadeImage.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        isGameActive = true;
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
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

        // ▼▼▼ 変更点1: コルーチンの呼び出しを削除 ▼▼▼
        // StartCoroutine(TriggerFadeOutAfterDelay(notificationObject, imageDisplayDuration));
    }

    // ▼▼▼ 変更点2: 不要になったコルーチンを削除 ▼▼▼
    /*
    private IEnumerator TriggerFadeOutAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("fadeout");
        }
    }
    */

    // ▼▼▼ 変更点3: ボタンから呼び出すためのメソッドを追加 ▼▼▼
    public void DismissNotificationA()
    {
        TriggerFadeOut(imageObjectA);
    }

    public void DismissNotificationB()
    {
        TriggerFadeOut(imageObjectB);
    }

    private void TriggerFadeOut(GameObject obj)
    {
        if (obj == null) return;
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("fadeout");
        }
    }
    // ▲▲▲ ここまでが変更点3 ▲▲▲

    private void TriggerTimesUp()
    {
        Debug.Log("時間切れ！ゲームオーバー。");
        SetInputEnabled(false);
        OnTimeUp?.Invoke();
    }

    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
    }

    public float GetCurrentTime() { return currentTime; } // メソッド名を修正
    public float GetTotalTime() { return totalTimeInSeconds; } // メソッド名を修正
}