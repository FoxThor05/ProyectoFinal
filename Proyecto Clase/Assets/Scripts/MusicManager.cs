using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip menuMusic; // New: Menu music clip

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Play menu music immediately since game starts at main menu
        PlayMenuMusic();
    }

    void Start()
    {
        // Apply saved volume on startup
        if (GameManager.Instance != null)
            musicSource.volume = GameManager.Instance.Settings.musicVolume;
    }

    // ---------------- MUSIC CONTROL ----------------

    public void PlayNormalMusic()
    {
        Play(normalMusic);
    }

    public void PlayBossMusic()
    {
        Play(bossMusic);
    }

    public void PlayMenuMusic()
    {
        Play(menuMusic);
    }

    void Play(AudioClip clip)
    {
        if (!clip) return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopNormalMusic()
    {
        if (musicSource.clip == normalMusic)
            musicSource.Stop();
    }

    public void StopBossMusic()
    {
        if (musicSource.clip == bossMusic)
            musicSource.Stop();
    }

    public void StopAllMusic()
    {
        musicSource.Stop();
    }

    // ---------------- VOLUME API ----------------

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        musicSource.volume = volume;

        if (GameManager.Instance != null)
            GameManager.Instance.Settings.musicVolume = volume;
    }
}
