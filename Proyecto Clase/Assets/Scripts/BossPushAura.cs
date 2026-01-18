using UnityEngine;

[RequireComponent(typeof(CapsuleCollider2D))]
public class BossPushAura : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int contactDamage = 5;
    [SerializeField] private float damageCooldown = 0.45f;

    [Header("Knockback (Deterministic Push-Out)")]
    [Tooltip("How far to push the player away from the boss on contact (world units).")]
    [SerializeField] private float pushDistance = 0.9f;

    [Tooltip("How many small steps to use when pushing (higher = safer around walls).")]
    [Range(1, 12)]
    [SerializeField] private int pushSteps = 6;

    [Tooltip("After pushing out, optionally set a small velocity away from the boss.")]
    [SerializeField] private float postPushVelocity = 6f;

    [Tooltip("How long (seconds) we 'own' the player's velocity to prevent immediate re-stick.")]
    [SerializeField] private float velocityHoldTime = 0.12f;

    [Tooltip("Layers that block push-out (usually your Ground/Platforms layer).")]
    [SerializeField] private LayerMask blockingLayers;

    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    private CapsuleCollider2D col;
    private float nextAllowedHitTime;

    // We keep a tiny per-player timer so we don't keep overwriting velocity forever.
    private float velocityHoldUntil;
    private Rigidbody2D lastRb;
    private Vector2 lastDir;

    void Awake()
    {
        col = GetComponent<CapsuleCollider2D>();
        col.isTrigger = true;
    }

    void FixedUpdate()
    {
        // Maintain a brief velocity hold so the player can't immediately reattach due to controller.
        if (lastRb != null && Time.time < velocityHoldUntil)
        {
            lastRb.linearVelocity = lastDir * postPushVelocity;
        }
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag))
            return;

        if (Time.time < nextAllowedHitTime)
            return;

        // Deal damage
        var player = other.GetComponent<PlayerController>();
        if (player != null)
            player.TakeDamage(contactDamage);

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            Transform bossRoot = transform.parent ? transform.parent : transform;
            Vector2 away = (rb.position - (Vector2)bossRoot.position);

            if (away.sqrMagnitude < 0.0001f)
                away = Vector2.up;

            away.Normalize();

            // Push the player out in small steps (safer near walls)
            Vector2 start = rb.position;
            Vector2 target = start + away * pushDistance;

            Vector2 finalPos = ComputePushOutPosition(other, start, target);

            // MovePosition is physics-friendly
            rb.MovePosition(finalPos);

            // Set a short “held” velocity so it feels like knockback even if controller fights it.
            lastRb = rb;
            lastDir = away;
            velocityHoldUntil = Time.time + Mathf.Max(0.01f, velocityHoldTime);
        }

        nextAllowedHitTime = Time.time + Mathf.Max(0.05f, damageCooldown);
    }

    Vector2 ComputePushOutPosition(Collider2D playerCol, Vector2 start, Vector2 target)
    {
        Vector2 delta = (target - start);
        if (delta.sqrMagnitude < 0.000001f)
            return start;

        int steps = Mathf.Max(1, pushSteps);
        Vector2 step = delta / steps;

        Vector2 pos = start;

        // We do a capsule overlap check at each step.
        // Use the player's collider bounds as a proxy; this isn't perfect but works well in practice.
        // If you want perfect, we can do ColliderDistance2D / Cast with the exact collider shape.
        for (int i = 0; i < steps; i++)
        {
            Vector2 next = pos + step;

            // If blocked, stop early
            if (IsBlocked(playerCol, next))
                break;

            pos = next;
        }

        return pos;
    }

    bool IsBlocked(Collider2D playerCol, Vector2 proposedCenter)
    {
        // Approximate with an OverlapBox using the player's bounds size.
        // This tends to be reliable enough for push-out. If your player collider is not box-ish,
        // tell me which collider type it is and I’ll adapt this to OverlapCapsule, etc.
        Vector2 size = playerCol.bounds.size;
        float angle = 0f;

        Collider2D hit = Physics2D.OverlapBox(proposedCenter, size * 0.95f, angle, blockingLayers);
        return hit != null;
    }

    void OnDrawGizmosSelected()
    {
        // Optional visualization; no-op if not playing
    }
}
