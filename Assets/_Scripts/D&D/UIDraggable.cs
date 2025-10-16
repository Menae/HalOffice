using UnityEngine;
using UnityEngine.EventSystems;

// このスクリプトはUIのImageにアタッチすることを想定
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class UIDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("設定")]
    [Tooltip("このUIアイコンをドラッグした際に、ゲーム世界に生成されるアイテムのプレハブ")]
    public GameObject itemPrefab;

    // ドラッグが開始された瞬間に呼び出される
    public void OnBeginDrag(PointerEventData eventData)
    {
        // プレハブが設定されていなければ何もしない
        if (itemPrefab == null)
        {
            Debug.LogError("UIDraggableにitemPrefabが設定されていません！", this.gameObject);
            return;
        }

        // 司令塔に、UIからのドラッグ開始を通知する
        DragDropManager.Instance.StartDragFromUI(this, eventData);
    }

    // ドラッグ中に毎フレーム呼び出される（現在は何もしないが、インターフェースのために必要）
    public void OnDrag(PointerEventData eventData)
    {
        // ドラッグ中の追従はDragDropManagerが一括して行う
    }

    // ドラッグが終了した瞬間に呼び出される（ドロップされた時、されなかった時両方）
    public void OnEndDrag(PointerEventData eventData)
    {
        // ドラッグ終了処理もDragDropManagerが一括して行う
        // ここでは何もしない
    }
}