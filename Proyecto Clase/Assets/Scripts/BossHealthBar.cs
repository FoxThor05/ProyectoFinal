using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [Header("UI")]
    public Image fillImage;

    [Header("Boss Reference")]
    public BossAI boss;

    void Start()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!boss)
            return;

        if (boss.currentHealth <= 0)
        {
            Hide();
            return;
        }

        float hpPercent = (float)boss.currentHealth / boss.maxHealth;
        fillImage.fillAmount = hpPercent;
    }

    public void Show(BossAI targetBoss)
    {
        boss = targetBoss;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        boss = null;
    }
}
