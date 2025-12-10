using UnityEngine;

// Unityエディタのメニューから作成できるようにする属性
[CreateAssetMenu(fileName = "NewItemData", menuName = "HalOffice/Item Data")]
/// <summary>
/// ゲーム内で利用するアイテムに関するデータを保持するScriptableObject。
/// Inspectorで設定したアセットや識別情報を参照渡しで扱うデータコンテナ。
/// シリアライズ可能な設定のみを含め、実行時の振る舞いは別コンポーネントで管理。
/// </summary>
public class ItemData : ScriptableObject
{
    [Header("基本情報")]
    /// <summary>
    /// デバッグや識別用のアイテム名。Inspectorで編集。
    /// 表示ラベルやログ出力に使用する識別文字列。
    /// 空文字やnullは未設定扱いとなる可能性あり。
    /// </summary>
    [Tooltip("デバッグや識別のためのアイテム名")]
    public string itemName;

    /// <summary>
    /// アイテムの種類。UI表示やスロットの配置制限の判定に使用。
    /// 値を追加する際は表示マッピングとシリアライズ先の連携も更新。
    /// </summary>
    [Tooltip("アイテムの種類（スロットの配置制限などで使用）")]
    public ItemType itemType;

    [Header("関連アセット")]
    /// <summary>
    /// ゲーム内に配置する際にInstantiateするPrefab。InspectorでD&D。
    /// 配置処理側でnullチェックを行うこと。
    /// </summary>
    [Tooltip("ゲーム内に配置される際のプレハブ")]
    public GameObject itemPrefab;

    /// <summary>
    /// ドラッグ中に配置可能な場所で表示するハイライト用スプライト。InspectorでD&D。
    /// 代替表示が必要な場合はnullチェックを行うこと。
    /// </summary>
    [Tooltip("ドラッグ中、配置可能な場所に来た時に表示するハイライト画像")]
    public Sprite highlightSprite;

    /// <summary>
    /// ハイライト画像の表示位置の微調整（X, Y）。中心がズレている場合に設定。
    /// デフォルトは (0,0)。
    /// </summary>
    [Tooltip("ハイライト画像の表示位置の微調整（X, Y）。中心がズレている場合に使用")]
    public Vector2 highlightOffset = Vector2.zero;

    /// <summary>
    /// ハイライト画像の拡大率（X, Y）。(1, 1)が等倍。
    /// レイアウト調整用の倍率。
    /// </summary>
    [Tooltip("ハイライト画像の拡大率（X, Y）。(1, 1)が等倍")]
    public Vector2 highlightScale = Vector2.one;

    /// <summary>
    /// アイテム説明用のInkファイル。InspectorでD&D。
    /// 表示処理側でnullチェックを行い、未設定時のフォールバックを用意すること。
    /// </summary>
    [Tooltip("アイテムをクリックした際に表示される説明文のInkファイル")]
    public TextAsset descriptionInk;

    /// <summary>
    /// アイテムが消失した際にNPCが反応するためのInkファイル。InspectorでD&D。
    /// 再生処理側でnullチェックを行うこと。
    /// </summary>
    [Tooltip("このアイテムが消失した際にNPCが反応する時のInkファイル")]
    public TextAsset missingReactionInk;

    /// <summary>
    /// アイテムがスロットに配置されたときにNPCが反応するInkファイル。InspectorでD&D。
    /// null時は反応なし扱いとする。
    /// </summary>
    [Tooltip("このアイテムがスロットに配置されたのをNPCが発見した時のInkファイル")]
    public TextAsset placedReactionInk;
}