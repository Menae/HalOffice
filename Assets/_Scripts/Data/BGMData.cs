using UnityEngine;

/// <summary>
/// BGMデータを定義するScriptableObject。Projectウィンドウからアセット作成用。
/// シーン名に一致するシーンでBGMを再生するためのデータを保持。Inspectorで編集。
/// </summary>
[CreateAssetMenu(fileName = "BGMData", menuName = "Audio/Create BGM Data")]
public class BGMData : ScriptableObject
{
    /// <summary>
    /// このBGMを再生するシーンの名前。Inspectorで指定。nullまたは空文字は再生判定に影響。
    /// </summary>
    [Tooltip("このBGMを再生するシーンの名前")]
    public string sceneName;

    /// <summary>
    /// 再生するBGMのオーディオクリップ。InspectorでD&D。nullの場合は再生できない。
    /// </summary>
    [Tooltip("再生するBGMのオーディオクリップ")]
    public AudioClip bgmClip;

    /// <summary>
    /// このBGMの音量(0〜1)。Inspectorで調整。0で無音。初期値は0.5。
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("このBGMの音量")]
    public float volume = 0.5f;

    /// <summary>
    /// シーンロード時に自動で再生を開始するかどうか。Inspectorで設定。
    /// falseの場合は手動で再生を開始する。
    /// </summary>
    [Tooltip("シーンロード時に自動で再生を開始するかどうか")]
    public bool playOnLoad = true;
}