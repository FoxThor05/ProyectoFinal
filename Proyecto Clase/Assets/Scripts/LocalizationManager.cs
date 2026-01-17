using System;
using System.Globalization;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public enum Language
    {
        English = 0,
        Spanish = 1
    }

    [Header("Table")]
    [SerializeField] private LocalizationTableAsset table;

    [Header("Language")]
    [SerializeField] private Language currentLanguage = Language.English;

    [Header("Exceptions / Ignore Roots")]
    [Tooltip("Any TMP text under these roots will be ignored by auto-apply tooling and can be ignored by LocalizedText.")]
    [SerializeField] private Transform[] ignoreRoots;

    private const string PREF_LANG = "language";

    public event Action OnLanguageChanged;

    public Language Current => currentLanguage;
    public LocalizationTableAsset Table => table;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLanguage();

        if (table != null)
            table.RebuildLookup();
    }

    public void SetLanguage(Language lang)
    {
        if (currentLanguage == lang) return;

        currentLanguage = lang;
        PlayerPrefs.SetInt(PREF_LANG, (int)currentLanguage);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();
    }

    public string Get(string key, string fallbackEnglish = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallbackEnglish ?? string.Empty;

        if (table == null)
            return fallbackEnglish ?? $"[{key}]";

        if (!table.TryGet(key, out var entry) || entry == null)
            return fallbackEnglish ?? $"[{key}]";

        string result = currentLanguage switch
        {
            Language.English => entry.english,
            Language.Spanish => entry.spanish,
            _ => entry.english
        };

        // Fallback behavior:
        // - If Spanish missing, show English
        // - If English missing, show fallback or [key]
        if (string.IsNullOrEmpty(result))
        {
            if (currentLanguage == Language.Spanish && !string.IsNullOrEmpty(entry.english))
                return entry.english;

            return fallbackEnglish ?? $"[{key}]";
        }

        return result;
    }

    public bool IsUnderIgnoreRoot(Transform t)
    {
        if (ignoreRoots == null || ignoreRoots.Length == 0 || t == null) return false;

        for (int i = 0; i < ignoreRoots.Length; i++)
        {
            if (!ignoreRoots[i]) continue;
            if (t == ignoreRoots[i] || t.IsChildOf(ignoreRoots[i]))
                return true;
        }

        return false;
    }

    void LoadLanguage()
    {
        if (!PlayerPrefs.HasKey(PREF_LANG)) return;
        currentLanguage = (Language)Mathf.Clamp(PlayerPrefs.GetInt(PREF_LANG), 0, 1);
    }
}
