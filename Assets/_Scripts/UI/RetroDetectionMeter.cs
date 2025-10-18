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
    [Tooltip("メーターが満タンになる見つかり度の最大値")]
    public float maxDetectionValue = 100f;
    [Tooltip("目盛りがオフの時の色")]
    public Color colorOff = Color.gray;
    [Tooltip("目盛りがオンの時の色")]
    public Color colorOn = Color.red;

    // 生成した目盛りのImageコンポーネントを管理するためのリスト
    private List<Image> meterTicks = new List<Image>();

    void Start()
    {
        // 起動時にメーターを自動生成する
        GenerateMeter();
    }

    void Update()
    {
        // detectionManagerが設定されていなければ何もしない
        if (detectionManager == null) return;

        // メーターの表示を更新する
        UpdateMeterDisplay();
    }

    /// <summary>
    /// 設定に基づいてメーターの目盛りを自動生成する
    /// </summary>
    private void GenerateMeter()
    {
        // 念のため、既にあればクリアする
        foreach (Transform child in ticksParent)
        {
            Destroy(child.gameObject);
        }
        meterTicks.Clear();

        // 設定された数の目盛りを生成し、リストに追加する
        for (int i = 0; i < numberOfTicks; i++)
        {
            GameObject tickObject = Instantiate(meterTickPrefab, ticksParent);
            Image tickImage = tickObject.GetComponent<Image>();
            if (tickImage != null)
            {
                tickImage.color = colorOff; // 初期色はオフ
                meterTicks.Add(tickImage);
            }
        }
    }

    /// <summary>
    /// 現在の見つかり度に応じてメーターの見た目を更新する
    /// </summary>
    private void UpdateMeterDisplay()
    {
        // 現在の見つかり度を取得
        float currentDetection = detectionManager.GetCurrentDetection();

        // 見つかり度を0-1の割合に変換
        float ratio = Mathf.Clamp01(currentDetection / maxDetectionValue);

        // オンにすべき目盛りの数を計算する
        // (例: ratioが0.26なら、20 * 0.26 = 5.2 -> 5個の目盛りがオンになる)
        int litTicksCount = Mathf.FloorToInt(ratio * numberOfTicks);

        // 全ての目盛りをループし、オンかオフかを判定して色を設定する
        for (int i = 0; i < meterTicks.Count; i++)
        {
            // iがオンにすべき数より小さい場合、その目盛りはオンにする
            if (i < litTicksCount)
            {
                meterTicks[i].color = colorOn;
            }
            else // そうでなければオフにする
            {
                meterTicks[i].color = colorOff;
            }
        }
    }
}