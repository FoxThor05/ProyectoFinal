using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public enum MusicMode
    {
        None,
        Menu,
        Normal,
        Boss,
        Victory
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip bossMusic;

    [Header("Victory Music (Default)")]
    [SerializeField] private AudioClip victoryMusic;

    [Header("Victory Music (Optional Variants)")]
    [Tooltip("If assigned, this will be used when difficulty == Nightmare (3), unless flawless override applies.")]
    [SerializeField] private AudioClip victoryMusicNightmare;

    [Tooltip("If assigned, this will be used when difficulty == Nightmare (3) AND flawless == true.")]
    [SerializeField] private AudioClip victoryMusicNightmareFlawless;

    [Header("Playback")]
    [SerializeField, Range(0f, 3f)] private float fadeDuration = 0.6f;
    [SerializeField] private bool loopVictory = false;

    [Header("Scene Auto-Logic")]
    [Tooltip("Any scene name in this list will auto-play menuMusic on load.")]
    [SerializeField] private string[] menuSceneNames = new string[] { "MenuInicial", "MainMenu" };

    private Coroutine fadeRoutine;
    private MusicMode currentMode = MusicMode.None;
    private AudioClip currentClip = null;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!musicSource)
            musicSource = GetComponent<AudioSource>();

        if (!musicSource)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;

        SceneManager.sceneLoaded += OnSceneLoaded;

        ApplySceneMusic(SceneManager.GetActiveScene().name, force: true);
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        ApplySavedVolume();
    }

    // ---------------- PUBLIC API ----------------

    public MusicMode CurrentMode => currentMode;

    public void PlayMenuMusic(bool force = false) => RequestPlay(menuMusic, MusicMode.Menu, loop: true, force: force);
    public void PlayNormalMusic(bool force = false) => RequestPlay(normalMusic, MusicMode.Normal, loop: true, force: force);
    public void PlayBossMusic(bool force = false) => RequestPlay(bossMusic, MusicMode.Boss, loop: true, force: force);

    public void PlayVictoryMusic(bool force = false)
    {
        // Backwards-compatible default call
        RequestPlay(victoryMusic, MusicMode.Victory, loop: loopVictory, force: force);
    }

    /// <summary>
    /// Conditional victory music swap.
    /// difficulty: 0 Easy, 1 Normal, 2 Hard, 3 Nightmare
    /// flawless: true if the run qualifies for flawless victory (you decide the criteria)
    /// </summary>
    public void PlayVictoryMusic(int difficulty, bool flawless, bool force = false)
    {
        AudioClip chosen = ChooseVictoryClip(difficulty, flawless);
        RequestPlay(chosen, MusicMode.Victory, loop: loopVictory, force: force);
    }

    public void StopAllMusic(float fadeOutSeconds = 0f)
    {
        if (!musicSource) return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (fadeOutSeconds <= 0f)
        {
            musicSource.Stop();
            currentClip = null;
            currentMode = MusicMode.None;
            return;
        }

        fadeRoutine = StartCoroutine(FadeOutAndStop(fadeOutSeconds));
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (musicSource)
            musicSource.volume = volume;

        if (GameManager.Instance != null && GameManager.Instance.Settings != null)
            GameManager.Instance.Settings.musicVolume = volume;
    }

    public void ApplySavedVolume()
    {
        if (musicSource == null) return;

        if (GameManager.Instance != null && GameManager.Instance.Settings != null)
            musicSource.volume = Mathf.Clamp01(GameManager.Instance.Settings.musicVolume);
    }

    // ---------------- SCENE AUTO MUSIC ----------------

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneMusic(scene.name, force: false);
    }

    void ApplySceneMusic(string sceneName, bool force)
    {
        if (IsMenuScene(sceneName))
        {
            PlayMenuMusic(force: force);
        }
        else
        {
            // Default gameplay music unless boss/victory is already active.
            if (currentMode != MusicMode.Boss && currentMode != MusicMode.Victory)
                PlayNormalMusic(force: force);
        }
    }

    bool IsMenuScene(string sceneName)
    {
        if (menuSceneNames == null) return false;

        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(menuSceneNames[i]) && menuSceneNames[i] == sceneName)
                return true;
        }

        return false;
    }

    // ---------------- INTERNAL PLAYBACK ----------------

    AudioClip ChooseVictoryClip(int difficulty, bool flawless)
    {
        // Nightmare flawless override
        if (difficulty >= 3 && flawless && victoryMusicNightmareFlawless != null)
            return victoryMusicNightmareFlawless;

        // Nightmare variant
        if (difficulty >= 3 && victoryMusicNightmare != null)
            return victoryMusicNightmare;

        // Default
        return victoryMusic;
    }

    void RequestPlay(AudioClip clip, MusicMode mode, bool loop, bool force)
    {
        if (!musicSource || clip == null)
            return;

        // If already playing that exact clip and mode, do nothing
        if (!force && currentClip == clip && currentMode == mode && musicSource.isPlaying)
        {
            musicSource.loop = loop;
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (fadeDuration <= 0f)
        {
            PlayInstant(clip, mode, loop);
            return;
        }

        fadeRoutine = StartCoroutine(CrossfadeTo(clip, mode, loop, fadeDuration));
    }

    void PlayInstant(AudioClip clip, MusicMode mode, bool loop)
    {
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();

        currentClip = clip;
        currentMode = mode;
    }

    IEnumerator CrossfadeTo(AudioClip clip, MusicMode mode, bool loop, float seconds)
    {
        float startVolume = musicSource.volume;

        // Fade out
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            musicSource.volume = Mathf.Lerp(startVolume, 0f, k);
            yield return null;
        }

        musicSource.volume = 0f;

        // Switch clip
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();

        currentClip = clip;
        currentMode = mode;

        // Fade in to desired volume (respect settings)
        float targetVolume = startVolume;
        if (GameManager.Instance != null && GameManager.Instance.Settings != null)
            targetVolume = Mathf.Clamp01(GameManager.Instance.Settings.musicVolume);

        t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            musicSource.volume = Mathf.Lerp(0f, targetVolume, k);
            yield return null;
        }

        musicSource.volume = targetVolume;
        fadeRoutine = null;
    }

    IEnumerator FadeOutAndStop(float seconds)
    {
        float startVolume = musicSource.volume;

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            musicSource.volume = Mathf.Lerp(startVolume, 0f, k);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;

        currentClip = null;
        currentMode = MusicMode.None;
        fadeRoutine = null;
    }
}
