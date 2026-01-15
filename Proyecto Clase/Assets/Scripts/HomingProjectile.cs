using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;
    public float turnSpeed = 180f; // degrees per second
    public float lifeTime = 6f;

    [Header("Damage")]
    public int damage = 10;

    [Header("Target")]
    public Transform target;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (!target)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
                target = player.transform;
        }

        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (!target)
            return;

        // Direction to target
        Vector2 direction = (Vector2)target.position - rb.position;
        direction.Normalize();

        // Current facing direction
        float rotateAmount = Vector3.Cross(direction, transform.right).z;

        // Rotate smoothly toward target
        rb.angularVelocity = -rotateAmount * turnSpeed;

        // Move forward
        rb.linearVelocity = transform.right * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
