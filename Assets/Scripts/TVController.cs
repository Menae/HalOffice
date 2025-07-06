using UnityEngine;
using UnityEngine.Video;

public class TVController : MonoBehaviour
{
    [Header("必須コンポーネント")]
    public VideoPlayer videoPlayer; // 映像を再生する本体
    public MeshRenderer screenRenderer; // 映像を映す画面のレンダラー

    [Header("マテリアル設定")]
    public Material tvOffMaterial; // TVがオフの時に使う真っ黒なマテリアル

    private Material tvOnMaterial; // TVがオンの時のマテリアル（映像が映るもの）
    private bool isTVOn = false;   // TVの現在の状態

    void Start()
    {
        // 1. 開始時に、現在画面に設定されている「映像が映るマテリアル」を記憶しておく
        tvOnMaterial = screenRenderer.material;

        // 2. ループ設定を確認し、再生を開始する（これで裏でずっと流れ続ける）
        videoPlayer.isLooping = true;
        videoPlayer.Play();

        // 3. ただし、最初はTVオフの状態にする
        TurnOffTV();
    }

    void OnMouseDown()
    {
        // クリックされたら、状態をトグル（切り替え）する
        if (isTVOn)
        {
            TurnOffTV();
        }
        else
        {
            TurnOnTV();
        }
    }

    void TurnOnTV()
    {
        // 画面のマテリアルを「映像が映るマテリアル」に切り替える
        screenRenderer.material = tvOnMaterial;
        // 音声をオンにする
        videoPlayer.SetDirectAudioMute(0, false);

        isTVOn = true;
    }

    void TurnOffTV()
    {
        // 画面のマテリアルを「真っ黒なマテリアル」に切り替える
        screenRenderer.material = tvOffMaterial;
        // 音声をミュートにする
        videoPlayer.SetDirectAudioMute(0, true);

        isTVOn = false;
    }
}