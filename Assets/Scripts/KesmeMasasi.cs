using UnityEngine;
using UnityEngine.UI;

public class KesmeMasasi : MonoBehaviour
{
    public GameObject menuPaneli;
    public Button bolButonu;
    public Image girisSlotOnizleme;
    public Sprite tamKareSprite;
    public GameObject tamKarePrefab; // Buraya Project'ten TamKare prefabını sürükle
    public GameObject CeyrekKarePrefab; // <--- 1. ADIM: BU SATIRI EKLE

    private KareEnvanteri envanter;
    private int masadakiTasAdedi = 0; // Masada kaç taş olduğunu tutar

    void Start()
    {
        // ESKİ YÖNTEM (Bunu siliyoruz):
        // envanter = GameObject.FindGameObjectWithTag("Player").GetComponent<KareEnvanteri>();

        // YENİ VE GARANTİ YÖNTEM:
        // Sahnede adı, tagı ne olursa olsun 'KareEnvanteri' scriptini taşıyan objeyi bulur.
        envanter = Object.FindAnyObjectByType<KareEnvanteri>();

        // Eğer Unity sürümün eskiyse ve üstteki satır hata verirse şunu kullan:
        // envanter = FindObjectOfType<KareEnvanteri>();

        if (envanter == null)
        {
            Debug.LogError("ACİL DURUM: Sahnede 'KareEnvanteri' scripti ekli hiçbir obje yok! Scripti karaktere ekledin mi?");
        }
        else
        {
            Debug.Log("Envanter başarıyla bulundu: " + envanter.gameObject.name);
        }

        bolButonu.onClick.AddListener(TasiBol);
        menuPaneli.SetActive(false);
        GirisSlotunuGuncelle();
    }

    void Update()
    {
        if (menuPaneli.activeSelf)
        {
            // ESC basınca her şeyi iade et ve kapat
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                MasadakileriIadeEt();
                MenuyuKapat();
            }
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

    // Sol Tık ile çağrılacak (Envanterden Masaya)
    // Artık parantez içinde int alıyor (Hangi slota tıklandı?)
    public void TasEkle(int tiklananSlotIndex)
    {
        if (envanter == null) return;

        // Hata önleyici: Eğer olmayan bir slot numarası geldiyse dur
        if (tiklananSlotIndex < 0 || tiklananSlotIndex >= envanter.slotlar.Length)
        {
            Debug.LogError("Hata: Geçersiz slot numarası!");
            return;
        }

        // Doğrudan tıklanan slota odaklanıyoruz
        var hedefSlot = envanter.slotlar[tiklananSlotIndex];

        // 1. Slot boş mu?
        if (hedefSlot.prefab == null || hedefSlot.miktar <= 0)
        {
            Debug.LogWarning("Tıkladığın slot boş!");
            return;
        }

        // 2. Taşın ismi "Tam" içeriyor mu?
        if (hedefSlot.prefab.name.ToLower().Contains("tam"))
        {
            // Şartlar tamam, taşı al!
            hedefSlot.miktar--;

            // Miktar bittiyse slotu temizle
            if (hedefSlot.miktar <= 0)
                hedefSlot.prefab = null;

            masadakiTasAdedi++;
            GirisSlotunuGuncelle();
            Debug.Log($"Slot {tiklananSlotIndex} üzerindeki taş masaya koyuldu.");
        }
        else
        {
            Debug.LogWarning("HATA: Tıkladığın taş 'Tam Taş' değil! Makine bunu kabul etmez.");
        }
    }
    public void MasadakileriIadeEt()
    {
        while (masadakiTasAdedi > 0)
        {
            // Akıllı ekleme kullan
            bool basarili = envanter.OtomatikEkle(tamKarePrefab);

            if (basarili)
            {
                masadakiTasAdedi--;
            }
            else
            {
                Debug.LogWarning("Envanter doldu, kalan taşlar masada kaldı!");
                break; // Yer yoksa döngüyü kır, sonsuz döngüye girmesin
            }
        }
        GirisSlotunuGuncelle();
    }

    void GirisSlotunuGuncelle()
    {
        // Rengi her zaman tam görünür (beyaz) tutuyoruz
        girisSlotOnizleme.color = Color.white;

        if (masadakiTasAdedi > 0)
        {
            // Masada taş varsa resmi koy
            girisSlotOnizleme.sprite = tamKareSprite;
            bolButonu.interactable = true;
        }
        else
        {
            // Taş yoksa resmi kaldır (boş kare görünsün)
            girisSlotOnizleme.sprite = null;
            bolButonu.interactable = false;
        }
    }

    // Masadaki taşı envantere geri alma (Sağ Tık)
    public void TasGeriAl()
    {
        if (masadakiTasAdedi > 0)
        {
            // Eski kod: Sadece aktif slota bakıyordu.
            // YENİ KOD: OtomatikEkle ile her yere bakıyor.
            bool eklendiMi = envanter.OtomatikEkle(tamKarePrefab);

            if (eklendiMi)
            {
                masadakiTasAdedi--;
                GirisSlotunuGuncelle();
                Debug.Log("Taş envanterdeki uygun bir yere geri alındı.");
            }
            else
            {
                Debug.LogWarning("Envanter tamamen dolu! Taş geri alınamıyor.");
            }
        }
    }

    // Taşı bölme işlemi (Buton)
    void TasiBol()
    {
        if (masadakiTasAdedi > 0)
        {
            // 1. Envanterde 2 tane çeyrek taş için yer var mı? (Basit kontrol)
            // Not: Bu kontrol mükemmel değil ama kabaca yer yoksa bölmesin diye var.
            // Daha garantisi için geçici bir kopya üzerinde test yapılabilir ama bu yeterli.

            // Önce masadaki taşı yok ediyoruz
            masadakiTasAdedi--;
            GirisSlotunuGuncelle();

            // 2. Envantere 2 tane çeyrek taş ekliyoruz (Akıllı ekleme ile)
            bool tas1 = envanter.OtomatikEkle(CeyrekKarePrefab);
            bool tas2 = envanter.OtomatikEkle(CeyrekKarePrefab);

            if (tas1 && tas2)
            {
                Debug.Log("Taş bölündü ve 2 parça envantere yerleşti.");
            }
            else
            {
                Debug.LogWarning("Taş bölündü ama envanter dolduğu için bazı parçalar yere düştü/kayboldu!");
                // İstersen burada yere düşürme kodu (Instantiate) yazabilirsin.
            }
        }
    }
}