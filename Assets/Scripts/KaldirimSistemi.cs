using UnityEngine;
using System.Collections.Generic;

public class KaldirimSistemi : MonoBehaviour
{
    [System.Serializable]
    public class TasTanimi
    {
        public string tasAdi;
        public GameObject tasPrefabi;
    }

    [Header("Klavye Ayarları")]
    public KeyCode DonmeTusu = KeyCode.R;
    public KeyCode EtkilesimTusu = KeyCode.E;
    public KeyCode HassasModTusu = KeyCode.LeftShift;

    [Header("Dinamik Taş Listesi")]
    public List<TasTanimi> tasListesi;

    [Header("El (Model) Ayarları")]
    public Transform elPozisyonu;
    private GameObject eldekiModel;

    [Header("Mekanik Ayarlar")]
    public float YerlestirmeMenzili = 12.0f;
    public float DonmeHassasiyeti = 15f;
    public int MaxKatSayisi = 6;
    public float HareketYumusatma = 45f; // Daha hızlı tepki versin

    [Header("Snap Ayarları")]
    public float SnapAramaYaricapi = 5.0f;
    [Range(0.1f, 1.5f)]
    public float MiknatisEsigi = 0.8f;

    private KareEnvanteri envanter;
    private HayaletYonetici hayalet;
    private float suankiDonmeY = 0f;
    private Vector3 duzlestirilmisPos;

    void Awake()
    {
        envanter = GetComponent<KareEnvanteri>();
        hayalet = GetComponent<HayaletYonetici>();
    }

    void Start()
    {
        hayalet.HayaletiGoster(false);
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        EldekiModeliGuncelle();

        if (!Physics.Raycast(ray, out RaycastHit hit, YerlestirmeMenzili))
        {
            hayalet.HayaletiGoster(false);
            return;
        }

        // KESME MASASI
        if (hit.collider.CompareTag("KesmeMasasi"))
        {
            hayalet.HayaletiGoster(false);
            if (Input.GetKeyDown(EtkilesimTusu))
                hit.collider.GetComponent<KesmeMasasi>().MenuyuAc();
            return;
        }

        // KUTU KONTROL
        if (KutuKontrol(hit)) return;

        // GERİ ALMA
        if (Input.GetMouseButtonDown(1) && hit.collider.CompareTag("Tas"))
        {
            GeriAl(hit);
            return;
        }

        // YERLEŞTİRME
        GameObject suankiPrefab = envanter.SuankiKarePrefabi;
        if (suankiPrefab == null)
        {
            hayalet.HayaletiGoster(false);
            return;
        }

        bool elindeTasVar = envanter.SuankiMiktar > 0;
        hayalet.HayaletiGoster(elindeTasVar);
        if (!elindeTasVar) return;

        Vector3 s = suankiPrefab.transform.localScale;

        // 1) DÖNÜŞ
        Quaternion finalRot = DonmeHesapla();

        // 2) MOD KONTROLÜ
        bool hassasMod = Input.GetKey(HassasModTusu);
        
        Vector3 hedefXz;
        float hedefY;

        if (hassasMod)
        {
            // --- SHIFT: DUVAR SÜRTÜNME MODU ---
            // Mouse nereye bakıyorsa orayı al
            Vector3 hamPos = hit.point;
            
            // Yüksekliği zemine sabitle
            hedefY = s.y / 2f;
            Vector3 testPos = new Vector3(hamPos.x, hedefY, hamPos.z);

            // Taşı diğer taşların dışına it (Duvar etkisi)
            hedefXz = FizikselCarpismaHesapla(testPos, finalRot, s);
        }
        else
        {
            // --- NORMAL: SNAP MODU ---
            Vector3 gercekBoyut = GercekBoyutHesapla(s, finalRot);
            float xGrid = Mathf.Floor(hit.point.x / gercekBoyut.x) * gercekBoyut.x + (gercekBoyut.x / 2f);
            float zGrid = Mathf.Floor(hit.point.z / gercekBoyut.z) * gercekBoyut.z + (gercekBoyut.z / 2f);
            
            Vector3 gridPos = new Vector3(xGrid, 0f, zGrid);
            hedefXz = AgresifSnapHesapla(gridPos, finalRot);
            
            // Yükseklik hesabı
            float zeminY = ZeminYuksekliginiHesapla(hedefXz, s.x, s.z);
            hedefY = zeminY + (s.y / 2f);
        }

        Vector3 finalPos = new Vector3(hedefXz.x, hedefY, hedefXz.z);

        // 5) Smooth
        duzlestirilmisPos = Vector3.Lerp(duzlestirilmisPos, finalPos, Time.deltaTime * HareketYumusatma);

        // 6) Yerleştirme uygun mu?
        bool yerlestirilebilir = YerlestirmeUygunmu(finalPos, finalRot, hassasMod);

        // 7) Hayalet güncelle
        hayalet.GorselGuncelle(duzlestirilmisPos, finalRot, s, yerlestirilebilir);

        // 8) Yerleştir
        if (Input.GetMouseButtonDown(0) && yerlestirilebilir)
        {
            Instantiate(suankiPrefab, finalPos, finalRot);
            envanter.KareKullan();

            if (envanter.SuankiMiktar > 0)
                hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
            else
                hayalet.HayaletiGoster(false);
        }
    }

