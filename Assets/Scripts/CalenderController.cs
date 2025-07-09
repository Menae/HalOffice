using UnityEngine;
using System.Collections.Generic; // Queueを使うために必要

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class CalendarController : MonoBehaviour
{
    [Header("スプライト設定")]
    [Tooltip("めくる前のカレンダーのスプライト")]
    public Sprite initialSprite;
    [Tooltip("めくった後のカレンダーのスプライト")]
    public Sprite turnedPageSprite;

    // ★★★ ここからが追加部分 ★★★
    [Header("見つかり度設定")]
    [Tooltip("見つかり度を上げるためのイベントチャンネル")]
    public FloatEventChannelSO detectionIncreaseChannel;
    [Tooltip("通常時の見つかり度上昇量")]
    public float slowDetectionAmount = 10f;
    [Tooltip("素早くめくった時の見つかり度上昇量")]
    public float fastDetectionAmount = 30f;
    [Tooltip("「素早い」と判断される、短時間での合計回転量のしきい値")]
    public float fastScrollThreshold = 1.0f;
    // ★★★ ここまで追加 ★★★

    public bool IsPageTurned { get; private set; } = false;
    private bool isMouseOver = false;
    private SpriteRenderer spriteRenderer;

    // ホイールの回転量を記憶するための変数
    private Queue<float> scrollHistory = new Queue<float>();
    private const int frameHistoryCount = 10; // 10フレーム分の履歴を記憶する

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = initialSprite;
    }

    private void Update()
    {
        // 1. 毎フレーム、最新の回転量を記憶し、古いものを忘れる
        scrollHistory.Enqueue(Input.GetAxis("Mouse ScrollWheel"));
        if (scrollHistory.Count > frameHistoryCount)
        {
            scrollHistory.Dequeue();
        }

        // 2. 操作の前提条件をチェック
        if (!isMouseOver || !Input.GetMouseButton(1) || IsPageTurned)
        {
            return;
        }

        // 3. このフレームでホイールが動いたかチェック
        // Peek()は最後の要素を見るだけなので、最新のフレームの値を取得
        float currentFrameScroll = scrollHistory.Peek();
        if (currentFrameScroll == 0f) return;

        // 4. 条件を満たしていたら、速さを計算してページをめくる
        float recentScrollSum = 0f;
        foreach (float scrollValue in scrollHistory)
        {
            recentScrollSum += scrollValue;
        }

        // どちらかの方向に回ったら、合計の勢いでページをめくる
        TurnPage(Mathf.Abs(recentScrollSum));
    }

    private void OnMouseEnter()
    {
        isMouseOver = true;
    }

    private void OnMouseExit()
    {
        isMouseOver = false;
    }

    // ★★★ このメソッドを修正 ★★★
    private void TurnPage(float scrollSum)
    {
        // 一度しかめくれないようにする
        if (IsPageTurned) return;

        IsPageTurned = true;
        spriteRenderer.sprite = turnedPageSprite;

        // 勢いに応じて見つかり度を決定
        float amount = (scrollSum > fastScrollThreshold) ? fastDetectionAmount : slowDetectionAmount;

        // イベントを発行
        if (detectionIncreaseChannel != null)
        {
            detectionIncreaseChannel.RaiseEvent(amount);
        }
        Debug.Log($"カレンダーをめくった！ 直近の回転量合計: {scrollSum}, 見つかり度上昇: {amount}");
    }
}