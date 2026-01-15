using UnityEngine;

public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get; private set; }

    public UserProfile CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null;

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

    public void SetUser(UserProfile profile)
    {
        CurrentUser = profile;
    }

    public void Logout()
    {
        CurrentUser = null;
        PlayerPrefs.DeleteKey("refresh_token");
    }

    public bool LoadUserFromPrefs()
    {
        string refreshToken = PlayerPrefs.GetString("refresh_token", null);
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        // Just store the refresh token for now; token refresh could be implemented later
        CurrentUser = new UserProfile { refreshToken = refreshToken, username = "Guest" };
        return true;
    }
}
