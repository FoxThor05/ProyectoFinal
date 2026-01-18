using UnityEngine;

public class CollectiblePickup : MonoBehaviour
{
    [Header("Collectible Info")]
    public string displayName = "Collectible";
    [TextArea] public string description = "Does something helpful.";
    public Sprite icon;

    [Header("Effect")]
    public CollectibleEffectType effectType;

    [Header("Pickup")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnPickup = true;

    private bool collected = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag(playerTag)) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (!player) return;

        // Apply effect to player
        player.ApplyCollectible(effectType);

        // Add to HUD
        if (CollectiblesHUD.Instance)
            CollectiblesHUD.Instance.AddCollectible(icon, displayName, description);

        collected = true;

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
