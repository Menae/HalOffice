using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// デイ1用のシーン進行と時間管理を行うマネージャー。
/// </summary>
/// <remarks>
/// ・シーン開始時にフェードインを実行し、チュートリアル開始を委譲する。  
/// ・カウントダウンタイマーと時間経過トリガー（通知表示）を管理する。  
/// ・タイムアップ時にシーン内の終了イベントを発行する。  
/// Inspectorへの参照設定必須のフィールドあり（nullチェックは一部のみ）。
/// </remarks>
public class Day1Manager : MonoBehaviour
{
    [Header("ゲーム開始設定")]
    [Tooltip("画面をフェードインさせるための黒い画像 (InspectorでD&D)")]
    public Image screenFadeImage;
    [Tooltip("フェードインにかかる時間（秒）")]
    public float fadeInDuration = 2.0f;

    [Header("チュートリアル設定")]
    [Tooltip("チュートリアル完了後に表示する『ゲーム開始ボタン』 (InspectorでD&D)")]
    public Button startGameButton;

    [Header("タイマー設定")]
    [Tooltip("カウントダウンの開始時間（秒）")]
    public float totalTimeInSeconds = 300f;

    [Header("時間経過イベント")]
    [Tooltip("この残り秒数になったら、オブジェクトAを表示 (InspectorでD&D)")]
    public float eventA_TriggerTime = 120f;
    public GameObject imageObjectA;
    [Tooltip("この残り秒数になったら、オブジェクトBを表示 (InspectorでD&D)")]
    public float eventB_TriggerTime = 60f;
    public GameObject imageObjectB;

    [Header("通知エフェクト設定")]
    [Tooltip("通知が表示される時の効果音")]
    public AudioClip notificationSound;
    [Range(0f, 1f)]
    [Tooltip("通知効果音の音量")]
    public float notificationVolume = 1.0f;

    [Tooltip("評価処理を開始するトリガー（InspectorでD&D）")]
    public EvaluationTrigger evaluationTrigger;
    [Tooltip("チュートリアル管理クラス（InspectorでD&D）")]
    public TutorialManager tutorialManager;

    /// <summary>
    /// ゲームがアクティブかどうかを外部から読み取るプロパティ。
    /// ゲーム開始後にtrueとなり、Updateによるタイマー進行を許可する。
    /// </summary>
    public bool isGameActive { get; private set; } = false;

    // 内部状態管理
    private float currentTime;
    private bool eventATriggered = false;
    private bool eventBTriggered = false;
    private AudioSource audioSource;
    private bool isTimeUp = false;

    /// <summary>
    /// このシーン内での時間切れイベント。購読者は場面遷移や終了処理を登録する。
    /// </summary>
    public static event Action OnTimeUp;

    /// <summary>
    /// Unity Start。シーン初期化処理を行う。
    /// 呼び出しタイミング: MonoBehaviour.Start（Awakeの後、最初のフレームの直前）。
    /// 初期化内容: AudioSource取得、タイマー初期化、通知オブジェクト非表示、フェード開始またはチュートリアル開始。
    /// 注意: GameManagerやTutorialManagerが未設定だと一部の動作をスキップする。
    /// </summary>
    void Start()
    {
        if (GameManager.Instance != null)
        {
            // チュートリアル開始前は入力を無効化しておく
            GameManager.Instance.SetInputEnabled(false);
        }

        audioSource = GetComponent<AudioSource>();
        currentTime = totalTimeInSeconds;

        // 通知オブジェクトを事前に非表示にする（null許容）
        if (imageObjectA != null) imageObjectA.SetActive(false);
        if (imageObjectB != null) imageObjectB.SetActive(false);

        isGameActive = false;

        if (screenFadeImage != null)
        {
            screenFadeImage.gameObject.SetActive(true);
            screenFadeImage.color = Color.black;
            StartCoroutine(SceneStartSequence());
        }
        else
        {
            // フェードイメージが設定されていない場合は直接チュートリアル開始
            if (tutorialManager != null)
            {
                tutorialManager.StartTutorial();
            }
            else
            {
                Debug.LogError("TutorialManagerが設定されていません！");
            }
        }
    }

