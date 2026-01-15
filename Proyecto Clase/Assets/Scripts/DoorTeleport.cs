using UnityEngine;

public class DoorTeleport : MonoBehaviour
{
    public Transform teleportTarget;

    private bool playerInside = false;
    private PlayerController player;

    void Update()
    {
        if (!playerInside || player == null)
            return;

        // Press UP (W / Up Arrow)
        if (Input.GetAxisRaw("Vertical") > 0)
        {
            Teleport();
        }
    }

    void Teleport()
    {
        if (!teleportTarget) return;

        // Stop player momentum (important)
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb)
            rb.linearVelocity = Vector2.zero;

        player.transform.position = teleportTarget.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            player = other.GetComponent<PlayerController>();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            player = null;
        }
    }
}
