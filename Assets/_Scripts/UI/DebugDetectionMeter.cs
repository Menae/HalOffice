using UnityEngine;
using UnityEngine.UI; // Scrollbarを扱うために必要

[RequireComponent(typeof(Scrollbar))]
/// <summary>
/// デバッグ用に検知ゲージの表示サイズをScrollbarに反映するコンポーネント。
/// DetectionManagerの現在値に基づき、Scrollbarのハンドルサイズを毎フレーム更新する。
/// </summary>
/// <remarks>
/// Unityのライフサイクル: Awakeでコンポーネント参照を取得。Updateで毎フレーム反映。
/// InspectorでDetectionManagerを割当てること（未設定時はUpdateで処理を行わない）。
/// </remarks>
public class DebugDetectionMeter : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("監視対象のDetectionManager")]
    /// <summary>
    /// 監視対象のDetectionManager。InspectorでD&D。
    /// null時はUpdateで処理を行わない。
    /// </summary>
    public DetectionManager detectionManager;

    /// <summary>
    /// このGameObjectにアタッチされたScrollbarコンポーネントの参照。
    /// AwakeでGetComponentにより取得してキャッシュする。
    /// </summary>
    private Scrollbar scrollbar;

    /// <summary>
    /// Awakeで初期化を行う。MonoBehaviour.Awakeはインスタンス化直後、Startより前に呼ばれる。
    /// Scrollbarコンポーネントを取得してキャッシュする。
    /// </summary>
    void Awake()
    {
        scrollbar = GetComponent<Scrollbar>();
    }

    /// <summary>
    /// 毎フレーム呼ばれる。MonoBehaviour.Updateにて実行。
    /// DetectionManagerから現在値と最大値を取得し、比率を計算してScrollbar.sizeに反映する。
    /// DetectionManagerが未設定の場合は何もしない。
    /// </summary>
    /// <remarks>
    /// maxが0の場合はゼロ比率を採用してゼロ除算を回避する。
    /// Scrollbar.sizeは0〜1にClampして設定する。
    /// </remarks>
    void Update()
    {
        // detectionManagerがセットされていなければ何もしない
        if (detectionManager == null) return;

        // DetectionManagerから現在の値と最大値を取得
        float current = detectionManager.GetCurrentDetection();
        float max = detectionManager.GetMaxDetection();

        // 見つかり度の割合を計算 (0.0 ～ 1.0)
        // maxが0だとエラーになるのを防ぐ
        float ratio = (max > 0) ? current / max : 0;

        // Scrollbarのハンドルのサイズを見つかり度の割合に合わせて変更する
        // 値が0～1の範囲に収まるようにClamp01を使う
        scrollbar.size = Mathf.Clamp01(ratio);
    }
}