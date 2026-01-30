using UnityEngine;
using UnityEngine.EventSystems;

public class KesmeGirisSlotu : MonoBehaviour, IPointerClickHandler
{
    private KesmeMasasi masa;
    private KareEnvanteri envanter;

    void Start()
    {
        masa = Object.FindAnyObjectByType<KesmeMasasi>();
        envanter = Object.FindAnyObjectByType<KareEnvanteri>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && masa != null && envanter != null)
        {
            // 1. ADIM: Önce o an seçili (aktif) slotu kontrol et
            if (envanter.slotlar[envanter.aktifSlotIndex].prefab != null)
            {
                masa.TasEkle(envanter.aktifSlotIndex);
                Debug.Log("Seçili slottan taş alındı.");
            }
            // 2. ADIM: Seçili slot boşsa, tüm envanteri tara
            else
            {
                for (int i = 0; i < envanter.slotlar.Length; i++)
                {
                    if (envanter.slotlar[i].prefab != null)
                    {
                        masa.TasEkle(i); // Taş olan ilk slotu masaya gönder
                        Debug.Log("Boş slot atlandı, " + i + ". slottaki taş bulundu ve alındı.");
                        break; // Bir tane bulduğumuzda duruyoruz
                    }
                }
            }
        }
    }
}

/*using UnityEngine;
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
}*/