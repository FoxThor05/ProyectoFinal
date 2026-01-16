using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUIHandler : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button guestButton;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        guestButton.onClick.AddListener(OnGuestClicked);
    }

    void OnLoginClicked()
    {
        feedbackText.text = "Logging in...";
        string username = usernameField.text.Trim();
        string password = passwordField.text;

        BackendService.Instance.Login(username, password, (success, message) =>
        {
            if (!success)
            {
                feedbackText.text = "Login failed: " + message;
                return;
            }

            feedbackText.text = "Logged in!";

            // Fetch achievements ONLY after login succeeded (token is now set)
            BackendService.Instance.FetchUnlockedAchievements(unlocked =>
            {
                AchievementManager.Instance.SetUnlockedAchievements(unlocked);
            });
        });
    }

    void OnRegisterClicked()
    {
        feedbackText.text = "Registering...";
        string username = usernameField.text.Trim();
        string password = passwordField.text;

        BackendService.Instance.Register(username, password, (success, message) =>
        {
            if (!success)
            {
                feedbackText.text = "Register failed: " + message;
                return;
            }

            feedbackText.text = "Registered and logged in!";

            BackendService.Instance.FetchUnlockedAchievements(unlocked =>
            {
                AchievementManager.Instance.SetUnlockedAchievements(unlocked);
            });
        });
    }

    void OnGuestClicked()
    {
        BackendService.Instance.PlayAsGuest(() =>
        {
            feedbackText.text = "Playing as Guest";
            AchievementManager.Instance.SetUnlockedAchievements(new string[0]);
        });
    }

    public void Back()
    {
        gameObject.SetActive(false);
    }
}
