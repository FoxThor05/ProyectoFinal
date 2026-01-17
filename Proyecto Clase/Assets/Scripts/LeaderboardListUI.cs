using System.Collections.Generic;
using UnityEngine;

public class LeaderboardListUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject leaderboardItemPrefab;

    [Header("Data")]
    [SerializeField] private int maxEntries = 50;

    [Header("Placeholders")]
    [SerializeField] private Sprite placeholderAvatar;
    [SerializeField] private Sprite placeholderAchievementIcon;

    private Dictionary<string, string> idToIconKey = new();

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!BackendService.Instance)
        {
            Debug.LogError("[LeaderboardListUI] BackendService.Instance missing.");
            return;
        }

        BackendService.Instance.FetchAllAchievements(achievements =>
        {
            idToIconKey.Clear();

            if (achievements != null)
            {
                foreach (var a in achievements)
                {
                    if (a == null || string.IsNullOrEmpty(a.id)) continue;
                    idToIconKey[a.id] = a.icon_key; // may be null until you fill it
                }
            }

            BackendService.Instance.FetchLeaderboard(maxEntries, entries =>
            {
                Populate(entries ?? new BackendService.LeaderboardEntryDTO[0]);
            });
        });
    }

    void Populate(BackendService.LeaderboardEntryDTO[] entries)
    {
        if (!contentParent || !leaderboardItemPrefab)
        {
            Debug.LogError("[LeaderboardListUI] Missing contentParent or leaderboardItemPrefab.");
            return;
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var e in entries)
        {
            var go = Instantiate(leaderboardItemPrefab, contentParent);
            var ui = go.GetComponent<LeaderboardItemUI>();
            if (!ui)
            {
                Debug.LogError("[LeaderboardListUI] Prefab missing LeaderboardItemUI.");
                continue;
            }

            var unlockedSet = new HashSet<string>();
            if (e.unlocked != null)
            {
                for (int i = 0; i < e.unlocked.Length; i++)
                {
                    if (!string.IsNullOrEmpty(e.unlocked[i]))
                        unlockedSet.Add(e.unlocked[i]);
                }
            }

            ui.Setup(
                username: e.username,
                totalScore: e.score,
                unlockedAchievementIds: unlockedSet,
                idToIconKey: idToIconKey,
                placeholderAvatar: placeholderAvatar,
                placeholderAchievementIcon: placeholderAchievementIcon
            );
        }
    }
}
