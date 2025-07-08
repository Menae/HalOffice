using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Collider2D))]
public class TVController : MonoBehaviour
{
    [Header("必須コンポーネント")]
    public VideoPlayer videoPlayer;
    public MeshRenderer screenRenderer;

    [Header("マテリアル設定")]
    public Material tvOffMaterial;

    private Material tvOnMaterial;
    private bool isTVOn = false;

    public bool IsTVOn { get { return isTVOn; } } // 外部に現在の状態を教えるための窓口

    void Start()
    {
        tvOnMaterial = screenRenderer.material;
        videoPlayer.isLooping = true;
        videoPlayer.Play();
        TurnOffTV();
    }

    void Update()
    {
        // 右クリックで電源をオン・オフする処理
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(Camera.main.ScreenPointToRay(Input.mousePosition));
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                if (Input.GetMouseButtonDown(1)) // 1: 右クリック
                {
                    if (isTVOn)
                    {
                        TurnOffTV();
                    }
                    else
                    {
                        TurnOnTV();
                    }
                    break;
                }
            }
        }
    }


    void TurnOnTV()
    {
        screenRenderer.material = tvOnMaterial;
        videoPlayer.SetDirectAudioMute(0, false);
        isTVOn = true;
    }

    void TurnOffTV()
    {
        screenRenderer.material = tvOffMaterial;
        videoPlayer.SetDirectAudioMute(0, true);
        isTVOn = false;
    }
}