using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthbar : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Image fillImage;

    [Header("Colors")]
    public Color healthyColor = Color.green;
    public Color midColor = Color.yellow;
    public Color lowColor = Color.red;

    void Start()
    {
        // Auto-find player if not assigned
        if (!player)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj)
                player = playerObj.GetComponent<PlayerController>();
        }
    }

    void Update()
    {
        if (!player) return;

        float healthPercent = (float)player.currentHealth / player.maxHealth;

        // Clamp just in case
        healthPercent = Mathf.Clamp01(healthPercent);

        // Update fill
        fillImage.fillAmount = healthPercent;

        // Update color
        if (healthPercent > 0.5f)
        {
            fillImage.color = healthyColor;
        }
        else if (healthPercent > 0.25f) // between 50 and above 25
        {
            fillImage.color = midColor;
        }
        else
        {
            fillImage.color = lowColor;
        }
    }
}
