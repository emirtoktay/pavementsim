using UnityEngine;
using System.Collections.Generic;

public class KaldirimSistemi : MonoBehaviour
{
    [System.Serializable]
    public class TasTanimi
    {
        public string tasAdi; // Kutu tag'lerinde aramak için (Örn: "tam" veya "ceyrek")
        public GameObject tasPrefabi;
    }

    [Header("Dinamik Taş Listesi")]
    public List<TasTanimi> tasListesi; // Inspector'dan doldurulacak

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

    private KareEnvanteri envanter;
    private HayaletYonetici hayalet;
    private float suankiDonmeY = 0f;
    private Vector3 duzlestirilmisPos;

    void Awake()
    {
        envanter = GetComponent<KareEnvanteri>();
        hayalet = GetComponent<HayaletYonetici>();
    }

    void Start() { hayalet.HayaletiGoster(false); }

    void Update()
    {
        if (Time.timeScale == 0) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        EldekiModeliGuncelle();

        if (Physics.Raycast(ray, out RaycastHit hit, YerlestirmeMenzili))
        {
            // KESME MASASI KONTROLÜ
            if (hit.collider.CompareTag("KesmeMasasi"))
            {
                hayalet.HayaletiGoster(false);
                if (Input.GetKeyDown(KeyCode.E)) hit.collider.GetComponent<KesmeMasasi>().MenuyuAc();
                return;
            }

            // KAYNAK KUTUSU KONTROLÜ
            if (KutuKontrol(hit)) return;

            // YERLEŞTİRME MANTIĞI
            GameObject suankiPrefab = envanter.SuankiKarePrefabi;
            if (suankiPrefab == null) { hayalet.HayaletiGoster(false); }
            else
            {
                Vector3 s = suankiPrefab.transform.localScale;
                float xGrid = Mathf.Floor(hit.point.x / s.x) * s.x + (s.x / 2f);
                float zGrid = Mathf.Floor(hit.point.z / s.z) * s.z + (s.z / 2f);

                Vector3 hedefXz = (Input.GetKey(KeyCode.LeftShift) && envanter.SuankiMiktar > 0)
                    ? AgresifSnapHesapla(hit.point) : new Vector3(xGrid, 0, zGrid);

                float hedefY = Input.GetKey(KeyCode.LeftShift)
                    ? (s.y / 2f) : ZeminYuksekliginiHesapla(hedefXz, s.x, s.z) + (s.y / 2f);

                Vector3 finalPos = new Vector3(hedefXz.x, hedefY, hedefXz.z);
                duzlestirilmisPos = Vector3.Lerp(duzlestirilmisPos, finalPos, Time.deltaTime * HareketYumusatma);

                Quaternion finalRot = DonmeHesapla();
                bool yerlestirilebilir = YerlestirmeUygunmu(finalPos, finalRot, finalPos.y, s.y);
                bool elindeTasVar = envanter.SuankiMiktar > 0;

                hayalet.HayaletiGoster(elindeTasVar);
                if (elindeTasVar)
                {
                    hayalet.GorselGuncelle(duzlestirilmisPos, finalRot, s, yerlestirilebilir);

                    if (Input.GetMouseButtonDown(0) && yerlestirilebilir)
                    {
                        Instantiate(suankiPrefab, finalPos, finalRot);
                        envanter.KareKullan();
                        if (envanter.SuankiMiktar > 0) hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                        else hayalet.HayaletiGoster(false);
                    }
                }
            }

            // SAĞ TIK: GERİ ALMA (Dinamik Liste Kullanıyor)
            if (Input.GetMouseButtonDown(1) && hit.collider.CompareTag("Tas"))
            {
                GameObject enUst = EnUsttekiTasiBul(hit.collider.transform.position.x, hit.collider.transform.position.z);
                if (enUst != null)
                {
                    GameObject geriGelen = null;
                    // Listede bu taşa uyan prefabı ara
                    foreach (var tanim in tasListesi)
                    {
                        if (enUst.name.Contains(tanim.tasPrefabi.name))
                        {
                            geriGelen = tanim.tasPrefabi;
                            break;
                        }
                    }

                    if (geriGelen != null && envanter.KareAlabilirmi(geriGelen))
                    {
                        envanter.KareEkle(geriGelen);
                        Destroy(enUst);
                        if (envanter.SuankiKarePrefabi != null) hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                    }
                }
            }
        }
        else { hayalet.HayaletiGoster(false); }
    }

