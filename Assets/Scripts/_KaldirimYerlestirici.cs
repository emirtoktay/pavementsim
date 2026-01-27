using UnityEngine;

public class KaldirimYerlestirici : MonoBehaviour
{
    [Header("Kare Prefabları")]
    public GameObject TamKarePrefab;      // 1.0 boyutundaki normal kare
    public GameObject CeyrekKarePrefab;   // 0.25 boyutundaki çeyrek kare
    
    [Header("Görsel Ayarlar")]
    public Material hayaletMaterial;  // Yerleştirilebilir şeffaf materyal
    public Material hataMaterial;     // Hatalı/Dolu yerler için kırmızı materyal
    
    [Header("Mekanik Ayarlar")]
    public float gridBoyutu = 1.0f;           // Izgara genişliği
    public float yerlestirmeMenzili = 8.0f;   // Taşa ulaşma mesafesi
    public float donmeHassasiyeti = 15f;      // Tekerlek ile dönme hızı
    public int maxKatSayisi = 6;              // Üst üste en fazla kaç taş gelebilir
    public float miknatisEsigi = 0.35f;       // Shift modundaki yapışma gücü

    [Header("Envanter Sistemi")]
    public int EldekiKareSayisi = 0;           // Şu an elinde olan kare sayısı
    public int MaxKareKapasitesi = 2;          // Elinde taşıyabileceğin max kare

    private GameObject SuankiKarePrefabi;      // O an elimizde hangi kare türü varsa o
    private GameObject hayaletObje;
    private bool yerlestirilebilirmi = true;
    private float suankiDonmeY = 0f;
    private float kareYuksekligi;

    void Start()
    {
        // Başlangıçta tam kare ile başlıyoruz
        SuankiKarePrefabi = TamKarePrefab;
        HayaletKareyiGuncelle();
    }

    // Yeni bir kare türü alınca (Kutulardan) hayaleti güncelleyen fonksiyon
    void HayaletKareyiGuncelle()
    {
        if (hayaletObje != null) Destroy(hayaletObje);
        
        hayaletObje = Instantiate(SuankiKarePrefabi);
        if (hayaletObje.GetComponent<Collider>())
            hayaletObje.GetComponent<Collider>().enabled = false;
        
        hayaletObje.GetComponent<Renderer>().material = hayaletMaterial;
        
        // Kare yüksekliğini o anki prefabın ölçeğinden al
        kareYuksekligi = SuankiKarePrefabi.transform.localScale.y;
    }

