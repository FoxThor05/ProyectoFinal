using UnityEngine;

public class SlashAttack : MonoBehaviour
{
    [Header("Hit Settings")]
    public LayerMask enemyLayers;

    private int baseDamage;
    private float critChance;
    private float critMultiplier = 1.5f;

    private bool hasHit = false;
    private Collider2D slashCol;

    // Called by Player when spawned
    public void Initialize(int damage, float critChance, float critMultiplier)
    {
        this.baseDamage = damage;
        this.critChance = critChance;
        this.critMultiplier = critMultiplier;
    }

    void Awake()
    {
        slashCol = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        bool isCrit = Random.value < critChance;
        int damage = isCrit
            ? Mathf.RoundToInt(baseDamage * critMultiplier)
            : baseDamage;

        // Apply damage
        damageable.TakeDamage(damage);

        // Hit point: closest point on the enemy collider from the slash collider center
        Vector3 origin = slashCol ? slashCol.bounds.center : transform.position;
        Vector3 hitPoint = other.ClosestPoint(origin);

        // Slight upward/random offset so it looks nicer
        hitPoint += new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.15f, 0.25f), 0f);

        // Global popup
        if (DamagePopupManager.Instance)
            DamagePopupManager.Instance.Spawn(damage, isCrit, hitPoint);

        hasHit = true;
    }

    // CALLED BY ANIMATION EVENT (last frame)
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
