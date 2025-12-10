using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways] // エディタ上での変更にも即座に反応
/// <summary>
/// 指定したアスペクト比に合わせてカメラの表示領域をクロップし、必要に応じて上下または左右に黒帯を表示する。Editorでも即時反映する。
/// </summary>
public class AspectRatioController : MonoBehaviour
{
    [Header("アスペクト比設定")]
    [Tooltip("目標アスペクト比の幅")]
    /// <summary>
    /// 目標アスペクト比の幅。Inspectorで設定。0以下は無効として扱う。
    /// </summary>
    public float targetWidth = 4.0f;

    [Tooltip("目標アスペクト比の高さ")]
    /// <summary>
    /// 目標アスペクト比の高さ。Inspectorで設定。0以下の場合はUpdateCropで処理を行わない。
    /// </summary>
    public float targetHeight = 3.0f;

    private Camera cameraComponent;

    /// <summary>
    /// 起動時にCameraコンポーネントを取得し、初期のクロップを適用する。Awakeタイミングで呼ばれる。
    /// </summary>
    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        UpdateCrop();
    }

    /// <summary>
    /// エディタやウィンドウサイズ変更時に呼ばれる。画面サイズの変化に応じてクロップを再計算する。
    /// </summary>
    void OnRectTransformDimensionsChange()
    {
        UpdateCrop();
    }

    /// <summary>
    /// 毎フレーム呼ばれる。Editorでの即時反映のためにクロップを更新する。
    /// </summary>
    void Update()
    {
        UpdateCrop();
    }

    /// <summary>
    /// 現在の画面比と目標比を比較し、Camera.rectを調整して上下または左右に黒帯を表示する。
    /// </summary>
    /// <remarks>
    /// targetHeightが0以下の場合は処理を中断する。cameraComponentが未設定の場合は何もしない。
    /// 画面比の差に基づきrectを設定する。
    /// </remarks>
    public void UpdateCrop()
    {
        if (cameraComponent == null) return;

        // 0で割るのを防ぐための安全チェック
        if (targetHeight <= 0) return;
        float targetAspect = targetWidth / targetHeight;

        // 画面の現在のアスペクト比
        float screenAspect = (float)Screen.width / Screen.height;
        // 目標のアスペクト比と現在の比率との差
        float scaleRatio = screenAspect / targetAspect;

        Rect rect = cameraComponent.rect;

        if (scaleRatio < 1.0f)
        {
            // 現在の画面が目標より縦長の場合（上下に黒帯）
            rect.width = 1.0f;
            rect.height = scaleRatio;
            rect.x = 0;
            rect.y = (1.0f - scaleRatio) / 2.0f;
        }
        else
        {
            // 現在の画面が目標より横長の場合（左右に黒帯）
            float scaleRatioWidth = 1.0f / scaleRatio;
            rect.width = scaleRatioWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleRatioWidth) / 2.0f;
            rect.y = 0;
        }

        cameraComponent.rect = rect;
    }
}