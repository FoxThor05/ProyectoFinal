using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public class WeatherTimeService : MonoBehaviour
{
    public static WeatherTimeService Instance { get; private set; }

    [Header("Time / Date Formatting")]
    [Tooltip("If true, the date is always shown in English (en-US). If false, it uses the device language.")]
    [SerializeField] private bool forceEnglishDate = true;

    [Header("Weather API (OpenWeatherMap)")]
    [SerializeField] private string apiKey = "PUT_API_KEY_HERE";
    [SerializeField] private string city = "Barcelona";
    [SerializeField] private string countryCode = "ES";
    [Tooltip("Cache duration in minutes to avoid hitting API limits.")]
    [SerializeField] private int cacheMinutes = 30;

    [Header("Debug")]
    [Tooltip("If enabled, forces a weather refresh on Start (ignores cache). Useful for testing.")]
    [SerializeField] private bool forceRefreshOnStart = false;
    [SerializeField] private bool verboseLogs = true;

    private const string PREF_WEATHER = "cached_weather";
    private const string PREF_WEATHER_TIME = "cached_weather_time_ticks";

    public WeatherType CurrentWeather { get; private set; } = WeatherType.Unknown;

    private CultureInfo EnglishCulture => CultureInfo.GetCultureInfo("en-US");

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadCachedWeather();
        StartCoroutine(FetchWeatherIfNeeded());
    }

    // ---------------- TIME ----------------

    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("HH:mm");
    }

    public string GetCurrentDate()
    {
        var culture = forceEnglishDate ? EnglishCulture : CultureInfo.CurrentCulture;
        return DateTime.Now.ToString("dddd, dd MMM", culture);
    }

    // ---------------- WEATHER ----------------

    IEnumerator FetchWeatherIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "PUT_API_KEY_HERE")
        {
            if (verboseLogs)
                Debug.LogWarning("[WeatherTimeService] OpenWeather apiKey is missing. Weather will remain Unknown.");
            yield break;
        }

        if (!forceRefreshOnStart && IsCacheValid())
        {
            if (verboseLogs)
                Debug.Log("[WeatherTimeService] Using cached weather (cache still valid).");
            yield break;
        }

        string query = string.IsNullOrWhiteSpace(countryCode)
            ? city
            : $"{city},{countryCode}";

        // metric units optional; doesn't affect "weather.main" but keeps payload conventional
        string url = $"https://api.openweathermap.org/data/2.5/weather?q={UnityWebRequest.EscapeURL(query)}&appid={apiKey}&units=metric";

        if (verboseLogs)
            Debug.Log($"[WeatherTimeService] Fetching weather: {url.Replace(apiKey, "****")}");

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[WeatherTimeService] Weather API request failed: {req.error}\nResponse: {req.downloadHandler.text}");
            yield break;
        }

        // OpenWeather sometimes returns 200 with error JSON for bad queries? Usually it uses non-200,
        // but we’ll still try to parse safely.
        WeatherResponse data = null;
        try
        {
            data = JsonUtility.FromJson<WeatherResponse>(req.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WeatherTimeService] JSON parse failed: {ex.Message}\nRaw: {req.downloadHandler.text}");
            yield break;
        }

        if (data == null || data.weather == null || data.weather.Length == 0)
        {
            Debug.LogWarning($"[WeatherTimeService] Weather payload did not include 'weather[0].main'. Raw: {req.downloadHandler.text}");
            yield break;
        }

        CurrentWeather = ParseWeather(data.weather[0].main);

        // Only write cache on SUCCESS
        PlayerPrefs.SetString(PREF_WEATHER, CurrentWeather.ToString());
        PlayerPrefs.SetString(PREF_WEATHER_TIME, DateTime.Now.Ticks.ToString());
        PlayerPrefs.Save();

        if (verboseLogs)
            Debug.Log($"[WeatherTimeService] Weather updated: {CurrentWeather}");
    }

    bool IsCacheValid()
    {
        if (!PlayerPrefs.HasKey(PREF_WEATHER_TIME))
            return false;

        string ticksStr = PlayerPrefs.GetString(PREF_WEATHER_TIME, "");
        if (!long.TryParse(ticksStr, out long ticks) || ticks <= 0)
        {
            // Bad cache data; treat as invalid and clean it up.
            PlayerPrefs.DeleteKey(PREF_WEATHER_TIME);
            return false;
        }

        DateTime last;
        try
        {
            last = new DateTime(ticks);
        }
        catch
        {
            PlayerPrefs.DeleteKey(PREF_WEATHER_TIME);
            return false;
        }

        return (DateTime.Now - last).TotalMinutes < Mathf.Max(1, cacheMinutes);
    }

    void LoadCachedWeather()
    {
        if (!PlayerPrefs.HasKey(PREF_WEATHER))
            return;

        string s = PlayerPrefs.GetString(PREF_WEATHER, WeatherType.Unknown.ToString());
        if (Enum.TryParse(s, out WeatherType cached))
            CurrentWeather = cached;
        else
            CurrentWeather = WeatherType.Unknown;
    }

    WeatherType ParseWeather(string main) =>
        main.ToLower() switch
        {
            "clear" => WeatherType.Clear,
            "clouds" => WeatherType.Cloudy,
            "rain" or "drizzle" or "thunderstorm" => WeatherType.Rain,
            "snow" => WeatherType.Snow,
            _ => WeatherType.Unknown
        };
}

// ---------------- DATA ----------------

[Serializable]
public class WeatherResponse
{
    public WeatherInfo[] weather;
}

[Serializable]
public class WeatherInfo
{
    public string main;
}

public enum WeatherType
{
    Clear,
    Cloudy,
    Rain,
    Snow,
    Unknown
}
