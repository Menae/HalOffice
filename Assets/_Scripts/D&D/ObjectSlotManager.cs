using System.Collections.Generic;
using UnityEngine;

public class ObjectSlotManager : MonoBehaviour
{
    [Tooltip("このシーン内に存在するすべてのオブジェクトスロットをここに登録する")]
    public List<ObjectSlot> objectSlots;

    private void Awake()
    {
        // ゲーム開始時に、各スロットのTransformにアタッチされているDropZoneコンポーネントに、
        // 対応するObjectSlotデータ（自分自身）を教えて関連付けを行う。
        // これにより、DropZoneが自分がどのスロットなのかを認識できるようになる。
        foreach (var slot in objectSlots)
        {
            if (slot.slotTransform != null)
            {
                DropZone zone = slot.slotTransform.GetComponent<DropZone>();
                if (zone != null)
                {
                    // DropZoneに、このObjectSlotのインスタンスを渡す
                    zone.associatedSlot = slot;
                }
                else
                {
                    Debug.LogWarning($"スロット '{slot.slotTransform.name}' にDropZoneコンポーネントがアタッチされていません。", slot.slotTransform);
                }
            }
        }
    }

    /// <summary>
    /// 指定されたDraggableオブジェクトがどのスロットにあるかを探して返す
    /// </summary>
    public ObjectSlot FindSlotForDraggable(Draggable draggable)
    {
        foreach (var slot in objectSlots)
        {
            if (slot.currentObject == draggable)
            {
                return slot;
            }
        }
        return null; // 見つからなかった
    }
}