using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スクリーン座標（マウス位置など）を、RenderTexture上のワールド座標に変換するユーティリティクラス。
/// UI上に表示されたゲーム画面（RawImage）内でのクリック位置を、実際のゲームワールド座標に変換する。
/// </summary>
public class ScreenToWorldConverter : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("ゲーム画面を映しているRaw Image")]
    public RawImage gameScreen;

    [Tooltip("ゲーム世界を撮影しているカメラ (MainCamera)")]
    public Camera worldCamera;

    [Tooltip("UIを描画しているカメラ (UI-Camera)")]
    public Camera uiCamera;

    public static ScreenToWorldConverter Instance { get; private set; }

    /// <summary>
    /// シングルトンインスタンスを初期化。
    /// 重複インスタンスが存在する場合は破棄する。
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// スクリーン座標（マウス位置）から、RenderTexture上のワールド座標を取得する。
    /// RawImageの矩形範囲内での座標をUV座標に変換し、Raycastを用いてZ=0平面上のワールド座標を算出する。
    /// </summary>
    /// <param name="screenPosition">スクリーン座標（例: Input.mousePosition）</param>
    /// <param name="worldPosition">取得したワールド座標（Z=0平面上）</param>
    /// <returns>変換に成功した場合true、範囲外またはカメラ未設定の場合false</returns>
    public bool GetWorldPosition(Vector2 screenPosition, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (gameScreen == null || worldCamera == null) return false;

        // Raw Imageの矩形範囲内で、スクリーン座標をローカル座標に変換
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gameScreen.rectTransform,
            screenPosition,
            uiCamera,
            out Vector2 localPoint))
        {
            // Raw Imageの矩形範囲に対する、0-1の割合（UV座標）を計算
            float uvX = Mathf.InverseLerp(gameScreen.rectTransform.rect.xMin, gameScreen.rectTransform.rect.xMax, localPoint.x);
            float uvY = Mathf.InverseLerp(gameScreen.rectTransform.rect.yMin, gameScreen.rectTransform.rect.yMax, localPoint.y);

            // カーソルがRaw Imageの範囲外なら、処理を中断
            if (uvX < 0 || uvX > 1 || uvY < 0 || uvY > 1) return false;

            // Viewport座標からRay（光線）を生成
            Ray ray = worldCamera.ViewportPointToRay(new Vector3(uvX, uvY, 0));

            // Rayを2Dの平面（Z=0）に投影して、ワールド座標を計算
            // これにより、Z座標の問題を完全に排除
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }
        }

        return false;
    }
}