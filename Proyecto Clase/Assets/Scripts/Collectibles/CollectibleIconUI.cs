using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CollectibleIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    private string title;
    private string desc;
    private CollectiblesHUD hud;

    public void Setup(Sprite icon, string title, string description, CollectiblesHUD hud)
    {
        this.title = title;
        this.desc = description;
        this.hud = hud;

        if (iconImage)
            iconImage.sprite = icon;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hud) return;
        hud.ShowTooltip($"{title}\n{desc}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!hud) return;
        hud.HideTooltip();
    }
}
