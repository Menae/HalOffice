using UnityEngine;

public class DragDropManager : MonoBehaviour
{
    // シングルトンとして実装
    public static DragDropManager Instance { get; private set; }

    [Header("参照")]
    [Tooltip("座標変換を行うScreenToWorldConverter")]
    public ScreenToWorldConverter screenToWorldConverter;

    // --- 内部変数 ---
    private Draggable currentDraggedObject; // 現在ドラッグ中のオブジェクト
    private Vector3 originalPosition;       // ドラッグ開始時の元の位置
    private ObjectSlot originalSlot;        // ドラッグ開始時の元のスロット

    private void Awake()
    {
        // シングルトンの設定
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        // もしオブジェクトをドラッグ中なら
        if (currentDraggedObject != null)
        {
            // マウスのスクリーン座標をゲーム世界の座標に変換
            if (screenToWorldConverter.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
            {
                // ドラッグ中のオブジェクトをマウスに追従させる
                currentDraggedObject.transform.position = worldPos;
            }

            // マウスの左ボタンを離したら、ドラッグを終了する
            if (Input.GetMouseButtonUp(0))
            {
                StopDrag();
            }
        }
    }

    /// <summary>
    /// Draggableオブジェクトから呼び出され、ドラッグを開始する
    /// </summary>
    public void StartDrag(Draggable draggable)
    {
        currentDraggedObject = draggable;
        originalPosition = draggable.transform.position;

        // オブジェクトが元々どのスロットにあったかを探して記憶する
        originalSlot = ObjectSlotManager.Instance.FindSlotForDraggable(draggable);
        if (originalSlot != null)
        {
            // スロットから一時的にオブジェクトを取り除く
            originalSlot.currentObject = null;
        }

        Debug.Log($"{draggable.name} のドラッグを開始しました。");
    }

    /// <summary>
    /// ドラッグを終了する（現時点では元の位置に戻すだけ）
    /// </summary>
    private void StopDrag()
    {
        if (currentDraggedObject == null) return;

        // ★★★ 将来的には、ここにドロップゾーンの判定ロジックが入る ★★★

        // 現時点では、単純に元の位置に戻す
        if (originalSlot != null)
        {
            // 元のスロットに戻す
            currentDraggedObject.transform.position = originalSlot.slotTransform.position;
            originalSlot.currentObject = currentDraggedObject;
        }
        else
        {
            // もしスロットに所属していなかった場合は、元の座標に戻す
            currentDraggedObject.transform.position = originalPosition;
        }

        Debug.Log($"{currentDraggedObject.name} のドラッグを終了しました。");

        // ドラッグ状態を解除
        currentDraggedObject = null;
        originalSlot = null;
    }
}