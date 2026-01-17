using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WeatherTimeService : MonoBehaviour
{
    public static WeatherTimeService Instance { get; private set; }

    [Header("Weather API")]
    [SerializeField] private string apiKey = "PUT_API_KEY_HERE";
    [SerializeField] private string city = "Barcelona";
    [SerializeField] private string countryCode = "ES";

    private const string PREF_WEATHER = "cached_weather";
    private const string PREF_WEATHER_TIME = "cached_weather_time";

    public WeatherType CurrentWeather { get; private set; } = WeatherType.Unknown;

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

    public string GetCurrentTime() =>
        DateTime.Now.ToString("HH:mm");

    public string GetCurrentDate() =>
        DateTime.Now.ToString("dddd, dd MMM");

    // ---------------- WEATHER ----------------

    IEnumerator FetchWeatherIfNeeded()
    {
        // 30 minutes cache
        if (PlayerPrefs.HasKey(PREF_WEATHER_TIME))
        {
            long ticks = long.Parse(PlayerPrefs.GetString(PREF_WEATHER_TIME));
            var last = new DateTime(ticks);

            if ((DateTime.Now - last).TotalMinutes < 30)
                yield break;
        }

        string url =
            $"https://api.openweathermap.org/data/2.5/weather?q={city},{countryCode}&appid={apiKey}";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Weather API error: {req.error}");
            yield break;
        }

        var data = JsonUtility.FromJson<WeatherResponse>(req.downloadHandler.text);
        if (data.weather == null || data.weather.Length == 0)
            yield break;

        CurrentWeather = ParseWeather(data.weather[0].main);

        PlayerPrefs.SetString(PREF_WEATHER, CurrentWeather.ToString());
        PlayerPrefs.SetString(PREF_WEATHER_TIME, DateTime.Now.Ticks.ToString());
    }

    void LoadCachedWeather()
    {
        if (PlayerPrefs.HasKey(PREF_WEATHER))
        {
            Enum.TryParse(
                PlayerPrefs.GetString(PREF_WEATHER),
                out WeatherType cached);
            CurrentWeather = cached;
        }
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
