using UnityEngine;

public class GroundEnemyAI : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float detectionRadius = 6f;
    public float attackRange = 1.2f;

    [Header("Attack")]
    public float attackWindup = 0.5f;
    public float attackRadius = 0.8f;
    public int attackDamage = 10;
    public Transform attackPoint;
    public LayerMask playerLayer;
    public int maxHealth = 50;

    [Header("Ground / Wall")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private PlayerController player;

    private int currentHealth;
    private bool facingRight = true;
    private bool isCharging;
    private bool isAttacking;
    private bool isHurt;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
            player = playerObj.GetComponent<PlayerController>();
    }

    void FixedUpdate()
    {
        if (!player) return;

        // Lock enemy during hurt / charge / attack
        if (isHurt || isCharging || isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsMoving", false);
            return;
        }

        float dist = Vector2.Distance(transform.position, player.transform.position);

        if (dist <= attackRange)
        {
            StartCharge();
        }
        else if (dist <= detectionRadius)
        {
            Chase();
        }
        else
        {
            Wander();
        }
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
        Destroy(gameObject);
    }

    // ---------------- MOVEMENT ----------------

    void Wander()
    {
        anim.SetBool("IsMoving", true);
        Move(facingRight ? 1 : -1);

        if (HitWall() || AtEdge())
            Flip();
    }

    void Chase()
    {
        anim.SetBool("IsMoving", true);

        float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        Move(dir);

        if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
            Flip();
    }

    void Move(float dir)
    {
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    bool HitWall()
    {
        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        return Physics2D.Raycast(transform.position, dir, 0.4f, groundLayer);
    }

    bool AtEdge()
    {
        return !Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );
    }

    // ---------------- ATTACK ----------------

    void StartCharge()
    {
        if (isCharging || isAttacking) return;

        isCharging = true;
        anim.SetBool("IsCharging", true);
        anim.SetBool("IsMoving", false);

        rb.linearVelocity = Vector2.zero;
        Invoke(nameof(PerformAttack), attackWindup);
    }

    void PerformAttack()
    {
        isCharging = false;
        isAttacking = true;

        anim.SetBool("IsCharging", false);
        anim.SetTrigger("AttackTrigger");
        Invoke(nameof(EndAttack), 0.1f); // match your attack animation length
    }

    public void SpawnAttackHitbox()
    {
        Collider2D hit = Physics2D.OverlapCircle(
        attackPoint.position,
        attackRadius,
        playerLayer
        );

        if (hit)
        {
            PlayerController pc = hit.GetComponent<PlayerController>();
            if (pc)
            pc.TakeDamage(attackDamage);
        }
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    // ---------------- DEBUG ----------------

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (attackPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
