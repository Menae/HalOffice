using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways] //エディタ上での変更にも即座に反応
public class AspectRatioController : MonoBehaviour
{
    // 以下の定数を削除
    // private const float TARGET_ASPECT = 4.0f / 3.0f;

    // ▼▼▼ Inspectorで設定できる変数に置き換え ▼▼▼
    [Header("アスペクト比設定")]
    [Tooltip("目標アスペクト比の幅")]
    public float targetWidth = 4.0f;
    [Tooltip("目標アスペクト比の高さ")]
    public float targetHeight = 3.0f;

    private Camera cameraComponent;

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        UpdateCrop();
    }

    //エディタ上でウィンドウサイズが変わった時にも即座に比率を再計算
    void OnRectTransformDimensionsChange()
    {
        UpdateCrop();
    }

    //毎フレーム呼ばれるが、エディタでの変更に追従するために残しておく
    void Update()
    {
        UpdateCrop();
    }

    public void UpdateCrop()
    {
        if (cameraComponent == null) return;

        // ▼▼▼ Inspectorの値から目標アスペクト比を計算 ▼▼▼
        // 0で割るのを防ぐための安全チェック
        if (targetHeight <= 0) return;
        float targetAspect = targetWidth / targetHeight;

        //画面の現在のアスペクト比
        float screenAspect = (float)Screen.width / Screen.height;
        //目標のアスペクト比と現在の比率との差
        float scaleRatio = screenAspect / targetAspect;

        Rect rect = cameraComponent.rect;

        if (scaleRatio < 1.0f)
        {
            //現在の画面が目標より縦長の場合 (上下に黒帯)
            rect.width = 1.0f;
            rect.height = scaleRatio;
            rect.x = 0;
            rect.y = (1.0f - scaleRatio) / 2.0f;
        }
        else
        {
            //現在の画面が目標より横長の場合 (左右に黒帯)
            float scaleRatioWidth = 1.0f / scaleRatio;
            rect.width = scaleRatioWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleRatioWidth) / 2.0f;
            rect.y = 0;
        }

        cameraComponent.rect = rect;
    }
}