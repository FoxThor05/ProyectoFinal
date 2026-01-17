using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeatherCardUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dateText;

    [Header("Icon")]
    [SerializeField] private Image weatherIcon;

    [Header("Weather Icons")]
    [SerializeField] private Sprite clearIcon;
    [SerializeField] private Sprite cloudyIcon;
    [SerializeField] private Sprite rainIcon;
    [SerializeField] private Sprite snowIcon;
    [SerializeField] private Sprite unknownIcon;

    void Start()
    {
        UpdateAll();
        InvokeRepeating(nameof(UpdateTime), 0f, 1f);
    }

    void UpdateAll()
    {
        UpdateTime();
        UpdateWeather();
    }

    void UpdateTime()
    {
        if (!WeatherTimeService.Instance) return;

        timeText.text = WeatherTimeService.Instance.GetCurrentTime();
        dateText.text = WeatherTimeService.Instance.GetCurrentDate();
    }

    void UpdateWeather()
    {
        if (!WeatherTimeService.Instance) return;

        weatherIcon.sprite = WeatherTimeService.Instance.CurrentWeather switch
        {
            WeatherType.Clear => clearIcon,
            WeatherType.Cloudy => cloudyIcon,
            WeatherType.Rain => rainIcon,
            WeatherType.Snow => snowIcon,
            _ => unknownIcon
        };
    }
}
