using System.Collections.Generic;
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

    // Track unique targets hit by this slash instance
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    // Combo achievement (id "3") should trigger once per slash
    private bool comboTriggered = false;

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
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        // Prevent multi-hitting the same enemy with the same slash
        if (hitTargets.Contains(damageable)) return;

        hitTargets.Add(damageable);

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

        // Combo achievement: hit 2 enemies with one attack (slash instance)
        if (!comboTriggered && hitTargets.Count >= 2)
        {
            comboTriggered = true;
            AchievementManager.Instance?.Unlock("3");
        }
    }

    // CALLED BY ANIMATION EVENT (last frame)
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
