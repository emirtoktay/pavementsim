using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class KesmeMasasi : MonoBehaviour
{
    [System.Serializable]
    public class KesmeTarifi
    {
        public string tarifAdi;        
        public GameObject girisPrefabi; 
        public GameObject cikisPrefabi; 
        public int cikisMiktari = 2;   
        public Sprite girisSprite;     
        public Sprite cikisSprite; 
    }

    [Header("Tarif Ayarları")]
    public List<KesmeTarifi> tarifler; 

    [Header("UI Referansları")]
    public GameObject menuPaneli;
    public Button bolButonu;
    public Image girisSlotOnizleme;
    public Text masadakiMiktarYazisi;

    [Header("Sağ Slot Önizleme")]
    public Image cikisSlotOnizleme; 
    public Text cikisMiktarYazisi;  

    private KareEnvanteri envanter;
    private int masadakiTasAdedi = 0;
    private KesmeTarifi aktifTarif; 

    void Start()
    {
        envanter = Object.FindAnyObjectByType<KareEnvanteri>();
        if (bolButonu != null)
            bolButonu.onClick.AddListener(TasiBol);
            
        menuPaneli.SetActive(false);
        GirisSlotunuGuncelle();
    }

    void Update()
    {
        if (menuPaneli.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            MasadakileriIadeEt();
            MenuyuKapat();
        }
    }

    public void MenuyuAc()
    {
        menuPaneli.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void MenuyuKapat()
    {
        menuPaneli.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    public void TasEkle(int tiklananSlotIndex)
    {
        if (envanter == null) return;
        var hedefSlot = envanter.slotlar[tiklananSlotIndex];
        if (hedefSlot.prefab == null || hedefSlot.miktar <= 0) return;

        KesmeTarifi bulunanTarif = null;
        foreach (var tarif in tarifler)
        {
            if (hedefSlot.prefab.name.Contains(tarif.girisPrefabi.name))
            {
                bulunanTarif = tarif;
                break;
            }
        }

        if (bulunanTarif != null)
        {
            if (masadakiTasAdedi > 0 && aktifTarif != bulunanTarif) return;
            hedefSlot.miktar--;
            if (hedefSlot.miktar <= 0) hedefSlot.prefab = null;
            aktifTarif = bulunanTarif;
            masadakiTasAdedi++;
            GirisSlotunuGuncelle();
        }
    }

    public void TasiBol()
    {
        if (masadakiTasAdedi > 0 && aktifTarif != null)
        {
            masadakiTasAdedi--;
            for (int i = 0; i < aktifTarif.cikisMiktari; i++)
            {
                envanter.OtomatikEkle(aktifTarif.cikisPrefabi);
            }
            if (masadakiTasAdedi <= 0) aktifTarif = null;
            GirisSlotunuGuncelle();
        }
    }

    public void MasadakileriIadeEt()
    {
        while (masadakiTasAdedi > 0 && aktifTarif != null)
        {
            if (envanter.OtomatikEkle(aktifTarif.girisPrefabi))
                masadakiTasAdedi--;
            else break;
        }
        aktifTarif = null;
        GirisSlotunuGuncelle();
    }

    public void TasGeriAl()
    {
        if (masadakiTasAdedi > 0 && aktifTarif != null)
        {
            if (envanter.OtomatikEkle(aktifTarif.girisPrefabi))
            {
                masadakiTasAdedi--;
                if (masadakiTasAdedi <= 0) aktifTarif = null;
                GirisSlotunuGuncelle();
            }
        }
    }

    // --- DÜZELTİLEN KISIM BURASI ---
    void GirisSlotunuGuncelle()
    {
        girisSlotOnizleme.color = Color.white;
        if(cikisSlotOnizleme != null) cikisSlotOnizleme.color = Color.white;

        if (masadakiTasAdedi > 0 && aktifTarif != null)
        {
            // SOL
            girisSlotOnizleme.sprite = aktifTarif.girisSprite;
            if (masadakiMiktarYazisi != null)
            {
                masadakiMiktarYazisi.text = masadakiTasAdedi.ToString();
                masadakiMiktarYazisi.enabled = true;
            }

            // SAĞ (Önizleme Aktif)
            if (cikisSlotOnizleme != null)
            {
                cikisSlotOnizleme.sprite = aktifTarif.cikisSprite;
            }
            if (cikisMiktarYazisi != null)
            {
                cikisMiktarYazisi.text = aktifTarif.cikisMiktari.ToString();
                cikisMiktarYazisi.enabled = true;
            }

            if(bolButonu != null) bolButonu.interactable = true;
        }
        else
        {
            // SOL TEMİZLİK
            girisSlotOnizleme.sprite = null;
            if (masadakiMiktarYazisi != null)
            {
                masadakiMiktarYazisi.text = "";
                masadakiMiktarYazisi.enabled = false;
            }

            // SAĞ TEMİZLİK (Artık gizleme yapmıyor, sadece sprite siliyor)
            if (cikisSlotOnizleme != null)
            {
                cikisSlotOnizleme.sprite = null;
            }
            if (cikisMiktarYazisi != null)
            {
                cikisMiktarYazisi.text = "";
                cikisMiktarYazisi.enabled = false;
            }

            if(bolButonu != null) bolButonu.interactable = false;
        }
    }
}

/*using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class KesmeMasasi : MonoBehaviour
{
    [System.Serializable]
    public class KesmeTarifi
    {
        public string tarifAdi;        
        public GameObject girisPrefabi; 
        public GameObject cikisPrefabi; 
        public int cikisMiktari = 2;   
        public Sprite girisSprite;     
    }

    [Header("Tarif Ayarları")]
    public List<KesmeTarifi> tarifler; 

    [Header("UI Referansları")]
    public GameObject menuPaneli;
    public Button bolButonu;
    public Image girisSlotOnizleme;
    public Text masadakiMiktarYazisi; // Miktarı gösterecek Text objesi

    private KareEnvanteri envanter;
    private int masadakiTasAdedi = 0;
    private KesmeTarifi aktifTarif; 

    void Start()
    {
        // Envanter scriptini sahnede otomatik bulur
        envanter = Object.FindAnyObjectByType<KareEnvanteri>();
        
        if (bolButonu != null)
            bolButonu.onClick.AddListener(TasiBol);
            
        menuPaneli.SetActive(false);
        GirisSlotunuGuncelle();
    }

    void Update()
    {
        // Menü açıkken ESC ile kapatma ve eşyaları iade etme
        if (menuPaneli.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            MasadakileriIadeEt();
            MenuyuKapat();
        }
    }

    // Kaldırım Sistemi için gerekli fonksiyonlar
    public void MenuyuAc()
    {
        menuPaneli.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void MenuyuKapat()
    {
        menuPaneli.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    // Tıklanan slota göre masaya taş ekleme
    public void TasEkle(int tiklananSlotIndex)
    {
        if (envanter == null) return;

        var hedefSlot = envanter.slotlar[tiklananSlotIndex];
        if (hedefSlot.prefab == null || hedefSlot.miktar <= 0) return;

        // Tıklanan taşın tariflerde olup olmadığını kontrol et
        KesmeTarifi bulunanTarif = null;
        foreach (var tarif in tarifler)
        {
            if (hedefSlot.prefab.name.Contains(tarif.girisPrefabi.name))
            {
                bulunanTarif = tarif;
                break;
            }
        }

        if (bulunanTarif != null)
        {
            // Eğer masada zaten farklı bir taş tipi varsa ekleme yapma
            if (masadakiTasAdedi > 0 && aktifTarif != bulunanTarif)
            {
                Debug.LogWarning("Masada başka tip taş var!");
                return;
            }

            hedefSlot.miktar--;
            if (hedefSlot.miktar <= 0) hedefSlot.prefab = null;

            aktifTarif = bulunanTarif;
            masadakiTasAdedi++;
            GirisSlotunuGuncelle();
        }
    }

    // Bölme butonu fonksiyonu
    void TasiBol()
    {
        if (masadakiTasAdedi > 0 && aktifTarif != null)
        {
            masadakiTasAdedi--;
            
            // Tarifteki miktar kadar çıkış prefabını envantere ekle
            for (int i = 0; i < aktifTarif.cikisMiktari; i++)
            {
                envanter.OtomatikEkle(aktifTarif.cikisPrefabi);
            }

            if (masadakiTasAdedi <= 0) aktifTarif = null;
            GirisSlotunuGuncelle();
        }
    }

    // Menü kapanırken masadakileri envantere geri ver
    public void MasadakileriIadeEt()
    {
        while (masadakiTasAdedi > 0 && aktifTarif != null)
        {
            if (envanter.OtomatikEkle(aktifTarif.girisPrefabi))
                masadakiTasAdedi--;
            else break;
        }
        aktifTarif = null;
        GirisSlotunuGuncelle();
    }

    // Sağ tık ile masadan tek tek geri alma
    public void TasGeriAl()
    {
        if (masadakiTasAdedi > 0 && aktifTarif != null)
        {
            if (envanter.OtomatikEkle(aktifTarif.girisPrefabi))
            {
                masadakiTasAdedi--;
                if (masadakiTasAdedi <= 0) aktifTarif = null;
                GirisSlotunuGuncelle();
            }
        }
    }

    // Arayüzü (Resim ve Miktar) güncelleyen yer
    void GirisSlotunuGuncelle()
    {
        // Renk bug'ını önlemek için her zaman sabit beyaz tutuyoruz
        girisSlotOnizleme.color = Color.white;

        if (masadakiTasAdedi > 0 && aktifTarif != null)
        {
            girisSlotOnizleme.sprite = aktifTarif.girisSprite;
            
            // Miktar Yazısını Güncelle
            if (masadakiMiktarYazisi != null)
            {
                masadakiMiktarYazisi.text = masadakiTasAdedi.ToString();
                masadakiMiktarYazisi.enabled = true;
            }

            bolButonu.interactable = true;
        }
        else
        {
            girisSlotOnizleme.sprite = null;
            
            // Miktar Yazısını Gizle
            if (masadakiMiktarYazisi != null)
            {
                masadakiMiktarYazisi.text = "";
                masadakiMiktarYazisi.enabled = false;
            }

            bolButonu.interactable = false;
        }
    }
}*/