using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// 画面上にカスタムカーソルを表示してマウス位置に追従させ、クリック時に効果音を再生する。Cursor.visible を管理してシステムカーソルを非表示にする。Inspectorで `cursorImage` と `parentCanvas` の割り当て必須。
/// </summary>
public class SimpleCursorController : MonoBehaviour
{
    /// <summary>
    /// カーソルとして表示する UI の Image。InspectorでD&Dして割り当てる。未設定時はコンポーネントを無効化する。
    /// </summary>
    [Header("UI参照")]
    [Tooltip("カーソルとして表示するUIのImageコンポーネント")]
    public Image cursorImage;

    /// <summary>
    /// カーソルが属する親の Canvas。Overlay モードを想定。InspectorでD&Dして割り当てる。
    /// </summary>
    [Tooltip("カーソルが属する親のCanvas")]
    public Canvas parentCanvas;

    /// <summary>
    /// カーソル表示位置の微調整（X:左右, Y:上下）。
    /// </summary>
    [Header("位置調整")]
    [Tooltip("カーソルの表示位置を微調整します (X:左右, Y:上下)")]
    public Vector2 cursorOffset;

    /// <summary>
    /// 左クリック時に再生する効果音。未設定なら音を再生しない。
    /// </summary>
    [Header("効果音設定")]
    [Tooltip("左クリックした時に鳴らす効果音")]
    public AudioClip leftClickSound;

    /// <summary>
    /// 左クリック効果音の音量（0〜1）。
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("左クリック効果音の音量")]
    public float leftClickVolume = 1.0f;

    /// <summary>
    /// 右クリック時に再生する効果音。未設定なら音を再生しない。
    /// </summary>
    [Tooltip("右クリックした時に鳴らす効果音")]
    public AudioClip rightClickSound;

    /// <summary>
    /// 右クリック効果音の音量（0〜1）。
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("右クリック効果音の音量")]
    public float rightClickVolume = 1.0f;

    /// <summary>
    /// アタッチされている AudioSource。Start で取得して再生に使用する。
    /// </summary>
    private AudioSource audioSource;

    /// <summary>
    /// MonoBehaviour.Start。最初のフレームの直前に一度呼ばれる。AudioSource を取得し、システムカーソルを非表示にして初期チェックを行う。Inspector の割り当てがない場合はコンポーネントを無効化する。
    /// </summary>
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        Cursor.visible = false;

        if (cursorImage == null || parentCanvas == null)
        {
            Debug.LogError("Cursor Image または Parent Canvas が設定されていません！このコンポーネントを無効にします。");
            this.enabled = false;
            return;
        }

        // Canvas 上の UI がマウスイベントを受け取らないように設定
        cursorImage.raycastTarget = false;
    }

    /// <summary>
    /// MonoBehaviour.Update。毎フレーム呼ばれる。マウス位置に合わせて UI カーソルの位置を更新し、クリック入力で効果音を再生する。
    /// </summary>
    void Update()
    {
        if (cursorImage == null) return;

        // Overlay モード想定: マウスのスクリーン座標をそのまま RectTransform.position に設定して表示位置を制御
        cursorImage.rectTransform.position = (Vector2)Input.mousePosition + cursorOffset;

        // 左クリック判定と効果音再生（0 = 左クリック）
        if (Input.GetMouseButtonDown(0))
        {
            if (leftClickSound != null)
            {
                audioSource.PlayOneShot(leftClickSound, leftClickVolume);
            }
        }

        // 右クリック判定と効果音再生（1 = 右クリック）
        if (Input.GetMouseButtonDown(1))
        {
            if (rightClickSound != null)
            {
                audioSource.PlayOneShot(rightClickSound, rightClickVolume);
            }
        }
    }

    /// <summary>
    /// オブジェクト破棄時にシステムカーソルを再表示する。Editor や終了処理時の状態復帰を想定。
    /// </summary>
    private void OnDestroy()
    {
        Cursor.visible = true;
    }

    /// <summary>
    /// コンポーネント無効化時にシステムカーソルを再表示する。Disable により UI 操作に戻すため。
    /// </summary>
    private void OnDisable()
    {
        Cursor.visible = true;
    }
}