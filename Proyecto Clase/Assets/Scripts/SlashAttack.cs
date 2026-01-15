using UnityEngine;

public class SlashAttack : MonoBehaviour
{
    [Header("Hit Settings")]
    public LayerMask enemyLayers;

    private int baseDamage;
    private float critChance;
    private int critDamage;

    private GameObject normalPopup;
    private GameObject critPopup;

    private bool hasHit = false;

    // Called by Player when spawned
    public void Initialize(
        int damage,
        float critChance,
        int critDamage,
        GameObject normalPopup,
        GameObject critPopup
    )
    {
        this.baseDamage = damage;
        this.critChance = critChance;
        this.critDamage = critDamage;
        this.normalPopup = normalPopup;
        this.critPopup = critPopup;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Prevent multi-hitting the same enemy
        if (hasHit) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;
        Debug.Log("Slash hit: " + other.name);

        bool isCrit = Random.value < critChance;
        int damage = isCrit ? critDamage : baseDamage;

        damageable.TakeDamage(damage);

        // Popup
        GameObject popup = isCrit ? critPopup : normalPopup;
        if (popup)
        {
            Vector3 offset = new Vector3(
                Random.Range(-0.2f, 0.2f),
                Random.Range(0.4f, 0.6f),
                0f
            );

            Instantiate(popup, other.transform.position + offset, Quaternion.identity);
        }

        hasHit = true;
    }

    // CALLED BY ANIMATION EVENT (last frame)
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
