using System.Collections.Generic;
using UnityEngine;

public class KareEnvanteri : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public GameObject prefab;
        public int miktar;
    }

    [Header("Envanter Ayarları")]
    public int MaxSlotKapasitesi = 8;
    public Slot[] slotlar = new Slot[2]; 
    public int aktifSlotIndex = 0;

    // Kolay erişim özellikleri
    public GameObject SuankiKarePrefabi => slotlar[aktifSlotIndex].prefab;
    public int SuankiMiktar => slotlar[aktifSlotIndex].miktar;

    void Update()
    {
        // 1 ve 2 Tuşlarıyla slot seçimi
        if (Input.GetKeyDown(KeyCode.Alpha1)) aktifSlotIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) aktifSlotIndex = 1;

        // Mouse tekerleği ile slot değişimi
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) aktifSlotIndex = (aktifSlotIndex + 1) % 2;
        else if (scroll < 0f) aktifSlotIndex = (aktifSlotIndex == 0) ? 1 : 0;
    }

    /// <summary>
    /// Sadece aktif olan slotun yeni bir kare alıp alamayacağını kontrol eder.
    /// </summary>
    public bool KareAlabilirmi(GameObject eklenecekPrefab)
    {
        if (eklenecekPrefab == null) return false;

        Slot aktifSlot = slotlar[aktifSlotIndex];

        // 1. Durum: Slot boşsa alabilir.
        if (aktifSlot.prefab == null) 
            return true;

        // 2. Durum: Slot aynı taşla doluysa ve kapasite aşılmadıysa alabilir.
        if (aktifSlot.prefab.name == eklenecekPrefab.name && aktifSlot.miktar < MaxSlotKapasitesi)
            return true;

        // Diğer durumlar (Slot dolu veya farklı taş var): Alamaz.
        return false;
    }

    /// <summary>
    /// Taşı sadece seçili olan aktif slota ekler. Otomatik slot değiştirmez.
    /// </summary>
    public void KareEkle(GameObject yeniPrefab)
    {
        if (yeniPrefab == null) return;

        Slot aktifSlot = slotlar[aktifSlotIndex];

        // Slot tamamen boşsa
        if (aktifSlot.prefab == null)
        {
            aktifSlot.prefab = yeniPrefab;
            aktifSlot.miktar = 1;
        }
        // Slot zaten bu taştan içeriyorsa ve yer varsa
        else if (aktifSlot.prefab.name == yeniPrefab.name && aktifSlot.miktar < MaxSlotKapasitesi)
        {
            aktifSlot.miktar++;
        }
        // Not: Eğer slot doluysa veya farklı taş varsa, kod buraya gelmeden 
        // KareAlabilirmi kontrolünde takılacağı için işlem yapmaz.
    }

    /// <summary>
    /// Aktif slottaki taşı kullanır ve biterse slotu temizler.
    /// </summary>
    public void KareKullan()
    {
        if (slotlar[aktifSlotIndex].miktar > 0)
        {
            slotlar[aktifSlotIndex].miktar--;
            
            // Taş bittiyse slottan prefab referansını temizle
            if (slotlar[aktifSlotIndex].miktar <= 0)
            {
                slotlar[aktifSlotIndex].prefab = null;
            }
        }
    }
}
/*using System.Collections.Generic;
using UnityEngine;

public class KareEnvanteri : MonoBehaviour
{
    [Header("Envanter Ayarları")]
    public int MaxKareKapasitesi = 2;
    
    private List<GameObject> kareListesi = new List<GameObject>();

    public int EldekiKareSayisi => kareListesi.Count;
    // Bu özellik artık listenin son elemanını döndürür (Read-only)
    public GameObject SuankiKarePrefabi => kareListesi.Count > 0 ? kareListesi[kareListesi.Count - 1] : null;

    public bool KareAlabilirmi() => kareListesi.Count < MaxKareKapasitesi;
    
    public void KareEkle(GameObject yeniPrefab)
    {
        kareListesi.Add(yeniPrefab);
    }

    public void KareKullan()
    {
        if (kareListesi.Count > 0) 
            kareListesi.RemoveAt(kareListesi.Count - 1);
    }
}*/