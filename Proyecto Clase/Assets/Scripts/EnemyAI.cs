using UnityEngine;

public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("Detection (same for all difficulties)")]
    public float detectionRadius = 8f;
    public float preferredDistance = 3f;
    public float tooCloseDistance = 1.5f;

    [Header("Difficulty")]
    [SerializeField] private Difficulty difficultyOverride = Difficulty.Normal;
    [SerializeField] private bool useGameSettingsDifficulty = true;

    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Nightmare = 3
    }

    [System.Serializable]
    public class EnemyDifficultyTuning
    {
        [Header("Spawn")]
        public bool enabled = true;

        [Header("Health")]
        public int maxHealth = 50;

        [Header("Movement")]
        public float chaseSpeed = 2f;
        public float retreatSpeed = 1f;
        public float stopSmoothing = 5f;

        [Header("Attack")]
        public float fireRate = 1.2f;
        public float projectileSpeed = 6f;
        public int projectileDamage = 10;
    }

    [Header("Difficulty Presets")]
    public EnemyDifficultyTuning easy = new EnemyDifficultyTuning();
    public EnemyDifficultyTuning normal = new EnemyDifficultyTuning();
    public EnemyDifficultyTuning hard = new EnemyDifficultyTuning();
    public EnemyDifficultyTuning nightmare = new EnemyDifficultyTuning();

    Difficulty CurrentDifficulty =>
        useGameSettingsDifficulty && GameManager.Instance
            ? (Difficulty)GameManager.Instance.Settings.difficulty
            : difficultyOverride;

    EnemyDifficultyTuning Tuning
    {
        get
        {
            switch (CurrentDifficulty)
            {
                case Difficulty.Easy: return easy;
                case Difficulty.Hard: return hard;
                case Difficulty.Nightmare: return nightmare;
                default: return normal;
            }
        }
    }

    [Header("Projectile Prefab")]
    public GameObject projectilePrefab;

    private int currentHealth;
    private Rigidbody2D rb;
    private PlayerController player;
    private float fireTimer;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    private SpriteRenderer spriteRenderer;
    private Coroutine flashRoutine;

    void Awake()
    {
        // If this enemy type should not exist on this difficulty, remove it immediately.
        if (Tuning != null && !Tuning.enabled)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        // Apply difficulty-tuned physics settings
        rb.linearDamping = Tuning.stopSmoothing;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentHealth = Tuning.maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
            player = playerObj.GetComponent<PlayerController>();

        fireTimer = 0f;
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
        AchievementManager.Instance?.Unlock("1");
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
            rb.linearVelocity = dirToPlayer * Tuning.chaseSpeed;
        else if (dist < tooCloseDistance)
            rb.linearVelocity = -dirToPlayer * Tuning.retreatSpeed;
        else
            SmoothStop();

        fireTimer -= Time.fixedDeltaTime;
        if (fireTimer <= 0f)
        {
            Shoot(dirToPlayer);
            fireTimer = Tuning.fireRate;
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
        // We keep the "stop smoothing" concept but tune it per difficulty.
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Tuning.stopSmoothing * Time.fixedDeltaTime);
    }

    void Shoot(Vector2 dir)
    {
        if (!projectilePrefab) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.Euler(0f, 0f, angle));

        Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
        if (prb)
            prb.linearVelocity = dir * Tuning.projectileSpeed;

        EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
        if (ep)
            ep.damage = Tuning.projectileDamage;
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
