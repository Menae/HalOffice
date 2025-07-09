using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic; // Listを使うために必要

// IPointerDownHandlerを追加して、クリックされたことを検知できるようにする
public class DraggableStamp : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("このスタンプが承認（Approve）か却下（Reject）か")]
    public bool isApproveStamp = true;

    private Vector3 startPosition;
    private Transform startParent;
    private Canvas parentCanvas;
    private bool isBeingHeld = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        // 開始時の位置と親を記憶しておく
        startPosition = rectTransform.position;
        startParent = transform.parent;
    }

    void Update()
    {
        // 「掴んでいる」状態でなければ、何もしない
        if (!isBeingHeld) return;

        // マウスの位置に追従させる
        rectTransform.position = Input.mousePosition;

        // もう一度左クリックされたら、「スタンプを押す」処理
        if (Input.GetMouseButtonDown(0))
        {
            // マウスの下にあるUI要素をすべて取得
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            // 取得したUI要素の中にJudgementZoneがあるかチェック
            foreach (RaycastResult result in results)
            {
                JudgementZone zone = result.gameObject.GetComponent<JudgementZone>();
                if (zone != null)
                {
                    // 見つけたら、色を変えるように命令し、自分は元の位置に戻る
                    zone.ApplyStamp(isApproveStamp);
                    ResetStamp();
                    return; // 処理完了
                }
            }
        }

        // 右クリックされたら、「キャンセル」処理
        if (Input.GetMouseButtonDown(1))
        {
            ResetStamp();
        }
    }

    // このオブジェクトがクリックされた時に呼ばれる
    public void OnPointerDown(PointerEventData eventData)
    {
        // 他のスタンプが掴まれていなければ、自分を「掴む」状態にする
        // また、入力が有効なときだけ反応する
        if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;

        isBeingHeld = true;
        // ドラッグ中は最前面に表示されるように、一時的に親をCanvas直下にする
        transform.SetParent(parentCanvas.transform, true);
    }

    // スタンプを元の位置に戻す処理
    private void ResetStamp()
    {
        isBeingHeld = false;
        transform.SetParent(startParent, true);
        rectTransform.position = startPosition;
    }

    // 見た目の制御用のRectTransform変数
    private RectTransform rectTransform;
}