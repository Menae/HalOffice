using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways] // エディタ上での変更にも即座に反応するようにする
public class AspectRatioController : MonoBehaviour
{
    // 目標のアスペクト比 (16:9)
    private const float TARGET_ASPECT = 4.0f / 3.0f;

    private Camera cameraComponent;

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        UpdateCrop();
    }

    // エディタ上でウィンドウサイズが変わった時にも即座に比率を再計算する
    void OnRectTransformDimensionsChange()
    {
        UpdateCrop();
    }

    // 毎フレーム呼ばれるが、エディタでの変更に追従するために残しておく
    void Update()
    {
        UpdateCrop();
    }

    public void UpdateCrop()
    {
        if (cameraComponent == null) return;

        // 画面の現在のアスペクト比
        float screenAspect = (float)Screen.width / Screen.height;
        // 目標のアスペクト比と、現在の比率との差
        float scaleRatio = screenAspect / TARGET_ASPECT;

        Rect rect = cameraComponent.rect;

        if (scaleRatio < 1.0f)
        {
            // 現在の画面が目標より「縦長」の場合 (上下に黒帯)
            rect.width = 1.0f;
            rect.height = scaleRatio;
            rect.x = 0;
            rect.y = (1.0f - scaleRatio) / 2.0f;
        }
        else
        {
            // 現在の画面が目標より「横長」の場合 (左右に黒帯)
            float scaleRatioWidth = 1.0f / scaleRatio;
            rect.width = scaleRatioWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleRatioWidth) / 2.0f;
            rect.y = 0;
        }

        cameraComponent.rect = rect;
    }
}