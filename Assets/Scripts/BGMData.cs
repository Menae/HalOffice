using UnityEngine;

// このスクリプトを元に、Projectウィンドウから新しいアセットを作れるようにするおまじない
[CreateAssetMenu(fileName = "BGMData", menuName = "Audio/Create BGM Data")]
public class BGMData : ScriptableObject
{
    [Tooltip("このBGMを再生するシーンの名前")]
    public string sceneName;
    [Tooltip("再生するBGMのオーディオクリップ")]
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    [Tooltip("このBGMの音量")]
    public float volume = 0.5f;
}