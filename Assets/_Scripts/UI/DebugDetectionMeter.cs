using UnityEngine;
using UnityEngine.UI; //Scrollbarを扱うために必要

[RequireComponent(typeof(Scrollbar))]
public class DebugDetectionMeter : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("監視対象のDetectionManager")]
    public DetectionManager detectionManager;

    private Scrollbar scrollbar;

    void Awake()
    {
        scrollbar = GetComponent<Scrollbar>();
    }

    void Update()
    {
        //detectionManagerがセットされていなければ何もしない
        if (detectionManager == null) return;

        //DetectionManagerから現在の値と最大値を取得
        float current = detectionManager.GetCurrentDetection();
        float max = detectionManager.GetMaxDetection();

        //見つかり度の割合を計算 (0.0 ～ 1.0)
        //maxが0だとエラーになるのを防ぐ
        float ratio = (max > 0) ? current / max : 0;

        //Scrollbarのハンドルのサイズを見つかり度の割合に合わせて変更する
        //値が0～1の範囲に収まるようにClamp01を使う
        scrollbar.size = Mathf.Clamp01(ratio);
    }
}