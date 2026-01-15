using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 10;
    public float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc)
        {
            pc.TakeDamage(damage);
            Destroy(gameObject);
        }

        // Optional: destroy on walls
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
