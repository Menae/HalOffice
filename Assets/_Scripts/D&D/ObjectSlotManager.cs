using System.Collections.Generic;
using UnityEngine;

public class ObjectSlotManager : MonoBehaviour
{
    // シングルトンとして実装し、どこからでもアクセスできるようにする
    public static ObjectSlotManager Instance { get; private set; }

    [Tooltip("シーン内に存在するすべてのオブジェクトスロットをここに登録する")]
    public List<ObjectSlot> objectSlots;

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