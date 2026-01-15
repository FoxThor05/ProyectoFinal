using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider musicSlider;

    [Header("Gameplay")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Keybind Texts")]
    [SerializeField] private TMP_Text fire1KeyText;
    [SerializeField] private TMP_Text fire2KeyText;
    [SerializeField] private TMP_Text dashKeyText;
    [SerializeField] private TMP_Text jumpKeyText;

    private Resolution[] resolutions;

    private bool waitingForKey;
    private GameSettings.BindAction currentBind;
    private TMP_Text currentBindText;

    // ---------------- LIFECYCLE ----------------
    void Start()
    {
        SetupResolutions();
        LoadUIFromSettings();
    }

    void Update()
    {
        if (!waitingForKey) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Cancel rebind
            waitingForKey = false;
            UpdateKeyTexts();
            return;
        }

        if (!Input.anyKeyDown) return;

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key))
            {
                HandleKeyRebind(key);
                break;
            }
        }
    }

    // ---------------- INITIALIZATION ----------------
    void SetupResolutions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        foreach (var r in resolutions)
            options.Add($"{r.width}x{r.height} @{r.refreshRate}Hz");

        resolutionDropdown.AddOptions(options);
    }

    void LoadUIFromSettings()
    {
        var s = GameManager.Instance.Settings;

        musicSlider.value = s.musicVolume;
        difficultyDropdown.value = s.difficulty;
        fullscreenToggle.isOn = s.fullscreen;
        resolutionDropdown.value = s.resolutionIndex;

        UpdateKeyTexts();

        // Apply the saved display/audio settings live
        Screen.fullScreen = s.fullscreen;
        Resolution r = resolutions[s.resolutionIndex];
        Screen.SetResolution(r.width, r.height, s.fullscreen);

        if (MusicManager.Instance)
            MusicManager.Instance.SetMusicVolume(s.musicVolume);
    }

    void UpdateKeyTexts()
    {
        var s = GameManager.Instance.Settings;

        fire1KeyText.text = s.fire1Key.ToString();
        fire2KeyText.text = s.fire2Key.ToString();
        dashKeyText.text = s.dashKey.ToString();
        jumpKeyText.text = s.jumpKey.ToString();
    }

    // ---------------- REBINDING ----------------
    void BeginRebind(GameSettings.BindAction bind, TMP_Text text)
    {
        currentBind = bind;
        currentBindText = text;
        waitingForKey = true;
        text.text = "Press a key...";
    }

    void HandleKeyRebind(KeyCode key)
    {
        waitingForKey = false;

        bool success = GameManager.Instance.Settings.TryRebind(currentBind, key);
        UpdateKeyTexts();

        if (!success)
        {
            Debug.Log("Key already bound or invalid. Rebind cancelled.");
        }
    }

    public void RebindFire1() => BeginRebind(GameSettings.BindAction.Fire1, fire1KeyText);
    public void RebindFire2() => BeginRebind(GameSettings.BindAction.Fire2, fire2KeyText);
    public void RebindDash() => BeginRebind(GameSettings.BindAction.Dash, dashKeyText);
    public void RebindJump() => BeginRebind(GameSettings.BindAction.Jump, jumpKeyText);

    // ---------------- UI CALLBACKS ----------------
    public void OnMusicVolumeChanged()
    {
        float value = musicSlider.value;
        GameManager.Instance.Settings.ApplyMusicVolume(value);
    }

    public void OnDifficultyChanged()
    {
        int value = difficultyDropdown.value;
        GameManager.Instance.Settings.ApplyDifficulty(value);
    }

    public void OnFullscreenToggled()
    {
        bool value = fullscreenToggle.isOn;
        GameManager.Instance.Settings.ApplyFullscreen(value);
    }

    public void OnResolutionChanged()
    {
        int index = resolutionDropdown.value;
        GameManager.Instance.Settings.ApplyResolution(index, resolutions);
    }

    // ---------------- SAVE / CANCEL ----------------
    public void SaveSettings()
    {
        GameManager.Instance.Settings.Save();
    }

    public void Back()
    {
        // Reload saved settings from disk to undo unsaved changes
        GameManager.Instance.Settings.LoadSettingsFromPlayerPrefs();
        LoadUIFromSettings();
        gameObject.SetActive(false);
    }
}
