using System.Threading.Tasks;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
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

    private bool isDashing = false;
    private float dashCooldownTimer = 0f;

    [Header("Dash Trail Settings")]
    public float dashTrailSpacing = 0.1f;
    public float dashTrailOffset = 0.3f;

    [Header("Dash Charge Settings")]
    public int maxDashCharges = 2;
    public int currentDashCharges = 2;

    // Existing prefabs in your project:
    public GameObject firstDashTrailPrefab;   // (blue)
    public GameObject secondDashTrailPrefab;  // (pink)
    public GameObject middleDashTrailPrefab;  // (white)

    [Header("Dash Refill Tuning")]
    [Tooltip("After dash ends, wait this long before allowing dash refills due to ground contact.")]
    [SerializeField] private float dashEndRefillDelay = 0.08f;

    [Header("Grounding (Robust Contact Based)")]
    [Tooltip("How 'upward' a contact normal must be to count as ground.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float groundNormalMinY = 0.6f;

    [Tooltip("Extra tolerance above collider bottom for contact points.")]
    [SerializeField] private float groundPointTolerance = 0.08f;

    private float dashRefillAllowedAtTime = 0f;

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

    [Header("Slash Attack")]
    public GameObject slashPrefab;
    public Transform slashSpawnPoint;

    [Header("Attack Settings")]
    public float attackCooldown = 0.3f;

    [Header("Critical Hit")]
    [Range(0f, 1f)]
    public float critChance = 0.10f;

    [Tooltip("Critical hit multiplier. 1.5 = +50% damage.")]
    public float critMultiplier = 1.5f;

    private float nextAttackTime = 0f;

    private Animator anim;
    private Rigidbody2D rb;

    private float inputX;

    // Jump
    private bool isJumping;
    private float jumpHoldTimer;
    private bool isGrounded;
    private bool jumpTriggered = false;

    // Damage flash
    [Header("Damage Flash")]
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    private SpriteRenderer spriteRenderer;
    private Coroutine flashRoutine;

    // Reusable contact arrays (avoids allocations)
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
    private ContactFilter2D groundFilter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;

        currentHealth = maxHealth;
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        groundFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = groundLayer,
            useTriggers = false
        };
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");

        HandleParry();

        if (!isDashing)
            Move();

        HandleJump();
        HandleAttackInput();
        HandleDash();
        UpdateParryUI();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        UpdateGroundedState();

        // Refill ONLY when grounded, not dashing, and after dash refill lock
        if (isGrounded && !isDashing && Time.time >= dashRefillAllowedAtTime)
        {
            if (currentDashCharges < maxDashCharges)
                currentDashCharges = maxDashCharges;

            isJumping = false;
        }
    }

    // ---------------- Grounding (ROBUST) ----------------
    void UpdateGroundedState()
    {
        isGrounded = false;
        if (!rb || !col) return;

        int count = rb.GetContacts(groundFilter, contacts);
        if (count <= 0) return;

        float bottomY = col.bounds.min.y + groundPointTolerance;

        for (int i = 0; i < count; i++)
        {
            var c = contacts[i];

            // Must be a "floor-like" contact
            if (c.normal.y < groundNormalMinY)
                continue;

            // Contact point should be near/below the bottom of collider
            if (c.point.y > bottomY)
                continue;

            isGrounded = true;
            return;
        }
    }

    // ---------------- Collectibles ----------------
    public void ApplyCollectible(CollectibleEffectType type)
    {
        switch (type)
        {
            case CollectibleEffectType.DamagePlus5:
                attackDamage += 5;
                break;

            case CollectibleEffectType.ExtraDashCharge:
                maxDashCharges += 1;
                currentDashCharges = maxDashCharges;
                hasDashItem = true;
                break;

            case CollectibleEffectType.ParryCooldownMinus1:
                parrySuccessCooldown = Mathf.Max(0.5f, parrySuccessCooldown - 1f);
                parryFailCooldown = Mathf.Max(0.5f, parryFailCooldown - 1f);
                break;

            case CollectibleEffectType.MoveAndJumpBoost:
                moveSpeed += 0.6f;
                jumpForce += 1.0f;
                break;
        }
    }

    // ---------------- Parry ----------------
    void HandleParry()
    {
        if (parryCooldownTimer > 0f)
        {
            parryCooldownTimer -= Time.deltaTime;
            return;
        }

        if (Input.GetKeyDown(GameManager.Instance.Settings.fire2Key))
            StartCoroutine(ParryRoutine());
    }

    System.Collections.IEnumerator ParryRoutine()
    {
        isParryActive = true;
        parrySuccessful = false;

        activeParryShield = Instantiate(parryShieldPrefab, transform.position, Quaternion.identity, transform);

        float timer = 0f;
        while (timer < parryWindow)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isParryActive = false;

        if (activeParryShield)
            Destroy(activeParryShield);

        currentParryCooldownDuration = parrySuccessful ? parrySuccessCooldown : parryFailCooldown;
        parryCooldownTimer = currentParryCooldownDuration;
    }

    void UpdateParryUI()
    {
        if (!parryCooldownFillImage) return;

        parryCooldownFillImage.transform.parent
            .GetComponent<UnityEngine.UI.Image>().color =
            parryCooldownTimer > 0f ? parryCooldownColor : parryReadyColor;

        if (parryCooldownTimer <= 0f)
            parryCooldownFillImage.fillAmount = 1f;
        else
            parryCooldownFillImage.fillAmount = 1f - (parryCooldownTimer / currentParryCooldownDuration);
    }

    // ---------------- Movement ----------------
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

        if (Input.GetKeyDown(GameManager.Instance.Settings.jumpKey) && isJumping)
        {
            if (jumpHoldTimer > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + jumpHoldForce * Time.deltaTime);
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
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    // ---------------- Dash ----------------
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

    Vector2 GetDashDirection()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(x, y);
        if (dir.magnitude > 1) dir.Normalize();
        return dir;
    }

    System.Collections.IEnumerator PerformDash(Vector2 direction)
    {
        isDashing = true;

        // Spend a charge immediately and deterministically
        currentDashCharges = Mathf.Max(0, currentDashCharges - 1);

        dashCooldownTimer = dashCooldown;

        // Lock refills until after dash ends + delay (prevents "free dash")
        dashRefillAllowedAtTime = Time.time + dashDuration + dashEndRefillDelay;

        // Decide trail color for this dash:
        // - If maxDashCharges >= 3, the EXTRA dash (third) is fully white.
        // We define "third dash" as: when you started the dash with 1 charge remaining after spending.
        // Example: start at 3 -> after spending: 2 (dash #1), then 1 (dash #2), then 0 (dash #3 / extra)
        bool thisIsExtraWhiteDash = (maxDashCharges >= 3 && currentDashCharges == 0);

        GameObject edgeTrailPrefab = thisIsExtraWhiteDash ? middleDashTrailPrefab : GetDefaultEdgeTrailPrefab();

        col.sharedMaterial = noFrictionMaterial;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashTime = 0f;
        Vector2 dashDir = direction.normalized;
        Vector2 perp = new Vector2(-dashDir.y, dashDir.x);

        while (dashTime < dashDuration)
        {
            rb.linearVelocity = dashDir * dashForce;

            // Center trail (white)
            if (middleDashTrailPrefab)
            {
                var mid = Instantiate(middleDashTrailPrefab, transform.position, Quaternion.identity);
                Destroy(mid, 1f);
            }

            // Edge trails
            if (edgeTrailPrefab)
            {
                Vector2 leftPos = (Vector2)transform.position + perp * dashTrailOffset;
                var left = Instantiate(edgeTrailPrefab, leftPos, Quaternion.identity);
                Destroy(left, 1f);

                Vector2 rightPos = (Vector2)transform.position - perp * dashTrailOffset;
                var right = Instantiate(edgeTrailPrefab, rightPos, Quaternion.identity);
                Destroy(right, 1f);
            }

            yield return new WaitForSeconds(dashTrailSpacing);
            dashTime += dashTrailSpacing;
        }

        rb.gravityScale = originalGravity;
        col.sharedMaterial = normalMaterial;
        isDashing = false;
    }

    GameObject GetDefaultEdgeTrailPrefab()
    {
        // Preserve your original behavior:
        // if currentDashCharges == 1 -> firstDashTrailPrefab, else -> secondDashTrailPrefab
        // (this mirrors what you had before, just centralized)
        return (currentDashCharges == 1) ? firstDashTrailPrefab : secondDashTrailPrefab;
    }

    // ---------------- Attack ----------------
    void HandleAttackInput()
    {
        if (Time.time < nextAttackTime) return;

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

        GameObject slashObj = Instantiate(slashPrefab, slashSpawnPoint.position, Quaternion.identity);

        Vector3 scale = slashObj.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(transform.localScale.x);
        slashObj.transform.localScale = scale;

        SlashAttack slash = slashObj.GetComponent<SlashAttack>();
        if (slash)
        {
            slash.Initialize(
                attackDamage,
                critChance,
                critMultiplier
            );
        }
    }

    // ---------------- Animations ----------------
    void UpdateAnimations()
    {
        if (isGrounded)
        {
            float speed = inputX;
            if (speed < 0f) speed *= -1f;
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

    // ---------------- Damage ----------------
    public void TakeDamage(int amount)
    {
        if (isParryActive)
        {
            parrySuccessful = true;
            isParryActive = false;
            ParryEffect();
            return;
        }

        currentHealth -= amount;
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
        spriteRenderer.color = damageFlashColor;

        float t = 0f;
        while (t < damageFlashDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = original;
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
                Destroy(hit.gameObject);
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        GameManager.Instance.SetState(GameManager.GameState.Dead);
    }
}
