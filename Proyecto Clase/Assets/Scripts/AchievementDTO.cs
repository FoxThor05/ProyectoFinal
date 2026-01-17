[System.Serializable]
public class AchievementDTO
{
    public string id;
    public string name;
    public string description;
    public string rarity;
    public int score;
    public string icon_key;
}

[System.Serializable]
public class UserAchievementDTO
{
    public string achievement_id;
}
