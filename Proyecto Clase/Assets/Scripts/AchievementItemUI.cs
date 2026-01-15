using UnityEngine;
using UnityEngine.UI;

public class AchievementItemUI : MonoBehaviour
{
    public Text title;
    public Text description;
    public GameObject lockedOverlay;

    public void Setup(AchievementDTO data, bool unlocked)
    {
        title.text = data.name;
        description.text = data.description;
        lockedOverlay.SetActive(!unlocked);
    }
}