    bool KutuKontrol(RaycastHit hit)
    {
        string tag = hit.collider.tag.ToLower();
        foreach (var tanim in tasListesi)
        {
            // Tag içerisinde tanımladığımız "tasAdi" geçiyor mu? (Örn: "tam" veya "ceyrek")
            if (tag.Contains(tanim.tasAdi.ToLower()))
            {
                hayalet.HayaletiGoster(false);
                if (Input.GetKeyDown(KeyCode.E) && envanter.KareAlabilirmi(tanim.tasPrefabi))
                {
                    envanter.KareEkle(tanim.tasPrefabi);
                    hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                }
                return true;
            }
        }
        return false;
    }

    // --- Diğer matematiksel fonksiyonlar (ZeminYuksekliginiHesapla, EnUsttekiTasiBul vb.) 
    // senin attığın koddaki gibi kalabilir, liste mantığına engel değiller. ---

    // (Kodun kısalması için matematik fonksiyonlarını buraya tekrar yazmıyorum ama projende kalsınlar)

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
        else if (suankiPrefab == null && eldekiModel != null) { Destroy(eldekiModel); }
    }
    float ZeminYuksekliginiHesapla(Vector3 pos, float sizeX, float sizeZ)
    {
        float enYuksekY = 0f;
        Collider[] altindakiTaslar = Physics.OverlapBox(new Vector3(pos.x, 10f, pos.z), new Vector3(sizeX * 0.45f, 10f, sizeZ * 0.45f));
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
        // Dikey sütun taraması (0.2f genişlik ile hassas odaklama)
        Collider[] sutun = Physics.OverlapBox(new Vector3(x, 25f, z), new Vector3(0.2f, 25f, 0.2f));
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

    // Diğer fonksiyonların (DonmeHesapla, YerlestirmeUygunmu, KutuKontrol, AgresifSnapHesapla) 
    // mevcuttaki halleri bu yapıyla uyumludur, değiştirmene gerek yok.

    bool YerlestirmeUygunmu(Vector3 pos, Quaternion rot, float suankiY, float h)
    {
        if (envanter.SuankiKarePrefabi == null) return false;
        if (!Input.GetKey(KeyCode.LeftShift) && suankiY > (MaxKatSayisi * h + (h / 2f))) return false;

        Vector3 kBoyut = (envanter.SuankiKarePrefabi.transform.localScale / 2) * 0.90f;
        Collider[] hits = Physics.OverlapBox(pos, kBoyut, rot);
        foreach (var c in hits)
            if (c.CompareTag("Tas") && Mathf.Abs(c.transform.position.y - pos.y) < (h * 0.5f)) return false;

        return true;
    }

    Quaternion DonmeHesapla()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            suankiDonmeY += Input.mouseScrollDelta.y * DonmeHassasiyeti;
            return Quaternion.Euler(0, suankiDonmeY, 0);
        }
        suankiDonmeY = 0f;
        return Quaternion.identity;
    }

    Vector3 AgresifSnapHesapla(Vector3 hamPos)
    {
        if (envanter.SuankiKarePrefabi == null) return hamPos;
        Vector3 snapPos = hamPos;
        float myX = envanter.SuankiKarePrefabi.transform.localScale.x;
        float myZ = envanter.SuankiKarePrefabi.transform.localScale.z;

        Collider[] komsular = Physics.OverlapSphere(hamPos, 1.4f);
        foreach (var komsu in komsular)
        {
            if (komsu.CompareTag("Tas"))
            {
                Vector3 kPos = komsu.transform.position;
                Vector3 kScl = komsu.transform.localScale;
                float mX = (myX + kScl.x) / 2f;
                float mZ = (myZ + kScl.z) / 2f;
                float dX = hamPos.x - kPos.x;
                float dZ = hamPos.z - kPos.z;

                if (Mathf.Abs(Mathf.Abs(dX) - mX) < MiknatisEsigi || (Mathf.Abs(dX) < mX && Mathf.Abs(dZ) < mZ))
                    if (Mathf.Abs(dX) / mX > Mathf.Abs(dZ) / mZ) snapPos.x = kPos.x + (dX > 0 ? mX : -mX);

                if (Mathf.Abs(Mathf.Abs(dZ) - mZ) < MiknatisEsigi || (Mathf.Abs(dX) < mX && Mathf.Abs(dZ) < mZ))
                    if (Mathf.Abs(dZ) / mZ >= Mathf.Abs(dX) / mX) snapPos.z = kPos.z + (dZ > 0 ? mZ : -mZ);
            }
        }
        return snapPos;
    }
    // ... (ZeminYuksekliginiHesapla, EnUsttekiTasiBul, YerlestirmeUygunmu, DonmeHesapla, AgresifSnapHesapla fonksiyonlarını buraya ekle)
}


