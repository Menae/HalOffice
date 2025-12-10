using UnityEngine;

/// <summary>
/// ドロップ操作の対象領域を表すコンポーネント。
/// ドラッグ中のアイテム受け入れ判定とUI上のゴミ箱操作を管理する。
/// </summary>
/// <remarks>
/// インスペクタで zoneType や isTutorialZone を設定。GameSlot の場合は外部から associatedSlot を割り当てること。
/// Collider や入力処理などの依存は外部で管理。使用時は関連オブジェクトの null チェックを行うこと。
/// </remarks>
public class DropZone : MonoBehaviour
{
    /// <summary>
    /// このドロップゾーンが持つ振る舞いの種類。受け入れ挙動を切り替える。
    /// </summary>
    public enum ZoneType
    {
        /// <summary>
        /// ゲーム世界のオブジェクト配置用スロット。対応する ObjectSlot を割り当てる。
        /// </summary>
        GameSlot,

        /// <summary>
        /// UI上のゴミ箱。ドロップされたアイテムの破棄やキャンセル処理を行う。
        /// </summary>
        TrashCan
    }

    [Tooltip("このドロップゾーンの種類を選択してください")]
    /// <summary>
    /// このドロップゾーンの種類。Inspectorで設定。デフォルトは GameSlot。
    /// </summary>
    public ZoneType zoneType = ZoneType.GameSlot;

    [Tooltip("チェックを入れると、チュートリアル用の「練習ゾーン」として扱います（アイテムの実体化を行いません）")]
    /// <summary>
    /// チュートリアル用の練習ゾーンとして扱うフラグ。true の場合、アイテムの実体化を行わない。
    /// Inspectorで設定。ゲーム判定に影響する可能性があるため使用時は挙動を確認すること。
    /// </summary>
    public bool isTutorialZone = false;

    [HideInInspector]
    /// <summary>
    /// この DropZone が GameSlot の場合に対応する ObjectSlot を参照するフィールド。Inspectorには表示しない。
    /// </summary>
    /// <remarks>
    /// ZoneType が GameSlot のときに外部から割り当てる。未設定(null)の可能性あり。使用時は必ず null チェックを行うこと。
    /// </remarks>
    public ObjectSlot associatedSlot;
}