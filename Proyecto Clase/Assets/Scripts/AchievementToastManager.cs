using UnityEngine;

public class AchievementToastManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform toastContainer; // Top-right anchored container
    [SerializeField] private AchievementToastUI toastPrefab;

    [Header("Queue")]
    [SerializeField] private float minIntervalBetweenToasts = 0.35f;

    private float nextAllowedTime;

    void OnEnable()
    {
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked += HandleUnlocked;
    }

    void OnDisable()
    {
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked -= HandleUnlocked;
    }

    void HandleUnlocked(AchievementDTO dto)
    {
        if (!toastContainer || !toastPrefab || dto == null) return;

        if (Time.unscaledTime < nextAllowedTime)
            return;

        nextAllowedTime = Time.unscaledTime + minIntervalBetweenToasts;

        var toast = Instantiate(toastPrefab, toastContainer);
        toast.Show(dto);
    }
}
