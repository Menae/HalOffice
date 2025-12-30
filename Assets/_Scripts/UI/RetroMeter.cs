using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// マス目（グリッド）を積み上げて表示するレトロなメーターUI。
/// インスペクタで指定した解像度（maxSteps）に応じてマス目を自動生成する。
/// </summary>
[RequireComponent(typeof(VerticalLayoutGroup))]
public class RetroMeter : MonoBehaviour
{
    [Header("Meter Settings")]
    [Tooltip("メーターのメモリの細かさ（最大値）。例：10なら10個のマスができる")]
    public int maxSteps = 10;

    [Tooltip("マス目同士の間隔")]
    public float spacing = 2.0f;

    [Header("Visuals")]
    [Tooltip("点灯している時の色")]
    public Color activeColor = new Color(0f, 1f, 0f, 1f); // 緑

    [Tooltip("消灯している時の色（背景色）")]
    public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 暗いグレー

    [Tooltip("マス目として使用する画像のプレハブ（指定がなければデフォルトの四角形を生成）")]
    public Image cellPrefab;

    // 内部キャッシュ用リスト
    private List<Image> cells = new List<Image>();
    private RectTransform rectTransform;
    private VerticalLayoutGroup layoutGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutGroup = GetComponent<VerticalLayoutGroup>();

        // レイアウト設定の初期化
        SetupLayout();
    }

    /// <summary>
    /// メーターの初期構築を行う。
    /// 既存のマスがあれば消去し、maxSteps分のマスを再生成する。
    /// </summary>
    public void InitializeMeter()
    {
        // 既存の子オブジェクトを掃除
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        cells.Clear();

        // マス目のサイズ計算
        // 全体の高さから隙間分の合計を引き、個数で割る
        float totalHeight = rectTransform.rect.height;
        float totalSpacing = spacing * (maxSteps - 1);
        float cellHeight = (totalHeight - totalSpacing) / maxSteps;

        // マス目の生成
        for (int i = 0; i < maxSteps; i++)
        {
            Image newCell;
            if (cellPrefab != null)
            {
                newCell = Instantiate(cellPrefab, transform);
            }
            else
            {
                // プレハブがない場合はコードで生成
                GameObject go = new GameObject($"Cell_{i}", typeof(Image));
                go.transform.SetParent(transform, false);
                newCell = go.GetComponent<Image>();
            }

            // レイアウト要素として高さを指定
            LayoutElement le = newCell.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = cellHeight;
            le.flexibleHeight = 0;
            le.minHeight = 0;

            // 幅は親に合わせる
            le.preferredWidth = -1;
            le.flexibleWidth = 1;

            newCell.color = inactiveColor;
            cells.Add(newCell);
        }

        // 下から積み上げるために逆順にする必要があればここで調整できるが、
        // VerticalLayoutGroupのChild AlignmentがBottomならリスト順で積み上がる。
    }

    /// <summary>
    /// LayoutGroupの設定をコード側で強制適用する
    /// </summary>
    private void SetupLayout()
    {
        if (layoutGroup == null) layoutGroup = GetComponent<VerticalLayoutGroup>();

        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.LowerCenter; // 下から上へ
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false; // LayoutElementに従う
        layoutGroup.childForceExpandWidth = true;
    }

    /// <summary>
    /// 現在値を反映して表示を更新する。
    /// </summary>
    /// <param name="currentValue">現在の量（0 ～ maxSteps）</param>
    public void UpdateMeter(int currentValue)
    {
        // メーターがまだ生成されていない、またはステップ数が変わった場合は再生成
        if (cells.Count != maxSteps)
        {
            InitializeMeter();
        }

        // clamp処理（範囲外の数値を丸める）
        int targetCount = Mathf.Clamp(currentValue, 0, maxSteps);

        // VerticalLayoutGroupはindex 0を上に置くため、
        // 下から積み上げるには、リストの末尾から点灯させる必要がある。
        int thresholdIndex = maxSteps - targetCount;

        for (int i = 0; i < cells.Count; i++)
        {
            // 閾値以上のインデックスなら点灯（リストの後ろ側＝画面の下側）
            if (i >= thresholdIndex)
            {
                cells[i].color = activeColor;
            }
            else
            {
                cells[i].color = inactiveColor;
            }
        }
    }

    /// <summary>
    /// 0.0f～1.0fの割合で値を設定する場合（完成度など）
    /// </summary>
    public void UpdateMeterNormalized(float normalizedValue)
    {
        int value = Mathf.RoundToInt(normalizedValue * maxSteps);
        UpdateMeter(value);
    }

    // インスペクタで値をいじった時に即座に反映させるための処理
    private void OnValidate()
    {
        if (Application.isPlaying && cells.Count > 0)
        {
            // 実行中に変更されたら再構築（重いので注意だがデバッグには便利）
            // InitializeMeter(); // ※安全のためコメントアウト推奨。必要なら呼ぶ。
        }
    }
}