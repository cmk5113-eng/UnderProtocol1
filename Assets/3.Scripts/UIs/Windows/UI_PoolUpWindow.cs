using UnityEngine;
using UnityEngine.EventSystems;

public class UI_PoolUpWindow: MonoBehaviour,IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }
}
