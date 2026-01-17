using TMPro;
using UnityEngine;

public class VictoryScreenUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;

    [Header("Buttons")]
    [SerializeField] private TMP_Text mainMenuButtonText;
    [SerializeField] private TMP_Text newRunButtonText;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (GameManager.Instance == null || GameManager.Instance.Settings == null)
            return;

        int difficulty = GameManager.Instance.Settings.difficulty;

        // ---------------- TITLE ----------------
        if (titleText)
            titleText.text = "VICTORY!";

        // ---------------- DETAIL ----------------
        if (detailText)
        {
            string difficultyName = difficulty switch
            {
                0 => "Easy",
                1 => "Normal",
                2 => "Hard",
                3 => "Nightmare",
                _ => "Unknown"
            };

            detailText.text = $"Cleared on {difficultyName}.";
        }

        // ---------------- BUTTON TEXT ----------------
        if (mainMenuButtonText)
            mainMenuButtonText.text = "Back to Menu";

        if (newRunButtonText)
        {
            newRunButtonText.text = difficulty switch
            {
                0 => "Try Normal Mode",
                1 => "Try Hard Mode",
                2 => "Nightmare Mode",
                3 => GetNightmareButtonText(),
                _ => "New Run"
            };
        }

        // Optional: Special messaging for flawless Nightmare clears
        if (difficulty == 3 && GameManager.Instance.HasTakenDamageThisRun() == false)
        {
            if (titleText)
                titleText.text = "LEGENDARY!";

            if (detailText)
                detailText.text = "Nightmare cleared without taking damage.";
        }
    }

    string GetNightmareButtonText()
    {
        if (GameManager.Instance == null)
            return "New Run";

        bool flawless = GameManager.Instance.HasTakenDamageThisRun() == false;
        return flawless ? "One More!" : "You can do better!";
    }

    // ---------------- BUTTON CALLBACKS ----------------

    public void OnMainMenuPressed()
    {
        GameManager.Instance?.CloseVictoryAndReturnToMenu();
    }

    public void OnNewRunPressed()
    {
        if (GameManager.Instance == null || GameManager.Instance.Settings == null)
            return;

        var settings = GameManager.Instance.Settings;

        // Increase difficulty only if not already Nightmare
        if (settings.difficulty < 3)
        {
            settings.difficulty++;
            PlayerPrefs.SetInt("Difficulty", settings.difficulty);
            PlayerPrefs.Save();
        }

        GameManager.Instance.StartNewGame();
    }

    public void OnQuitPressed()
    {
        GameManager.Instance?.QuitGame();
    }
}
