using UnityEngine;

public class KaldirimSistemi : MonoBehaviour
{
    [Header("Kare Prefabları")]
    public GameObject TamKarePrefab;
    public GameObject CeyrekKarePrefab;

    [Header("Mekanik Ayarlar")]
    public float YerlestirmeMenzili = 8.0f;
    public float DonmeHassasiyeti = 15f;
    public int MaxKatSayisi = 6;
    public float GridBoyutu = 1.0f;

    [Range(0.3f, 0.7f)]
    [Tooltip("Mıknatıslanma gücü. Taşlar zor yapışıyorsa 0.50f - 0.60f yapabilirsin.")]
    public float MiknatisEsigi = 0.55f;

    [Header("Görsel Yumuşatma")]
    public float HareketYumusatma = 35f;

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
        hayalet.HayaletiGuncelle(TamKarePrefab);
        hayalet.HayaletiGoster(false);
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, YerlestirmeMenzili))
        {
            // KutuKontrol fonksiyonu artık içeride 'secilen' prefabı kontrol ediyor
            if (KutuKontrol(hit)) return;

            // 1. DİNAMİK GRID
            float adimX = envanter.SuankiKarePrefabi != null ? envanter.SuankiKarePrefabi.transform.localScale.x : 1.0f;
            float adimZ = envanter.SuankiKarePrefabi != null ? envanter.SuankiKarePrefabi.transform.localScale.z : 1.0f;

            float xGrid = Mathf.Round(hit.point.x / adimX) * adimX;
            float zGrid = Mathf.Round(hit.point.z / adimZ) * adimZ;

            // 2. MIKNATIS (SuankiMiktar kontrolü eklendi)
            Vector3 hedefXz = (Input.GetKey(KeyCode.LeftShift) && envanter.SuankiMiktar > 0)
                ? AgresifSnapHesapla(hit.point)
                : new Vector3(xGrid, 0, zGrid);

            // 3. YÜKSEKLİK HESABI
            float kareH = envanter.SuankiKarePrefabi != null ? envanter.SuankiKarePrefabi.transform.localScale.y : 1f;
            float hedefY = Input.GetKey(KeyCode.LeftShift) ? (kareH / 2.0f) : (ZeminYuksekliginiHesapla(hedefXz, kareH) + (kareH / 2.0f));

            Vector3 finalPos = new Vector3(hedefXz.x, hedefY, hedefXz.z);
            duzlestirilmisPos = Vector3.Lerp(duzlestirilmisPos, finalPos, Time.deltaTime * HareketYumusatma);

            // 4. YERLEŞTİRİLEBİLİRLİK
            Quaternion finalRot = DonmeHesapla();
            bool yerlestirilebilir = YerlestirmeUygunmu(finalPos, finalRot, finalPos.y, kareH);

            // HAYALET GÖSTERİMİ (Miktar kontrolüne bağlandı)
            bool elindeTasVarmi = envanter.SuankiMiktar > 0;
            hayalet.HayaletiGoster(elindeTasVarmi);

            if (elindeTasVarmi)
                hayalet.GorselGuncelle(duzlestirilmisPos, finalRot, envanter.SuankiKarePrefabi.transform.localScale, yerlestirilebilir);

            // 5. SOL TIK: YERLEŞTİRME
            if (Input.GetMouseButtonDown(0) && yerlestirilebilir && envanter.SuankiMiktar > 0)
            {
                Instantiate(envanter.SuankiKarePrefabi, finalPos, finalRot);
                envanter.KareKullan(); // Bu metod artık aktif slottan 1 azaltıyor

                // Eğer hala taş varsa hayaleti güncelle, bittiyse gizle
                if (envanter.SuankiMiktar > 0) hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                else hayalet.HayaletiGoster(false);
            }

            // 6. SAĞ TIK: SİLME VE TOPLAMA
            if (Input.GetMouseButtonDown(1) && hit.collider.CompareTag("Tas"))
            {
                GameObject enUsttekiTas = EnUsttekiTasiBul(hit.collider.transform.position.x, hit.collider.transform.position.z);

                if (enUsttekiTas != null)
                {
                    string objName = enUsttekiTas.name.ToLower();
                    GameObject geriGelen = objName.Contains("ceyrek") ? CeyrekKarePrefab : TamKarePrefab;

                    // Envanterde bu taş için yer var mı?
                    if (envanter.KareAlabilirmi(geriGelen))
                    {
                        envanter.KareEkle(geriGelen);
                        hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                        Destroy(enUsttekiTas);
                    }
                    else
                    {
                        Debug.Log("Bu taş için slot dolu!");
                    }
                }
            }
        }
        else { hayalet.HayaletiGoster(false); }
    }

    // AGRESİF MIKNATIS: Önce En Yakın Kenarı Bulur ve Oraya Kilitler
    Vector3 AgresifSnapHesapla(Vector3 hamPos)
    {
        if (envanter.SuankiKarePrefabi == null) return hamPos;

        Vector3 snapPos = hamPos;
        float myX = envanter.SuankiKarePrefabi.transform.localScale.x;
        float myZ = envanter.SuankiKarePrefabi.transform.localScale.z;

        // Çevredeki en yakın taşı bulmak için tarama yapıyoruz
        Collider[] komsular = Physics.OverlapSphere(hamPos, 1.4f);
        foreach (var komsu in komsular)
        {
            if (komsu.CompareTag("Tas"))
            {
                Vector3 kPos = komsu.transform.position;
                Vector3 kScl = komsu.transform.localScale;

                float mesafeX = (myX + kScl.x) / 2.0f;
                float mesafeZ = (myZ + kScl.z) / 2.0f;

                float diffX = hamPos.x - kPos.x;
                float diffZ = hamPos.z - kPos.z;

                // MIKNATIS MANTIĞI: Eğer kenara MiknatisEsigi kadar yakınsak oraya yapıştır
                // Aynı zamanda taşın içine girmeye çalışılıyorsa dışarı it (Penetration Resolution)
                if (Mathf.Abs(Mathf.Abs(diffX) - mesafeX) < MiknatisEsigi || (Mathf.Abs(diffX) < mesafeX && Mathf.Abs(diffZ) < mesafeZ))
                {
                    if (Mathf.Abs(diffX) / mesafeX > Mathf.Abs(diffZ) / mesafeZ)
                        snapPos.x = kPos.x + (diffX > 0 ? mesafeX : -mesafeX);
                }

                if (Mathf.Abs(Mathf.Abs(diffZ) - mesafeZ) < MiknatisEsigi || (Mathf.Abs(diffX) < mesafeX && Mathf.Abs(diffZ) < mesafeZ))
                {
                    if (Mathf.Abs(diffZ) / mesafeZ >= Mathf.Abs(diffX) / mesafeX)
                        snapPos.z = kPos.z + (diffZ > 0 ? mesafeZ : -mesafeZ);
                }
            }
        }
        return snapPos;
    }

    bool YerlestirmeUygunmu(Vector3 pos, Quaternion rot, float suankiY, float h)
    {
        if (envanter.SuankiKarePrefabi == null) return false;

        // YÜKSEKLİK KONTROLÜ: 
        // Eğer taşın yüksekliği, (Maksimum Kat * Taş Boyu) değerini aşıyorsa izin verme.
        // Shift basılıysa bu kuralı görmezden gel.
        if (!Input.GetKey(KeyCode.LeftShift) && suankiY > (MaxKatSayisi * h + (h / 2f)))
        {
            return false;
        }

        // ÇAKIŞMA KONTROLÜ:
        // Taşın yerleşeceği kutuyu %10 daraltıyoruz ki yan yana duran taşlar birbirini engellemesin.
        Vector3 kBoyut = (envanter.SuankiKarePrefabi.transform.localScale / 2) * 0.90f;
        Collider[] hits = Physics.OverlapBox(pos, kBoyut, rot);

        foreach (var c in hits)
        {
            // Eğer çarptığımız obje bir "Tas" ise ve bizimle hemen hemen aynı yükseklikteyse (iç içe geçme durumu)
            if (c.CompareTag("Tas") && Mathf.Abs(c.transform.position.y - pos.y) < (h * 0.5f))
                return false;
        }

        return true;
    }

    bool KutuKontrol(RaycastHit hit)
    {
        string tag = hit.collider.tag;
        // Etiket kontrolünü daha temiz yapalım
        bool tamKutu = tag == "tamkaretaskaynakkutusu";
        bool ceyrekKutu = tag == "ceyrekkaretaskaynakkutusu";

        if (tamKutu || ceyrekKutu || tag == "KaynakKutusu")
        {
            hayalet.HayaletiGoster(false);
            GameObject secilen = (ceyrekKutu || tag.ToLower().Contains("ceyrek")) ? CeyrekKarePrefab : TamKarePrefab;

            // GetKeyDown kullanarak tek bir karede sadece 1 kez çalışmasını sağlıyoruz
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (envanter.KareAlabilirmi(secilen))
                {
                    envanter.KareEkle(secilen);
                    // Envanterdeki güncel prefabı hayalete gönder
                    if (envanter.SuankiKarePrefabi != null)
                        hayalet.HayaletiGuncelle(envanter.SuankiKarePrefabi);
                }
            }
            return true;
        }
        return false;
    }

    /*int KatSayisiniHesapla(float x, float z)
    {
        int s = 0;
        foreach (var c in Physics.OverlapBox(new Vector3(x, 5f, z), new Vector3(0.01f, 10f, 0.01f), Quaternion.identity))
            if (c.CompareTag("Tas")) s++;
        return s;
    }*/

    float ZeminYuksekliginiHesapla(Vector3 kontrolPos, float kareH)
    {
        // Kontrol kutusunu yerleştirilecek taşın boyutuna göre ayarlıyoruz (hafif daraltılmış)
        Vector3 kutuBoyutu = new Vector3(0.4f, 10f, 0.4f);
        Collider[] altindakiTaslar = Physics.OverlapBox(new Vector3(kontrolPos.x, 10f, kontrolPos.z), kutuBoyutu);

        float enYuksekY = 0f;

        foreach (var c in altindakiTaslar)
        {
            if (c.CompareTag("Tas"))
            {
                // Taşın en üst noktasını bul (merkez Y + boyutu/2)
                float tasUstNoktasi = c.transform.position.y + (c.transform.localScale.y / 2f);
                if (tasUstNoktasi > enYuksekY)
                {
                    enYuksekY = tasUstNoktasi;
                }
            }
        }
        return enYuksekY;
    }

    GameObject EnUsttekiTasiBul(float x, float z)
    {
        GameObject bulunanEnUst = null;
        float maxYukseklik = -100f;

        // Belirtilen X ve Z koordinatında dikey bir sütun boyunca tüm taşları tara
        // OverlapBox ile o koordinattaki tüm objeleri alıyoruz
        Collider[] sütundakiObjeler = Physics.OverlapBox(new Vector3(x, 50f, z), new Vector3(0.1f, 100f, 0.1f));

        foreach (var c in sütundakiObjeler)
        {
            if (c.CompareTag("Tas"))
            {
                if (c.transform.position.y > maxYukseklik)
                {
                    maxYukseklik = c.transform.position.y;
                    bulunanEnUst = c.gameObject;
                }
            }
        }
        return bulunanEnUst;
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
}
