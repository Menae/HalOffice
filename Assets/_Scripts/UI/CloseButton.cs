using UnityEngine;
using UnityEngine.EventSystems; // UIイベントに必須

public class CloseButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("カーソルが乗った時に表示するオブジェクト")]
    public GameObject targetObject;

    /// <summary>
    /// カーソルがこのUIの上に入った時に呼ばれる
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }

    /// <summary>
    /// カーソルがこのUIから出た時に呼ばれる
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }
}