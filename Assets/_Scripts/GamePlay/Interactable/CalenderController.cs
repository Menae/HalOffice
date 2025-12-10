using System.Collections.Generic; // Queueを使うために必要
using UnityEngine;

/// <summary>
/// カレンダーの表示とめくり操作を管理。マウスのホイール操作と左クリックの組み合わせでページをめくり、
/// 見つかり度の増加イベントを発行する。
/// </summary>
/// <remarks>
/// MonoBehaviour。__Awake__でスプライトを初期化。__Update__は毎フレーム実行され、マウス位置の判定とスクロール履歴の蓄積を行う。
/// ScreenToWorldConverterが利用可能でない場合はマウス位置に基づく判定を行わない。イベント発行はdetectionIncreaseChannelがnullでない場合のみ実施。
/// </remarks>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class CalendarController : MonoBehaviour
{
    [Header("スプライト設定")]
    [Tooltip("めくる前のカレンダーのスプライト")]
    public Sprite initialSprite;
    [Tooltip("めくった後のカレンダーのスプライト")]
    public Sprite turnedPageSprite;

    [Header("見つかり度設定")]
    [Tooltip("見つかり度を上げるためのイベントチャンネル")]
    public FloatEventChannelSO detectionIncreaseChannel;
    [Tooltip("通常時の見つかり度上昇量")]
    public float slowDetectionAmount = 10f;
    [Tooltip("素早くめくった時の見つかり度上昇量")]
    public float fastDetectionAmount = 30f;
    [Tooltip("「素早い」と判断される、短時間での合計回転量のしきい値")]
    public float fastScrollThreshold = 1.0f;

    /// <summary>
    /// ページが既にめくられているかを示す。外部からは読み取りのみ可能、内部でのみ更新。
    /// </summary>
    public bool IsPageTurned { get; private set; } = false;

    /// <summary>
    /// 毎フレームのマウスがこのオブジェクト上にあるかを示すフラグ。
    /// Updateで再評価されるため一時的な値。
    /// </summary>
    private bool isMouseOver = false;

    /// <summary>
    /// このコンポーネントがアタッチされたSpriteRendererへの参照。__Awake__で取得。
    /// nullチェックは行っていない前提（RequireComponentにより保証）。
    /// </summary>
    private SpriteRenderer spriteRenderer;

    // ホイールの回転量を記憶するための変数
    private Queue<float> scrollHistory = new Queue<float>();
    private const int frameHistoryCount = 10; // 10フレーム分の履歴を記憶

    /// <summary>
    /// Unityの初期化処理。Startより先に呼ばれる可能性があるため、スプライト初期化など早期初期化をここで行う。
    /// SpriteRendererの取得と初期スプライト設定を実行。
    /// </summary>
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = initialSprite;
    }

    /// <summary>
    /// 毎フレーム呼ばれる。マウス位置判定、スクロール履歴の蓄積、左クリック中の操作判定を行う。
    /// ScreenToWorldConverterがnullのときはマウスオーバー判定をスキップする。
    /// </summary>
    private void Update()
    {
        // ▼▼▼ 以下の判定ロジックをUpdateの冒頭に追加 ▼▼▼
        // ScreenToWorldConverterを使って、マウス下のオブジェクトを特定する
        isMouseOver = false; // 毎フレーム、一旦falseにリセット
        if (ScreenToWorldConverter.Instance != null &&
            ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                isMouseOver = true; // カーソルが自身の上にあればtrueにする
            }
        }

        // 毎フレーム最新の回転量を記憶し、古いものを忘れる
        scrollHistory.Enqueue(Input.GetAxis("Mouse ScrollWheel"));
        if (scrollHistory.Count > frameHistoryCount)
        {
            scrollHistory.Dequeue();
        }

        // 操作の前提条件をチェック（マウスオーバー・左クリック・未めくり）
        if (!isMouseOver || !Input.GetMouseButton(0) || IsPageTurned)
        {
            return;
        }

        // このフレームでホイールが動いたかチェック
        // Peek()は最後の要素を見るだけなので、最新のフレームの値を取得
        float currentFrameScroll = scrollHistory.Peek();
        if (currentFrameScroll == 0f) return;

        // 条件を満たしていたら、速さを計算してページをめくる
        float recentScrollSum = 0f;
        foreach (float scrollValue in scrollHistory)
        {
            recentScrollSum += scrollValue;
        }

        // どちらかの方向に回ったら、合計の勢いでページをめくる
        TurnPage(Mathf.Abs(recentScrollSum));
    }

    /// <summary>
    /// ページをめくり、表示スプライトを切り替え、見つかり度増加イベントを発行する。
    /// 既にめくられている場合は何もしない。
    /// </summary>
    /// <param name="scrollSum">直近フレームのスクロール量合計。絶対値で速度判定を行う。</param>
    private void TurnPage(float scrollSum)
    {
        // 一度しかめくれないようにする
        if (IsPageTurned) return;

        IsPageTurned = true;
        spriteRenderer.sprite = turnedPageSprite;

        // 勢いに応じて見つかり度を決定
        float amount = (scrollSum > fastScrollThreshold) ? fastDetectionAmount : slowDetectionAmount;

        // イベントを発行（チャンネルが割り当てられていない場合は発行しない）
        if (detectionIncreaseChannel != null)
        {
            detectionIncreaseChannel.RaiseEvent(amount);
        }
        Debug.Log($"カレンダーをめくった！ 直近の回転量合計: {scrollSum}, 見つかり度上昇: {amount}");
    }
}