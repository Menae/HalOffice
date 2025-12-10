using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーン内のオブジェクトスロットの管理を行うコンポーネント。
/// 初期配置されたアイテムの記録と、プレイ中に「新しく配置された」スロットの追跡を担当する。
/// </summary>
public class ObjectSlotManager : MonoBehaviour
{
    /// <summary>
    /// インスペクタで設定するスロットのリスト。スロットの順序はシーン上の論理順に合わせる。
    /// Inspectorで割り当て必須ではないが、処理対象が無い場合は操作が行われない。
    /// </summary>
    public List<ObjectSlot> objectSlots;

    /// <summary>
    /// Awake時点で各スロットに配置されていたアイテムのマッピング（スロット -> ItemData）。
    /// 外部からは読み取り専用。スロットにアイテムが無い場合はエントリを作成しない。
    /// </summary>
    public Dictionary<ObjectSlot, ItemData> InitialSlotContents { get; private set; }

    /// <summary>
    /// UnityのAwakeで呼ばれる初期化処理。
    /// スクリプトインスタンス生成時に実行され、インスペクタで割り当てられた
    /// <see cref="objectSlots"/> を巡回して初期配置を記録し、対応する <see cref="DropZone"/> に関連付けを行う。
    /// </summary>
    private void Awake()
    {
        InitialSlotContents = new Dictionary<ObjectSlot, ItemData>();

        foreach (var slot in objectSlots)
        {
            if (slot.slotTransform != null)
            {
                DropZone zone = slot.slotTransform.GetComponent<DropZone>();
                if (zone != null)
                {
                    zone.associatedSlot = slot;
                }

                if (slot.IsOccupied())
                {
                    slot.currentObject.currentSlot = slot;
                    InitialSlotContents[slot] = slot.currentObject.itemData;
                }
            }
        }
    }

    // 新しく配置されたスロットを記憶。
    private HashSet<ObjectSlot> newlyPlacedSlots = new HashSet<ObjectSlot>();

    /// <summary>
    /// 指定したスロットが「新しく配置された」とマークされているか判定する。
    /// nullを渡した場合はfalseを返すため呼び出し側でのnullチェックは必須ではないが推奨。
    /// </summary>
    /// <param name="slot">判定対象のスロット。</param>
    /// <returns>新しく配置された場合にtrue。それ以外はfalse。</returns>
    public bool IsNewlyPlaced(ObjectSlot slot)
    {
        return newlyPlacedSlots.Contains(slot);
    }

    /// <summary>
    /// 指定したスロットを「新しく配置された」としてマークする。
    /// nullチェックを行い、nullの場合は何もしない。
    /// </summary>
    /// <param name="slot">マーク対象のスロット。</param>
    public void MarkSlotAsNewlyPlaced(ObjectSlot slot)
    {
        if (slot != null) newlyPlacedSlots.Add(slot);
    }

    /// <summary>
    /// 指定したスロットの「新しく配置された」マークを解除する（発見済み扱いにする）。
    /// nullチェックを行い、nullの場合は何もしない。
    /// </summary>
    /// <param name="slot">マーク解除対象のスロット。</param>
    public void MarkSlotAsSeen(ObjectSlot slot)
    {
        if (slot != null) newlyPlacedSlots.Remove(slot);
    }
}