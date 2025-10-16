using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.RawImage))]
public class InputBridge : MonoBehaviour, IPointerDownHandler
{
    [Header("判定設定")]
    [Tooltip("ドラッグ対象として判定する物理レイヤー")]
    public LayerMask draggableLayer; // インスペクターから"Draggable"レイヤーを選択

    // OnDragとOnPointerUpは不要になったので削除

    // クリックされた瞬間に呼び出される
    public void OnPointerDown(PointerEventData eventData)
    {
        // 司令塔に、物理的なraycastでオブジェクトを探してドラッグを開始するように命令
        DragDropManager.Instance.StartDragFromInputBridge(eventData, draggableLayer);
    }
}