    /// <summary>
    /// Unity Update。フレームごとのタイマー進行と時間トリガーチェックを行う。
    /// 呼び出しタイミング: 毎フレーム。isGameActiveがfalse、または既に時間切れなら処理を行わない。
    /// 副作用: currentTimeを減算し、残り時間に応じて通知を発火する。
    /// </summary>
    void Update()
    {
        if (!isGameActive || isTimeUp) return;

        if (currentTime > 0f)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                TriggerTimesUp();
            }
        }
        else
        {
            // 保険措置: 既に0以下の場合は時間切れ処理を確実に呼ぶ
            TriggerTimesUp();
        }

        CheckTimedEvents();
    }

    /// <summary>
    /// シーン開始時のフェードインとチュートリアル開始を行うコルーチン。
    /// </summary>
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

        if (tutorialManager != null)
        {
            tutorialManager.StartTutorial();
        }
        else
        {
            Debug.LogError("TutorialManagerが設定されていません！");
        }
    }

    /// <summary>
    /// ゲーム開始ボタンから呼ばれる。プレイヤー入力を解放し、ゲームをアクティブ化する。
    /// 副作用: GameManagerの入力許可をtrueに設定、startGameButtonを非表示にする。
    /// </summary>
    public void StartGame()
    {
        Debug.Log("StartGame() has been called! isGameActive will be set to true.");
        isGameActive = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInputEnabled(true);
        }

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// チュートリアル終了時に呼び出す。ゲーム開始ボタンを表示する。
    /// 副作用: startGameButtonをアクティブにする。未設定時はエラーログ出力。
    /// </summary>
    public void ShowStartGameButton()
    {
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(true);
            return;
        }
        Debug.LogError("StartGameButtonがDay1Managerに設定されていません！");
    }

    /// <summary>
    /// 残り時間に応じて発火する通知イベントをチェックする。
    /// 副作用: トリガー済みフラグを立て、各通知を表示する。
    /// </summary>
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

    /// <summary>
    /// 通知オブジェクトを表示し、効果音とアニメーションを再生する。
    /// 副作用: notificationObjectをActiveにし、Animatorに"fadein"トリガーを送る。
    /// </summary>
    /// <param name="notificationObject">表示対象のGameObject。nullなら無処理。</param>
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

    /// <summary>
    /// 通知Aを閉じる。UIボタン等から呼び出す。
    /// </summary>
    public void DismissNotificationA()
    {
        TriggerFadeOut(imageObjectA);
    }

    /// <summary>
    /// 通知Bを閉じる。UIボタン等から呼び出す。
    /// </summary>
    public void DismissNotificationB()
    {
        TriggerFadeOut(imageObjectB);
    }

    /// <summary>
    /// 指定オブジェクトのAnimatorに"fadeout"トリガーを送る。nullチェックあり。
    /// </summary>
    /// <param name="obj">フェードアウトさせるオブジェクト</param>
    private void TriggerFadeOut(GameObject obj)
    {
        if (obj == null) return;
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("fadeout");
        }
    }

    /// <summary>
    /// 時間切れ処理を実行する。デスクトップUI非表示、ゲーム停止、OnTimeUp発行を行う。
    /// 副作用: isGameActiveをfalseにし、OnTimeUpイベントを発火する。複数回実行されないようガードあり。
    /// </summary>
    private void TriggerTimesUp()
    {
        if (isTimeUp) return;

        isTimeUp = true;

        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.SetDesktopUIVisibility(false);
        }

        Debug.Log("時間切れ！");
        isGameActive = false;

        OnTimeUp?.Invoke();

        // 評価処理はコメントアウト済み。必要に応じてevaluationTriggerを使用する。
        // evaluationTrigger.EndDayAndEvaluate();
    }

    /// <summary>
    /// 調査を終了し、次のシーンへ遷移する。ボタンのOnClick等から呼び出す想定。
    /// 副作用:
    ///  - InvestigationManagerへ収集証拠の伝達を行う（失敗時はエラーログ）。
    ///  - 指定したシーン名をロードする。
    /// </summary>
    /// <param name="nextSceneName">次にロードするシーンの名前（空文字は許容しない）</param>
    public void EndInvestigation(string nextSceneName)
    {
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.SetDesktopUIVisibility(false);
        }

        if (InvestigationManager.Instance != null)
        {
            InvestigationManager.Instance.PassCluesToGameManager();
        }
        else
        {
            Debug.LogError("InvestigationManagerが見つかりません！証拠の引き継ぎに失敗しました。");
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// 現在の残り時間を取得する。
    /// </summary>
    /// <returns>残り秒数（float）</returns>
    public float GetCurrentTime() { return currentTime; }

    /// <summary>
    /// 設定された合計時間を取得する。
    /// </summary>
    /// <returns>合計秒数（float）</returns>
    public float GetTotalTime() { return totalTimeInSeconds; }
}