using TMPro;
using UnityEngine;

public class DamagePopupText : MonoBehaviour
{
    [SerializeField] private TMP_Text tmp;
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float lifetime = 0.7f;

    void Awake()
    {
        if (!tmp)
            tmp = GetComponentInChildren<TMP_Text>();
    }

    public void Set(int amount, Color textColor, Color outlineColor)
    {
        if (!tmp) return;

        tmp.text = amount.ToString();
        tmp.color = textColor;

        // Outline: TMP uses material properties
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = outlineColor;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
    }
}
