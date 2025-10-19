using System.Collections.Generic;
using UnityEngine;

public class ObjectSlotManager : MonoBehaviour
{
    public List<ObjectSlot> objectSlots;

    public Dictionary<ObjectSlot, ItemData> InitialSlotContents { get; private set; }

    private void Awake()
    {
        InitialSlotContents = new Dictionary<ObjectSlot, ItemData>();

        foreach (var slot in objectSlots)
        {
            if (slot.slotTransform != null)
            {
                DropZone zone = slot.slotTransform.GetComponent<DropZone>();
                if (zone != null)
                {
                    zone.associatedSlot = slot;
                }

                if (slot.IsOccupied())
                {
                    slot.currentObject.currentSlot = slot;
                    InitialSlotContents[slot] = slot.currentObject.itemData;
                }
            }
        }
    }

    // 新しくアイテムが配置されたスロットを記憶しておくためのセット
    private HashSet<ObjectSlot> newlyPlacedSlots = new HashSet<ObjectSlot>();

    /// <summary>
    /// 指定されたスロットが「新しく配置された」状態か確認する
    /// </summary>
    public bool IsNewlyPlaced(ObjectSlot slot)
    {
        return newlyPlacedSlots.Contains(slot);
    }

    /// <summary>
    /// スロットを「新しく配置された」状態としてマークする
    /// </summary>
    public void MarkSlotAsNewlyPlaced(ObjectSlot slot)
    {
        if (slot != null) newlyPlacedSlots.Add(slot);
    }

    /// <summary>
    /// スロットを「発見済み」とし、マークを解除する
    /// </summary>
    public void MarkSlotAsSeen(ObjectSlot slot)
    {
        if (slot != null) newlyPlacedSlots.Remove(slot);
    }
}