    // --- DUVAR SÜRTÜNME (SHIFT) ---
    Vector3 FizikselCarpismaHesapla(Vector3 istenenPos, Quaternion rot, Vector3 scale)
    {
        Vector3 duzeltilmisPos = istenenPos;
        Vector3 mySize = GercekBoyutHesapla(scale, rot);
        
        // Etrafımdaki taşlara bak
        // 0.9f ile biraz daraltılmış alanda arıyoruz ki uzaktakilerle uğraşmayalım
        Collider[] komsular = Physics.OverlapBox(istenenPos, mySize / 2.1f, rot); 

        foreach (var komsu in komsular)
        {
            if (!komsu.CompareTag("Tas")) continue;

            Vector3 kPos = komsu.transform.position;
            Vector3 kSize = GercekBoyutHesapla(komsu.transform.localScale, komsu.transform.rotation);

            // Ne kadar iç içeyiz?
            float minX = (mySize.x / 2f) + (kSize.x / 2f);
            float minZ = (mySize.z / 2f) + (kSize.z / 2f);

            float distX = Mathf.Abs(duzeltilmisPos.x - kPos.x);
            float distZ = Mathf.Abs(duzeltilmisPos.z - kPos.z);

            float overlapX = minX - distX;
            float overlapZ = minZ - distZ;

            // Eğer hafif bile olsa içindeysek (0.01f tolerans)
            if (overlapX > 0.01f && overlapZ > 0.01f)
            {
                // En kolay çıkış yolunu bul ve oraya it
                if (overlapX < overlapZ)
                {
                    // X ekseninde it
                    float itmeYonu = (duzeltilmisPos.x > kPos.x) ? 1f : -1f;
                    duzeltilmisPos.x = kPos.x + (itmeYonu * minX);
                }
                else
                {
                    // Z ekseninde it
                    float itmeYonu = (duzeltilmisPos.z > kPos.z) ? 1f : -1f;
                    duzeltilmisPos.z = kPos.z + (itmeYonu * minZ);
                }
            }
        }
        return duzeltilmisPos;
    }

