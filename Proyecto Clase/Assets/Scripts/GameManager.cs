using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ---------------- SETTINGS ----------------
    public GameSettings Settings { get; private set; }

    // ---------------- GAME STATE ----------------
    public enum GameState
    {
        Gameplay,
        Paused,
        Menu,
        Dead
    }

    public GameState CurrentState { get; private set; } = GameState.Menu;

    // ---------------- SCENES ----------------
    [Header("Scene Names")]
    [SerializeField] private string gameScene = "GameScene";
    [SerializeField] private string mainMenuScene = "MainMenu";

    // ---------------- UI ----------------
    [Header("UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject loginMenu;
    [SerializeField] private GameObject achievementsMenu;
    [SerializeField] private GameObject difficultyRestartPopup;

    // ---------------- LIFECYCLE ----------------
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Settings = GameSettings.Load();
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (SceneManager.GetActiveScene().name != "MenuInicial")
        {
            SetState(GameState.Menu); // sets timeScale = 0, hides gameplay UI
            SceneManager.LoadScene("MenuInicial"); // switch to your starting menu
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if ((CurrentState == GameState.Gameplay || CurrentState == GameState.Paused) &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void OnApplicationQuit()
    {
        // Prevent half-applied difficulty from being saved
        if (!Settings.difficultyPendingRestart)
            Settings.Save();
    }

    // ---------------- SCENE HANDLING ----------------
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HideAllUI();
    }

    void HideAllUI()
    {
        if (pauseMenu) pauseMenu.SetActive(false);
        if (deathScreen) deathScreen.SetActive(false);
        if (achievementsMenu) achievementsMenu.SetActive(false);

    }

    // ---------------- GAME FLOW ----------------
    public void StartNewGame()
    {
        // HARD GATE: require an active profile (logged-in OR guest)
        if (UserManager.Instance == null || !UserManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("StartNewGame blocked: no active profile. Opening login menu.");
            OpenLoginMenu();
            return;
        }

        Time.timeScale = 1f;
        SetState(GameState.Gameplay);
        SceneManager.LoadScene(gameScene);

        if (MusicManager.Instance)
            MusicManager.Instance.PlayNormalMusic();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.Menu);
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OpenSettings()
    {
        if (settingsMenu)
            settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsMenu)
            settingsMenu.SetActive(false);
    }

    public void QuitGame()
    {
        if (!Settings.difficultyPendingRestart)
            Settings.Save();

        Application.Quit();
    }

    // ---------------- PAUSE ----------------
    public void TogglePause()
    {
        if (CurrentState == GameState.Gameplay)
            PauseGame();
        else if (CurrentState == GameState.Paused)
            ResumeGame();
    }

    void PauseGame() => SetState(GameState.Paused);
    void ResumeGame() => SetState(GameState.Gameplay);

    // ---------------- STATE MACHINE ----------------
    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Gameplay:
                Time.timeScale = 1f;
                SetPauseMenu(false);
                SetDeathScreen(false);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                SetPauseMenu(true);
                SetDeathScreen(false);
                break;

            case GameState.Menu:
                Time.timeScale = 0f;
                SetPauseMenu(false);
                SetDeathScreen(false);
                break;

            case GameState.Dead:
                Time.timeScale = 0f;
                SetPauseMenu(false);
                SetDeathScreen(true);
                break;
        }
    }

    // ---------------- DIFFICULTY HANDLING ----------------
    public void HandleSettingsSaved()
    {
        // Show popup if a difficulty change is pending
        if (Settings.difficultyPendingRestart)
        {
            ShowDifficultyRestartPopup();
        }
    }

    public void ShowDifficultyRestartPopup()
    {
        if (difficultyRestartPopup)
        {
            difficultyRestartPopup.SetActive(true);
            Time.timeScale = 0f; // Pause the game while popup is active
        }
    }

    public void ConfirmDifficultyRestart()
    {
        // Commit difficulty
        PlayerPrefs.SetInt("Difficulty", Settings.difficulty);
        PlayerPrefs.Save();

        Settings.previousDifficulty = Settings.difficulty;
        Settings.difficultyPendingRestart = false;

        Time.timeScale = 1f;
        difficultyRestartPopup.SetActive(false);

        // Send player to main menu after confirming
        ReturnToMainMenu();
    }

    public void CancelDifficultyRestart()
    {
        // Revert difficulty change
        Settings.difficulty = Settings.previousDifficulty;
        Settings.difficultyPendingRestart = false;

        Time.timeScale = 1f;
        difficultyRestartPopup.SetActive(false);

        // Refresh the dropdown in the settings menu, if it's open
        var menu = FindObjectOfType<SettingsMenu>();
        if (menu != null)
            menu.RefreshDifficultyDropdown();
    }

    // ---------------- SAVE FROM MENU ----------------
    public void SaveSettingsFromMenu()
    {
        // Save all non-difficulty settings
        Settings.Save();

        // Check if difficulty changed and mark pending if so
        if (Settings.CheckDifficultyChanged())
        {
            // Show popup
            HandleSettingsSaved();
        }
    }

    // ---------------- UI HELPERS ----------------

    public void OpenAchievementsMenu()
    {
        if (UserManager.Instance == null || !UserManager.Instance.IsLoggedIn)
        {
            OpenLoginMenu();
            return;
        }

        if (achievementsMenu)
            achievementsMenu.SetActive(true);
    }

    public void CloseAchievementsMenu()
    {
        if (achievementsMenu)
            achievementsMenu.SetActive(false);
    }

    void SetPauseMenu(bool visible)
    {
        if (pauseMenu)
            pauseMenu.SetActive(visible);
    }

    void SetDeathScreen(bool visible)
    {
        if (deathScreen)
            deathScreen.SetActive(visible);
    }

    public void OpenLoginMenu()
    {
        if (loginMenu)
            loginMenu.SetActive(true);

        PlayMenuMusic();
    }

    void PlayMenuMusic()
    {
        if (MusicManager.Instance)
            MusicManager.Instance.PlayMenuMusic();
    }

    public void CloseLoginMenu()
    {
        if (loginMenu)
            loginMenu.SetActive(false);
    }
}
