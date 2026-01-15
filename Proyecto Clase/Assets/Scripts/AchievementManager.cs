using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private HashSet<string> unlockedAchievements = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsUnlocked(string achievementId)
    {
        return unlockedAchievements.Contains(achievementId);
    }

    public void Unlock(string achievementId)
    {
        if (unlockedAchievements.Contains(achievementId))
            return;

        unlockedAchievements.Add(achievementId);

        Debug.Log($"Achievement unlocked: {achievementId}");

        // Notify UI
        // Save locally
        // Send to backend
        BackendService.Instance?.SendAchievementUnlock(achievementId);
    }
}
