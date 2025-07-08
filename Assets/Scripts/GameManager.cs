using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // --- シングルトンと入力管理 ---
    public static GameManager Instance { get; private set; }
    public bool isInputEnabled { get; private set; } = true;

    // --- タイマー設定 ---
    [Header("タイマー設定")]
    [Tooltip("カウントダウンの開始時間（秒）")]
    public float totalTimeInSeconds = 300f;
    // ▼▼▼ 削除：テキスト表示用の変数を削除しました ▼▼▼
    // public TextMeshProUGUI timerText;

    // --- 時間経過イベント ---
    [Header("時間経過イベント")]
    [Tooltip("この残り秒数になったら、オブジェクトAを表示する")]
    public float eventA_TriggerTime = 120f;
    public GameObject imageObjectA;
    [Tooltip("この残り秒数になったら、オブジェクトBを表示する")]
    public float eventB_TriggerTime = 60f;
    public GameObject imageObjectB;
    [Tooltip("イベントで表示した画像が消えるまでの時間（秒）")]
    public float imageDisplayDuration = 5.0f;

    // --- 内部で使う変数 ---
    private float currentTime;
    private bool eventATriggered = false;
    private bool eventBTriggered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        currentTime = totalTimeInSeconds;
        if (imageObjectA != null) imageObjectA.SetActive(false);
        if (imageObjectB != null) imageObjectB.SetActive(false);
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
        }
        else
        {
            currentTime = 0;
        }

        // ▼▼▼ 削除：テキスト表示の呼び出しを削除しました ▼▼▼
        // UpdateTimerDisplay();
        CheckTimedEvents();
    }

    // ▼▼▼ 削除：テキスト表示用のメソッドを丸ごと削除しました ▼▼▼
    // private void UpdateTimerDisplay() { ... }

    private void CheckTimedEvents()
    {
        // 3分経過イベント
        if (!eventATriggered && currentTime <= eventA_TriggerTime)
        {
            eventATriggered = true;
            if (imageObjectA != null)
            {
                imageObjectA.SetActive(true);
                StartCoroutine(HideObjectAfterDelay(imageObjectA, imageDisplayDuration));
            }
        }

        // 4分経過イベント
        if (!eventBTriggered && currentTime <= eventB_TriggerTime)
        {
            eventBTriggered = true;
            if (imageObjectB != null)
            {
                imageObjectB.SetActive(true);
                StartCoroutine(HideObjectAfterDelay(imageObjectB, imageDisplayDuration));
            }
        }
    }

    private IEnumerator HideObjectAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
    }

    // --- 外部から現在の時間を読み取るための関数（変更なし） ---
    public float GetCurrentDetection() { return currentTime; } // 名前はDetectionのままですが機能します
    public float GetMaxDetection() { return totalTimeInSeconds; } // 名前はDetectionのままですが機能します
}