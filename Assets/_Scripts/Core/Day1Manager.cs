using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class Day1Manager : MonoBehaviour
{
    [Header("ゲーム開始設定")]
    [Tooltip("操作説明などを表示するパネル")]
    public GameObject instructionPanel;
    [Tooltip("画面をフェードインさせるための黒い画像")]
    public Image screenFadeImage;
    [Tooltip("フェードインにかかる時間（秒）")]
    public float fadeInDuration = 2.0f;

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

    public EvaluationTrigger evaluationTrigger;

    // ▼▼▼ この行を修正 ▼▼▼
    // ゲームがアクティブかどうかを管理するフラグ。外部から読み取り可能にする。
    public bool isGameActive { get; private set; } = false;

    // 内部変数
    private float currentTime;
    private bool eventATriggered = false;
    private bool eventBTriggered = false;
    private AudioSource audioSource;
    private bool isTimeUp = false;

    // このシーン内でのみ有効なイベント
    public static event Action OnTimeUp;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        currentTime = totalTimeInSeconds;
        if (imageObjectA != null) imageObjectA.SetActive(false);
        if (imageObjectB != null) imageObjectB.SetActive(false);
        
        isGameActive = false;
        if (instructionPanel != null) instructionPanel.SetActive(true);
        if (screenFadeImage != null)
        {
            screenFadeImage.gameObject.SetActive(true);
            screenFadeImage.color = Color.black;
            StartCoroutine(SceneStartSequence());
        }
        else
        {
            StartGame();
        }
    }

    void Update()
    {
        Debug.Log($"Update running. isGameActive={isGameActive}, Time.timeScale={Time.timeScale}");

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
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            screenFadeImage.color = Color.Lerp(Color.black, Color.clear, timer / fadeInDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        screenFadeImage.color = Color.clear;
        screenFadeImage.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        Debug.Log("StartGame() has been called! isGameActive will be set to true."); // ← この行を追加
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
    }
    
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

    private void TriggerTimesUp()
    {
        // if (isTimeUp) return; // ← この行を削除またはコメントアウト
        isTimeUp = true;
        Debug.Log("時間切れ！");

        evaluationTrigger.EndDayAndEvaluate();
    }

    /// <summary>
    /// 調査を終了し、次のシーンに遷移する
    /// </summary>
    /// <param name="nextSceneName">次にロードするシーンの名前</param>
    public void EndInvestigation(string nextSceneName)
    {
        // 1. InvestigationManagerに、集めた証拠をGameManagerに渡すよう命令
        if (InvestigationManager.Instance != null)
        {
            InvestigationManager.Instance.PassCluesToGameManager();
        }
        else
        {
            Debug.LogError("InvestigationManagerが見つかりません！証拠の引き継ぎに失敗しました。");
        }

        // 2. 次のシーンをロードする
        //    （もしフェードアウトなどの演出が必要なら、ここにその処理を挟む）
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    public float GetCurrentTime() { return currentTime; }
    public float GetTotalTime() { return totalTimeInSeconds; }
}