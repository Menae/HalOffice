using UnityEngine;

[RequireComponent(typeof(RectTransform))]
/// <summary>
/// ゴミ箱UIの蓋の開閉を管理。
/// ドラッグ中のカーソル位置に基づき、AnimatorのBoolパラメータを更新して蓋の開閉を制御。
/// </summary>
public class TrashCanController : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("蓋の開閉を制御するAnimator")]
    /// <summary>
    /// 蓋の開閉を制御するAnimator。InspectorでD&D。
    /// UpdateやHandleDragEnd内でnullチェックを行うため、未設定時は安全に無処理。
    /// </summary>
    public Animator lidAnimator;

    [Tooltip("蓋が開くアニメーションのBoolパラメータ名")]
    /// <summary>
    /// 開閉状態を示すAnimatorのBoolパラメータ名。Inspectorで設定。
    /// Animator側のパラメータ名と一致させること。
    /// </summary>
    public string openParameterName = "IsOpen";

    [Tooltip("このUIが属するCanvasのRender Camera（UI Camera）")]
    /// <summary>
    /// このUIが属するCanvasのRender Camera（UI Camera）。InspectorでD&D。
    /// Screen座標変換に使用。未設定時は警告を出し蓋を閉じる。
    /// </summary>
    public Camera uiCamera;

    [Header("設定")]
    [Tooltip("カーソルがこの距離（ピクセル）以内に近づいたら蓋を開く")]
    /// <summary>
    /// カーソルがこの距離（ピクセル）以内に近づいたら蓋を開くしきい値。
    /// UIはスクリーン座標で判定するため、ピクセル単位で直感的な調整が可能。
    /// </summary>
    public float detectionRadius = 150f;

    /// <summary>
    /// RectTransformのキャッシュ。Awakeで初期化。
    /// 毎フレームのGetComponent呼び出しを回避してパフォーマンスを確保。
    /// </summary>
    private RectTransform rectTransform;

    /// <summary>
    /// ドラッグ中フラグ。DragDropManagerのイベントで更新。
    /// ドラッグ中のみ近接判定を行い不要な処理を抑制。
    /// </summary>
    private bool isDragInProgress = false;

    /// <summary>
    /// Unity Awake。インスタンス生成直後に呼ばれ、依存コンポーネントをキャッシュ。
    /// Startより先に実行されるため初期化順序の依存対策に利用。
    /// </summary>
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Unity OnEnable。コンポーネント有効化時にDragDropManagerのイベント購読を行う。
    /// イベント購読はOnDisableで解除することでメモリリークを防ぐ。
    /// </summary>
    private void OnEnable()
    {
        // DragDropManagerからのイベントを購読
        DragDropManager.OnDragStarted += HandleDragStart;
        DragDropManager.OnDragEnded += HandleDragEnd;
    }

    /// <summary>
    /// Unity OnDisable。コンポーネント無効化時にイベント購読を解除。
    /// 無効化時のコールバック残留を防止。
    /// </summary>
    private void OnDisable()
    {
        // 購読を解除
        DragDropManager.OnDragStarted -= HandleDragStart;
        DragDropManager.OnDragEnded -= HandleDragEnd;
    }

    /// <summary>
    /// ドラッグ開始イベントハンドラ。フラグを設定してUpdateでの近接判定を有効化。
    /// </summary>
    private void HandleDragStart()
    {
        isDragInProgress = true;
    }

    /// <summary>
    /// ドラッグ終了イベントハンドラ。フラグを解除し、蓋を確実に閉じる。
    /// lidAnimatorがnullの場合は何もしない。
    /// </summary>
    private void HandleDragEnd()
    {
        isDragInProgress = false;
        // ドラッグが終わったら必ず蓋を閉じる
        if (lidAnimator != null)
        {
            lidAnimator.SetBool(openParameterName, false);
        }
    }

    /// <summary>
    /// Unity Update。毎フレーム呼ばれ、ドラッグ中のみ実行してパフォーマンスを保つ。
    /// uiCameraが未設定の場合は一度だけ警告を出し蓋を閉じる。ゴミ箱とマウスのスクリーン座標間の距離で開閉を判定。
    /// </summary>
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