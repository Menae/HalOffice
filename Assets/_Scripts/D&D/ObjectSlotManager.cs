using System.Collections.Generic;
using UnityEngine;

public class ObjectSlotManager : MonoBehaviour
{
    [Tooltip("このシーン内に存在するすべてのオブジェクトスロットをここに登録する")]
    public List<ObjectSlot> objectSlots;

    private void Awake()
    {
        foreach (var slot in objectSlots)
        {
            if (slot.slotTransform != null)
            {
                DropZone zone = slot.slotTransform.GetComponent<DropZone>();
                if (zone != null)
                {
                    zone.associatedSlot = slot;
                }
                else
                {
                    Debug.LogWarning($"スロット '{slot.slotTransform.name}' にDropZoneコンポーネントがアタッチされていません。", slot.slotTransform);
                }

                // ▼▼▼ 以下の処理を追加 ▼▼▼
                // もしスロットに最初からオブジェクトが配置されているなら、
                // そのオブジェクトにスロットの情報を教える
                if (slot.IsOccupied())
                {
                    slot.currentObject.currentSlot = slot;
                }
            }
        }
    }
}