using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スクロール可能なアイテムスロットのグリッドレイアウトを管理するコンポーネント。
/// GridLayoutGroupを利用し、プレハブから指定数のスロットを動的に生成・破棄する。
/// Viewport幅に応じたセルサイズの自動フィット、差分更新による既存スロットの保護に対応。
/// UIDraggableとの連携を想定し、必要に応じてCanvasGroupを自動付与する。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIItemSlotGrid : MonoBehaviour
{
    // -----------------------------
    // References
    // -----------------------------
    [Header("参照")]
    /// <summary>
    /// このスロット一覧をスクロールさせるScrollRect。
    /// 未指定時は親階層から自動取得を試みる。
    /// </summary>
    [Tooltip("このスロット一覧をスクロールさせる ScrollRect。未指定なら親から自動取得します。")]
    public ScrollRect scrollRect;

    /// <summary>
    /// ScrollRectのContent（スロットの親Transform）。
    /// 未指定時はscrollRect.content、それも無ければ自身のRectTransformを使用。
    /// </summary>
    [Tooltip("ScrollRect の Content。未指定なら scrollRect.content、なければ自分自身の RectTransform を使用します。")]
    public RectTransform content;

    /// <summary>
    /// 縦スクロール用のScrollbar（任意）。
    /// 割り当てるとScrollRectと連動して自動スクロール表示する。
    /// </summary>
    [Tooltip("縦スクロール用の Scrollbar（任意）。割り当てると連動します。")]
    public Scrollbar verticalScrollbar;

    // -----------------------------
    // Spawning
    // -----------------------------
    [Header("スロット生成")]
    /// <summary>
    /// スロットとして生成するプレハブのルートGameObject。
    /// UIDraggableは子階層に配置されていても動作する（例: IconEach）。
    /// InspectorでD&D。
    /// </summary>
    [Tooltip("スロットのプレハブ（ルート）。UIDraggable は子に付いていてOKです（例：IconEach）。")]
    public GameObject slotPrefabRoot;

    /// <summary>
    /// 生成するスロットの合計数。
    /// 既存スロット数との差分に応じて追加/削除を行う。
    /// </summary>
    [Tooltip("生成するスロットの合計数。")]
    [Min(0)] public int totalSlots = 10;

    // -----------------------------
    // Layout
    // -----------------------------
    [Header("レイアウト")]
    /// <summary>
    /// 1行に並べるスロット数（列数）。
    /// GridLayoutGroupのConstraintCountとして使用される。
    /// </summary>
    [Tooltip("1 行に並べるスロット数（列数）。")]
    [Min(1)] public int itemsPerRow = 5;

    /// <summary>
    /// スロット同士の間隔（x=左右, y=上下）。
    /// GridLayoutGroupのspacingに反映される。
    /// </summary>
    [Tooltip("スロット同士の間隔（x=左右, y=上下）。")]
    public Vector2 spacing = new Vector2(8, 8);

    /// <summary>
    /// 外枠の余白（左・右・上・下）。
    /// Awake/OnValidateで初期化され、GridLayoutGroupのpaddingに反映される。
    /// </summary>
    [Tooltip("外枠の余白（左・右・上・下）。※ コンストラクタで生成せず、Awake/OnValidate で初期化します。")]
    public RectOffset padding;

    /// <summary>
    /// セルサイズをプレハブルートのRectTransformから自動取得するか。
    /// trueの場合、sizeDeltaまたはrect.sizeを基準値として使用する。
    /// </summary>
    [Tooltip("セルサイズをプレハブ（ルート）の RectTransform から自動取得する。")]
    public bool autoCellSizeFromPrefab = true;

    /// <summary>
    /// Viewportの幅に合わせて、列数固定のままセル幅を自動フィットするか。
    /// trueの場合、利用可能な幅を列数で均等分割してセル幅を算出する。
    /// </summary>
    [Tooltip("Viewport の幅に合わせて、列数固定のままセル幅を自動フィットする。")]
    public bool fitCellWidthToViewport = true;

    /// <summary>
    /// セルサイズを手動指定する場合のサイズ。
    /// 自動取得がOFFのとき、またはフォールバック時に有効。
    /// </summary>
    [Tooltip("セルサイズを手動指定する場合のサイズ（自動がOFFのとき有効）。")]
    public Vector2 manualCellSize = new Vector2(64, 64);

    // -----------------------------
    // Behavior
    // -----------------------------
    [Header("動作設定")]
    /// <summary>
    /// Start時に自動でRebuildを実行するか。
    /// 再生開始直後に一度だけスロットを生成したい場合にtrue。
    /// </summary>
    [Tooltip("再生開始時に自動で再生成します。")]
    public bool rebuildOnStart = true;

    /// <summary>
    /// Rebuild時にContent配下の子オブジェクトを全てクリアするか。
    /// trueの場合、手動配置したスロットも含めて削除される点に注意。
    /// </summary>
    [Tooltip("再生成時に Content の子オブジェクトを全てクリアします。")]
    public bool clearBeforeBuild = true;

    /// <summary>
    /// マウスホイールのスクロール感度。
    /// ScrollRect.scrollSensitivityに反映される。
    /// </summary>
    [Tooltip("マウスホイールのスクロール感度。")]
    public float scrollSensitivity = 30f;

    // -----------------------------
    // Compat / Debug
    // -----------------------------
    [Header("UIDraggable 互換オプション（任意）")]
    /// <summary>
    /// ドラッグ中にblocksRaycastsを切り替える想定のため、
    /// UIDraggableを持つ子GameObjectにCanvasGroupが無ければ自動で付与する。
    /// </summary>
    [Tooltip("ドラッグ中に blocksRaycasts を切り替える想定のため、足りなければ CanvasGroup を自動で付与します。")]
    public bool autoAddCanvasGroupForDrag = true;

    [Header("デバッグ")]
    /// <summary>
    /// Rebuild時に原因特定用のログを出力するか。
    /// エディタ/ランタイム両方で動作確認時に有用。
    /// </summary>
    [Tooltip("再生成時に原因特定用のログを出力します。")]
    public bool debugLogs = true;

    /// <summary>
    /// 生成直後に可視化の保険（強制Active、最小限のデバッグ背景）を挿入するか。
    /// 透明スロットの問題切り分け用。
    /// </summary>
    [Tooltip("生成直後に可視化の保険（強制Active、最小限のデバッグ背景）を入れます。問題切り分け用。")]
    public bool debugEnsureVisible = true;

    private GridLayoutGroup grid;
    private bool builtOnceAtRuntime = false;
    private bool deferredLayoutScheduled = false;

    // -----------------------------
    // Lifecycle
    // -----------------------------

    /// <summary>
    /// InspectorでResetされた際の初期化処理。
    /// 参照解決とpadding初期化を行う。
    /// </summary>
    void Reset()
    {
        ResolveReferences();
        EnsurePadding();
    }

    /// <summary>
    /// ゲーム開始時の初期化。
    /// 参照解決、padding初期化、GridLayoutGroup確保を行う。
    /// </summary>
    void Awake()
    {
        ResolveReferences();
        EnsurePadding();
        EnsureGrid();
    }

    /// <summary>
    /// 有効化された際の処理。
    /// ランタイムかつ未初期化の場合、Content配下が空でプレハブが設定されていれば即座にRebuildを実行する。
    /// </summary>
    void OnEnable()
    {
        if (Application.isPlaying && !builtOnceAtRuntime)
        {
            ResolveReferences();
            EnsurePadding();
            EnsureGrid();

            if (content != null && content.childCount == 0 &&
                slotPrefabRoot != null)
            {
                Rebuild();
            }
        }
    }

    /// <summary>
    /// ゲーム開始直後の初期化。
    /// rebuildOnStartがtrueかつ未初期化の場合にRebuildを実行する。
    /// </summary>
    void Start()
    {
        if (rebuildOnStart && !builtOnceAtRuntime)
            Rebuild();
    }

    /// <summary>
    /// Inspector編集時の検証処理。
    /// OnValidate内での即時レイアウト変更はUnityの禁止事項のため、
    /// EditorApplication.delayCallを用いて次の描画タイミングまで処理を遅延させる。
    /// </summary>
    void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || gameObject == null) return;

            EnsurePadding();
            ResolveReferences();
            EnsureGrid();
            ApplyLayoutOnly();
        };