    void Update()
    {
        // FPS kamerası için ekranın tam ortasından bir ışın (ray) gönderir
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, yerlestirmeMenzili))
        {
            // --- 1. TAM KARE KAYNAK KUTUSU ETKİLEŞİMİ (Tag: KaynakKutusu) ---
            if (hit.collider.gameObject.CompareTag("tamkaretaskaynakkutusu"))
            {
                hayaletObje.transform.position = new Vector3(0, -100, 0); // Kutudayken hayaleti sakla

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (EldekiKareSayisi < MaxKareKapasitesi)
                    {
                        EldekiKareSayisi++;
                        SuankiKarePrefabi = TamKarePrefab; // Tam kareyi seç
                        HayaletKareyiGuncelle();
                        Debug.Log("Tam kare alındı! Envanter: " + EldekiKareSayisi);
                    }
                }
                return;
            }

            // --- 2. ÇEYREK KARE KAYNAK KUTUSU ETKİLEŞİMİ (Tag: CeyrekKaynakKutusu) ---
            if (hit.collider.gameObject.CompareTag("ceyrekkaretaskaynakkutusu"))
            {
                hayaletObje.transform.position = new Vector3(0, -100, 0); // Kutudayken hayaleti sakla

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (EldekiKareSayisi < MaxKareKapasitesi)
                    {
                        EldekiKareSayisi++;
                        SuankiKarePrefabi = CeyrekKarePrefab; // Çeyrek kareyi seç
                        HayaletKareyiGuncelle();
                        Debug.Log("0.25'lik kare alındı! Envanter: " + EldekiKareSayisi);
                    }
                }
                return;
            }

            // --- 3. POZİSYON VE YÜKSEKLİK HESAPLAMALARI ---
            float xGrid = Mathf.Round(hit.point.x / gridBoyutu) * gridBoyutu;
            float zGrid = Mathf.Round(hit.point.z / gridBoyutu) * gridBoyutu;
            
            // O sütundaki mevcut taş sayısını bul (Üst üste dizme için)
            int katSayisi = KatSayisiniHesapla(xGrid, zGrid);
            
            // Yüksekliği (Y) her iki mod için ortak sabitle (Zıplamayı önler)
            float sabitY = (katSayisi * kareYuksekligi) + (kareYuksekligi / 2.0f) + 0.01f;

            Vector3 finalPos;

            // --- 4. MOD KONTROLLERİ (SHIFT VE NORMAL) ---
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // SHIFT MODU: X ve Z ekseninde mıknatıslı ama serbest hareket
                Vector3 hamMiknatisPos = SoftSnapHesapla(hit.point);
                finalPos = new Vector3(hamMiknatisPos.x, sabitY, hamMiknatisPos.z);

                // Tekerlekle serbest döndürme
                float tekerlek = Input.mouseScrollDelta.y;
                if (tekerlek != 0) suankiDonmeY += tekerlek * donmeHassasiyeti;
                hayaletObje.transform.rotation = Quaternion.Euler(0, suankiDonmeY, 0);
            }
            else
            {
                // NORMAL MOD: Tam ızgara (Grid) hizalaması ve açı sıfırlama
                finalPos = new Vector3(xGrid, sabitY, zGrid);
                suankiDonmeY = 0f;
                hayaletObje.transform.rotation = Quaternion.identity;
            }

            // --- 5. ENVANTER KONTROLÜ VE HAYALET DURUMU ---
            if (EldekiKareSayisi <= 0)
            {
                hayaletObje.transform.position = new Vector3(0, -100, 0);
                yerlestirilebilirmi = false;
            }
            else
            {
                hayaletObje.transform.position = finalPos;

                // Çakışma ve limit kontrolleri
                bool katLimitAsildi = (!Input.GetKey(KeyCode.LeftShift) && katSayisi >= maxKatSayisi);
                bool cakisiyor = CheckOverlap(finalPos);

                if (katLimitAsildi || cakisiyor)
                {
                    yerlestirilebilirmi = false;
                    hayaletObje.GetComponent<Renderer>().material = hataMaterial;
                }
                else
                {
                    yerlestirilebilirmi = true;
                    hayaletObje.GetComponent<Renderer>().material = hayaletMaterial;
                }
            }

            // --- 6. YERLEŞTİRME (SOL TIK) ---
            if (Input.GetMouseButtonDown(0) && yerlestirilebilirmi && EldekiKareSayisi > 0)
            {
                Instantiate(SuankiKarePrefabi, finalPos, hayaletObje.transform.rotation);
                EldekiKareSayisi--; 
            }

            // --- 7. SİLME VE GERİ TOPLAMA (SAĞ TIK) ---
            if (Input.GetMouseButtonDown(1))
            {
                if (hit.collider.gameObject.CompareTag("Tas"))
                {
                    if (EldekiKareSayisi < MaxKareKapasitesi)
                    {
                        EldekiKareSayisi++;
                        Destroy(hit.collider.gameObject);
                    }
                    else
                    {
                        Destroy(hit.collider.gameObject);
                    }
                }
            }
        }
        else 
        {
            // Menzil dışındaysa hayaleti sakla
            hayaletObje.transform.position = new Vector3(0, -100, 0);
        }
    }

    // --- YARDIMCI FONKSİYONLAR ---

    Vector3 SoftSnapHesapla(Vector3 hamPos)
    {
        Collider[] komsular = Physics.OverlapSphere(hamPos, gridBoyutu * 1.5f);
        Vector3 snapPos = hamPos;

        foreach (var komsu in komsular)
        {
            if (komsu.gameObject.CompareTag("Tas"))
            {
                Vector3 kPos = komsu.transform.position;
                if (Mathf.Abs(hamPos.x - kPos.x) < miknatisEsigi) snapPos.x = kPos.x;
                if (Mathf.Abs(hamPos.z - kPos.z) < miknatisEsigi) snapPos.z = kPos.z;

                if (Mathf.Abs(Mathf.Abs(hamPos.x - kPos.x) - gridBoyutu) < miknatisEsigi)
                    snapPos.x = kPos.x + (hamPos.x > kPos.x ? gridBoyutu : -gridBoyutu);
                
                if (Mathf.Abs(Mathf.Abs(hamPos.z - kPos.z) - gridBoyutu) < miknatisEsigi)
                    snapPos.z = kPos.z + (hamPos.z > kPos.z ? gridBoyutu : -gridBoyutu);
            }
        }
        return snapPos;
    }

    int KatSayisiniHesapla(float x, float z)
    {
        Vector3 taramaMerkezi = new Vector3(x, 5f, z); 
        Vector3 taramaBoyutu = new Vector3(gridBoyutu * 0.4f, 10f, gridBoyutu * 0.4f);
        Collider[] hitColliders = Physics.OverlapBox(taramaMerkezi, taramaBoyutu, Quaternion.identity);
        
        int sayac = 0;
        foreach (var col in hitColliders)
        {
            if (col.gameObject.CompareTag("Tas")) sayac++;
        }
        return sayac;
    }

    bool CheckOverlap(Vector3 pos)
    {
        // Çakışma kontrolünü prefabın o anki ölçeğine göre dinamik yapıyoruz
        Vector3 kontrolBoyutu = (SuankiKarePrefabi.transform.localScale / 2) * 0.92f;
        Collider[] hitColliders = Physics.OverlapBox(pos, kontrolBoyutu, hayaletObje.transform.rotation);
        
        foreach (var col in hitColliders)
        {
            if (col.gameObject.CompareTag("Tas"))
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    if (Mathf.Abs(col.transform.position.y - pos.y) > (kareYuksekligi * 0.8f)) return true;
                }
                else return true;
            }
        }
        return false;
    }
}
/*using UnityEngine;

public class KaldirimYerlestirici : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public GameObject tasPrefab;      // Yerleştirilecek asıl taş prefabı
    public Material hayaletMaterial;  // Yerleştirilebilir şeffaf materyal
    public Material hataMaterial;    // Hatalı/Dolu yerler için kırmızı materyal
    
    [Header("Mekanik Ayarlar")]
    public float gridBoyutu = 1.0f;           // Izgara genişliği
    public float yerlestirmeMenzili = 8.0f;   // Taşa ulaşma mesafesi
    public float donmeHassasiyeti = 15f;      // Tekerlek ile dönme hızı
    public int maxKatSayisi = 6;              // Üst üste en fazla kaç taş gelebilir
    public float miknatisEsigi = 0.35f;       // Shift modundaki yapışma gücü

    [Header("Envanter Sistemi")]
    public int eldekiTasSayisi = 0;           // Şu an elinde olan taş
    public int maxTasKapasitesi = 2;          // Elinde taşıyabileceğin max taş

    private GameObject hayaletObje;
    private bool yerlestirilebilirmi = true;
    private float suankiDonmeY = 0f;
    private float tasYuksekligi;

    void Start()
    {
        // Hayalet objeyi oluştur ve collider'ını kapat (raycast'i bozmasın diye)
        hayaletObje = Instantiate(tasPrefab);
        if (hayaletObje.GetComponent<Collider>())
            hayaletObje.GetComponent<Collider>().enabled = false;
        
        hayaletObje.GetComponent<Renderer>().material = hayaletMaterial;
        
        // Taşın yüksekliğini prefab'ın Scale Y değerinden otomatik al
        tasYuksekligi = tasPrefab.transform.localScale.y;
    }

    void Update()
    {
        // FPS kamerası için ekranın tam ortasından bir ışın (ray) gönderir
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, yerlestirmeMenzili))
        {
            // --- 1. KAYNAK KUTUSU ETKİLEŞİMİ (TAŞ ALMA) ---
            if (hit.collider.gameObject.CompareTag("KaynakKutusu"))
            {
                hayaletObje.transform.position = new Vector3(0, -100, 0); // Kutudayken hayaleti sakla

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (eldekiTasSayisi < maxTasKapasitesi)
                    {
                        eldekiTasSayisi++;
                        Debug.Log("Kutudan taş alındı! Envanter: " + eldekiTasSayisi + "/" + maxTasKapasitesi);
                    }
                    else
                    {
                        Debug.Log("Envanter zaten dolu!");
                    }
                }
                return; // Kutuya bakarken taş döşeme işlemlerini çalıştırma
            }

            // --- 2. POZİSYON VE YÜKSEKLİK HESAPLAMALARI ---
            float xGrid = Mathf.Round(hit.point.x / gridBoyutu) * gridBoyutu;
            float zGrid = Mathf.Round(hit.point.z / gridBoyutu) * gridBoyutu;
            
            // O sütundaki mevcut taş sayısını bul (Üst üste dizme için)
            int katSayisi = KatSayisiniHesapla(xGrid, zGrid);
            
            // Yüksekliği (Y) her iki mod için ortak sabitle (Zıplamayı önler)
            float sabitY = (katSayisi * tasYuksekligi) + (tasYuksekligi / 2.0f) + 0.01f;

            Vector3 finalPos;

            // --- 3. MOD KONTROLLERİ (SHIFT VE NORMAL) ---
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // SHIFT MODU: X ve Z ekseninde mıknatıslı ama serbest hareket
                Vector3 hamMiknatisPos = SoftSnapHesapla(hit.point);
                finalPos = new Vector3(hamMiknatisPos.x, sabitY, hamMiknatisPos.z);

                // Tekerlekle serbest döndürme
                float tekerlek = Input.mouseScrollDelta.y;
                if (tekerlek != 0) suankiDonmeY += tekerlek * donmeHassasiyeti;
                hayaletObje.transform.rotation = Quaternion.Euler(0, suankiDonmeY, 0);
            }
            else
            {
                // NORMAL MOD: Tam ızgara (Grid) hizalaması ve açı sıfırlama
                finalPos = new Vector3(xGrid, sabitY, zGrid);
                suankiDonmeY = 0f;
                hayaletObje.transform.rotation = Quaternion.identity;
            }

            // --- 4. ENVANTER KONTROLÜ VE HAYALET DURUMU ---
            if (eldekiTasSayisi <= 0)
            {
                hayaletObje.transform.position = new Vector3(0, -100, 0);
                yerlestirilebilirmi = false;
            }
            else
            {
                hayaletObje.transform.position = finalPos;

                // Çakışma ve limit kontrolleri (6 kat sınırı ve başka taşla iç içe geçme)
                bool katLimitAsildi = (!Input.GetKey(KeyCode.LeftShift) && katSayisi >= maxKatSayisi);
                bool cakisiyor = CheckOverlap(finalPos);

                if (katLimitAsildi || cakisiyor)
                {
                    yerlestirilebilirmi = false;
                    hayaletObje.GetComponent<Renderer>().material = hataMaterial;
                }
                else
                {
                    yerlestirilebilirmi = true;
                    hayaletObje.GetComponent<Renderer>().material = hayaletMaterial;
                }
            }

            // --- 5. YERLEŞTİRME (SOL TIK) ---
            if (Input.GetMouseButtonDown(0) && yerlestirilebilirmi && eldekiTasSayisi > 0)
            {
                Instantiate(tasPrefab, finalPos, hayaletObje.transform.rotation);
                eldekiTasSayisi--; // Taş yerleştirince envanterden düş
                Debug.Log("Taş yerleştirildi. Kalan: " + eldekiTasSayisi);
            }

            // --- 6. SİLME VE GERİ TOPLAMA (SAĞ TIK) ---
            if (Input.GetMouseButtonDown(1))
            {
                if (hit.collider.gameObject.CompareTag("Tas"))
                {
                    // Envanterde yer varsa (2'den azsa) yerden taşı geri al
                    if (eldekiTasSayisi < maxTasKapasitesi)
                    {
                        eldekiTasSayisi++;
                        Destroy(hit.collider.gameObject);
                        Debug.Log("Yerden taş toplandı. Envanter: " + eldekiTasSayisi);
                    }
                    else
                    {
                        Debug.Log("Envanter dolu! Taşı sadece silebilirsin ama toplayamazsın.");
                        // Eğer doluyken silmesini istemiyorsan Destroy'u bu else'in dışına alma
                        Destroy(hit.collider.gameObject);
                    }
                }
            }
        }
        else 
        {
            // Menzil dışındaysa hayaleti sakla
            hayaletObje.transform.position = new Vector3(0, -100, 0);
        }
    }

    // --- YARDIMCI FONKSİYONLAR (ALGORİTMALAR) ---

    // Milimetrik boşlukları kapatan "Mıknatıslı" yapışma algoritması
    Vector3 SoftSnapHesapla(Vector3 hamPos)
    {
        Collider[] komsular = Physics.OverlapSphere(hamPos, gridBoyutu * 1.5f);
        Vector3 snapPos = hamPos;

        foreach (var komsu in komsular)
        {
            if (komsu.gameObject.CompareTag("Tas"))
            {
                Vector3 kPos = komsu.transform.position;
                
                // Merkeze hizalama
                if (Mathf.Abs(hamPos.x - kPos.x) < miknatisEsigi) snapPos.x = kPos.x;
                if (Mathf.Abs(hamPos.z - kPos.z) < miknatisEsigi) snapPos.z = kPos.z;

                // Kenara yapışma (Izgara boyutuna göre milimetrik kilitlenme)
                if (Mathf.Abs(Mathf.Abs(hamPos.x - kPos.x) - gridBoyutu) < miknatisEsigi)
                    snapPos.x = kPos.x + (hamPos.x > kPos.x ? gridBoyutu : -gridBoyutu);
                
                if (Mathf.Abs(Mathf.Abs(hamPos.z - kPos.z) - gridBoyutu) < miknatisEsigi)
                    snapPos.z = kPos.z + (hamPos.z > kPos.z ? gridBoyutu : -gridBoyutu);
            }
        }
        return snapPos;
    }

    // Belirli bir koordinattaki taşları yukarıdan aşağıya tarayarak sayar
    int KatSayisiniHesapla(float x, float z)
    {
        Vector3 taramaMerkezi = new Vector3(x, 5f, z); 
        Vector3 taramaBoyutu = new Vector3(gridBoyutu * 0.4f, 10f, gridBoyutu * 0.4f);
        Collider[] hitColliders = Physics.OverlapBox(taramaMerkezi, taramaBoyutu, Quaternion.identity);
        
        int sayac = 0;
        foreach (var col in hitColliders)
        {
            if (col.gameObject.CompareTag("Tas")) sayac++;
        }
        return sayac;
    }

    // Taşların birbirinin içine geçmesini kontrol eden sistem
    bool CheckOverlap(Vector3 pos)
    {
        Vector3 kontrolBoyutu = (tasPrefab.transform.localScale / 2) * 0.92f;
        Collider[] hitColliders = Physics.OverlapBox(pos, kontrolBoyutu, hayaletObje.transform.rotation);
        
        foreach (var col in hitColliders)
        {
            if (col.gameObject.CompareTag("Tas"))
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    // Normal modda dikey (üst üste) çakışmayı hata kabul etme (kat çıkıyoruz)
                    if (Mathf.Abs(col.transform.position.y - pos.y) > (tasYuksekligi * 0.8f)) return true;
                }
                else return true; // Shift modunda her türlü çakışma hatadır
            }
        }
        return false;
    }
}*/
/*using UnityEngine;

public class KaldirimYerlestirici : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public GameObject tasPrefab;
    public Material hayaletMaterial;
    public Material hataMaterial;
    
    [Header("Mekanik Ayarlar")]
    public float gridBoyutu = 1.0f;
    public float yerlestirmeMenzili = 8.0f;
    public float donmeHassasiyeti = 15f;
    public int maxKatSayisi = 6; 
    public float miknatisEsigi = 0.35f;

    [Header("Envanter Sistemi")]
    public int eldekiTasSayisi = 0;
    public int maxTasKapasitesi = 2;

    private GameObject hayaletObje;
    private bool yerlestirilebilirmi = true;
    private float suankiDonmeY = 0f;
    private float tasYuksekligi;

    void Start()
    {
        // Hayalet objeyi oluştur ve collider'ını kapat
        hayaletObje = Instantiate(tasPrefab);
        if (hayaletObje.GetComponent<Collider>())
            hayaletObje.GetComponent<Collider>().enabled = false;
        
        hayaletObje.GetComponent<Renderer>().material = hayaletMaterial;
        tasYuksekligi = tasPrefab.transform.localScale.y;
    }

    void Update()
    {
        // FPS kamerası için ekranın tam ortasından ışın gönderir
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, yerlestirmeMenzili))
        {
            // --- 1. KAYNAK KUTUSU ETKİLEŞİMİ (E TUŞU) ---
            if (hit.collider.gameObject.CompareTag("KaynakKutusu"))
            {
                hayaletObje.transform.position = new Vector3(0, -100, 0); // Kutudayken hayaleti sakla

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (eldekiTasSayisi < maxTasKapasitesi)
                    {
                        eldekiTasSayisi++;
                        Debug.Log("Kutudan taş alındı. Envanter: " + eldekiTasSayisi);
                    }
                }
                return; // Kutunun içine taş döşenmesini engelle
            }

            // --- 2. TEMEL POZİSYON HESAPLAMALARI ---
            float xGrid = Mathf.Round(hit.point.x / gridBoyutu) * gridBoyutu;
            float zGrid = Mathf.Round(hit.point.z / gridBoyutu) * gridBoyutu;
            
            Vector3 hedefXz;
            
            // --- 3. MOD KONTROLLERİ (SHIFT VE NORMAL) ---
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // SHIFT MODU: Serbest hareket ve güçlü mıknatıslanma
                hedefXz = SoftSnapHesapla(hit.point);
                
                // Mouse tekerleği ile döndürme
                float tekerlek = Input.mouseScrollDelta.y;
                if (tekerlek != 0) suankiDonmeY += tekerlek * donmeHassasiyeti;
                hayaletObje.transform.rotation = Quaternion.Euler(0, suankiDonmeY, 0);
            }
            else
            {
                // NORMAL MOD: Izgara hizalaması ve açı sıfırlama
                hedefXz = new Vector3(xGrid, 0, zGrid);
                suankiDonmeY = 0f;
                hayaletObje.transform.rotation = Quaternion.identity;
            }

            // --- 4. YÜKSEKLİK VE ÜST ÜSTE DİZME SİSTEMİ ---
            // Mıknatıslanan veya gridlenen noktadaki kat sayısını bul
            int katSayisi = KatSayisiniHesapla(hedefXz.x, hedefXz.z);
            
            // Yüksekliği (Y) her iki mod için ortak hesapla (Zıplamayı önler)
            float sabitY = (katSayisi * tasYuksekligi) + (tasYuksekligi / 2.0f) + 0.01f;
            Vector3 finalPos = new Vector3(hedefXz.x, sabitY, hedefXz.z);

            // --- 5. ENVANTER VE HAYALET GÖRÜNÜRLÜĞÜ ---
            if (eldekiTasSayisi <= 0)
            {
                hayaletObje.transform.position = new Vector3(0, -100, 0);
                yerlestirilebilirmi = false;
            }
            else
            {
                hayaletObje.transform.position = finalPos;

                // Çakışma ve limit kontrolleri
                bool katLimitAsildi = (!Input.GetKey(KeyCode.LeftShift) && katSayisi >= maxKatSayisi);
                bool cakisiyor = CheckOverlap(finalPos);

                if (katLimitAsildi || cakisiyor)
                {
                    yerlestirilebilirmi = false;
                    hayaletObje.GetComponent<Renderer>().material = hataMaterial;
                }
                else
                {
                    yerlestirilebilirmi = true;
                    hayaletObje.GetComponent<Renderer>().material = hayaletMaterial;
                }
            }

            // --- 6. YERLEŞTİRME (SOL TIK) ---
            if (Input.GetMouseButtonDown(0) && yerlestirilebilirmi && eldekiTasSayisi > 0)
            {
                Instantiate(tasPrefab, finalPos, hayaletObje.transform.rotation);
                eldekiTasSayisi--; // Envanterden bir taş düş
            }

            // --- 7. SİLME VE GERİ ALMA (SAĞ TIK) ---
            if (Input.GetMouseButtonDown(1))
            {
                if (hit.collider.gameObject.CompareTag("Tas"))
                {
                    // Envanterde yer varsa silinen taşı geri ekle
                    if (eldekiTasSayisi < maxTasKapasitesi)
                    {
                        eldekiTasSayisi++;
                        Debug.Log("Yerden taş geri alındı. Envanter: " + eldekiTasSayisi);
                    }
                    
                    Destroy(hit.collider.gameObject);
                }
            }
        }
        else 
        {
            // Menzil dışındaysa hayaleti sakla
            hayaletObje.transform.position = new Vector3(0, -100, 0);
        }
    }

    // --- YARDIMCI FONKSİYONLAR ---

    // Milimetrik boşlukları kapatan mıknatıs sistemi
    Vector3 SoftSnapHesapla(Vector3 hamPos)
    {
        Collider[] komsular = Physics.OverlapSphere(hamPos, gridBoyutu * 1.5f);
        Vector3 snapPos = hamPos;
        foreach (var komsu in komsular)
        {
            if (komsu.gameObject.CompareTag("Tas"))
            {
                Vector3 kPos = komsu.transform.position;
                if (Mathf.Abs(hamPos.x - kPos.x) < miknatisEsigi) snapPos.x = kPos.x;
                if (Mathf.Abs(hamPos.z - kPos.z) < miknatisEsigi) snapPos.z = kPos.z;

                if (Mathf.Abs(Mathf.Abs(hamPos.x - kPos.x) - gridBoyutu) < miknatisEsigi)
                    snapPos.x = kPos.x + (hamPos.x > kPos.x ? gridBoyutu : -gridBoyutu);
                
                if (Mathf.Abs(Mathf.Abs(hamPos.z - kPos.z) - gridBoyutu) < miknatisEsigi)
                    snapPos.z = kPos.z + (hamPos.z > kPos.z ? gridBoyutu : -gridBoyutu);
            }
        }
        return snapPos;
    }

    // Belirli bir sütundaki taş sayısını dikey tarama ile bulur
    int KatSayisiniHesapla(float x, float z)
    {
        Vector3 taramaMerkezi = new Vector3(x, 5f, z); 
        Vector3 taramaBoyutu = new Vector3(gridBoyutu * 0.4f, 10f, gridBoyutu * 0.4f);
        Collider[] hitColliders = Physics.OverlapBox(taramaMerkezi, taramaBoyutu, Quaternion.identity);
        int sayac = 0;
        foreach (var col in hitColliders) { if (col.gameObject.CompareTag("Tas")) sayac++; }
        return sayac;
    }

    // Taşların iç içe geçip geçmediğini kontrol eder
    bool CheckOverlap(Vector3 pos)
    {
        Vector3 kontrolBoyutu = (tasPrefab.transform.localScale / 2) * 0.92f;
        Collider[] hitColliders = Physics.OverlapBox(pos, kontrolBoyutu, hayaletObje.transform.rotation);
        foreach (var col in hitColliders)
        {
            if (col.gameObject.CompareTag("Tas"))
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    // Üst üste dizerken sadece dikey çakışmayı kabul eder
                    if (Mathf.Abs(col.transform.position.y - pos.y) > (tasYuksekligi * 0.8f)) return true;
                }
                else return true; // Shift modunda her türlü çakışma hatadır
            }
        }
        return false;
    }
}*/
/*using UnityEngine;

public class KaldirimYerlestirici : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public GameObject tasPrefab;
    public Material hayaletMaterial;
    public Material hataMaterial;
    
    [Header("Mekanik Ayarlar")]
    public float gridBoyutu = 1.0f;
    public float yerlestirmeMenzili = 8.0f;
    public float donmeHassasiyeti = 15f;
    public int maxKatSayisi = 6; 
    public float miknatisEsigi = 0.35f;

    private GameObject hayaletObje;
    private bool yerlestirilebilirmi = true;
    private float suankiDonmeY = 0f;
    private float tasYuksekligi;

    void Start()
    {
        hayaletObje = Instantiate(tasPrefab);
        if (hayaletObje.GetComponent<Collider>())
            hayaletObje.GetComponent<Collider>().enabled = false;
        
        hayaletObje.GetComponent<Renderer>().material = hayaletMaterial;
        tasYuksekligi = tasPrefab.transform.localScale.y;
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, yerlestirmeMenzili))
        {
            Vector3 finalPos;
            
            // 1. Önce Izgara (Grid) Pozisyonunu ve Kat Sayısını Bul
            float xGrid = Mathf.Round(hit.point.x / gridBoyutu) * gridBoyutu;
            float zGrid = Mathf.Round(hit.point.z / gridBoyutu) * gridBoyutu;
            int katSayisi = KatSayisiniHesapla(xGrid, zGrid);
            
            // 2. HER İKİ MOD İÇİN ORTAK YÜKSEKLİK HESAPLA (Pop-up hatasını bu çözer)
            float sabitY = (katSayisi * tasYuksekligi) + (tasYuksekligi / 2.0f) + 0.01f;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                // --- SHIFT MODU: Sadece X ve Z'de Mıknatıslı Hareket ---
                Vector3 hamPos = SoftSnapHesapla(hit.point);
                
                // Yüksekliği değiştirmeden (kalkmadan) sadece X ve Z'yi ata
                finalPos = new Vector3(hamPos.x, sabitY, hamPos.z);

                // Döndürme
                float tekerlek = Input.mouseScrollDelta.y;
                if (tekerlek != 0) suankiDonmeY += tekerlek * donmeHassasiyeti;
                hayaletObje.transform.rotation = Quaternion.Euler(0, suankiDonmeY, 0);
            }
            else
            {
                // --- NORMAL MOD: Izgara ve Üst Üste Dizme ---
                finalPos = new Vector3(xGrid, sabitY, zGrid);

                suankiDonmeY = 0f;
                hayaletObje.transform.rotation = Quaternion.identity;
            }

            hayaletObje.transform.position = finalPos;

            // --- HATA VE ÇAKIŞMA KONTROLÜ ---
            bool katLimitAsildi = (!Input.GetKey(KeyCode.LeftShift) && katSayisi >= maxKatSayisi);
            bool cakisiyor = CheckOverlap(finalPos);

            if (katLimitAsildi || cakisiyor)
            {
                yerlestirilebilirmi = false;
                hayaletObje.GetComponent<Renderer>().material = hataMaterial;
            }
            else
            {
                yerlestirilebilirmi = true;
                hayaletObje.GetComponent<Renderer>().material = hayaletMaterial;
            }

            if (Input.GetMouseButtonDown(0) && yerlestirilebilirmi)
                Instantiate(tasPrefab, finalPos, hayaletObje.transform.rotation);

            if (Input.GetMouseButtonDown(1) && hit.collider.gameObject.CompareTag("Tas"))
                Destroy(hit.collider.gameObject);
        }
        else 
        {
            hayaletObje.transform.position = new Vector3(0, -100, 0);
        }
    }

    Vector3 SoftSnapHesapla(Vector3 hamPos)
    {
        Collider[] komsular = Physics.OverlapSphere(hamPos, gridBoyutu * 1.5f);
        Vector3 snapPos = hamPos;

        foreach (var komsu in komsular)
        {
            if (komsu.gameObject.CompareTag("Tas"))
            {
                Vector3 kPos = komsu.transform.position;
                if (Mathf.Abs(hamPos.x - kPos.x) < miknatisEsigi) snapPos.x = kPos.x;
                if (Mathf.Abs(hamPos.z - kPos.z) < miknatisEsigi) snapPos.z = kPos.z;
                
                if (Mathf.Abs(Mathf.Abs(hamPos.x - kPos.x) - gridBoyutu) < miknatisEsigi)
                    snapPos.x = kPos.x + (hamPos.x > kPos.x ? gridBoyutu : -gridBoyutu);
                
                if (Mathf.Abs(Mathf.Abs(hamPos.z - kPos.z) - gridBoyutu) < miknatisEsigi)
                    snapPos.z = kPos.z + (hamPos.z > kPos.z ? gridBoyutu : -gridBoyutu);
            }
        }
        return snapPos;
    }

    int KatSayisiniHesapla(float x, float z)
    {
        Vector3 taramaMerkezi = new Vector3(x, 5f, z); 
        Vector3 taramaBoyutu = new Vector3(gridBoyutu * 0.4f, 10f, gridBoyutu * 0.4f);
        Collider[] hitColliders = Physics.OverlapBox(taramaMerkezi, taramaBoyutu, Quaternion.identity);
        int sayac = 0;
        foreach (var col in hitColliders)
        {
            if (col.gameObject.CompareTag("Tas")) sayac++;
        }
        return sayac;
    }

    bool CheckOverlap(Vector3 pos)
    {
        Vector3 kontrolBoyutu = (tasPrefab.transform.localScale / 2) * 0.92f;
        Collider[] hitColliders = Physics.OverlapBox(pos, kontrolBoyutu, hayaletObje.transform.rotation);
        foreach (var col in hitColliders)
        {
            if (col.gameObject.CompareTag("Tas"))
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    if (Mathf.Abs(col.transform.position.y - pos.y) > (tasYuksekligi * 0.8f)) return true;
                }
                else return true;
            }
        }
        return false;
    }
}

*/
