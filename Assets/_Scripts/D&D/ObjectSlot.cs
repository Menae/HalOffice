// ObjectSlot.cs

using UnityEngine;
using System.Collections.Generic; // Listを使うために必要

[System.Serializable]
public class ObjectSlot
{
    public Transform slotTransform;
    public Draggable currentObject;

    [Header("スロット設定")]
    [Tooltip("このスロットに配置可能なアイテムの種類のリスト。空の場合は何でも配置できる。")]
    public List<ItemType> allowedItemTypes = new List<ItemType>();

    /// <summary>
    /// 指定されたアイテムの種類をこのスロットが受け入れ可能か判定する
    /// </summary>
    public bool CanAccept(ItemType typeToCheck)
    {
        // 許可リストが空の場合、どんなアイテムでも受け入れる（従来の挙動）
        if (allowedItemTypes == null || allowedItemTypes.Count == 0)
        {
            return true;
        }
        // 許可リストに、チェックしたいアイテムの種類が含まれているか返す
        return allowedItemTypes.Contains(typeToCheck);
    }

    public bool IsOccupied()
    {
        return currentObject != null;
    }
}