#endif
    }

    // -----------------------------
    // Public
    // -----------------------------

    /// <summary>
    /// スロットを再生成する。
    /// clearBeforeBuildがtrueの場合は全削除後に生成、falseの場合は差分更新（既存を保護）を行う。
    /// 生成後、ScrollRect設定とレイアウト反映を実施する。
    /// ContextMenuからも呼び出し可能。
    /// </summary>
    [ContextMenu("再生成 (Rebuild)")]
    public void Rebuild()
    {
        ResolveReferences();
        EnsurePadding();
        EnsureGrid();

        if (content == null)
        {
            if (debugLogs) Debug.LogWarning("[UIItemSlotGrid] content が未解決のため生成を中止しました。", this);
            return;
        }
        if (slotPrefabRoot == null)
        {
            if (debugLogs) Debug.LogWarning("[UIItemSlotGrid] slotPrefabRoot が設定されていません。生成を中止します。", this);
            return;
        }

        // 強制クリアモード（手動設定も消える）
        if (clearBeforeBuild)
        {
            for (int i = content.childCount - 1; i >= 0; --i)
            {
                var child = content.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        // 差分更新ロジック（既存を守りつつ増減）
        int currentCount = content.childCount;
        int targetCount = totalSlots;

        // 不足分を追加生成
        if (currentCount < targetCount)
        {
            int addCount = targetCount - currentCount;
            for (int i = 0; i < addCount; i++)
            {
                var go = Instantiate(slotPrefabRoot, content);

                if (!go.activeSelf) go.SetActive(true);

                var rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    rt.anchoredPosition3D = Vector3.zero;
                }

                // UIDraggable用のCanvasGroup自動付与
                if (autoAddCanvasGroupForDrag)
                {
                    var draggable = go.GetComponentInChildren<UIDraggable>(true);
                    if (draggable != null && draggable.GetComponent<CanvasGroup>() == null)
                    {
                        draggable.gameObject.AddComponent<CanvasGroup>();
                    }
                }

                // 可視化の保険（デバッグ表示用）
                if (debugEnsureVisible)
                {
                    bool hasRenderable = false;
                    var graphics = go.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                    foreach (var g in graphics)
                    {
                        if (g is UnityEngine.UI.Image img)
                        {
                            if (img.sprite != null && img.color.a > 0f && img.enabled) { hasRenderable = true; break; }
                        }
                        else if (g.color.a > 0f && g.enabled)
                        {
                            hasRenderable = true; break;
                        }
                    }

                    if (!hasRenderable)
                    {
                        var dbgGO = new GameObject("_DebugBG", typeof(UnityEngine.UI.Image));
                        dbgGO.transform.SetParent(go.transform, false);
                        var dbgImg = dbgGO.GetComponent<UnityEngine.UI.Image>();
                        dbgImg.raycastTarget = false;
#if UNITY_EDITOR
                        dbgImg.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
#endif
                        dbgImg.color = new Color(1f, 0f, 1f, 0.4f);

                        var dbgRT = dbgGO.GetComponent<RectTransform>();
                        dbgRT.anchorMin = Vector2.zero;
                        dbgRT.anchorMax = Vector2.one;
                        dbgRT.offsetMin = Vector2.zero;
                        dbgRT.offsetMax = Vector2.zero;
                    }
                }
            }
        }
        // 過剰分を末尾から削除（前方の手動設定スロットを保護）
        else if (currentCount > targetCount)
        {
            int removeCount = currentCount - targetCount;
            for (int i = 0; i < removeCount; i++)
            {
                var child = content.GetChild(content.childCount - 1);
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        // スクロール設定とレイアウト更新
        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = scrollSensitivity;
            if (verticalScrollbar != null)
                scrollRect.verticalScrollbar = verticalScrollbar;
        }

        // Viewportサイズが未確定な場合があるため、次フレームで再レイアウト
        if (Application.isPlaying) StartDeferredLayout();
        else ApplyLayoutOnly();

        builtOnceAtRuntime = true;
        if (debugLogs) Debug.Log($"[UIItemSlotGrid] Updated slots. Current Total: {content.childCount}", this);
    }

    /// <summary>
    /// 指定したアイテムデータリストをスロットに流し込む。
    /// Content配下の子オブジェクト数とリスト件数の少ない方までループし、
    /// 各UIDraggableにitemDataを設定する。
    /// </summary>
    /// <param name="items">流し込むアイテムデータのリスト。nullチェックは呼び出し側で行うこと。</param>
    public void PopulateItems(IList<ItemData> items)
    {
        int childCount = content != null ? content.childCount : 0;
        int count = Mathf.Min(items.Count, childCount);
        for (int i = 0; i < count; i++)
        {
            var child = content.GetChild(i);
            var draggable = child.GetComponentInChildren<UIDraggable>(true);
            if (draggable != null)
            {
                draggable.itemData = items[i];
            }
        }
    }

    // -----------------------------
    // Internal helpers
    // -----------------------------

    /// <summary>
    /// 参照を自動解決する。
    /// scrollRectが未設定なら親階層から取得、contentが未設定ならscrollRect.contentまたは自身を使用。
    /// contentのアンカー・ピボットを上揃え（0,1）（1,1）に統一する。
    /// </summary>
    private void ResolveReferences()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInParent<ScrollRect>(true);

        if (content == null)
        {
            if (scrollRect != null && scrollRect.content != null)
                content = scrollRect.content;
            else
                content = GetComponent<RectTransform>();
        }

        if (content != null)
        {
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// paddingがnullの場合、デフォルト値（8, 8, 8, 8）で初期化する。
    /// </summary>
    private void EnsurePadding()
    {
        if (padding == null)
            padding = new RectOffset(8, 8, 8, 8);
    }

    /// <summary>
    /// Content上のGridLayoutGroupを取得または追加し、基本設定を行う。
    /// constraint: FixedColumnCount、startAxis: Horizontal、startCorner: UpperLeftに固定。
    /// </summary>
    private void EnsureGrid()
    {
        if (content == null) return;

        grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
    }

    /// <summary>
    /// レイアウト設定のみを再適用する公開メソッド。
    /// スロット生成を行わず、セルサイズやspacing、paddingの再計算と反映のみ実施。
    /// </summary>
    public void ApplyLayoutOnly()
    {
        ApplyLayoutOnlyInternal(force: false);
    }

    /// <summary>
    /// レイアウト設定を内部的に再適用する。
    /// セルサイズの自動取得、Viewport幅へのフィット、Content高さの算出を行い、
    /// GridLayoutGroupとContentのRectTransformに反映する。
    /// forceがtrueの場合は遅延スケジュール中でも即座に実行する。
    /// </summary>
    /// <param name="force">trueの場合、Viewport幅が未確定でも強制的にレイアウトを適用する。</param>
    private void ApplyLayoutOnlyInternal(bool force)
    {
        if (content == null || grid == null) return;

        Vector2 cell = manualCellSize;

        // プレハブから自動取得（stretch対策フォールバック）
        if (autoCellSizeFromPrefab && slotPrefabRoot != null)
        {
            var rt = slotPrefabRoot.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 cand = rt.sizeDelta;
                if (cand.x <= 0f || cand.y <= 0f) cand = rt.rect.size;
                if (cand.x > 0f) cell.x = cand.x;
                if (cand.y > 0f) cell.y = cand.y;
            }
        }

        // 幅フィット：Viewportの幅が未確定なら次フレームに再実行
        if (fitCellWidthToViewport)
        {
            float viewportW = GetViewportWidth();
            if (viewportW > 1f)
            {
                float totalSpace = spacing.x * (itemsPerRow - 1);
                float totalPad = (padding != null ? padding.left + padding.right : 0);
                float w = (viewportW - totalSpace - totalPad) / Mathf.Max(1, itemsPerRow);
                cell.x = Mathf.Max(1f, w);
            }
            else if (!deferredLayoutScheduled && Application.isPlaying && !force)
            {
                StartDeferredLayout();
            }
        }

        // 高さフォールバック（ゼロを許さない）
        if (cell.y <= 0f)
        {
            if (manualCellSize.y > 0f) cell.y = manualCellSize.y;
            else if (cell.x > 0f) cell.y = cell.x;
            else cell.y = 64f;
        }

        // GridLayoutGroupに反映
        grid.cellSize = new Vector2(Mathf.Max(1f, cell.x), Mathf.Max(1f, cell.y));
        grid.spacing = spacing;
        grid.padding = padding ?? new RectOffset();
        grid.constraintCount = Mathf.Max(1, itemsPerRow);

        // Contentの高さを算出して設定
        int cols = Mathf.Max(1, itemsPerRow);
        int count = Mathf.Max(totalSlots, content.childCount);
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)count / cols));

        float padV = (padding != null ? padding.top + padding.bottom : 0);
        float height = padV + rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    /// <summary>
    /// Viewportの幅を取得する。
    /// scrollRect.viewportが有効ならその幅、無効ならcontentの親RectTransformの幅を使用。
    /// それでも取得できなければCanvas.ForceUpdateCanvasesを呼んで再試行する。
    /// </summary>
    /// <returns>Viewportの幅（ピクセル単位）。取得失敗時は0以下。</returns>
    private float GetViewportWidth()
    {
        float width = 0f;
        if (scrollRect != null && scrollRect.viewport != null)
            width = scrollRect.viewport.rect.width;

        if (width <= 1f)
        {
            var p = content != null ? content.parent as RectTransform : null;
            if (p != null) width = p.rect.width;
        }
        if (width <= 1f)
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null && scrollRect.viewport != null)
                width = scrollRect.viewport.rect.width;
        }
        return width;
    }

    /// <summary>
    /// 遅延レイアウト更新を開始する。
    /// 既にスケジュール済みの場合は重複起動しない。
    /// </summary>
    private void StartDeferredLayout()
    {
        if (deferredLayoutScheduled) return;
        StartCoroutine(DeferredLayoutRoutine());
    }

    /// <summary>
    /// 1フレーム待機してからレイアウトを再適用するコルーチン。
    /// Canvas更新後にApplyLayoutOnlyInternal(force:true)を実行し、遅延フラグをリセットする。
    /// </summary>
    /// <returns>コルーチン処理。</returns>
    private IEnumerator DeferredLayoutRoutine()
    {
        deferredLayoutScheduled = true;
        yield return null;
        Canvas.ForceUpdateCanvases();
        ApplyLayoutOnlyInternal(force: true);
        deferredLayoutScheduled = false;
    }
}