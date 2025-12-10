using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// レトロ風の横並び検知メーターUI。
/// DetectionManagerから現在の検知値を取得し、目盛りの点灯数で可視化する。
/// </summary>
/// <remarks>
/// - Inspectorでの参照設定必須フィールドあり（nullチェックは一部のみ）。
/// - 目盛りの生成はStart時に1回のみ実行。Update毎に点灯状態を更新。
/// - OnValidateでインスペクタ編集時のリアルタイムプレビューに対応。
/// </remarks>
public class RetroDetectionMeter : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("監視対象のDetectionManager")]
    public DetectionManager detectionManager;

    [Tooltip("メーターの目盛りとして生成するプレハブ")]
    public GameObject meterTickPrefab;

    [Tooltip("目盛りを生成する親オブジェクト（Horizontal Layout Groupを持つオブジェクト）")]
    public Transform ticksParent;

    [Header("メーター設定")]
    [Tooltip("目盛りの総数")]
    public int numberOfTicks = 20;

    [Tooltip("目盛りと目盛りの間の距離（ピクセル単位）")]
    public float tickSpacing = 5f;

    [Tooltip("メーター全体の生成開始位置のオフセット。\nX = 左からの余白\nY = 上からの余白")]
    public Vector2Int meterOffset = Vector2Int.zero;

    [Tooltip("メーターが満タンになる見つかり度の最大値")]
    public float maxDetectionValue = 100f;

    [Tooltip("目盛りがオフの時の色")]
    public Color colorOff = Color.gray;

    [Tooltip("目盛りがオンの時の色")]
    public Color colorOn = Color.red;

    private List<Image> meterTicks = new List<Image>();

    /// <summary>
    /// Unity Start。メーターの目盛りを生成し、初期状態をセットアップする。
    /// 呼び出しタイミング: MonoBehaviour.Start（Awakeの後、最初のフレームの直前）。
    /// </summary>
    void Start()
    {
        GenerateMeter();
    }

    /// <summary>
    /// Unity Update。DetectionManagerから検知値を取得し、メーター表示を更新する。
    /// detectionManagerがnullの場合は処理をスキップ。
    /// </summary>
    void Update()
    {
        if (detectionManager == null) return;
        UpdateMeterDisplay();
    }

    /// <summary>
    /// Unity OnValidate。Inspector上で値を変更した際にリアルタイムで反映する。
    /// 呼び出しタイミング: Inspectorでの値変更時（エディタ専用）。
    /// </summary>
    private void OnValidate()
    {
        UpdateLayoutSettings();
    }

    /// <summary>
    /// HorizontalLayoutGroupに間隔とオフセット（Padding）を適用する。
    /// ticksParentまたはLayoutGroupがnullの場合は何もしない。
    /// </summary>
    private void UpdateLayoutSettings()
    {
        if (ticksParent != null)
        {
            HorizontalLayoutGroup layoutGroup = ticksParent.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.spacing = tickSpacing;
                layoutGroup.padding = new RectOffset(meterOffset.x, 0, meterOffset.y, 0);
            }
        }
    }

    /// <summary>
    /// 既存の目盛りを全削除し、設定に基づいて新しい目盛りを生成する。
    /// 生成された各目盛りは初期色（colorOff）で表示される。
    /// </summary>
    private void GenerateMeter()
    {
        UpdateLayoutSettings();

        foreach (Transform child in ticksParent)
        {
            Destroy(child.gameObject);
        }
        meterTicks.Clear();

        for (int i = 0; i < numberOfTicks; i++)
        {
            GameObject tickObject = Instantiate(meterTickPrefab, ticksParent);
            Image tickImage = tickObject.GetComponent<Image>();
            if (tickImage != null)
            {
                tickImage.color = colorOff;
                meterTicks.Add(tickImage);
            }
        }
    }

    /// <summary>
    /// 現在の検知値から点灯する目盛り数を計算し、各目盛りの色を更新する。
    /// 点灯数 = (現在値 / 最大値) * 総目盛り数（少数切り捨て）。
    /// </summary>
    private void UpdateMeterDisplay()
    {
        float currentDetection = detectionManager.GetCurrentDetection();
        float ratio = Mathf.Clamp01(currentDetection / maxDetectionValue);
        int litTicksCount = Mathf.FloorToInt(ratio * numberOfTicks);

        for (int i = 0; i < meterTicks.Count; i++)
        {
            if (i < litTicksCount)
            {
                meterTicks[i].color = colorOn;
            }
            else
            {
                meterTicks[i].color = colorOff;
            }
        }
    }
}