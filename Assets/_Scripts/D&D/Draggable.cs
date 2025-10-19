using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    public ObjectSlot currentSlot { get; set; }

    // public ItemType itemType;
    // public TextAsset descriptionInk;

    [Header("アイテム設定")]
    [Tooltip("このオブジェクトがどのアイテムデータに対応するかを設定")]
    public ItemData itemData;
}