using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HighlightScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject highlight;

    public void OnPointerEnter(PointerEventData eventData)
    {
        highlight.SetActive(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        highlight.SetActive(false);
    }
    
}