using UnityEngine;
using System.Collections.Generic; // Queueを使うために必要

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class CurtainController : MonoBehaviour
{
    [Header("スプライト設定")]
    public Sprite closedSprite;
    public Sprite openSprite;

    [Header("見つかり度設定")]
    public FloatEventChannelSO detectionIncreaseChannel;
    public float slowDetectionAmount = 10f;
    public float fastDetectionAmount = 30f;
    [Tooltip("「素早い」と判断される、短時間での合計回転量のしきい値")]
    public float fastScrollThreshold = 1.0f; // ★★★ しきい値の考え方が変わったため、大きな値がおすすめ ★★★

    public bool IsOpen { get; private set; } = false;

    private bool isMouseOver = false;
    private SpriteRenderer spriteRenderer;

    // ★★★ 追加：ホイールの回転量を記憶するための変数 ★★★
    private Queue<float> scrollHistory = new Queue<float>();
    private const int frameHistoryCount = 20; // 何フレーム分の履歴を記憶するか

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = closedSprite;
    }

    // ★★★ このメソッドのロジックを、あなたの提案通りに刷新 ★★★
    private void Update()
    {
        // --- 1. 毎フレーム、最新の回転量を記憶し、古いものを忘れる ---
        scrollHistory.Enqueue(Input.GetAxis("Mouse ScrollWheel"));
        if (scrollHistory.Count > frameHistoryCount)
        {
            scrollHistory.Dequeue();
        }

        // --- 2. 操作の前提条件をチェック ---
        // マウスが上になければ何もしない
        if (!isMouseOver) return;
        // 左クリックが押されていなければ何もしない
        if (!Input.GetMouseButton(0)) return;

        // --- 3. このフレームでホイールが動いたかチェック ---
        float currentFrameScroll = scrollHistory.Peek(); // 最後に追加された値（今のフレームの回転量）
        if (currentFrameScroll == 0f) return;

        // --- 4. 条件を満たしていたら、速さを計算してアクションを実行 ---
        // 記憶している直近の回転量を合計する
        float recentScrollSum = 0f;
        foreach (float scrollValue in scrollHistory)
        {
            recentScrollSum += scrollValue;
        }

        // ホイールが上に回転した場合
        if (currentFrameScroll > 0f && !IsOpen)
        {
            OpenCurtain(Mathf.Abs(recentScrollSum));
        }
        // ホイールが下に回転した場合
        else if (currentFrameScroll < 0f && IsOpen)
        {
            CloseCurtain(Mathf.Abs(recentScrollSum));
        }
    }

    private void OnMouseEnter()
    {
        isMouseOver = true;
    }

    private void OnMouseExit()
    {
        isMouseOver = false;
    }

    private void OpenCurtain(float scrollSum)
    {
        IsOpen = true;
        spriteRenderer.sprite = openSprite;
        float amount = (scrollSum > fastScrollThreshold) ? fastDetectionAmount : slowDetectionAmount;
        if (detectionIncreaseChannel != null)
        {
            detectionIncreaseChannel.RaiseEvent(amount);
        }
        Debug.Log($"カーテンを開けた！ 直近の回転量合計: {scrollSum}, 見つかり度上昇: {amount}");
    }

    private void CloseCurtain(float scrollSum)
    {
        IsOpen = false;
        spriteRenderer.sprite = closedSprite;
        float amount = (scrollSum > fastScrollThreshold) ? fastDetectionAmount : slowDetectionAmount;
        if (detectionIncreaseChannel != null)
        {
            detectionIncreaseChannel.RaiseEvent(amount);
        }
        Debug.Log($"カーテンを閉めた！ 直近の回転量合計: {scrollSum}, 見つかり度上昇: {amount}");
    }
}