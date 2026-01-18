using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private DamagePopupText popupPrefab;

    [Header("Style")]
    [SerializeField] private Color normalColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color normalOutline = Color.black;

    [SerializeField] private Color critColor = new Color(1f, 0.95f, 0.2f, 1f);
    [SerializeField] private Color critOutline = new Color(1f, 0.5f, 0f, 1f);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Spawn(int amount, bool isCrit, Vector3 worldPos)
    {
        if (!popupPrefab) return;

        DamagePopupText popup = Instantiate(popupPrefab, worldPos, Quaternion.identity);

        Color textColor = isCrit ? critColor : normalColor;
        Color outlineColor = isCrit ? critOutline : normalOutline;

        popup.Set(amount, textColor, outlineColor);
    }
}

