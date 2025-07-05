using UnityEngine;
using UnityEngine.UI; // UI.Image���������߂ɕK�v

public class CursorController : MonoBehaviour
{
    [Header("�J�[�\���̐ݒ�")]
    [Tooltip("�Q�[�����ŃJ�[�\���Ƃ��ĕ\������UI Image")]
    public Image cursorImage;

    [Header("�ʏ펞�̏��")]
    [Tooltip("���E�O�ɂ��鎞�̃J�[�\���̐F�i���Ō��̐F�j")]
    public Color normalColor = Color.white;
    [Range(0.1f, 5f)]
    [Tooltip("�ʏ펞�̃J�[�\���̕\���{��")]
    public float normalScale = 0.5f;

    [Header("���E���ł̕ω�")]
    // --- ������ ---
    [Tooltip("�y�������z�̐F")]
    public Color farColor = Color.yellow;
    [Range(0.1f, 5f)]
    [Tooltip("�y�������z�̕\���{��")]
    public float farScale = 0.6f;
    [Range(0f, 5f)]
    [Tooltip("�y�������z�̐k���̔{��")]
    public float farShakeMultiplier = 1.0f;

    // --- ������ ---
    [Tooltip("�y�������z�̐F")]
    public Color mediumColor = new Color(1.0f, 0.5f, 0f); // �I�����W�F
    [Range(0.1f, 5f)]
    [Tooltip("�y�������z�̕\���{��")]
    public float mediumScale = 0.7f;
    [Range(0f, 5f)]
    [Tooltip("�y�������z�̐k���̔{��")]
    public float mediumShakeMultiplier = 1.5f;
    [Tooltip("���̋������߂��ƃI�����W�F�ɂȂ鋫�E��")]
    public float mediumDistanceThreshold = 4.0f;

    // --- �ߋ��� ---
    [Tooltip("�y�ߋ����z�̐F")]
    public Color closeColor = Color.red;
    [Range(0.1f, 5f)]
    [Tooltip("�y�ߋ����z�̕\���{��")]
    public float closeScale = 0.8f;
    [Range(0f, 5f)]
    [Tooltip("�y�ߋ����z�̐k���̔{��")]
    public float closeShakeMultiplier = 2.5f;
    [Tooltip("���̋������߂��ƐԐF�ɂȂ鋫�E��")]
    public float closeDistanceThreshold = 2.0f;

    [Header("�Ď��Ώۂ�NPC")]
    [Tooltip("�k���̔���Ɏg������NPC�̃I�u�W�F�N�g")]
    public NPCMove_v1 targetNpc;

    // ������ �ύX�F��{�̐k���̋�����ݒ� ������
    [Header("�k���̊�{�ݒ�")]
    [Tooltip("�k���̊�{�ƂȂ鋭���B����Ɋe�����̔{�����|����")]
    public float baseShakeMagnitude = 2.0f;

    // ゲームが開始された時に一度だけ呼ばれる関数
    void Start()
    {
        // ... Start()�̒��g�͕ύX�Ȃ� ...
        Cursor.visible = false;
        if (cursorImage == null) { Debug.LogError("Cursor Image���Z�b�g����Ă��܂���I"); return; }
        cursorImage.raycastTarget = false;
        cursorImage.color = normalColor;
        cursorImage.rectTransform.localScale = Vector3.one * normalScale;
    }

    void Update()
    {
        if (cursorImage == null) return;

        Vector2 mousePosition = Input.mousePosition;

        if (targetNpc != null && targetNpc.isCursorInView)
        {
            // --- ���E���ɂ���ꍇ ---
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            float distance = Vector3.Distance(targetNpc.transform.position, mouseWorldPos);

            // ������ �ǉ��F���݂̐k���̋�����ێ�����ϐ���錾 ������
            float currentShakeMagnitude = baseShakeMagnitude;

            if (distance < closeDistanceThreshold)
            {
                // �ߋ����̏ꍇ
                cursorImage.color = closeColor;
                cursorImage.rectTransform.localScale = Vector3.one * closeScale;
                // ������ �ǉ��F�ߋ����̔{����K�p ������
                currentShakeMagnitude *= closeShakeMultiplier;
            }
            else if (distance < mediumDistanceThreshold)
            {
                // �������̏ꍇ
                cursorImage.color = mediumColor;
                cursorImage.rectTransform.localScale = Vector3.one * mediumScale;
                // ������ �ǉ��F�������̔{����K�p ������
                currentShakeMagnitude *= mediumShakeMultiplier;
            }
            else
            {
                // �������̏ꍇ
                cursorImage.color = farColor;
                cursorImage.rectTransform.localScale = Vector3.one * farScale;
                // ������ �ǉ��F�������̔{����K�p ������
                currentShakeMagnitude *= farShakeMultiplier;
            }

            // �ŏI�I�Ɍv�Z���ꂽ�����ŃJ�[�\����k�킹��
            Vector2 shakeOffset = Random.insideUnitCircle * currentShakeMagnitude;
            cursorImage.rectTransform.position = mousePosition + shakeOffset;
        }
        else
        {
            // --- ���E�O�ɂ���ꍇ ---
            cursorImage.color = normalColor;
            cursorImage.rectTransform.localScale = Vector3.one * normalScale;
            cursorImage.rectTransform.position = mousePosition;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        float distanceFromCamera = Mathf.Abs(targetNpc.transform.position.z - Camera.main.transform.position.z);
        mousePos.z = distanceFromCamera;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}