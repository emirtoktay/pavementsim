using UnityEngine;
using UnityEngine.EventSystems;

public class KesmeCikisSlotu : MonoBehaviour, IPointerClickHandler
{
    private KesmeMasasi masa;

    void Start()
    {
        masa = Object.FindAnyObjectByType<KesmeMasasi>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Sağdaki slota sol tıklandığında masadaki TasiBol fonksiyonunu çalıştır
        if (eventData.button == PointerEventData.InputButton.Left && masa != null)
        {
            masa.TasiBol();
        }
    }
}