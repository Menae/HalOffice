using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); }
        else { Instance = this; }
    }

    /// <summary>
    /// スクリーン座標（マウス位置）から、RenderTexture上のワールド座標を取得します。
    /// </summary>
    public bool GetWorldPosition(Vector2 screenPosition, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (gameScreen == null || worldCamera == null) return false;

        // Raw Imageの矩形範囲内で、スクリーン座標をローカル座標に変換
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gameScreen.rectTransform,
            screenPosition,
            uiCamera, // UIカメラを基準に変換
            out Vector2 localPoint))
        {
            // Raw Imageの矩形範囲に対する、0-1の割合（UV座標）を計算
            float uvX = Mathf.InverseLerp(gameScreen.rectTransform.rect.xMin, gameScreen.rectTransform.rect.xMax, localPoint.x);
            float uvY = Mathf.InverseLerp(gameScreen.rectTransform.rect.yMin, gameScreen.rectTransform.rect.yMax, localPoint.y);

            // カーソルがRaw Imageの範囲外なら、処理を中断
            if (uvX < 0 || uvX > 1 || uvY < 0 || uvY > 1) return false;

            // ▼▼▼ ここからが最重要修正 ▼▼▼
            // Viewport座標からRay（光線）を生成する
            Ray ray = worldCamera.ViewportPointToRay(new Vector3(uvX, uvY, 0));

            // Rayを2Dの平面（Z=0）に投影して、ワールド座標を計算する
            // これにより、Z座標の問題を完全に排除する
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }
            // ▲▲▲ ここまで ▲▲▲
        }
        return false;
    }
}