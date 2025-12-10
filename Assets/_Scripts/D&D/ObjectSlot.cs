using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectSlot
{
    /// <summary>
    /// スロットの配置場所を示すTransform。インスペクタでスロット位置のTransformを割り当てる。
    /// </summary>
    public Transform slotTransform;

    /// <summary>
    /// 現在このスロットに配置されているドラッグ可能オブジェクト。外部から参照・設定される想定。
    /// nullの場合は空のスロットを示す。
    /// </summary>
    public Draggable currentObject;

    [Header("スロット設定")]
    [Tooltip("このスロットに配置可能なアイテムの種類のリスト。空の場合は何でも配置できる。")]
    public List<ItemType> allowedItemTypes = new List<ItemType>();

    [Tooltip("このスロットに対する正解のアイテム種類")]
    public ItemType correctItemType;

    [Tooltip("このスロットが空の時に「正解」と判定する場合はチェック")]
    public bool isCorrectWhenEmpty = false;

    /// <summary>
    /// 指定したアイテム種別をこのスロットが受け入れ可能か判定する。
    /// リストが未割り当て(null)または空の場合は、すべての種別を受け入れる設計。
    /// </summary>
    /// <param name="typeToCheck">判定対象のアイテム種別。</param>
    /// <returns>受け入れ可能ならtrue、不可ならfalse。</returns>
    /// <remarks>
    /// allowedItemTypesの状態に依存する。nullチェックを行い、安全に判定する。
    /// </remarks>
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

    /// <summary>
    /// スロットが現在オブジェクトで占有されているか判定する。
    /// </summary>
    /// <returns>currentObjectがnullでない場合にtrueを返す。</returns>
    public bool IsOccupied()
    {
        return currentObject != null;
    }
}