// Draggable.cs

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    /// <summary>
    /// このオブジェクトが現在入っているスロットへの参照
    /// </summary>
    public ObjectSlot currentSlot { get; set; }
}