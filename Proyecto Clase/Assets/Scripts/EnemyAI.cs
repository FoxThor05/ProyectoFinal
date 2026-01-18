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

    private int currentHealth;
    private Rigidbody2D rb;
    private PlayerController player;
    private float fireTimer;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    private SpriteRenderer spriteRenderer;
    private Coroutine flashRoutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 2f;

        currentHealth = maxHealth;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
            player = playerObj.GetComponent<PlayerController>();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        FlashDamage();

        if (currentHealth <= 0)
            Die();
    }

    void FlashDamage()
    {
        if (!spriteRenderer) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = flashColor;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = original;
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
            SmoothStop();
            return;
        }

        Vector2 dirToPlayer = (player.transform.position - transform.position).normalized;

        FacePlayer();

        if (dist > preferredDistance)
            rb.linearVelocity = dirToPlayer * chaseSpeed;
        else if (dist < tooCloseDistance)
            rb.linearVelocity = -dirToPlayer * retreatSpeed;
        else
            SmoothStop();

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

        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.Euler(0f, 0f, angle));

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
