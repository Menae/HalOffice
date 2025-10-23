using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    [Header("アイテム設定")]
    [Tooltip("このオブジェクトがどのアイテムデータに対応するかを設定")]
    public ItemData itemData;

    [Header("ハイライト設定")]
    [Tooltip("選択時に表示する縁取りなどのハイライト用オブジェクト")]
    public GameObject highlightGraphic;

    public ObjectSlot currentSlot { get; set; }

    private void Start()
    {
        // ゲーム開始時は、必ずハイライトを非表示にしておく
        SetHighlight(false);
    }

    /// <summary>
    /// ハイライトの表示/非表示を切り替える
    /// </summary>
    public void SetHighlight(bool isActive)
    {
        if (highlightGraphic != null)
        {
            highlightGraphic.SetActive(isActive);
        }
    }
}