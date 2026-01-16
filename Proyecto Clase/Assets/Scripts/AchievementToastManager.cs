using System.Collections;
using UnityEngine;

public class AchievementToastManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform toastContainer;
    [SerializeField] private AchievementToastUI toastPrefab;

    [Header("Queue")]
    [SerializeField] private float minIntervalBetweenToasts = 0.35f;

    private float nextAllowedTime;

    private AchievementManager boundManager;
    private Coroutine bindRoutine;

    void OnEnable()
    {
        bindRoutine = StartCoroutine(BindLoop());
    }

    void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        Unbind();
    }

    IEnumerator BindLoop()
    {
        while (true)
        {
            if (AchievementManager.Instance == null)
            {
                yield return null;
                continue;
            }

            if (boundManager != AchievementManager.Instance)
            {
                Unbind();
                boundManager = AchievementManager.Instance;
                boundManager.OnAchievementUnlocked += HandleUnlocked;

                Debug.Log("[AchievementToastManager] Bound to AchievementManager.");
            }

            yield return null;
        }
    }

    void Unbind()
    {
        if (boundManager != null)
        {
            boundManager.OnAchievementUnlocked -= HandleUnlocked;
            boundManager = null;
        }
    }

    void HandleUnlocked(AchievementDTO dto)
    {
        if (!toastContainer || !toastPrefab || dto == null)
        {
            Debug.LogWarning("[AchievementToastManager] Missing toastContainer/toastPrefab or DTO.");
            return;
        }

        if (Time.unscaledTime < nextAllowedTime)
            return;

        nextAllowedTime = Time.unscaledTime + minIntervalBetweenToasts;

        var toast = Instantiate(toastPrefab, toastContainer);
        toast.Show(dto);

        Debug.Log($"[AchievementToastManager] Toast spawned for achievement id={dto.id}, name={dto.name}");
    }
}
