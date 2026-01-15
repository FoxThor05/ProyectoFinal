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
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ReloadSettingsFromDisk()
    {
        Settings = GameSettings.Load();
    }

    void Update()
    {
        if (CurrentState == GameState.Gameplay || CurrentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }
    }

    void OnApplicationQuit()
    {
        Settings.Save();
    }

    void PlayMenuMusic()
    {
        if (MusicManager.Instance)
            MusicManager.Instance.PlayMenuMusic();
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
    }

    // ---------------- GAME FLOW ----------------
    public void StartNewGame()
    {
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
        settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
    }

    public void QuitGame()
    {
        Settings.Save();
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("Quit Game (Editor)");
#endif
    }

    // ---------------- DEATH ----------------
    public void PlayerDied()
    {
        if (CurrentState == GameState.Dead)
            return;

        SetState(GameState.Dead);

        if (MusicManager.Instance)
        {
            MusicManager.Instance.StopBossMusic();
            MusicManager.Instance.StopNormalMusic();
        }
    }

    // ---------------- PAUSE ----------------
    public void TogglePause()
    {
        if (CurrentState == GameState.Gameplay)
            PauseGame();
        else if (CurrentState == GameState.Paused)
            ResumeGame();
    }

    public void PauseGame()
    {
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        SetState(GameState.Gameplay);
    }

    // ---------------- LOGIN MENU ----------------

    public void OpenLoginMenu()
    {
        if (loginMenu)
            loginMenu.SetActive(true);
        PlayMenuMusic();
    }

    public void CloseLoginMenu()
    {
        if (loginMenu)
            loginMenu.SetActive(false);
    }
    // ---------------- STATE MACHINE ----------------
    void SetState(GameState newState)
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
}