/*using UnityEngine;

public class KaldirimSistemi : MonoBehaviour
{

    [Header("El (Model) Ayarları")]
    public Transform elPozisyonu; // Karakterin sağ alt köşesindeki boş obje
    private GameObject eldekiModel; // O an elimizde tuttuğumuz taşın kopyası

    [Header("Kare Prefabları")]
    public GameObject TamKarePrefab;
    public GameObject CeyrekKarePrefab;

    [Header("Mekanik Ayarlar")]
    public float YerlestirmeMenzili = 8.0f;
    public float DonmeHassasiyeti = 15f;
    public int MaxKatSayisi = 6;
    public float HareketYumusatma = 35f;

    [Range(0.3f, 0.7f)]
    public float MiknatisEsigi = 0.55f;

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
        // Eğer Time.timeScale 0 ise (Menü açıkken öyle yapmıştık) kodun devamını çalıştırma.
        if (Time.timeScale == 0) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        // ELDEKİ MODELİ GÜNCELLE
        EldekiModeliGuncelle();
        // Update içindeki Raycast'in hemen altında:

        if (Physics.Raycast(ray, out RaycastHit hit, YerlestirmeMenzili))
        {
            // MASAYA BAKIYORSAK:
            if (hit.collider.CompareTag("KesmeMasasi"))
            {
                // Hayaleti hemen gizle ki masanın içindeyken görünmesin
                hayalet.HayaletiGoster(false);

                // Eğer menü açık değilse E ile aç
                if (Input.GetKeyDown(KeyCode.E))
                {
                    hit.collider.GetComponent<KesmeMasasi>().MenuyuAc();
                }

                // ÖNEMLİ: Eğer masaya bakıyorsak, Sol Tık yapılsa bile aşağıya geçmesin.
                // Mevcut kodunda return var ama hayaleti gizlemediğin için kafa karıştırıyor olabilir.
                return;
            }
            
            if (KutuKontrol(hit)) return;

            // 1. BOYUTLARI AL (Dinamik Hizalama İçin)
            GameObject suankiPrefab = envanter.SuankiKarePrefabi;
            if (suankiPrefab == null) { hayalet.HayaletiGoster(false); }
            else
            {
                Vector3 s = suankiPrefab.transform.localScale;

                // 2. DİNAMİK GRID (Çeyrek taşları 0.5'e, Tam taşları 1.0'a tam oturtan mantık)
                float xGrid = Mathf.Floor(hit.point.x / s.x) * s.x + (s.x / 2f);
                float zGrid = Mathf.Floor(hit.point.z / s.z) * s.z + (s.z / 2f);

                // 3. KONUM HESABI
                Vector3 hedefXz = (Input.GetKey(KeyCode.LeftShift) && envanter.SuankiMiktar > 0)
                    ? AgresifSnapHesapla(hit.point) : new Vector3(xGrid, 0, zGrid);

                float hedefY = Input.GetKey(KeyCode.LeftShift)
                    ? (s.y / 2f) : ZeminYuksekliginiHesapla(hedefXz, s.x, s.z) + (s.y / 2f);

                Vector3 finalPos = new Vector3(hedefXz.x, hedefY, hedefXz.z);
                duzlestirilmisPos = Vector3.Lerp(duzlestirilmisPos, finalPos, Time.deltaTime * HareketYumusatma);

                // 4. HAYALET VE YERLEŞTİRME
                Quaternion finalRot = DonmeHesapla();
                bool yerlestirilebilir = YerlestirmeUygunmu(finalPos, finalRot, finalPos.y, s.y);
                bool elindeTasVar = envanter.SuankiMiktar > 0;

                hayalet.HayaletiGoster(elindeTasVar);
                if (elindeTasVar)
                {
                    hayalet.GorselGuncelle(duzlestirilmisPos, finalRot, s, yerlestirilebilir);

                    if (Input.GetMouseButtonDown(0) && yerlestirilebilir)
                    {
                        Instantiate(suankiPrefab, finalPos, finalRot);
                        envanter.KareKullan();
                        if (envanter.SuankiMiktar > 0) hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                        else hayalet.HayaletiGoster(false);
                    }
                }
            }

            // 5. SAĞ TIK: EN ÜSTTEKİ TAŞI SİL VE GERİ AL
            if (Input.GetMouseButtonDown(1) && hit.collider.CompareTag("Tas"))
            {
                GameObject enUst = EnUsttekiTasiBul(hit.collider.transform.position.x, hit.collider.transform.position.z);
                if (enUst != null)
                {
                    GameObject geriGelen = enUst.name.ToLower().Contains("ceyrek") ? CeyrekKarePrefab : TamKarePrefab;
                    if (envanter.KareAlabilirmi(geriGelen))
                    {
                        envanter.KareEkle(geriGelen);
                        Destroy(enUst);
                        if (envanter.SuankiKarePrefabi != null) hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                    }
                }
            }
        }
        else { hayalet.HayaletiGoster(false); }
    }

    void EldekiModeliGuncelle()
    {
        GameObject suankiPrefab = envanter.SuankiKarePrefabi;

        // Eğer eldeki model ile envanterdeki taş uyuşmuyorsa (veya el boşsa)
        if (eldekiModel == null || (suankiPrefab != null && eldekiModel.name != suankiPrefab.name + "(Clone)"))
        {
            if (eldekiModel != null) Destroy(eldekiModel);

            if (suankiPrefab != null)
            {
                // Taşı karakterin eline (elPozisyonu) oluştur
                eldekiModel = Instantiate(suankiPrefab, elPozisyonu);
                eldekiModel.transform.localPosition = Vector3.zero;
                eldekiModel.transform.localRotation = Quaternion.identity;

                // Eldeki taşın etrafa çarpıp karakteri uçurmaması için collider'ı kapat
                if (eldekiModel.TryGetComponent(out Collider c)) c.enabled = false;

                // Eldeki taşın hayalet (transparan) görünmemesi için katmanını düzelt
                eldekiModel.layer = LayerMask.NameToLayer("Default");
            }
        }
        else if (suankiPrefab == null && eldekiModel != null)
        {
            // Envanter bittiyse eldekini sil
            Destroy(eldekiModel);
        }
    }

    float ZeminYuksekliginiHesapla(Vector3 pos, float sizeX, float sizeZ)
    {
        float enYuksekY = 0f;
        Collider[] altindakiTaslar = Physics.OverlapBox(new Vector3(pos.x, 10f, pos.z), new Vector3(sizeX * 0.45f, 10f, sizeZ * 0.45f));
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
        // Dikey sütun taraması (0.2f genişlik ile hassas odaklama)
        Collider[] sutun = Physics.OverlapBox(new Vector3(x, 25f, z), new Vector3(0.2f, 25f, 0.2f));
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

    // Diğer fonksiyonların (DonmeHesapla, YerlestirmeUygunmu, KutuKontrol, AgresifSnapHesapla) 
    // mevcuttaki halleri bu yapıyla uyumludur, değiştirmene gerek yok.

    bool YerlestirmeUygunmu(Vector3 pos, Quaternion rot, float suankiY, float h)
    {
        if (envanter.SuankiKarePrefabi == null) return false;
        if (!Input.GetKey(KeyCode.LeftShift) && suankiY > (MaxKatSayisi * h + (h / 2f))) return false;

        Vector3 kBoyut = (envanter.SuankiKarePrefabi.transform.localScale / 2) * 0.90f;
        Collider[] hits = Physics.OverlapBox(pos, kBoyut, rot);
        foreach (var c in hits)
            if (c.CompareTag("Tas") && Mathf.Abs(c.transform.position.y - pos.y) < (h * 0.5f)) return false;

        return true;
    }

    bool KutuKontrol(RaycastHit hit)
    {
        string tag = hit.collider.tag;
        if (tag == "tamkaretaskaynakkutusu" || tag == "ceyrekkaretaskaynakkutusu" || tag == "KaynakKutusu")
        {
            hayalet.HayaletiGoster(false);
            GameObject secilen = tag.ToLower().Contains("ceyrek") ? CeyrekKarePrefab : TamKarePrefab;
            if (Input.GetKeyDown(KeyCode.E) && envanter.KareAlabilirmi(secilen))
            {
                envanter.KareEkle(secilen);
                hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
            }
            return true;
        }
        return false;
    }

    Quaternion DonmeHesapla()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            suankiDonmeY += Input.mouseScrollDelta.y * DonmeHassasiyeti;
            return Quaternion.Euler(0, suankiDonmeY, 0);
        }
        suankiDonmeY = 0f;
        return Quaternion.identity;
    }

    Vector3 AgresifSnapHesapla(Vector3 hamPos)
    {
        if (envanter.SuankiKarePrefabi == null) return hamPos;
        Vector3 snapPos = hamPos;
        float myX = envanter.SuankiKarePrefabi.transform.localScale.x;
        float myZ = envanter.SuankiKarePrefabi.transform.localScale.z;

        Collider[] komsular = Physics.OverlapSphere(hamPos, 1.4f);
        foreach (var komsu in komsular)
        {
            if (komsu.CompareTag("Tas"))
            {
                Vector3 kPos = komsu.transform.position;
                Vector3 kScl = komsu.transform.localScale;
                float mX = (myX + kScl.x) / 2f;
                float mZ = (myZ + kScl.z) / 2f;
                float dX = hamPos.x - kPos.x;
                float dZ = hamPos.z - kPos.z;

                if (Mathf.Abs(Mathf.Abs(dX) - mX) < MiknatisEsigi || (Mathf.Abs(dX) < mX && Mathf.Abs(dZ) < mZ))
                    if (Mathf.Abs(dX) / mX > Mathf.Abs(dZ) / mZ) snapPos.x = kPos.x + (dX > 0 ? mX : -mX);

                if (Mathf.Abs(Mathf.Abs(dZ) - mZ) < MiknatisEsigi || (Mathf.Abs(dX) < mX && Mathf.Abs(dZ) < mZ))
                    if (Mathf.Abs(dZ) / mZ >= Mathf.Abs(dX) / mX) snapPos.z = kPos.z + (dZ > 0 ? mZ : -mZ);
            }
        }
        return snapPos;
    }
}*/