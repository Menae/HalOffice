using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour
{
    // このドロップゾーンがどのような種類かを定義する
    public enum ZoneType
    {
        GameSlot, // ゲーム世界のオブジェクトスロット
        TrashCan  // UIのゴミ箱
    }

    [Tooltip("このドロップゾーンの種類を選択してください")]
    public ZoneType zoneType = ZoneType.GameSlot;

    // このDropZoneがゲーム世界のObjectSlotである場合、
    // 対応するObjectSlotの情報をここに設定する
    [HideInInspector]
    public ObjectSlot associatedSlot;
}