using UnityEngine;
using UnityEngine.EventSystems; // Bu kütüphane UI olayları için şarttır

public class InventoryItem : MonoBehaviour, IPointerClickHandler // Tıklama özelliğini ekledik
{
    public int slotIndex; 
    private KesmeMasasi masa;

    void Start() {
        // Sahnedeki masayı bulur
        masa = Object.FindAnyObjectByType<KesmeMasasi>();
    }

    // UPDATE FONKSİYONUNU TAMAMEN SİLDİK
    // Unity bu fonksiyonu tıklandığında otomatik olarak SADECE BİR KEZ çağırır.
    public void OnPointerClick(PointerEventData eventData)
    {
        // Sadece SOL tık yapıldıysa ve menü açıksa
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (masa != null && masa.menuPaneli.activeSelf)
            {
                // Masaya sadece 1 adet taş ekler
                masa.TasEkle(slotIndex); 
                Debug.Log("Sadece 1 tık algılandı. Slot: " + slotIndex);
            }
        }
    }
}