using UnityEngine;
using System.Collections;

public class BoomerangProjectile : MonoBehaviour
{
    public float speed;
    public float returnSpeed;
    public Transform boss;
    public float delayBeforeReturn = 0.5f; // small pause before coming back

    private Vector2 dir;
    private bool returning = false;
    public bool startedReturn { get; private set; } = false; // NEW: notify when return begins

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
            {
                returning = true;
                if (!startedReturn)
                {
                    startedReturn = true; // signal the boss attack can end
                }
                StartCoroutine(ReturnAfterDelay());
            }
        }
    }

    IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeReturn);

        while (true)
        {
            Vector2 toBoss = (boss.position - transform.position).normalized;
            transform.right = toBoss;
            transform.position += (Vector3)(toBoss * returnSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, boss.position) < 0.1f)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }
}
