using System;
using UnityEngine;

/// <summary>
/// ゲーム全体で共有されるグローバルなイベントを一元管理する静的クラス。
/// </summary>
public static class GameEventManager
{
    /// <summary>
    /// オブジェクトがスロットから完全に取り除かれた時に発行されるイベント。
    /// 引数としてどのスロットが空になったかを受け取ることができる
    /// </summary>
    public static event Action<ObjectSlot> OnObjectRemovedFromSlot;

    public static void InvokeObjectRemoved(ObjectSlot removedFromSlot)
    {
        if (removedFromSlot == null)
        {
            Debug.LogWarning("InvokeObjectRemovedがnullのスロットで呼び出されました。");
            return;
        }

        Debug.Log($"<color=lightblue>[GameEvent]</color> Object removed from slot: <b>{removedFromSlot.slotTransform.name}</b>");

        // この '?' によるnullチェックにより、イベントに誰も登録（購読）していなくても
        // NullReferenceExceptionエラーが発生するのを防ぎます。
        OnObjectRemovedFromSlot?.Invoke(removedFromSlot);
    }
}