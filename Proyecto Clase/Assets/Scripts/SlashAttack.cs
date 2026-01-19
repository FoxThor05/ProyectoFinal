using System.Collections.Generic;
using UnityEngine;

public class SlashAttack : MonoBehaviour
{
    [Header("Hit Settings")]
    public LayerMask enemyLayers;

    private int baseDamage;
    private float critChance;
    private float critMultiplier = 1.5f;

    private Collider2D slashCol;

    // Track which targets have already been hit by this slash instance
    private readonly HashSet<int> hitTargets = new HashSet<int>();

    // Achievement ID for "hit two enemies with one slash"
    private const string COMBO_ACHIEVEMENT_ID = "3";

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
        // Filter by layer (recommended)
        if (((1 << other.gameObject.layer) & enemyLayers) == 0)
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            return;

        int targetId = other.GetInstanceID();

        // Prevent hitting the same enemy multiple times with the same slash
        if (hitTargets.Contains(targetId))
            return;

        hitTargets.Add(targetId);

        // ---- Achievement: hit two enemies with one slash ----
        if (hitTargets.Count == 2)
        {
            if (AchievementManager.Instance != null &&
                !AchievementManager.Instance.IsUnlocked(COMBO_ACHIEVEMENT_ID))
            {
                AchievementManager.Instance.Unlock(COMBO_ACHIEVEMENT_ID);
            }
        }
        // -----------------------------------------------------

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
        hitPoint += new Vector3(
            Random.Range(-0.1f, 0.1f),
            Random.Range(0.15f, 0.25f),
            0f
        );

        // Global popup
        if (DamagePopupManager.Instance)
            DamagePopupManager.Instance.Spawn(damage, isCrit, hitPoint);
    }

    // CALLED BY ANIMATION EVENT (last frame)
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
