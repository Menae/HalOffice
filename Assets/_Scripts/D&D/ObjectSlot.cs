using UnityEngine;

// [System.Serializable] を付けることで、インスペクタ上に表示できるようになる
[System.Serializable]
public class ObjectSlot
{
    [Tooltip("このスロットの場所を示すTransform（シーンに配置した空のGameObject）")]
    public Transform slotTransform;

    [Tooltip("現在このスロットに置かれているオブジェクト")]
    public Draggable currentObject;

    /// <summary>
    /// このスロットが使用中かどうかを返す
    /// </summary>
    public bool IsOccupied()
    {
        return currentObject != null;
    }
}