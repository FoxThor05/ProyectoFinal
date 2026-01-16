using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private List<string> unlockedAchievements = new();

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

    public void SetUnlockedAchievements(IEnumerable<string> ids)
    {
        unlockedAchievements.Clear();
        unlockedAchievements.AddRange(ids);
    }

    public bool IsUnlocked(string id)
    {
        return unlockedAchievements.Contains(id);
    }

    public void Unlock(string id)
    {
        if (IsUnlocked(id)) return;

        unlockedAchievements.Add(id);
        BackendService.Instance?.SendAchievementUnlock(id);
    }
}
