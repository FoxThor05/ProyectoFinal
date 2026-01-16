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
        GameManager.Instance.Settings.TryRebind(currentBind, key);
        UpdateKeyTexts();
    }

    public void RebindFire1() => BeginRebind(GameSettings.BindAction.Fire1, fire1KeyText);
    public void RebindFire2() => BeginRebind(GameSettings.BindAction.Fire2, fire2KeyText);
    public void RebindDash() => BeginRebind(GameSettings.BindAction.Dash, dashKeyText);
    public void RebindJump() => BeginRebind(GameSettings.BindAction.Jump, jumpKeyText);

    // ---------------- UI CALLBACKS ----------------
    public void OnMusicVolumeChanged()
    {
        GameManager.Instance.Settings.ApplyMusicVolume(musicSlider.value);
    }

    public void OnDifficultyChanged()
    {
        int value = difficultyDropdown.value;
        GameManager.Instance.Settings.ApplyDifficulty(value);

        // Do NOT show popup here
        // Popup will appear only on Save
    }


    public void OnFullscreenToggled()
    {
        GameManager.Instance.Settings.ApplyFullscreen(fullscreenToggle.isOn);
    }

    public void OnResolutionChanged()
    {
        GameManager.Instance.Settings.ApplyResolution(
            resolutionDropdown.value, resolutions);
    }

    // ---------------- SAVE / CANCEL ----------------
    public void SaveSettings()
    {
        GameManager.Instance.SaveSettingsFromMenu();
    }

    public void RefreshDifficultyDropdown()
    {
        difficultyDropdown.SetValueWithoutNotify(
            GameManager.Instance.Settings.difficulty);
    }

    public void Back()
    {
        GameManager.Instance.Settings.LoadSettingsFromPlayerPrefs();
        LoadUIFromSettings();
        gameObject.SetActive(false);
    }
}
