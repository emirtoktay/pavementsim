using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIYonetici : MonoBehaviour
{
    public KareEnvanteri envanter;

    [Header("Slot 1 UI")]
    public Image slot1Resim;
    public TextMeshProUGUI slot1Miktar;
    public GameObject slot1Cerceve;

    [Header("Slot 2 UI")]
    public Image slot2Resim;
    public TextMeshProUGUI slot2Miktar;
    public GameObject slot2Cerceve;

    [Header("Taş Resimleri (Sprite)")]
    public Sprite tamKareSprite;
    public Sprite ceyrekKareSprite;

    void Update()
    {
        // SLOT 1 GÜNCELLEME
        Guncelle(0, slot1Resim, slot1Miktar, slot1Cerceve);
        
        // SLOT 2 GÜNCELLEME
        Guncelle(1, slot2Resim, slot2Miktar, slot2Cerceve);
    }

    void Guncelle(int index, Image img, TextMeshProUGUI txt, GameObject cerceve)
    {
        var slot = envanter.slotlar[index];

        // Çerçeve kontrolü (Seçili slot parlasın)
        cerceve.SetActive(envanter.aktifSlotIndex == index);

        if (slot.prefab != null && slot.miktar > 0)
        {
            img.enabled = true;
            img.sprite = slot.prefab.name.ToLower().Contains("ceyrek") ? ceyrekKareSprite : tamKareSprite;
            txt.text = slot.miktar.ToString();
        }
        else
        {
            img.enabled = false;
            txt.text = "";
        }
    }
}