    // --- NORMAL SNAP ---
    Vector3 AgresifSnapHesapla(Vector3 hamPos, Quaternion rot)
    {
        if (envanter.SuankiKarePrefabi == null) return hamPos;
        Vector3 snapPos = hamPos;
        Vector3 mySize = GercekBoyutHesapla(envanter.SuankiKarePrefabi.transform.localScale, rot);
        Collider[] komsular = Physics.OverlapSphere(hamPos, SnapAramaYaricapi);
        
        foreach (var komsu in komsular)
        {
            if (!komsu.CompareTag("Tas")) continue;
            Vector3 kPos = komsu.transform.position;
            Vector3 kSize = GercekBoyutHesapla(komsu.transform.localScale, komsu.transform.rotation);
            float mX = (mySize.x + kSize.x) / 2f;
            float mZ = (mySize.z + kSize.z) / 2f;
            float dX = hamPos.x - kPos.x;
            float dZ = hamPos.z - kPos.z;
            bool xYakinda = Mathf.Abs(Mathf.Abs(dX) - mX) < MiknatisEsigi;
            bool zYakinda = Mathf.Abs(Mathf.Abs(dZ) - mZ) < MiknatisEsigi;
            bool icerde = (Mathf.Abs(dX) < mX && Mathf.Abs(dZ) < mZ);

            if ((xYakinda || icerde) && (Mathf.Abs(dX) / mX > Mathf.Abs(dZ) / mZ))
                snapPos.x = kPos.x + (dX > 0 ? mX : -mX);
            if ((zYakinda || icerde) && (Mathf.Abs(dZ) / mZ >= Mathf.Abs(dX) / mX))
                snapPos.z = kPos.z + (dZ > 0 ? mZ : -mZ);
        }
        return snapPos;
    }

    Vector3 GercekBoyutHesapla(Vector3 scale, Quaternion rot)
    {
        float rad = rot.eulerAngles.y * Mathf.Deg2Rad;
        float w = Mathf.Abs(Mathf.Cos(rad)) * scale.x + Mathf.Abs(Mathf.Sin(rad)) * scale.z;
        float d = Mathf.Abs(Mathf.Sin(rad)) * scale.x + Mathf.Abs(Mathf.Cos(rad)) * scale.z;
        return new Vector3(w, scale.y, d);
    }

    // --- ÇAKIŞMA KONTROLÜ (DÜZELTİLEN YER) ---
    bool YerlestirmeUygunmu(Vector3 pos, Quaternion rot, bool hassasMod)
    {
        if (envanter.SuankiKarePrefabi == null) return false;
        float h = envanter.SuankiKarePrefabi.transform.localScale.y;

        if (hassasMod)
        {
            if (pos.y > (h / 2f) + 0.05f) return false;
        }
        else
        {
            if (pos.y > (MaxKatSayisi * h + (h / 2f))) return false;
        }

        // ÖNEMLİ DÜZELTME:
        // Eskiden: 0.95f veya 1.0f -> Kenarlar birbirine değdiği an kırmızı yanıyordu.
        // Yeni: 0.85f -> Kutuyu %15 küçülttük. 
        // Böylece taşlar birbirine sıfıra sıfır değse bile "Kırmızı" yanmaz, "Yeşil" kalır.
        Vector3 kBoyut = (envanter.SuankiKarePrefabi.transform.localScale / 2f) * 0.85f; 
        
        Collider[] hits = Physics.OverlapBox(pos, kBoyut, rot);

        foreach (var c in hits)
        {
            if (!c.CompareTag("Tas")) continue;
            
            // Eğer yükseklik olarak çakışıyorsak (yani aynı kattaysak)
            if (Mathf.Abs(c.transform.position.y - pos.y) < (h * 0.9f)) 
                return false;
        }
        return true;
    }

    Quaternion DonmeHesapla()
    {
        if (Input.GetKeyDown(DonmeTusu))
        {
            suankiDonmeY += 90f;
            if (suankiDonmeY >= 360f) suankiDonmeY -= 360f;
        }
        return Quaternion.Euler(0, suankiDonmeY, 0);
    }

