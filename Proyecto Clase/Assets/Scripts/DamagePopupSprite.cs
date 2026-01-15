using UnityEngine;

public class DamagePopupSprite : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float lifetime = 0.6f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
    }
}
