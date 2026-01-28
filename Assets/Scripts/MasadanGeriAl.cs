using UnityEngine;
using UnityEngine.EventSystems;

public class MasadanGeriAl : MonoBehaviour, IPointerClickHandler
{
    public KesmeMasasi masa;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            masa.TasGeriAl();
        }
    }
}