    void GeriAl(RaycastHit hit)
    {
        GameObject enUst = EnUsttekiTasiBul(hit.collider.transform.position.x, hit.collider.transform.position.z);
        if (enUst == null) return;
        foreach (var tanim in tasListesi)
        {
            if (enUst.name.Contains(tanim.tasPrefabi.name))
            {
                if (envanter.KareAlabilirmi(tanim.tasPrefabi))
                {
                    envanter.KareEkle(tanim.tasPrefabi);
                    Destroy(enUst);
                    if (envanter.SuankiKarePrefabi != null)
                        hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                    break;
                }
            }
        }
    }

    bool KutuKontrol(RaycastHit hit)
    {
        string tag = hit.collider.tag.ToLower();
        foreach (var tanim in tasListesi)
        {
            if (tag.Contains(tanim.tasAdi.ToLower()))
            {
                hayalet.HayaletiGoster(false);
                if (Input.GetKeyDown(EtkilesimTusu) && envanter.KareAlabilirmi(tanim.tasPrefabi))
                {
                    envanter.KareEkle(tanim.tasPrefabi);
                    hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                }
                return true;
            }
        }
        return false;
    }

    void EldekiModeliGuncelle()
    {
        GameObject suankiPrefab = envanter.SuankiKarePrefabi;
        if (eldekiModel == null || (suankiPrefab != null && eldekiModel.name != suankiPrefab.name + "(Clone)"))
        {
            if (eldekiModel != null) Destroy(eldekiModel);
            if (suankiPrefab != null)
            {
                eldekiModel = Instantiate(suankiPrefab, elPozisyonu);
                eldekiModel.transform.localPosition = Vector3.zero;
                eldekiModel.transform.localRotation = Quaternion.identity;
                if (eldekiModel.TryGetComponent(out Collider c)) c.enabled = false;
                eldekiModel.layer = LayerMask.NameToLayer("Default");
            }
        }
        else if (suankiPrefab == null && eldekiModel != null) Destroy(eldekiModel);
    }

    float ZeminYuksekliginiHesapla(Vector3 pos, float sizeX, float sizeZ)
    {
        float enYuksekY = 0f;
        Collider[] altindakiTaslar = Physics.OverlapBox(
            new Vector3(pos.x, 10f, pos.z),
            new Vector3(sizeX * 0.1f, 10f, sizeZ * 0.1f)
        );
        foreach (var c in altindakiTaslar)
        {
            if (c.CompareTag("Tas"))
            {
                float ust = c.transform.position.y + (c.transform.localScale.y / 2f);
                if (ust > enYuksekY) enYuksekY = ust;
            }
        }
        return enYuksekY;
    }

    GameObject EnUsttekiTasiBul(float x, float z)
    {
        GameObject bulunanEnUst = null;
        float maxYukseklik = -100f;
        Collider[] sutun = Physics.OverlapBox(new Vector3(x, 25f, z), new Vector3(0.1f, 25f, 0.1f));
        foreach (var c in sutun)
        {
            if (c.CompareTag("Tas"))
            {
                float ust = c.transform.position.y + (c.transform.localScale.y / 2f);
                if (ust > maxYukseklik)
                {
                    maxYukseklik = ust;
                    bulunanEnUst = c.gameObject;
                }
            }
        }
        return bulunanEnUst;
    }
}

