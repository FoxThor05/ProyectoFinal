using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CollectiblesHUD : MonoBehaviour
{
    public static CollectiblesHUD Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform iconsContainer;          // HorizontalLayoutGroup container
    [SerializeField] private CollectibleIconUI iconPrefab;     // prefab with Image + hover handlers

    [Header("Tooltip (TMP)")]
    [SerializeField] private GameObject tooltipRoot;            // panel object
    [SerializeField] private TMP_Text tooltipText;              // TextMeshPro text

    private readonly List<CollectibleIconUI> spawned = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tooltipRoot)
            tooltipRoot.SetActive(false);
    }

    public void AddCollectible(Sprite icon, string name, string description)
    {
        if (!iconsContainer || !iconPrefab)
            return;

        CollectibleIconUI ui = Instantiate(iconPrefab, iconsContainer);
        ui.Setup(icon, name, description, this);
        spawned.Add(ui);
    }

    public void ShowTooltip(string text)
    {
        if (!tooltipRoot || !tooltipText)
            return;

        tooltipText.text = text;
        tooltipRoot.SetActive(true);
    }

    public void HideTooltip()
    {
        if (!tooltipRoot)
            return;

        tooltipRoot.SetActive(false);
    }
}
