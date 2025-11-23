// ファイル名: ItemData.cs
using UnityEngine;

// Unityエディタのメニューから作成できるようにする属性
[CreateAssetMenu(fileName = "NewItemData", menuName = "HalOffice/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("デバッグや識別のためのアイテム名")]
    public string itemName;

    [Tooltip("アイテムの種類（スロットの配置制限などで使用）")]
    public ItemType itemType;

    [Header("関連アセット")]
    [Tooltip("ゲーム内に配置される際のプレハブ")]
    public GameObject itemPrefab;

    [Tooltip("ドラッグ中、配置可能な場所に来た時に表示するハイライト画像")]
    public Sprite highlightSprite;

    [Tooltip("ハイライト画像の表示位置の微調整（X, Y）。中心がズレている場合に使用")]
    public Vector2 highlightOffset = Vector2.zero;

    [Tooltip("ハイライト画像の拡大率（X, Y）。(1, 1)が等倍")]
    public Vector2 highlightScale = Vector2.one;

    [Tooltip("アイテムをクリックした際に表示される説明文のInkファイル")]
    public TextAsset descriptionInk;

    [Tooltip("このアイテムが消失した際にNPCが反応する時のInkファイル")]
    public TextAsset missingReactionInk;

    [Tooltip("このアイテムがスロットに配置されたのをNPCが発見した時のInkファイル")]
    public TextAsset placedReactionInk;
}