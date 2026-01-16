using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAI : MonoBehaviour, IDamageable
{
    public enum BossAttack
    {
        CrossSpin,      // Attack 1
        CircleWave,     // Attack 2
        RandomCircle,   // Attack 3
        HomingShots,    // Attack 4
        DualAttack      // Attack 5
    }
    [Header("Difficulty")]
    [SerializeField] private Difficulty difficultyOverride = Difficulty.Normal;
    [SerializeField] private bool useGameSettingsDifficulty = true;

    [Header("Normal Difficulty")]
    [Range(0f, 1f)]
    public float dualAttackChance = 0.25f;

    [Header("Arena Detection")]
    public float activationRadius = 12f;
    public bool playerInArena = false;
    public BossHealthBar bossHealthBar;

    [Header("Boss Stats")]
    public int maxHealth = 1000;
    public int currentHealth;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public GameObject homingProjectilePrefab;
    public float baseProjectileSpeed = 8f;

    [Header("Attack Timing")]
    public float attackCooldown = 3f;
    public float lowHpCooldown = 1.5f;

    [Header("References")]
    public Transform firePoint;
    public Transform player;
    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Nightmare = 3
    }

    private Animator anim;
    private BossAttack? lastAttack = null;
    private bool isAttacking = false;
    private bool phase50Triggered = false;
    private bool phase25Triggered = false;
    [System.Serializable]
    public class BossDifficultyTuning
    {
        [Header("Health")]
        public float maxHpMultiplier = 1f;

        [Header("Attacks Enabled")]
        public bool allowDualAttack = true;

        [Header("Cross Spin")]
        public float crossSpinAngleStep = 7.5f;
        public bool randomSpinDirection = false;

        [Header("Circle Wave")]
        public int randomCircleProjectiles = 24;
        public int randomCircleWaves = 3;
        public int randomCircleWavesLowHP = 3;

        [Header("Homing Shots")]
        public int homingShotsNormal = 2;
        public int homingShotsLowHP = 3;
    }
    [Header("Difficulty Presets")]
    public BossDifficultyTuning easy;
    public BossDifficultyTuning normal;
    public BossDifficultyTuning hard;
    public BossDifficultyTuning nightmare;
    Difficulty CurrentDifficulty =>
        useGameSettingsDifficulty && GameManager.Instance
            ? (Difficulty)GameManager.Instance.Settings.difficulty
            : difficultyOverride;

    BossDifficultyTuning Tuning
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

    void Start()
    {
        anim = GetComponent<Animator>();
        maxHealth = Mathf.RoundToInt(maxHealth * Tuning.maxHpMultiplier);
        currentHealth = maxHealth;

        if (!player)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        StartCoroutine(BossLoop());
    }

    bool IsPlayerInArena()
    {
        if (!player) return false;
        return Vector2.Distance(transform.position, player.position) <= activationRadius;
    }

    // ---------------- DAMAGE ----------------
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (!phase50Triggered && currentHealth <= maxHealth * 0.5f)
        {
            phase50Triggered = true;
            anim.SetTrigger("Rage");
        }

        if (!phase25Triggered && currentHealth <= maxHealth * 0.25f)
        {
            phase25Triggered = true;
            anim.SetTrigger("Rage");
        }

        if (currentHealth <= 0)
            anim.SetTrigger("Defeat");
    }

    // CALLED BY DEFEAT ANIMATION EVENT
    public void DestroyBoss()
    {
        AchievementManager.Instance.Unlock("1");
        Destroy(gameObject);

    }

    // ---------------- MAIN LOOP ----------------

    IEnumerator BossLoop()
    {
        yield return new WaitForSeconds(1f); // small intro delay

        while (currentHealth > 0)
        {
            // Wait until player enters arena
            while (!IsPlayerInArena())
            {
                if (playerInArena)
                {
                    playerInArena = false;

                    Debug.Log("Boss arena exited");

                    MusicManager.Instance.PlayNormalMusic();

                    if (bossHealthBar)
                        bossHealthBar.Hide();
                }

                yield return null;
            }

            // Player just entered arena
            if (!playerInArena)
            {
                playerInArena = true;

                Debug.Log("Boss arena entered");

                MusicManager.Instance.PlayBossMusic();

                if (bossHealthBar)
                    bossHealthBar.Show(this);
            }

            yield return StartCoroutine(DoNextAttack());

            float wait = currentHealth <= maxHealth * 0.25f
                ? lowHpCooldown
                : attackCooldown;

            // Wait, but abort if player leaves
            float t = 0f;
            while (t < wait)
            {
                if (!IsPlayerInArena())
                    break;

                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator DoNextAttack()
    {
        isAttacking = true;

        BossAttack next = GetRandomAttack();
        lastAttack = next;

        switch (next)
        {
            case BossAttack.CrossSpin:
                yield return StartCoroutine(Attack_CrossSpin());
                break;

            case BossAttack.CircleWave:
                yield return StartCoroutine(Attack_CircleWave(false));
                break;

            case BossAttack.RandomCircle:
                yield return StartCoroutine(Attack_CircleWave(true));
                break;

            case BossAttack.HomingShots:
                yield return StartCoroutine(Attack_Homing());
                break;

            case BossAttack.DualAttack:
                yield return StartCoroutine(Attack_Dual());
                break;
        }

        isAttacking = false;
    }

    BossAttack GetRandomAttack()
    {
        List<BossAttack> pool = new List<BossAttack>
    {
        BossAttack.CrossSpin,
        BossAttack.CircleWave,
        BossAttack.RandomCircle
    };

        if (currentHealth <= maxHealth * 0.5f)
            pool.Add(BossAttack.HomingShots);

        if (Tuning.allowDualAttack &&
            CurrentDifficulty == Difficulty.Normal &&
            currentHealth <= maxHealth * 0.25f &&
            Random.value <= dualAttackChance)
        {
            pool.Add(BossAttack.DualAttack);
        }

        if (lastAttack.HasValue)
            pool.Remove(lastAttack.Value);

        return pool[Random.Range(0, pool.Count)];
    }

    IEnumerator Attack_CrossSpin()
    {
        anim.SetTrigger("Attack");

        float delay = currentHealth <= maxHealth * 0.25f ? 0.25f : 0.4f;
        float speed = currentHealth <= maxHealth * 0.25f ? 7f : 6f;
        float angleStep = Tuning.crossSpinAngleStep;
        int dir = 1;

        if (Tuning.randomSpinDirection)
            dir = Random.value > 0.5f ? 1 : -1;

        for (float offset = 0; offset <= 180; offset += angleStep)
        {
            float o = offset * dir;

            FireAtAngle(0 + o, speed);
            FireAtAngle(90 + o, speed);
            FireAtAngle(180 + o, speed);
            FireAtAngle(270 + o, speed);

            // Nightmare under 25% HP = add rotated set
            if (CurrentDifficulty == Difficulty.Nightmare &&
                currentHealth <= maxHealth * 0.25f)
            {
                FireAtAngle(45 + o, speed);
                FireAtAngle(135 + o, speed);
                FireAtAngle(225 + o, speed);
                FireAtAngle(315 + o, speed);
            }

            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator Attack_CircleWave(bool random)
    {
        anim.SetTrigger("Attack");

        int waves = 3;
        if (random)
        {
            waves = Tuning.randomCircleWaves;
            if (CurrentDifficulty == Difficulty.Nightmare &&
                currentHealth <= maxHealth * 0.25f)
            {
                waves = Tuning.randomCircleWavesLowHP;
            }
        }

        float delay = currentHealth <= maxHealth * 0.25f ? 0.75f : 1f;
        float speed = random ? 7f : 6f;

        float baseOffset = 0f;
        List<BoomerangProjectile> spawnedBooms = new List<BoomerangProjectile>();
        bool attackEnded = false;

        for (int w = 0; w < waves; w++)
        {
            float offset = random ? Random.Range(0f, 360f) : baseOffset;

            if (!random)
            {
                for (int angle = 0; angle < 360; angle += 15)
                {
                    float finalAngle = angle + offset;

                    if (CurrentDifficulty == Difficulty.Nightmare)
                    {
                        GameObject proj = Instantiate(
                            projectilePrefab,
                            firePoint.position,
                            Quaternion.Euler(0f, 0f, finalAngle)
                        );

                        BoomerangProjectile boom = proj.AddComponent<BoomerangProjectile>();
                        boom.speed = speed;
                        boom.returnSpeed = currentHealth <= maxHealth * 0.25f ? speed * 1.75f : speed * 1.25f;
                        boom.boss = transform;
                        boom.delayBeforeReturn = 0.5f;

                        spawnedBooms.Add(boom);
                    }
                    else
                    {
                        FireAtAngle(finalAngle, speed);
                    }
                }
            }
            else
            {
                int projectileCount = Tuning.randomCircleProjectiles;
                for (int i = 0; i < projectileCount; i++)
                {
                    float angle = Random.Range(0f, 360f);
                    FireAtAngle(angle, speed);
                }
            }

            baseOffset += 5f;
            yield return new WaitForSeconds(delay);
        }

        // ----------------
        // Nightmare: end attack as soon as any boomerang starts returning
        // ----------------
        if (CurrentDifficulty == Difficulty.Nightmare && spawnedBooms.Count > 0)
        {
            yield return new WaitUntil(() => spawnedBooms.Exists(b => b.startedReturn));
        }
    }

    IEnumerator Attack_Homing()
    {
        anim.SetTrigger("Attack4");

        int count =
        currentHealth <= maxHealth * 0.25f
        ? Tuning.homingShotsLowHP
        : Tuning.homingShotsNormal;

        Vector2[] dirs = count == 4
            ? new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right }
            : new[] { Vector2.left, Vector2.right };

        foreach (var d in dirs)
        {
            var proj = Instantiate(homingProjectilePrefab, firePoint.position, Quaternion.identity);
            proj.transform.right = d;
            yield return new WaitForSeconds(0.25f);
        }

    }

    IEnumerator Attack_Dual()
    {
        anim.SetTrigger("Attack");

        BossAttack a = GetRandomAttack();
        BossAttack b;

        do
        {
            b = GetRandomAttack();
        }
        while (b == a || b == BossAttack.HomingShots);

        StartCoroutine(ExecuteAttack(a));
        StartCoroutine(ExecuteAttack(b));

        yield return new WaitForSeconds(3f);
    }

    IEnumerator ExecuteAttack(BossAttack atk)
    {
        switch (atk)
        {
            case BossAttack.CrossSpin:
                yield return StartCoroutine(Attack_CrossSpin());
                break;
            case BossAttack.CircleWave:
                yield return StartCoroutine(Attack_CircleWave(false));
                break;
            case BossAttack.RandomCircle:
                yield return StartCoroutine(Attack_CircleWave(true));
                break;
        }
    }

    void FireAtAngle(float angle, float speed)
    {
        Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;

        float zRot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject proj = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.Euler(0f, 0f, zRot)
        );

        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb)
            rb.linearVelocity = dir.normalized * speed;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}


