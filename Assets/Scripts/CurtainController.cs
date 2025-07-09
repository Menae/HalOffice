using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class CurtainController : MonoBehaviour
{
    [Header("スプライト設定")]
    [Tooltip("カーテンが閉じている時のスプライト")]
    public Sprite closedSprite;
    [Tooltip("カーテンが開いている時のスプライト")]
    public Sprite openSprite;

    [Header("見つかり度設定")]
    [Tooltip("見つかり度を上げるためのイベントチャンネル")]
    public FloatEventChannelSO detectionIncreaseChannel;
    [Tooltip("通常時の見つかり度上昇量")]
    public float slowDetectionAmount = 10f;
    [Tooltip("素早く開閉した時の見つかり度上昇量")]
    public float fastDetectionAmount = 30f;
    // ★★★ 変更：ドラッグ速度から、ホイールの回転量のしきい値に変更 ★★★
    [Tooltip("「素早い」と判断されるマウスホイールの回転量のしきい値")]
    public float fastScrollThreshold = 0.2f;

    public bool IsOpen { get; private set; } = false;

    // ★★★ 追加：マウスがオブジェクト上にあるかを管理するフラグ ★★★
    private bool isMouseOver = false;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = closedSprite;
    }

    // ★★★ OnMouseDown / OnMouseUp を削除し、Updateメソッドを実装 ★★★
    private void Update()
    {
        // マウスがこのオブジェクトの上になければ、何もしない
        if (!isMouseOver) return;

        // 右マウスボタンが長押しされていなければ、何もしない
        if (!Input.GetMouseButton(1)) // 1: 右クリック
        {
            return;
        }

        // マウスホイールの回転量を取得
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        // ホイールが上に回転した場合
        if (scrollInput > 0f && !IsOpen)
        {
            OpenCurtain(scrollInput);
        }
        // ホイールが下に回転した場合
        else if (scrollInput < 0f && IsOpen)
        {
            CloseCurtain(Mathf.Abs(scrollInput)); // 速度は正の値として渡す
        }
    }

    // ★★★ 追加：マウスがオブジェクトに出入りしたことを検知する ★★★
    private void OnMouseEnter()
    {
        // マウスがオブジェクトの上に乗った
        isMouseOver = true;
    }

    private void OnMouseExit()
    {
        // マウスがオブジェクトの上から離れた
        isMouseOver = false;
    }


    // ★★★ 変更：引数を回転量(magnitude)に変更 ★★★
    private void OpenCurtain(float magnitude)
    {
        IsOpen = true;
        spriteRenderer.sprite = openSprite;
        float amount = (magnitude > fastScrollThreshold) ? fastDetectionAmount : slowDetectionAmount;
        detectionIncreaseChannel.RaiseEvent(amount);
        Debug.Log($"カーテンを開けた！ 回転量: {magnitude}, 見つかり度上昇: {amount}");
    }

    private void CloseCurtain(float magnitude)
    {
        IsOpen = false;
        spriteRenderer.sprite = closedSprite;
        float amount = (magnitude > fastScrollThreshold) ? fastDetectionAmount : slowDetectionAmount;
        detectionIncreaseChannel.RaiseEvent(amount);
        Debug.Log($"カーテンを閉めた！ 回転量: {magnitude}, 見つかり度上昇: {amount}");
    }
}