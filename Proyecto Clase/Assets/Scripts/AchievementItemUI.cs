using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text title;
    public TMP_Text description;
    public Image backgroundPanel;
    public GameObject lockedOverlay;

    // NEW: optional icon image in your prefab
    [Header("Icon (Optional)")]
    public Image iconImage;

    [Header("Text Colors")]
    public Color commonText = Color.white;
    public Color uncommonText = Color.cyan;
    public Color rareText = new Color(0.7f, 0.4f, 1f);
    public Color legendaryText = new Color(1f, 0.75f, 0.2f);

    [Header("Panel Colors")]
    public Color commonBg = new Color(0.2f, 0.2f, 0.2f);
    public Color uncommonBg = new Color(0.15f, 0.25f, 0.3f);
    public Color rareBg = new Color(0.25f, 0.15f, 0.35f);
    public Color legendaryBg = new Color(0.35f, 0.25f, 0.1f);

    public void Setup(AchievementDTO data, bool unlocked)
    {
        title.text = data.name;
        description.text = data.description;

        ApplyRarityVisuals(data.rarity);
        lockedOverlay.SetActive(!unlocked);

        if (!unlocked)
            DimVisuals();

        ApplyIcon(data);
    }

    void ApplyIcon(AchievementDTO data)
    {
        if (!iconImage) return;

        string key = string.IsNullOrEmpty(data.icon_key) ? data.id : data.icon_key;

        Sprite icon = AchievementIconLibrary.Instance
            ? AchievementIconLibrary.Instance.GetIcon(key)
            : null;

        if (icon == null)
        {
            iconImage.enabled = false;
            Debug.LogWarning($"[AchievementItemUI] No icon found for key '{key}'");
            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = icon;
    }


    void ApplyRarityVisuals(string rarity)
    {
        rarity = (rarity ?? "common").ToLower();

        Color bg;
        Color text;

        switch (rarity)
        {
            case "uncommon":
                bg = uncommonBg;
                text = uncommonText;
                break;

            case "rare":
                bg = rareBg;
                text = rareText;
                break;

            case "ultrarare":
                bg = legendaryBg;
                text = legendaryText;
                break;

            default:
                bg = commonBg;
                text = commonText;
                break;
        }

        backgroundPanel.color = bg;
        title.color = text;
        description.color = text;
    }

    void DimVisuals()
    {
        backgroundPanel.color *= 0.5f;
        title.color *= 0.6f;
        description.color *= 0.6f;

        if (iconImage)
            iconImage.color *= 0.6f;
    }
}
