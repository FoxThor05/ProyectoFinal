using UnityEngine;

[RequireComponent(typeof(CapsuleCollider2D))]
public class BossPushAura : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int contactDamage = 5;

    [Tooltip("Seconds between damage ticks while staying in the aura.")]
    [SerializeField] private float damageCooldown = 0.45f;

    [Header("Knockback")]
    [Tooltip("Impulse strength applied to the player away from the boss.")]
    [SerializeField] private float knockbackImpulse = 7f;

    [Tooltip("Optional clamp to prevent excessive speed.")]
    [SerializeField] private float maxPlayerSpeedAfterKnockback = 10f;

    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    private CapsuleCollider2D col;
    private float nextAllowedHitTime = 0f;

    void Awake()
    {
        col = GetComponent<CapsuleCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag))
            return;

        if (Time.time < nextAllowedHitTime)
            return;

        // Deal damage
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
        }

        // Knockback (requires Rigidbody2D on player)
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            Transform bossRoot = transform.parent ? transform.parent : transform;
            Vector2 away = (rb.position - (Vector2)bossRoot.position);

            if (away.sqrMagnitude < 0.0001f)
                away = Vector2.up;

            away.Normalize();

            // Small "snap away" impulse
            rb.AddForce(away * knockbackImpulse, ForceMode2D.Impulse);

            // Optional clamp
            if (rb.linearVelocity.magnitude > maxPlayerSpeedAfterKnockback)
                rb.linearVelocity = rb.linearVelocity.normalized * maxPlayerSpeedAfterKnockback;
        }

        nextAllowedHitTime = Time.time + Mathf.Max(0.05f, damageCooldown);
    }
}
