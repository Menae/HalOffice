using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TrashCanController : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("蓋の開閉を制御するAnimator")]
    public Animator lidAnimator;
    [Tooltip("蓋が開くアニメーションのBoolパラメータ名")]
    public string openParameterName = "IsOpen";

    [Header("設定")]
    [Tooltip("カーソルがこの距離（ピクセル）以内に近づいたら蓋を開く")]
    public float detectionRadius = 150f;

    private RectTransform rectTransform;
    private bool isDragInProgress = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // DragDropManagerからのイベントを購読
        DragDropManager.OnDragStarted += HandleDragStart;
        DragDropManager.OnDragEnded += HandleDragEnd;
    }

    private void OnDisable()
    {
        // 購読を解除
        DragDropManager.OnDragStarted -= HandleDragStart;
        DragDropManager.OnDragEnded -= HandleDragEnd;
    }

    private void HandleDragStart()
    {
        isDragInProgress = true;
    }

    private void HandleDragEnd()
    {
        isDragInProgress = false;
        // ドラッグが終わったら必ず蓋を閉じる
        if (lidAnimator != null)
        {
            lidAnimator.SetBool(openParameterName, false);
        }
    }

    private void Update()
    {
        // ドラッグ中でないなら、何もしない（高負荷な処理を避ける）
        if (!isDragInProgress || lidAnimator == null)
        {
            return;
        }

        // ゴミ箱のUI座標と、マウスのスクリーン座標の距離を計算
        float distance = Vector2.Distance(rectTransform.position, Input.mousePosition);

        // 距離がしきい値以内かどうかをAnimatorに通知
        if (distance < detectionRadius)
        {
            lidAnimator.SetBool(openParameterName, true);
        }
        else
        {
            lidAnimator.SetBool(openParameterName, false);
        }
    }
}