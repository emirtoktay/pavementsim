using UnityEngine;
using UnityEngine.EventSystems;

public class KesmeGirisSlotu : MonoBehaviour, IDropHandler
{
    public KesmeMasasi masaScripti;

    public void OnDrop(PointerEventData eventData) 
    {
        GameObject suruklenen = eventData.pointerDrag;
        
        if (suruklenen != null) 
        {
            // Sürüklenen objenin üzerindeki InventoryItem scriptini al
            InventoryItem itemScripti = suruklenen.GetComponent<InventoryItem>();

            // Eğer script varsa (yani bu bir envanter objesiyse)
            if (itemScripti != null)
            {
                // HATA ÇÖZÜMÜ: Parantez içine slotIndex'i gönderiyoruz
                masaScripti.TasEkle(itemScripti.slotIndex); 
                
                Debug.Log($"Slot {itemScripti.slotIndex} başarıyla slota bırakıldı!");
            }
        }
    }
}