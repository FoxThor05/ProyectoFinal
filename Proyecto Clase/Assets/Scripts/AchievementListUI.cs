using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementListUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject achievementItemPrefab;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        BackendService.Instance.FetchAllAchievements(allAchievements =>
        {
            if (allAchievements == null)
                allAchievements = new AchievementDTO[0];

            // Cache definitions (for toasts/icons)
            AchievementManager.Instance.SetAllAchievements(allAchievements);

            BackendService.Instance.FetchUnlockedAchievements(unlockedIds =>
            {
                AchievementManager.Instance.SetUnlockedAchievements(unlockedIds);

                // NEW: enforce deterministic order by numeric id
                var sorted = SortByNumericId(allAchievements);

                Populate(sorted);
            });
        });
    }

    AchievementDTO[] SortByNumericId(AchievementDTO[] achievements)
    {
        var list = new List<AchievementDTO>(achievements ?? Array.Empty<AchievementDTO>());

        list.Sort((a, b) =>
        {
            int ai = ParseIdAsInt(a?.id);
            int bi = ParseIdAsInt(b?.id);

            // If parsing fails, push those to the end but keep deterministic ordering
            int cmp = ai.CompareTo(bi);
            if (cmp != 0) return cmp;

            // Secondary stable tie-breaker
            return string.Compare(a?.id, b?.id, StringComparison.OrdinalIgnoreCase);
        });

        return list.ToArray();
    }

    int ParseIdAsInt(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return int.MaxValue;

        // Your ids are "1".."9". This also supports future "10", "11", etc.
        if (int.TryParse(id.Trim(), out int value))
            return value;

        return int.MaxValue;
    }

    void Populate(AchievementDTO[] achievements)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var achievement in achievements)
        {
            GameObject item = Instantiate(achievementItemPrefab, contentParent);
            var ui = item.GetComponent<AchievementItemUI>();

            bool unlocked = AchievementManager.Instance.IsUnlocked(achievement.id);
            ui.Setup(achievement, unlocked);
        }
    }
}
