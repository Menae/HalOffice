// ObjectSlot.cs

using System.Collections.Generic; // Listを使うために必要
using UnityEngine;

[System.Serializable]
public class ObjectSlot
{
    public Transform slotTransform;
    public Draggable currentObject;

    [Header("スロット設定")]
    [Tooltip("このスロットに配置可能なアイテムの種類のリスト。空の場合は何でも配置できる。")]
    public List<ItemType> allowedItemTypes = new List<ItemType>();
    [Tooltip("このスロットに対する正解のアイテム種類")]
    public ItemType correctItemType;

    [Tooltip("このスロットが空の時に「正解」と判定する場合はチェック")]
    public bool isCorrectWhenEmpty = false;

    /// <summary>
    /// 指定されたアイテムの種類をこのスロットが受け入れ可能か判定する
    /// </summary>
    public bool CanAccept(ItemType typeToCheck)
    {
        // リストが null または 要素数が 0 の場合は、無条件で許可(true)を返す
        if (allowedItemTypes == null || allowedItemTypes.Count == 0)
        {
            return true;
        }

        // リストに要素がある場合のみ、含まれているかチェックする
        return allowedItemTypes.Contains(typeToCheck);
    }

    public bool IsOccupied()
    {
        return currentObject != null;
    }
}