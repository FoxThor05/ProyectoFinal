using System.Threading.Tasks;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Variable Jump Settings")]
    public float maxJumpHoldTime = 0.2f;
    public float jumpHoldForce = 20f;

    [Header("Friction Handling")]
    public PhysicsMaterial2D normalMaterial;
    public PhysicsMaterial2D noFrictionMaterial;

    private Collider2D col;

    [Header("Player Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int attackDamage = 10;

    [Header("Dash Settings")]
    public bool hasDashItem = false;
    public float dashForce = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;
    public GameObject dashTrailPrefab;

    private bool isJumping;
    private float jumpHoldTimer;

    private bool isDashing = false;
    private float dashCooldownTimer = 0f;

    [Header("Parry Settings")]
    public GameObject parryShieldPrefab;
    public float parryWindow = 0.2f;
    public float parryRadius = 3f;
    public float parrySuccessCooldown = 5f;
    public float parryFailCooldown = 7f;
    public Color parryFlashColor = Color.yellow;
    private bool isParryActive = false;
    private bool parrySuccessful = false;
    private float parryCooldownTimer = 0f;

    private GameObject activeParryShield;

    [Header("Parry UI")]
    public UnityEngine.UI.Image parryCooldownFillImage;
    public Color parryReadyColor = Color.white;
    public Color parryCooldownColor = Color.gray;

    private float currentParryCooldownDuration;

    // NEW: parry streak tracking for achievement 4
    private int parryStreak = 0;

    [Header("Dash Trail Settings")]
    public float dashTrailSpacing = 0.1f;
    public float dashTrailOffset = 0.3f;

    [Header("Dash Charge Settings")]
    public int maxDashCharges = 2;
    public int currentDashCharges = 2;

    public GameObject firstDashTrailPrefab;
    public GameObject secondDashTrailPrefab;
    public GameObject middleDashTrailPrefab;

    [Header("Slash Attack")]
    public GameObject slashPrefab;
    public Transform slashSpawnPoint;

    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public float attackCooldown = 0.3f;
    public LayerMask enemyLayers;
    public float downAttackBounceForce = 10f;

    [Header("Critical Hit")]
    [Range(0f, 1f)]
    public float critChance = 0.10f;
    public int critDamage = 15;
    public GameObject normalDamagePopupPrefab;
    public GameObject critDamagePopupPrefab;

    private float nextAttackTime = 0f;
    private bool jumpTriggered = false;

    private Animator anim;
    private Rigidbody2D rb;
    private bool isGrounded;
    private float inputX;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;
        currentHealth = maxHealth;

        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        HandleParry();

        Move();
        HandleJump();
        CheckGround();
        HandleAttackInput();
        HandleDash();
        UpdateParryUI();

        UpdateAnimations();
    }

    void HandleParry()
    {
        if (parryCooldownTimer > 0f)
        {
            parryCooldownTimer -= Time.deltaTime;
            return;
        }

        if (Input.GetKeyDown(GameManager.Instance.Settings.fire2Key))
        {
            StartCoroutine(ParryRoutine());
        }
    }

    System.Collections.IEnumerator ParryRoutine()
    {
        isParryActive = true;
        parrySuccessful = false;

        // Spawn shield
        activeParryShield = Instantiate(
            parryShieldPrefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        float timer = 0f;

        while (timer < parryWindow)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isParryActive = false;

        // Cleanup shield
        if (activeParryShield)
            Destroy(activeParryShield);

        // If parry window ended with no success, reset streak (achievement 4 requirement)
        if (!parrySuccessful)
            parryStreak = 0;

        // Apply cooldown
        currentParryCooldownDuration = parrySuccessful
            ? parrySuccessCooldown
            : parryFailCooldown;

        parryCooldownTimer = currentParryCooldownDuration;
    }

    void UpdateParryUI()
    {
        if (!parryCooldownFillImage) return;

        parryCooldownFillImage.transform.parent
            .GetComponent<UnityEngine.UI.Image>().color =
            parryCooldownTimer > 0f ? parryCooldownColor : parryReadyColor;

        if (parryCooldownTimer <= 0f)
        {
            parryCooldownFillImage.fillAmount = 1f;
        }
        else
        {
            parryCooldownFillImage.fillAmount =
                1f - (parryCooldownTimer / currentParryCooldownDuration);
        }
    }

    void UpdateAnimations()
    {
        if (isGrounded)
        {
            float speed = inputX;
            if (speed < 0f)
                speed *= -1f;

            anim.SetFloat("Speed", speed);
        }
        else
        {
            anim.SetFloat("Speed", 0f);
        }

        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.1f)
            {
                if (!jumpTriggered)
                {
                    anim.SetTrigger("Jump");
                    jumpTriggered = true;
                }
                anim.SetBool("isFalling", false);
            }
            else if (rb.linearVelocity.y < -0.1f)
            {
                anim.SetBool("isFalling", true);
            }
        }
        else
        {
            if (anim.GetBool("isFalling"))
            {
                anim.SetBool("isFalling", false);
                anim.SetTrigger("Land");
            }

            jumpTriggered = false;
        }
    }

    System.Collections.IEnumerator PerformDash(Vector2 direction)
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        currentDashCharges--;

        GameObject edgeTrailPrefab = (currentDashCharges == 1) ? firstDashTrailPrefab : secondDashTrailPrefab;
        col.sharedMaterial = noFrictionMaterial;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashTime = 0f;

        Vector2 dashDir = direction.normalized;
        Vector2 perp = new Vector2(-dashDir.y, dashDir.x);

        while (dashTime < dashDuration)
        {
            rb.linearVelocity = dashDir * dashForce;

            if (middleDashTrailPrefab)
            {
                GameObject mid = Instantiate(middleDashTrailPrefab, transform.position, Quaternion.identity);
                Destroy(mid, 1f);
            }

            if (edgeTrailPrefab)
            {
                Vector2 leftPos = (Vector2)transform.position + perp * dashTrailOffset;
                GameObject left = Instantiate(edgeTrailPrefab, leftPos, Quaternion.identity);
                Destroy(left, 1f);
            }

            if (edgeTrailPrefab)
            {
                Vector2 rightPos = (Vector2)transform.position - perp * dashTrailOffset;
                GameObject right = Instantiate(edgeTrailPrefab, rightPos, Quaternion.identity);
                Destroy(right, 1f);
            }

            yield return new WaitForSeconds(dashTrailSpacing);

            dashTime += dashTrailSpacing;
        }

        rb.gravityScale = originalGravity;
        col.sharedMaterial = normalMaterial;
        isDashing = false;
    }

    Vector2 GetDashDirection()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 dir = new Vector2(x, y);

        if (dir.magnitude > 1)
            dir.Normalize();

        return dir;
    }

    void HandleDash()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (!hasDashItem) return;
        if (isDashing) return;
        if (dashCooldownTimer > 0) return;
        if (currentDashCharges <= 0) return;

        if (Input.GetKeyDown(GameManager.Instance.Settings.dashKey))
        {
            Vector2 dashDir = GetDashDirection();
            if (dashDir != Vector2.zero)
                StartCoroutine(PerformDash(dashDir));
        }
    }

    void Move()
    {
        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        if (inputX != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(inputX);
            transform.localScale = scale;
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(GameManager.Instance.Settings.jumpKey) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
            jumpHoldTimer = maxJumpHoldTime;
        }

        // NOTE: your original code used GetKeyDown again here; keeping as-is to avoid changing feel.
        if (Input.GetKeyDown(GameManager.Instance.Settings.jumpKey) && isJumping)
        {
            if (jumpHoldTimer > 0f)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y + jumpHoldForce * Time.deltaTime
                );
                jumpHoldTimer -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        if (Input.GetKeyUp(GameManager.Instance.Settings.jumpKey))
        {
            isJumping = false;

            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            }
        }
    }

    void CheckGround()
    {
        isGrounded = false;

        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckRadius,
            groundLayer
        );

        if (hit.collider != null)
        {
            if (Vector2.Angle(hit.normal, Vector2.up) < 45f)
            {
                isGrounded = true;
                currentDashCharges = maxDashCharges;
                isJumping = false;
            }
        }
    }

    // --------------------
    // ATTACKING
    // --------------------
    void HandleAttackInput()
    {
        if (Time.time < nextAttackTime)
            return;

        if (Input.GetKeyDown(GameManager.Instance.Settings.fire1Key))
        {
            nextAttackTime = Time.time + attackCooldown;
            PerformAttack();
        }
    }

    async Task PerformAttack()
    {
        anim.SetTrigger("Attack");
    }

    // CALLED BY ANIMATION EVENT
    public void SpawnSlash()
    {
        if (!slashPrefab || !slashSpawnPoint) return;

        GameObject slashObj = Instantiate(
            slashPrefab,
            slashSpawnPoint.position,
            Quaternion.identity
        );

        Vector3 scale = slashObj.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(transform.localScale.x);
        slashObj.transform.localScale = scale;

        SlashAttack slash = slashObj.GetComponent<SlashAttack>();
        if (slash)
        {
            slash.Initialize(
                attackDamage,
                critChance,
                critDamage,
                normalDamagePopupPrefab,
                critDamagePopupPrefab
            );
        }
    }

    // --------------------
    // DAMAGE
    // --------------------
    public void TakeDamage(int amount)
    {
        // Successful parry
        if (isParryActive)
        {
            parrySuccessful = true;
            isParryActive = false;

            RegisterParrySuccess();
            ParryEffect();
            return;
        }

        // Normal damage
        currentHealth -= amount;

        // Track "no damage run" (achievement 9)
        if (GameManager.Instance)
            GameManager.Instance.NotifyPlayerTookDamage();

        if (currentHealth <= 0)
            Die();
    }

    void RegisterParrySuccess()
    {
        // Achievement 2: first parry
        AchievementManager.Instance?.Unlock("2");

        // Achievement 4: 10 parries in a row without missing
        parryStreak++;
        if (parryStreak >= 10)
            AchievementManager.Instance?.Unlock("4");
    }

    void ParryEffect()
    {
        if (activeParryShield)
        {
            SpriteRenderer sr = activeParryShield.GetComponent<SpriteRenderer>();
            if (sr)
                sr.color = parryFlashColor;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, parryRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Projectile"))
            {
                Destroy(hit.gameObject);
            }
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        GameManager.Instance.SetState(GameManager.GameState.Dead);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, parryRadius);

        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
