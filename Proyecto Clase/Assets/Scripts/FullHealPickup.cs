using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FullHealPickup : MonoBehaviour
{
    [Tooltip("If true, disable object after pickup (safer if you want pooling). Otherwise destroy.")]
    [SerializeField] private bool disableInsteadOfDestroy = false;

    void Reset()
    {
        // Make sure collider is trigger
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    void Awake()
    {
        // Ensure trigger
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<PlayerController>();
        if (!player)
            player = other.GetComponentInParent<PlayerController>();

        if (!player)
            return;

        player.HealToFull();

        if (disableInsteadOfDestroy)
        {
            gameObject.SetActive(false);
        }
        else
        {
                Destroy(gameObject);
        }
    }
}
