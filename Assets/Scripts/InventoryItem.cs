using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    // Inspector'dan her slot için bunu elle gireceğiz (0, 1, 2...)
    public int slotIndex; 

    private KesmeMasasi masa;

    void Start() {
        masa = Object.FindAnyObjectByType<KesmeMasasi>();
    }

    void Update()
    {
        // Menü açıkken ve fare üzerindeyken
        if (masa != null && masa.menuPaneli.activeSelf && Time.timeScale == 0)
        {
            if (Input.GetMouseButtonDown(0)) // Sol tık
            {
                if (IsMouseOverMe())
                {
                    // ARTIK HANGİ SLOT OLDUĞUMUZU DA GÖNDERİYORUZ
                    masa.TasEkle(slotIndex); 
                    Debug.Log("Tıklanan Slot Index: " + slotIndex);
                }
            }
        }
    }

    bool IsMouseOverMe()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), 
            Input.mousePosition, 
            null);
    }
}