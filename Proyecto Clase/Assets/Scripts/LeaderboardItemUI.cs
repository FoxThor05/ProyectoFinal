using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardItemUI : MonoBehaviour
{
    [Header("Left")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text usernameText;

    [Header("Middle (Achievement Icons: slots 1..9)")]
    [SerializeField] private Image[] achievementIconSlots;

    [Header("Right")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Visuals")]
    [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.25f;

    public void Setup(
        string username,
        int totalScore,
        HashSet<string> unlockedAchievementIds,
        Dictionary<string, string> idToIconKey,
        Sprite placeholderAvatar = null,
        Sprite placeholderAchievementIcon = null)
    {
        if (usernameText) usernameText.text = username;
        if (scoreText) scoreText.text = totalScore.ToString();

        if (avatarImage)
        {
            if (placeholderAvatar) avatarImage.sprite = placeholderAvatar;
            avatarImage.enabled = avatarImage.sprite != null;
        }

        for (int i = 0; i < achievementIconSlots.Length; i++)
        {
            var img = achievementIconSlots[i];
            if (!img) continue;

            string achievementId = (i + 1).ToString();

            bool unlocked = unlockedAchievementIds != null && unlockedAchievementIds.Contains(achievementId);

            // Prefer the real icon if available
            Sprite sprite = null;

            if (AchievementIconLibrary.Instance != null && idToIconKey != null)
            {
                if (idToIconKey.TryGetValue(achievementId, out var iconKey) &&
                    !string.IsNullOrEmpty(iconKey))
                {
                    sprite = AchievementIconLibrary.Instance.GetIcon(iconKey);
                }
            }

            // Fallback to a placeholder icon (optional)
            if (sprite == null)
                sprite = placeholderAchievementIcon;

            img.sprite = sprite;
            img.enabled = (sprite != null);

            // Dim if locked
            var c = img.color;
            c.a = unlocked ? 1f : lockedAlpha;
            img.color = c;
        }
    }
}
