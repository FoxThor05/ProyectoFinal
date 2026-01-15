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

    private Animator anim;
    private BossAttack? lastAttack = null;
    private bool isAttacking = false;
    private bool phase50Triggered = false;
    private bool phase25Triggered = false;

    void Start()
    {
        anim = GetComponent<Animator>();
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

        if (currentHealth <= maxHealth * 0.25f)
            pool.Add(BossAttack.DualAttack);

        if (lastAttack.HasValue)
            pool.Remove(lastAttack.Value);

        return pool[Random.Range(0, pool.Count)];
    }

    IEnumerator Attack_CrossSpin()
    {
        anim.SetTrigger("Attack");

        float angleStep = 7.5f;
        float delay = currentHealth <= maxHealth * 0.25f ? 0.25f : 0.4f;
        float speed = currentHealth <= maxHealth * 0.25f ? 7f : 6f;

        for (float offset = 0; offset <= 180; offset += angleStep)
        {
            FireAtAngle(0 + offset, speed);
            FireAtAngle(90 + offset, speed);
            FireAtAngle(180 + offset, speed);
            FireAtAngle(270 + offset, speed);

            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator Attack_CircleWave(bool random)
    {
        anim.SetTrigger("Attack");

        int waves = 3;
        float delay = currentHealth <= maxHealth * 0.25f ? 0.75f : 1f;
        float speed = random ? 7f : 6f;

        float baseOffset = 0f;

        for (int w = 0; w < waves; w++)
        {
            float offset = random ? Random.Range(0f, 360f) : baseOffset;

            if (!random)
            {
                for (int angle = 0; angle < 360; angle += 15)
                {
                    FireAtAngle(angle + offset, speed);
                }
            }
            else
            {
                int projectileCount = 24; // same density as 360 / 15

                for (int i = 0; i < projectileCount; i++)
                {
                    float angle = Random.Range(0f, 360f);
                    FireAtAngle(angle, speed);
                }

            }
            baseOffset += 5f;
            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator Attack_Homing()
    {
        anim.SetTrigger("Attack4");

        int count = currentHealth <= maxHealth * 0.25f ? 3 : 2;

        for (int i = 0; i < count; i++)
        {
            Instantiate(homingProjectilePrefab, firePoint.position, Quaternion.identity);
            yield return new WaitForSeconds(0.3f);
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


