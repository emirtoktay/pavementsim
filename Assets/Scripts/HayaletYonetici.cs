using UnityEngine;

public class HayaletYonetici : MonoBehaviour
{
    [Header("Görsel Materyaller")]
    public Material HayaletMaterial; 
    public Material HataMaterial;    

    private GameObject hayaletObje;

    public void HayaletiGuncelle(GameObject yeniPrefab)
    {
        if (hayaletObje != null) Destroy(hayaletObje);
        if (yeniPrefab == null) return;
        
        hayaletObje = Instantiate(yeniPrefab);
        if (hayaletObje.GetComponent<Collider>())
            hayaletObje.GetComponent<Collider>().enabled = false;
        
        hayaletObje.GetComponent<Renderer>().material = HayaletMaterial;
    }

    public void GorselGuncelle(Vector3 pos, Quaternion rot, Vector3 scale, bool yerlestirilebilir)
    {
        if (hayaletObje == null) return;
        
        hayaletObje.transform.position = pos;
        hayaletObje.transform.rotation = rot;
        hayaletObje.transform.localScale = scale;
        hayaletObje.GetComponent<Renderer>().material = yerlestirilebilir ? HayaletMaterial : HataMaterial;
    }

    public void HayaletiGoster(bool goster)
    {
        if (hayaletObje != null) hayaletObje.SetActive(goster);
    }

    public Quaternion SuankiRotation() => hayaletObje != null ? hayaletObje.transform.rotation : Quaternion.identity;
}