using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    private float inputX;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Get input for movement animation
        inputX = Mathf.Abs(Input.GetAxisRaw("Horizontal"));

        // Check jump state
        CheckGround();
        UpdateVerticalState();

        // Attack input
        if (Input.GetButtonDown("Fire1"))
            anim.SetTrigger("Attack");

        // Movement animation
        anim.SetFloat("Speed", inputX);
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void UpdateVerticalState()
    {
        // Rising (jumping)
        if (!isGrounded && rb.linearVelocity.y > 0.1f)
        {
            anim.SetBool("isJumping", true);
            anim.SetBool("isFalling", false);
        }
        // Falling
        else if (!isGrounded && rb.linearVelocity.y < -0.1f)
        {
            anim.SetBool("isJumping", false);
            anim.SetBool("isFalling", true);
        }
        // Grounded
        else if (isGrounded)
        {
            anim.SetBool("isJumping", false);
            anim.SetBool("isFalling", false);
        }
    }

    // Optional: draw ground check radius in scene view
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
