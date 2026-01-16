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
        Dead,
        Victory
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
    [SerializeField] private GameObject difficultyRestartPopup;
    [SerializeField] private GameObject achievementsMenu;

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryScreen;

    // ---------------- RUN TRACKING ----------------
    private bool tookDamageThisRun = false;

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
            SetState(GameState.Menu);
            SceneManager.LoadScene("MenuInicial");
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
        if (victoryScreen) victoryScreen.SetActive(false);
    }

    // ---------------- GAME FLOW ----------------
    public void StartNewGame()
    {
        // Reset run flags for "no damage run" achievement
        tookDamageThisRun = false;

        Time.timeScale = 1f;
        SetState(GameState.Gameplay);
        SceneManager.LoadScene(gameScene);

        if (MusicManager.Instance)
            MusicManager.Instance.PlayNormalMusic();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        HideAllUI();
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
                SetVictoryScreen(false);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                SetPauseMenu(true);
                SetDeathScreen(false);
                SetVictoryScreen(false);
                break;

            case GameState.Menu:
                Time.timeScale = 0f;
                SetPauseMenu(false);
                SetDeathScreen(false);
                SetVictoryScreen(false);
                break;

            case GameState.Dead:
                Time.timeScale = 0f;
                SetPauseMenu(false);
                SetDeathScreen(true);
                SetVictoryScreen(false);
                break;

            case GameState.Victory:
                Time.timeScale = 0f;
                SetPauseMenu(false);
                SetDeathScreen(false);
                SetVictoryScreen(true);
                break;
        }
    }

    // ---------------- VICTORY ----------------
    public void NotifyPlayerTookDamage()
    {
        tookDamageThisRun = true;
    }

    public void OnBossDefeated()
    {
        // Difficulty achievements: ids 5..9
        // difficulty: 0 Easy, 1 Normal, 2 Hard, 3 Nightmare
        int d = Settings != null ? Settings.difficulty : 0;

        AchievementManager.Instance?.Unlock("5");          // Easy or higher (always)
        if (d >= 1) AchievementManager.Instance?.Unlock("6");
        if (d >= 2) AchievementManager.Instance?.Unlock("7");
        if (d >= 3) AchievementManager.Instance?.Unlock("8");

        if (d >= 3 && !tookDamageThisRun)
            AchievementManager.Instance?.Unlock("9");

        // Show victory UI
        SetState(GameState.Victory);

        // Optional music behavior: switch back to menu music
        if (MusicManager.Instance)
            MusicManager.Instance.PlayMenuMusic();
    }

    public void CloseVictoryAndReturnToMenu()
    {
        ReturnToMainMenu();
    }

    // ---------------- DIFFICULTY HANDLING ----------------
    public void HandleSettingsSaved()
    {
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
            Time.timeScale = 0f;
        }
    }

    public void ConfirmDifficultyRestart()
    {
        PlayerPrefs.SetInt("Difficulty", Settings.difficulty);
        PlayerPrefs.Save();

        Settings.previousDifficulty = Settings.difficulty;
        Settings.difficultyPendingRestart = false;

        Time.timeScale = 1f;
        difficultyRestartPopup.SetActive(false);

        ReturnToMainMenu();
    }

    public void CancelDifficultyRestart()
    {
        Settings.difficulty = Settings.previousDifficulty;
        Settings.difficultyPendingRestart = false;

        Time.timeScale = 1f;
        difficultyRestartPopup.SetActive(false);

        var menu = FindObjectOfType<SettingsMenu>();
        if (menu != null)
            menu.RefreshDifficultyDropdown();
    }

    public void SaveSettingsFromMenu()
    {
        Settings.Save();

        if (Settings.CheckDifficultyChanged())
        {
            HandleSettingsSaved();
        }
    }

    // ---------------- UI HELPERS ----------------
    void SetPauseMenu(bool visible)
    {
        if (pauseMenu)
            pauseMenu.SetActive(visible);
    }
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

    void SetDeathScreen(bool visible)
    {
        if (deathScreen)
            deathScreen.SetActive(visible);
    }

    void SetVictoryScreen(bool visible)
    {
        if (victoryScreen)
            victoryScreen.SetActive(visible);
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
