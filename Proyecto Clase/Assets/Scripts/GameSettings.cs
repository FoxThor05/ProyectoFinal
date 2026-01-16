using UnityEngine;
using System;

[Serializable]
public class GameSettings
{
    // ---------------- AUDIO ----------------
    public float musicVolume = 1f;

    // ---------------- GAMEPLAY ----------------
    public int difficulty = 0; // 0 = Easy, 1 = Normal, 2 = Hard, 3 = Nightmare
    [NonSerialized] public int previousDifficulty;
    [NonSerialized] public bool difficultyPendingRestart;

    // ---------------- DISPLAY ----------------
    public bool fullscreen = true;
    public int resolutionIndex = 0;

    // ---------------- KEYBINDS ----------------
    public enum BindAction { Fire1, Fire2, Dash, Jump }

    public KeyCode fire1Key = KeyCode.Mouse0;
    public KeyCode fire2Key = KeyCode.Mouse1;
    public KeyCode dashKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;

    // ---------------- APPLY METHODS ----------------
    public void ApplyMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMusicVolume(musicVolume);
    }

    public void ApplyDifficulty(int value)
    {
        difficulty = Mathf.Clamp(value, 0, 3);
        // Remove difficultyPendingRestart from here
    }
    public bool CheckDifficultyChanged()
    {
        if (difficulty != previousDifficulty)
        {
            difficultyPendingRestart = true;
            return true;
        }
        return false;
    }


    public void ApplyFullscreen(bool value)
    {
        fullscreen = value;
        Screen.fullScreen = value;
    }

    public void ApplyResolution(int index, Resolution[] availableResolutions)
    {
        if (index < 0 || index >= availableResolutions.Length) return;

        resolutionIndex = index;
        var r = availableResolutions[index];
        Screen.SetResolution(r.width, r.height, fullscreen);
    }

    // ---------------- DIFFICULTY COMMIT ----------------
    public void CommitDifficulty()
    {
        PlayerPrefs.SetInt("Difficulty", difficulty);
        PlayerPrefs.Save();

        previousDifficulty = difficulty;
        difficultyPendingRestart = false;
    }

    // ---------------- KEYBINDS ----------------
    public bool TryRebind(BindAction action, KeyCode newKey)
    {
        if (newKey == KeyCode.Escape) return false;
        if (IsKeyAlreadyBound(newKey)) return false;

        switch (action)
        {
            case BindAction.Fire1: fire1Key = newKey; break;
            case BindAction.Fire2: fire2Key = newKey; break;
            case BindAction.Dash: dashKey = newKey; break;
            case BindAction.Jump: jumpKey = newKey; break;
        }

        return true;
    }

    bool IsKeyAlreadyBound(KeyCode key)
    {
        return fire1Key == key || fire2Key == key ||
               dashKey == key || jumpKey == key;
    }

    // ---------------- SAVE / LOAD ----------------
    public void Save()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);

        PlayerPrefs.SetInt("Fire1Key", (int)fire1Key);
        PlayerPrefs.SetInt("Fire2Key", (int)fire2Key);
        PlayerPrefs.SetInt("DashKey", (int)dashKey);
        PlayerPrefs.SetInt("JumpKey", (int)jumpKey);

        PlayerPrefs.Save();
    }

    public void LoadSettingsFromPlayerPrefs()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        difficulty = PlayerPrefs.GetInt("Difficulty", 0);
        previousDifficulty = difficulty;
        difficultyPendingRestart = false;

        fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);

        fire1Key = (KeyCode)PlayerPrefs.GetInt("Fire1Key", (int)KeyCode.Mouse0);
        fire2Key = (KeyCode)PlayerPrefs.GetInt("Fire2Key", (int)KeyCode.Mouse1);
        dashKey = (KeyCode)PlayerPrefs.GetInt("DashKey", (int)KeyCode.LeftShift);
        jumpKey = (KeyCode)PlayerPrefs.GetInt("JumpKey", (int)KeyCode.Space);
    }

    public static GameSettings Load()
    {
        var s = new GameSettings();
        s.LoadSettingsFromPlayerPrefs();
        return s;
    }
}
