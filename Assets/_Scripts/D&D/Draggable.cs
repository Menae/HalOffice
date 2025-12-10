using UnityEngine;

[RequireComponent(typeof(Collider2D))]
/// <summary>
/// ドラッグ可能なオブジェクトを表すコンポーネント。スロットへの配置とハイライト表示を管理。
/// </summary>
/// <remarks>Collider2Dが必須。インスペクタでItemDataやハイライト用GameObjectを設定すること。</remarks>
public class Draggable : MonoBehaviour
{
    /// <summary>
    /// Inspectorで割り当てる対応するアイテムデータ。プレハブやハイライト用スプライト等を保持。
    /// </summary>
    [Header("アイテム設定")]
    [Tooltip("このオブジェクトがどのアイテムデータに対応するかを設定")]
    public ItemData itemData;

    /// <summary>
    /// 選択時に表示するハイライト用のGameObject。Inspectorでアサインする。
    /// </summary>
    [Header("ハイライト設定")]
    [Tooltip("選択時に表示する縁取りなどのハイライト用オブジェクト")]
    public GameObject highlightGraphic;

    /// <summary>
    /// 現在このオブジェクトが配置されているスロット。外部からの参照および設定を想定。
    /// </summary>
    public ObjectSlot currentSlot { get; set; }

    /// <summary>
    /// 初期化処理。UnityのStartで呼ばれる（スクリプト有効化後、最初のフレームの直前）。
    /// </summary>
    /// <remarks>起動時にハイライトを非表示にしておく。highlightGraphicが未設定でも安全に動作。</remarks>
    private void Start()
    {
        SetHighlight(false);
    }

    /// <summary>
    /// ハイライトの表示/非表示を切り替える。
    /// </summary>
    /// <param name="isActive">表示する場合はtrue、非表示はfalse。</param>
    /// <remarks>highlightGraphicがnullの場合は処理をスキップ。ハイライトの有無のみを管理。</remarks>
    public void SetHighlight(bool isActive)
    {
        if (highlightGraphic != null)
        {
            highlightGraphic.SetActive(isActive);
        }
    }
}