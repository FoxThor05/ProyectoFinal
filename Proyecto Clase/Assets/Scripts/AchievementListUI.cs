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
            BackendService.Instance.FetchUserAchievements(unlockedIds =>
            {
                AchievementManager.Instance.SetUnlockedAchievements(unlockedIds);
                Populate(allAchievements);
            });
        });
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
