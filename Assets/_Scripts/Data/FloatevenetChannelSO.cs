using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Float Event Channel")]
public class FloatEventChannelSO : ScriptableObject
{
    /// <summary>
    /// 浮動小数点値のイベント購読用デリゲート。ランタイムにコードから登録する。
    /// </summary>
    /// <remarks>
    /// UnityActionはInspectorに表示されないため、Inspectorでの割当は不可。購読解除は通常
    /// コンポーネントの __OnEnable__ / __OnDisable__ で行うこと。未登録時は null として扱う。
    /// </remarks>
    public UnityAction<float> OnEventRaised;

    /// <summary>
    /// 指定した値でイベントを発行する。
    /// </summary>
    /// <param name="value">送信する浮動小数点値。</param>
    /// <remarks>
    /// nullチェック済み。購読者が存在しない場合は何もしない。発行はメインスレッド上で行う前提。
    /// </remarks>
    public void RaiseEvent(float value)
    {
        OnEventRaised?.Invoke(value);
    }
}