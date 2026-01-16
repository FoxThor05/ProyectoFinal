using UnityEngine;

public class BoomerangProjectile : MonoBehaviour
{
    public float speed;
    public float returnSpeed;
    public Transform boss;
    private Vector2 dir;
    private bool returning = false;

    void Start()
    {
        dir = transform.right;
    }

    void Update()
    {
        if (!returning)
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);

            if (Vector2.Distance(transform.position, boss.position) >= boss.GetComponent<BossAI>().activationRadius)
                returning = true;
        }
        else
        {
            Vector2 toBoss = (boss.position - transform.position).normalized;
            transform.right = toBoss;
            transform.position += (Vector3)(toBoss * returnSpeed * Time.deltaTime);
        }
    }
}
