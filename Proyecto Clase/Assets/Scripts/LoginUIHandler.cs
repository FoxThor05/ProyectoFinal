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
            if (success)
                feedbackText.text = "Logged in!";
            else
                feedbackText.text = "Login failed: " + message;
        });
        BackendService.Instance.FetchUnlockedAchievements(unlocked =>
        {
            foreach (var id in unlocked)
                AchievementManager.Instance.Unlock(id);
        });

    }

    void OnRegisterClicked()
    {
        feedbackText.text = "Registering...";
        string username = usernameField.text.Trim();
        string password = passwordField.text;

        BackendService.Instance.Register(username, password, (success, message) =>
        {
            if (success)
                feedbackText.text = "Registered and logged in!";
            else
                feedbackText.text = "Register failed: " + message;
        });
    }

    void OnGuestClicked()
    {
        BackendService.Instance.PlayAsGuest(() =>
        {
            feedbackText.text = "Playing as Guest";
        });
    }

    public void Back()
    {
        gameObject.SetActive(false);
    }
}
