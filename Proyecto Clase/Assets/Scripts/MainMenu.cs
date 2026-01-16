using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Optional: New Game Button")]
    [SerializeField] private Button newGameButton;

    void OnEnable()
    {
        RefreshNewGameButton();
    }

    void Update()
    {
        // Keep it in sync if login occurs while this menu stays open.
        RefreshNewGameButton();
    }

    void RefreshNewGameButton()
    {
        if (!newGameButton) return;

        bool hasProfile = UserManager.Instance != null && UserManager.Instance.IsLoggedIn;
        newGameButton.interactable = hasProfile;
    }

    public void StartGame()
    {
        // Extra guard at UI level (GameManager also gates it).
        if (UserManager.Instance == null || !UserManager.Instance.IsLoggedIn)
        {
            GameManager.Instance.OpenLoginMenu();
            return;
        }

        GameManager.Instance.StartNewGame();
    }

    public void QuitGame()
    {
        GameManager.Instance.QuitGame();
    }
    public void OpenAchievements()
    {
        GameManager.Instance.OpenAchievementsMenu();
    }

    public void OpenSettings()
    {
        GameManager.Instance.OpenSettings();
    }

    public void OpenLogin()
    {
        GameManager.Instance.OpenLoginMenu();
    }
}
