using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TrashCanController : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("蓋の開閉を制御するAnimator")]
    public Animator lidAnimator;
    [Tooltip("蓋が開くアニメーションのBoolパラメータ名")]
    public string openParameterName = "IsOpen";
    [Tooltip("このUIが属するCanvasのRender Camera（UI Camera）")]
    public Camera uiCamera;

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

        if (uiCamera == null)
        {
            // 毎フレーム警告が出ないよう、一度だけ警告し、処理を止める
            if (isDragInProgress) // ドラッグ開始時のみ
            {
                Debug.LogWarning("TrashCanControllerに 'uiCamera' が設定されていません！ Inspectorを確認してください。", this);
            }
            // uiCameraがないと蓋が開かないため、閉じておく
            lidAnimator.SetBool(openParameterName, false);
            return;
        }

        // 1. ゴミ箱のWorld座標 (rectTransform.position) を Screen座標に変換
        Vector2 trashCanScreenPosition = uiCamera.WorldToScreenPoint(rectTransform.position);

        // 2. マウスのScreen座標 (Input.mousePosition) と、
        //    変換したゴミ箱のScreen座標で距離を計算
        float distance = Vector2.Distance(trashCanScreenPosition, Input.mousePosition);

        // 3. 距離がしきい値以内かどうかをAnimatorに通知
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