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

    [Header("見つかり度設定")]
    public FloatEventChannelSO detectionIncreaseChannel;
    public float detectionAmount = 30f;

    public bool IsTVOn { get { return isTVOn; } } //外部に現在の状態を教えるための窓口

    void Start()
    {
        tvOnMaterial = screenRenderer.material;
        videoPlayer.isLooping = true;
        videoPlayer.Play();
        TurnOffTV();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 0:左クリック
        {
            // ScreenToWorldConverterを使って、正しいワールド座標を取得
            if (ScreenToWorldConverter.Instance != null &&
                ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
            {
                // 取得した座標に何があるかチェック
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                {
                    // TVがヒットしたら、電源を切り替える
                    if (isTVOn)
                    {
                        TurnOffTV();
                    }
                    else
                    {
                        TurnOnTV();
                    }
                }
            }
        }
    }


    void TurnOnTV()
    {
        screenRenderer.material = tvOnMaterial;
        videoPlayer.SetDirectAudioMute(0, false);
        isTVOn = true;

        //見つかり度上昇イベントを発行する
        if (detectionIncreaseChannel != null)
        {
            detectionIncreaseChannel.RaiseEvent(detectionAmount);
        }
    }

    void TurnOffTV()
    {
        screenRenderer.material = tvOffMaterial;
        videoPlayer.SetDirectAudioMute(0, true);
        isTVOn = false;
    }
}