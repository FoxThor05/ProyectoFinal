using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // Fired only when an achievement is newly unlocked via Unlock()
    public event Action<AchievementDTO> OnAchievementUnlocked;

    private readonly List<string> unlockedAchievements = new();
    private readonly Dictionary<string, AchievementDTO> achievementDefs = new();

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

    public void SetAllAchievements(AchievementDTO[] achievements)
    {
        achievementDefs.Clear();
        if (achievements == null) return;

        foreach (var a in achievements)
        {
            if (a == null || string.IsNullOrEmpty(a.id)) continue;
            achievementDefs[a.id] = a;
        }
    }

    public bool TryGetAchievement(string id, out AchievementDTO dto)
    {
        return achievementDefs.TryGetValue(id, out dto);
    }

    public void SetUnlockedAchievements(IEnumerable<string> ids)
    {
        unlockedAchievements.Clear();
        if (ids == null) return;

        unlockedAchievements.AddRange(ids);
    }

    public bool IsUnlocked(string id)
    {
        return unlockedAchievements.Contains(id);
    }

    public void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (IsUnlocked(id)) return;

        unlockedAchievements.Add(id);

        // Send to backend (no-op in guest mode per your BackendService)
        BackendService.Instance?.SendAchievementUnlock(id);

        // Notify UI for toast
        if (achievementDefs.TryGetValue(id, out var dto) && dto != null)
        {
            OnAchievementUnlocked?.Invoke(dto);
        }
        else
        {
            // Best-effort fallback
            OnAchievementUnlocked?.Invoke(new AchievementDTO
            {
                id = id,
                name = $"Achievement {id}",
                description = "",
                rarity = "common",
                icon_key = id
            });
        }
    }
}
