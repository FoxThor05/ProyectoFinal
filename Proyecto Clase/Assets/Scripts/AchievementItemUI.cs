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

    [Header("Icon")]
    [Tooltip("Assign the Image component that should display the achievement icon.")]
    public Image iconImage;

    [Header("Text Colors")]
    public Color commonText = Color.white;
    public Color rareText = Color.cyan;
    public Color epicText = new Color(0.7f, 0.4f, 1f);
    public Color legendaryText = new Color(1f, 0.75f, 0.2f);

    [Header("Panel Colors")]
    public Color commonBg = new Color(0.2f, 0.2f, 0.2f);
    public Color rareBg = new Color(0.15f, 0.25f, 0.3f);
    public Color epicBg = new Color(0.25f, 0.15f, 0.35f);
    public Color legendaryBg = new Color(0.35f, 0.25f, 0.1f);

    public void Setup(AchievementDTO data, bool unlocked)
    {
        if (data == null)
        {
            Debug.LogWarning("[AchievementItemUI] Setup called with null data.");
            return;
        }

        // Default to the DB text
        title.text = data.name ?? "";
        description.text = data.description ?? "";

        ApplyRarityVisuals(data.rarity);
        lockedOverlay.SetActive(!unlocked);

        ApplyIcon(data);

        if (!unlocked)
            DimVisuals();
        else
            EnsureIconNotDimmed();
    }

    void ApplyIcon(AchievementDTO data)
    {
        if (!iconImage)
        {
            // If you haven’t assigned it yet, just skip silently (or leave this warning on).
            Debug.LogWarning("[AchievementItemUI] iconImage is not assigned in the inspector.");
            return;
        }

        // Ensure image is enabled and visible
        iconImage.enabled = true;
        iconImage.preserveAspect = true;
        iconImage.type = Image.Type.Simple;

        // Key priority: icon_key, fallback to id
        string key = !string.IsNullOrWhiteSpace(data.icon_key) ? data.icon_key : data.id;

        Sprite icon = null;

        if (AchievementIconLibrary.Instance != null)
            icon = AchievementIconLibrary.Instance.GetIcon(key);

        if (icon == null)
        {
            Debug.LogWarning($"[AchievementItemUI] No icon found for key '{key}'");
            // Keep sprite null; you can also disable image if you prefer:
            // iconImage.enabled = false;
            iconImage.sprite = null;
            return;
        }

        iconImage.sprite = icon;

        // Force alpha to 1 so it doesn’t “inherit” a transparent editor value
        var c = iconImage.color;
        c.a = 1f;
        iconImage.color = c;
    }

    void ApplyRarityVisuals(string rarity)
    {
        rarity = (rarity ?? "").ToLower();

        Color bg;
        Color text;

        switch (rarity)
        {
            case "uncommon":
                bg = rareBg;
                text = rareText;
                break;

            case "rare":
                bg = epicBg;
                text = epicText;
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

        if (backgroundPanel) backgroundPanel.color = bg;
        if (title) title.color = text;
        if (description) description.color = text;
    }

    void DimVisuals()
    {
        if (backgroundPanel) backgroundPanel.color *= 0.5f;
        if (title) title.color *= 0.6f;
        if (description) description.color *= 0.6f;

        if (iconImage)
        {
            // Dim the icon without killing alpha
            var c = iconImage.color;
            c.r *= 0.6f;
            c.g *= 0.6f;
            c.b *= 0.6f;
            c.a = 1f;
            iconImage.color = c;
        }
    }

    void EnsureIconNotDimmed()
    {
        if (!iconImage) return;
        // Reset to full brightness for unlocked (helps if prefab reused)
        var c = iconImage.color;
        c.r = 1f;
        c.g = 1f;
        c.b = 1f;
        c.a = 1f;
        iconImage.color = c;
    }
}
