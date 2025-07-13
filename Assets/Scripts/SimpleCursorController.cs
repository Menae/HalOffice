using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class SimpleCursorController : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("カーソルとして表示するUIのImageコンポーネント")]
    public Image cursorImage;
    [Tooltip("カーソルが属する親のCanvas")]
    public Canvas parentCanvas;

    [Header("効果音設定")]
    [Tooltip("左クリックした時に鳴らす効果音")]
    public AudioClip leftClickSound;
    [Range(0f, 1f)]
    [Tooltip("左クリック効果音の音量")]
    public float leftClickVolume = 1.0f;

    [Tooltip("右クリックした時に鳴らす効果音")]
    public AudioClip rightClickSound;
    [Range(0f, 1f)]
    [Tooltip("右クリック効果音の音量")]
    public float rightClickVolume = 1.0f;

    private AudioSource audioSource;

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

        cursorImage.raycastTarget = false;
    }

    void Update()
    {
        if (cursorImage == null) return;

        // --- カーソルの位置更新処理 ---
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            cursorImage.rectTransform.position = Input.mousePosition;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                Input.mousePosition,
                parentCanvas.worldCamera,
                out Vector2 localPoint
            );
            cursorImage.rectTransform.anchoredPosition = localPoint;
        }

        // ▼▼▼ 以下をすべて追加 ▼▼▼

        // --- 左クリック効果音の再生 ---
        if (Input.GetMouseButtonDown(0)) // 0は左クリック
        {
            if (leftClickSound != null)
            {
                audioSource.PlayOneShot(leftClickSound, leftClickVolume);
            }
        }

        // --- 右クリック効果音の再生 ---
        if (Input.GetMouseButtonDown(1)) // 1は右クリック
        {
            if (rightClickSound != null)
            {
                audioSource.PlayOneShot(rightClickSound, rightClickVolume);
            }
        }
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}