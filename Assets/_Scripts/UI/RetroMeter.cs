using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 親要素（RectTransform）の高さに基づいて、指定されたサイズのマス目を自動的に積み上げて表示するメーター。
/// グラデーション機能により、高さに応じた色の変化を表現可能。
/// </summary>
[RequireComponent(typeof(VerticalLayoutGroup))]
public class RetroMeter : MonoBehaviour
{
    [Header("Settings")]
    /// <summary>
    /// マス目同士の間隔。
    /// </summary>
    [Tooltip("マス目同士の間隔")]
    public float spacing = 2.0f;

    /// <summary>
    /// 1マスの基本高さ。
    /// </summary>
    [Tooltip("1マスの基本高さ")]
    public float segmentHeight = 20f;

    [Header("Visuals")]
    /// <summary>
    /// アクティブ時の色（グラデーション）。
    /// エディタのGradient設定で左側が「下」、右側が「上」の色に対応します。
    /// </summary>
    [Tooltip("アクティブ時の色設定。左(0.0)が下部、右(1.0)が上部の色に対応します。")]
    public Gradient activeGradient;

    /// <summary>
    /// 非アクティブ時の色。
    /// </summary>
    [Tooltip("非アクティブ時の色")]
    public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    /// <summary>
    /// 各マス目に使用する画像のプレハブ。
    /// </summary>
    [Tooltip("各マス目に使用する画像のプレハブ")]
    public Image cellPrefab;

    /// <summary>
    /// 現在生成されているマス目の総数。
    /// </summary>
    public int MaxSteps => cells.Count;

    // 内部キャッシュ
    private List<Image> cells = new List<Image>();
    private RectTransform rectTransform;
    private VerticalLayoutGroup layoutGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutGroup = GetComponent<VerticalLayoutGroup>();

        // デフォルトのグラデーションが未設定の場合のフォールバック（緑一色）
        if (activeGradient == null)
        {
            activeGradient = new Gradient();
            activeGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.green, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            );
        }

        SetupLayout();
    }

    /// <summary>
    /// 現在の設定と親要素の高さに基づいてメーターを構築する。
    /// 実行時にRectTransformの高さが変更された場合は、再度このメソッドを呼ぶ必要がある。
    /// </summary>
    public void InitializeMeter()
    {
        // 既存の要素をクリア
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        cells.Clear();

        float containerHeight = rectTransform.rect.height;
        float currentHeight = 0f;
        int fullCellCount = 0;

        // 親の高さを超えない範囲で、完全なサイズのマスがいくつ入るか計算
        while (true)
        {
            float nextSpaceNeeded = segmentHeight;
            // 2つ目以降はSpacing分を加算
            if (fullCellCount > 0) nextSpaceNeeded += spacing;

            // 浮動小数点の誤差を考慮して判定
            if (currentHeight + nextSpaceNeeded <= containerHeight + 0.1f)
            {
                currentHeight += nextSpaceNeeded;
                fullCellCount++;
            }
            else
            {
                break;
            }
        }

        // 余りのスペースを計算
        float remainder = containerHeight - currentHeight;

        // 余りマスを追加する場合、そのマスのためのSpacingを確保できるか確認
        float partialCellHeight = (fullCellCount > 0) ? remainder - spacing : remainder;

        // 描画する価値があるサイズ（1px以上）であれば余りマスを追加
        bool addPartialCell = partialCellHeight > 1.0f;

        // VerticalLayoutGroupの並び順に合わせて生成
        // index 0 が最上部（余りマス）となる仕様
        if (addPartialCell)
        {
            CreateCell(partialCellHeight);
        }

        for (int i = 0; i < fullCellCount; i++)
        {
            CreateCell(segmentHeight);
        }
    }

    /// <summary>
    /// 指定された高さでセルを生成し、リストに追加する。
    /// </summary>
    /// <param name="height">セルの高さ</param>
    private void CreateCell(float height)
    {
        Image newCell;
        if (cellPrefab != null)
        {
            newCell = Instantiate(cellPrefab, transform);
        }
        else
        {
            GameObject go = new GameObject($"Cell_{cells.Count}", typeof(Image));
            go.transform.SetParent(transform, false);
            newCell = go.GetComponent<Image>();
        }

        // LayoutElementで高さを固定する
        LayoutElement le = newCell.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleHeight = 0;
        le.minHeight = 0;
        le.preferredWidth = -1;
        le.flexibleWidth = 1;

        newCell.color = inactiveColor;
        cells.Add(newCell);
    }

    /// <summary>
    /// VerticalLayoutGroupの設定を強制的に適用する。
    /// </summary>
    private void SetupLayout()
    {
        if (layoutGroup == null) layoutGroup = GetComponent<VerticalLayoutGroup>();

        layoutGroup.spacing = spacing;
        // 下から積み上げるような見た目にするためLowerCenterを使用
        layoutGroup.childAlignment = TextAnchor.LowerCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        // マス目のサイズを個別に制御するため、ForceExpandは無効化する
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
    }

    /// <summary>
    /// 値を更新してメーターの表示を変更する。
    /// グラデーション設定に基づいて、高さに応じた色を適用する。
    /// </summary>
    /// <param name="currentValue">点灯させるマスの数</param>
    public void UpdateMeter(int currentValue)
    {
        // 初期化されていない、または不正な状態であれば再構築
        if (cells.Count == 0 || cells[0] == null)
        {
            InitializeMeter();
        }

        int max = cells.Count;
        int targetCount = Mathf.Clamp(currentValue, 0, max);

        // リストの先頭(index 0)が最上部、末尾が最下部となっている仕様に基づく計算
        int thresholdIndex = max - targetCount;

        for (int i = 0; i < max; i++)
        {
            if (i >= thresholdIndex)
            {
                // アクティブ状態
                // マスの位置に基づいてグラデーションから色を取得する
                // index 0（最上部） = time 1.0f
                // index max-1（最下部） = time 0.0f
                float time = 0f;
                if (max > 1)
                {
                    time = 1f - ((float)i / (max - 1));
                }
                else
                {
                    time = 1f; // 1つしかない場合は最上部扱い
                }

                cells[i].color = activeGradient.Evaluate(time);
            }
            else
            {
                // 非アクティブ状態
                cells[i].color = inactiveColor;
            }
        }
    }

    /// <summary>
    /// 0.0〜1.0の正規化された値でメーターを更新する。
    /// </summary>
    /// <param name="normalizedValue">正規化された値（0.0〜1.0）</param>
    public void UpdateMeterNormalized(float normalizedValue)
    {
        int value = Mathf.RoundToInt(normalizedValue * cells.Count);
        UpdateMeter(value);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (layoutGroup != null)
        {
            layoutGroup.spacing = spacing;
        }
    }
#endif
}