using UnityEngine;

public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("Detection")]
    public float detectionRadius = 8f;
    public float preferredDistance = 3f;
    public float tooCloseDistance = 1.5f;

    [Header("Movement")]
    public float chaseSpeed = 2f;
    public float retreatSpeed = 1f;
    public float stopSmoothing = 5f;

    [Header("Attack")]
    public GameObject projectilePrefab;
    public float fireRate = 1.2f;
    public float projectileSpeed = 6f;
    public int projectileDamage = 10;
    public int maxHealth = 50;
    public GameObject damagePopupPrefab;

    private int currentHealth;
    private Rigidbody2D rb;
    private PlayerController player;
    private float fireTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;          // float
        rb.linearDamping = 2f;                 // smooth movement
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
            player = playerObj.GetComponent<PlayerController>();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Enemy died!");
        AchievementManager.Instance.Unlock("1");
        Destroy(gameObject);
    }
    void FixedUpdate()
    {
        if (!player) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);

        if (dist > detectionRadius)
        {
            // Player too far → idle float
            SmoothStop();
            return;
        }

        Vector2 dirToPlayer = (player.transform.position - transform.position).normalized;

        // ---------------- MOVEMENT ----------------
        FacePlayer();
        if (dist > preferredDistance)
        {
            // Chase
            rb.linearVelocity = dirToPlayer * chaseSpeed;
        }
        else if (dist < tooCloseDistance)
        {
            // Retreat (slower)
            rb.linearVelocity = -dirToPlayer * retreatSpeed;
        }
        else
        {
            // Maintain distance
            SmoothStop();
        }

        // ---------------- ATTACK ----------------

        fireTimer -= Time.fixedDeltaTime;
        if (fireTimer <= 0f)
        {
            Shoot(dirToPlayer);
            fireTimer = fireRate;
        }
    }

    void FacePlayer()
    {
        Vector2 dir = player.transform.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // If your sprite faces RIGHT by default
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void SmoothStop()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, stopSmoothing * Time.fixedDeltaTime);
    }

    void Shoot(Vector2 dir)
    {
        if (!projectilePrefab) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject proj = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.Euler(0f, 0f, angle)
        );

        Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
        if (prb)
            prb.linearVelocity = dir * projectileSpeed;

        EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
        if (ep)
            ep.damage = projectileDamage;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);
    }
}
