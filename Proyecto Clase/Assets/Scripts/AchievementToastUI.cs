using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementToastUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Image iconImage;              // optional
    [SerializeField] private Image backgroundImage;        // optional

    [Header("Visuals (optional)")]
    [SerializeField] private Color commonBg = new Color(0.2f, 0.2f, 0.2f, 0.95f);
    [SerializeField] private Color rareBg = new Color(0.15f, 0.25f, 0.3f, 0.95f);
    [SerializeField] private Color epicBg = new Color(0.25f, 0.15f, 0.35f, 0.95f);
    [SerializeField] private Color legendaryBg = new Color(0.35f, 0.25f, 0.1f, 0.95f);

    [Header("Timing")]
    [SerializeField] private float lifeSeconds = 2.5f;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string showTrigger = "Show";

    public void Show(AchievementDTO dto)
    {
        if (titleText)
            titleText.text = string.IsNullOrEmpty(dto.name) ? dto.id : dto.name;

        string rarity = string.IsNullOrEmpty(dto.rarity) ? "common" : dto.rarity.ToLowerInvariant();
        if (rarityText)
            rarityText.text = rarity.ToUpperInvariant();

        if (backgroundImage)
        {
            backgroundImage.color = rarity switch
            {
                "rare" => rareBg,
                "epic" => epicBg,
                "legendary" => legendaryBg,
                _ => commonBg
            };
        }

        if (iconImage)
        {
            string key = string.IsNullOrEmpty(dto.icon_key) ? dto.id : dto.icon_key;
            Sprite s = AchievementIconLibrary.Instance ? AchievementIconLibrary.Instance.GetIcon(key) : null;

            iconImage.enabled = (s != null);
            iconImage.sprite = s;
        }

        if (animator && !string.IsNullOrEmpty(showTrigger))
            animator.SetTrigger(showTrigger);

        Destroy(gameObject, lifeSeconds);
    }
}
