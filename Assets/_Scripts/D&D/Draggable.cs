// Draggable.cs

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    /// <summary>
    /// このオブジェクトが現在入っているスロットへの参照
    /// </summary>
    public ObjectSlot currentSlot { get; set; }

    [Header("アイテム設定")]
    public ItemType itemType; // このアイテムの種類
    [Tooltip("このアイテムを選択した時に表示する会話（InkのJSONファイル）")]
    public TextAsset descriptionInk;
}