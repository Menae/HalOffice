using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    // メーター全体の開始位置オフセット
    [Tooltip("メーター全体の生成開始位置のオフセット。\nX = 左からの余白\nY = 上からの余白")]
    public Vector2Int meterOffset = Vector2Int.zero;

    [Tooltip("メーターが満タンになる見つかり度の最大値")]
    public float maxDetectionValue = 100f;
    [Tooltip("目盛りがオフの時の色")]
    public Color colorOff = Color.gray;
    [Tooltip("目盛りがオンの時の色")]
    public Color colorOn = Color.red;

    private List<Image> meterTicks = new List<Image>();

    void Start()
    {
        GenerateMeter();
    }

    void Update()
    {
        if (detectionManager == null) return;
        UpdateMeterDisplay();
    }

    private void OnValidate()
    {
        // インスペクタで値を変更した際にリアルタイム反映
        UpdateLayoutSettings();
    }

    /// <summary>
    /// レイアウトグループの設定（間隔とオフセット）を適用する
    /// </summary>
    private void UpdateLayoutSettings()
    {
        if (ticksParent != null)
        {
            HorizontalLayoutGroup layoutGroup = ticksParent.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.spacing = tickSpacing;

                // オフセットをPaddingとして適用
                layoutGroup.padding = new RectOffset(meterOffset.x, 0, meterOffset.y, 0);
            }
        }
    }

    private void GenerateMeter()
    {
        // 設定を適用
        UpdateLayoutSettings();

        // 既存の削除
        foreach (Transform child in ticksParent)
        {
            Destroy(child.gameObject);
        }
        meterTicks.Clear();

        // 生成
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