/*using UnityEngine;
using System.Collections.Generic;

public class KaldirimSistemi : MonoBehaviour
{
    [System.Serializable]
    public class TasTanimi
    {
        public string tasAdi;
        public GameObject tasPrefabi;
    }

    [Header("Klavye Ayarları")]
    public KeyCode DonmeTusu = KeyCode.R;
    public KeyCode EtkilesimTusu = KeyCode.E;
    public KeyCode HassasModTusu = KeyCode.LeftShift;

    [Header("Dinamik Taş Listesi")]
    public List<TasTanimi> tasListesi;

    [Header("El (Model) Ayarları")]
    public Transform elPozisyonu;
    private GameObject eldekiModel;

    [Header("Mekanik Ayarlar")]
    public float YerlestirmeMenzili = 8.0f;
    public float DonmeHassasiyeti = 15f;
    public int MaxKatSayisi = 6;
    public float HareketYumusatma = 35f;

    [Range(0.3f, 0.7f)]
    public float MiknatisEsigi = 0.55f;

    [Header("Snap Ayarları")]
    public float SnapAramaYaricapi = 2.2f;

    private KareEnvanteri envanter;
    private HayaletYonetici hayalet;
    private float suankiDonmeY = 0f;
    private Vector3 duzlestirilmisPos;

    void Awake()
    {
        envanter = GetComponent<KareEnvanteri>();
        hayalet = GetComponent<HayaletYonetici>();
    }

    void Start()
    {
        hayalet.HayaletiGoster(false);
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        EldekiModeliGuncelle();

        if (!Physics.Raycast(ray, out RaycastHit hit, YerlestirmeMenzili))
        {
            hayalet.HayaletiGoster(false);
            return;
        }

        // KESME MASASI
        if (hit.collider.CompareTag("KesmeMasasi"))
        {
            hayalet.HayaletiGoster(false);
            if (Input.GetKeyDown(EtkilesimTusu))
                hit.collider.GetComponent<KesmeMasasi>().MenuyuAc();
            return;
        }

        // KUTU KONTROL
        if (KutuKontrol(hit)) return;

        // GERİ ALMA
        if (Input.GetMouseButtonDown(1) && hit.collider.CompareTag("Tas"))
        {
            GeriAl(hit);
            return;
        }

        // YERLEŞTİRME
        GameObject suankiPrefab = envanter.SuankiKarePrefabi;
        if (suankiPrefab == null)
        {
            hayalet.HayaletiGoster(false);
            return;
        }

        bool elindeTasVar = envanter.SuankiMiktar > 0;
        hayalet.HayaletiGoster(elindeTasVar);
        if (!elindeTasVar) return;

        Vector3 s = suankiPrefab.transform.localScale;

        // 1) DÖNÜŞ
        Quaternion finalRot = DonmeHesapla();

        // 2) GRID
        float xGrid = Mathf.Floor(hit.point.x / s.x) * s.x + (s.x / 2f);
        float zGrid = Mathf.Floor(hit.point.z / s.z) * s.z + (s.z / 2f);
        Vector3 gridPos = new Vector3(xGrid, 0f, zGrid);

        // 3) SHIFT = HASSAS / MIKNATIS MODU
        bool hassasMod = Input.GetKey(HassasModTusu);

        Vector3 hedefXz;
        if (hassasMod)
        {
            // Shift basılıyken "ince ayar": grid yerine komşuya snap
            hedefXz = AgresifSnapHesapla(hit.point, finalRot);
        }
        else
        {
            hedefXz = gridPos;
        }

        // 4) Y HESABI
        // ❗ İSTEDİĞİN ŞEY: Shift basılıyken taşlar üst üste konulmasın.
        // Bu yüzden Shift'te Y daima zemine oturur.
        float zeminY = ZeminYuksekliginiHesapla(hedefXz, s.x, s.z);
        float hedefY;

        if (hassasMod)
        {
            // Shift: stacking yok -> her zaman zeminde
            hedefY = (s.y / 2f);
        }
        else
        {
            // Normal: stacking serbest
            hedefY = zeminY + (s.y / 2f);
        }

        Vector3 finalPos = new Vector3(hedefXz.x, hedefY, hedefXz.z);

        // 5) Smooth
        duzlestirilmisPos = Vector3.Lerp(duzlestirilmisPos, finalPos, Time.deltaTime * HareketYumusatma);

        // 6) Yerleştirme uygun mu?
        bool yerlestirilebilir = YerlestirmeUygunmu(finalPos, finalRot, hassasMod);

        // 7) Hayalet güncelle
        hayalet.GorselGuncelle(duzlestirilmisPos, finalRot, s, yerlestirilebilir);

        // 8) Yerleştir
        if (Input.GetMouseButtonDown(0) && yerlestirilebilir)
        {
            Instantiate(suankiPrefab, finalPos, finalRot);
            envanter.KareKullan();

            if (envanter.SuankiMiktar > 0)
                hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
            else
                hayalet.HayaletiGoster(false);
        }
    }

    // --- SNAP ---
    Vector3 AgresifSnapHesapla(Vector3 hamPos, Quaternion rot)
    {
        if (envanter.SuankiKarePrefabi == null) return hamPos;

        Vector3 snapPos = hamPos;

        // Benim gerçek boyutum (rotasyonlu)
        Vector3 mySize = GercekBoyutHesapla(envanter.SuankiKarePrefabi.transform.localScale, rot);

        Collider[] komsular = Physics.OverlapSphere(hamPos, SnapAramaYaricapi);
        foreach (var komsu in komsular)
        {
            if (!komsu.CompareTag("Tas")) continue;

            Vector3 kPos = komsu.transform.position;
            Vector3 kSize = GercekBoyutHesapla(komsu.transform.localScale, komsu.transform.rotation);

            // ideal merkez mesafesi
            float mX = (mySize.x + kSize.x) / 2f;
            float mZ = (mySize.z + kSize.z) / 2f;

            float dX = hamPos.x - kPos.x;
            float dZ = hamPos.z - kPos.z;

            bool xYakinda = Mathf.Abs(Mathf.Abs(dX) - mX) < MiknatisEsigi;
            bool zYakinda = Mathf.Abs(Mathf.Abs(dZ) - mZ) < MiknatisEsigi;

            bool icerde = (Mathf.Abs(dX) < mX && Mathf.Abs(dZ) < mZ);

            // Önce hangisi daha yakınsa onu snaple
            if ((xYakinda || icerde) && (Mathf.Abs(dX) / mX > Mathf.Abs(dZ) / mZ))
                snapPos.x = kPos.x + (dX > 0 ? mX : -mX);

            if ((zYakinda || icerde) && (Mathf.Abs(dZ) / mZ >= Mathf.Abs(dX) / mX))
                snapPos.z = kPos.z + (dZ > 0 ? mZ : -mZ);
        }

        return snapPos;
    }

    // Rotasyona göre XZ boyutunu bul
    Vector3 GercekBoyutHesapla(Vector3 scale, Quaternion rot)
    {
        float rad = rot.eulerAngles.y * Mathf.Deg2Rad;
        float w = Mathf.Abs(Mathf.Cos(rad)) * scale.x + Mathf.Abs(Mathf.Sin(rad)) * scale.z;
        float d = Mathf.Abs(Mathf.Sin(rad)) * scale.x + Mathf.Abs(Mathf.Cos(rad)) * scale.z;
        return new Vector3(w, scale.y, d);
    }

    // --- ÇAKIŞMA KONTROLÜ ---
    bool YerlestirmeUygunmu(Vector3 pos, Quaternion rot, bool hassasMod)
    {
        if (envanter.SuankiKarePrefabi == null) return false;

        float h = envanter.SuankiKarePrefabi.transform.localScale.y;

        // Shift modunda stacking yok zaten, ama yine de güvenlik:
        if (hassasMod)
        {
            // Shift: Y = h/2 olmalı
            if (pos.y > (h / 2f) + 0.01f) return false;
        }
        else
        {
            // normal mod: max kat sınırı
            if (pos.y > (MaxKatSayisi * h + (h / 2f))) return false;
        }

        // Çakışma kutusu
        Vector3 kBoyut = (envanter.SuankiKarePrefabi.transform.localScale / 2f) * 0.95f;
        Collider[] hits = Physics.OverlapBox(pos, kBoyut, rot);

        foreach (var c in hits)
        {
            if (!c.CompareTag("Tas")) continue;

            // Aynı katmanda çakışma varsa reddet
            if (Mathf.Abs(c.transform.position.y - pos.y) < (h * 0.6f))
                return false;
        }

        return true;
    }

    Quaternion DonmeHesapla()
    {
        if (Input.GetKeyDown(DonmeTusu))
        {
            suankiDonmeY += 90f;
            if (suankiDonmeY >= 360f) suankiDonmeY -= 360f;
        }
        return Quaternion.Euler(0, suankiDonmeY, 0);
    }

    void GeriAl(RaycastHit hit)
    {
        GameObject enUst = EnUsttekiTasiBul(hit.collider.transform.position.x, hit.collider.transform.position.z);
        if (enUst == null) return;

        foreach (var tanim in tasListesi)
        {
            if (enUst.name.Contains(tanim.tasPrefabi.name))
            {
                if (envanter.KareAlabilirmi(tanim.tasPrefabi))
                {
                    envanter.KareEkle(tanim.tasPrefabi);
                    Destroy(enUst);

                    if (envanter.SuankiKarePrefabi != null)
                        hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);

                    break;
                }
            }
        }
    }

    bool KutuKontrol(RaycastHit hit)
    {
        string tag = hit.collider.tag.ToLower();

        foreach (var tanim in tasListesi)
        {
            if (tag.Contains(tanim.tasAdi.ToLower()))
            {
                hayalet.HayaletiGoster(false);

                if (Input.GetKeyDown(EtkilesimTusu) && envanter.KareAlabilirmi(tanim.tasPrefabi))
                {
                    envanter.KareEkle(tanim.tasPrefabi);
                    hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                }

                return true;
            }
        }

        return false;
    }

    void EldekiModeliGuncelle()
    {
        GameObject suankiPrefab = envanter.SuankiKarePrefabi;

        if (eldekiModel == null || (suankiPrefab != null && eldekiModel.name != suankiPrefab.name + "(Clone)"))
        {
            if (eldekiModel != null) Destroy(eldekiModel);

            if (suankiPrefab != null)
            {
                eldekiModel = Instantiate(suankiPrefab, elPozisyonu);
                eldekiModel.transform.localPosition = Vector3.zero;
                eldekiModel.transform.localRotation = Quaternion.identity;

                if (eldekiModel.TryGetComponent(out Collider c)) c.enabled = false;
                eldekiModel.layer = LayerMask.NameToLayer("Default");
            }
        }
        else if (suankiPrefab == null && eldekiModel != null)
        {
            Destroy(eldekiModel);
        }
    }

    float ZeminYuksekliginiHesapla(Vector3 pos, float sizeX, float sizeZ)
    {
        float enYuksekY = 0f;

        Collider[] altindakiTaslar = Physics.OverlapBox(
            new Vector3(pos.x, 10f, pos.z),
            new Vector3(sizeX * 0.45f, 10f, sizeZ * 0.45f)
        );

        foreach (var c in altindakiTaslar)
        {
            if (c.CompareTag("Tas"))
            {
                float ust = c.transform.position.y + (c.transform.localScale.y / 2f);
                if (ust > enYuksekY) enYuksekY = ust;
            }
        }

        return enYuksekY;
    }

    GameObject EnUsttekiTasiBul(float x, float z)
    {
        GameObject bulunanEnUst = null;
        float maxYukseklik = -100f;

        Collider[] sutun = Physics.OverlapBox(
            new Vector3(x, 25f, z),
            new Vector3(0.2f, 25f, 0.2f)
        );

        foreach (var c in sutun)
        {
            if (c.CompareTag("Tas"))
            {
                float ust = c.transform.position.y + (c.transform.localScale.y / 2f);
                if (ust > maxYukseklik)
                {
                    maxYukseklik = ust;
                    bulunanEnUst = c.gameObject;
                }
            }
        }

        return bulunanEnUst;
    }
}
*/
