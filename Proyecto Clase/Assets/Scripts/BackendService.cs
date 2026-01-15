using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class BackendService : MonoBehaviour
{
    public static BackendService Instance;

    [SerializeField] string supabaseUrl;
    [SerializeField] string apiKey;

    private string accessToken;
    private string refreshToken;
    private string userId;

    public bool IsLoggedIn => !string.IsNullOrEmpty(accessToken);

    [System.Serializable]
    public class AuthUser
    {
        public string id;
        public string email;
    }

    [System.Serializable]
    public class AuthResponse
    {
        public string access_token;
        public string refresh_token;
        public AuthUser user;
    }

    [System.Serializable]
    class AchievementListWrapper
    {
        public AchievementDTO[] items;
    }

    [System.Serializable]
    class UserAchievementDTO
    {
        public string achievement_id;
    }

    [System.Serializable]
    class UserAchievementWrapper
    {
        public UserAchievementDTO[] items;
    }

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

    // ---------------- LOGIN ----------------
    public void Login(string username, string password, System.Action<bool, string> callback)
    {
        StartCoroutine(LoginRoutine(username, password, callback));
    }

    IEnumerator LoginRoutine(string username, string password, System.Action<bool, string> callback)
    {
        string email = username + "@fakeemail.local";
        string url = $"{supabaseUrl}/auth/v1/token?grant_type=password";
        string json = $"{{\"email\":\"{email}\",\"password\":\"{password}\"}}";

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            callback(false, req.downloadHandler.text);
            yield break;
        }

        var response = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);

        accessToken = response.access_token;
        refreshToken = response.refresh_token;
        userId = response.user.id;

        var profile = new UserProfile
        {
            id = response.user.id,
            username = username,
            accessToken = accessToken,
            refreshToken = refreshToken
        };

        UserManager.Instance.SetUser(profile);

        PlayerPrefs.SetString("refresh_token", refreshToken);

        callback(true, null);
    }

    // ---------------- REGISTER ----------------
    public void Register(string username, string password, System.Action<bool, string> callback)
    {
        StartCoroutine(RegisterRoutine(username, password, callback));
    }

    IEnumerator InsertProfileRoutine(string username)
    {
        string json = $"{{ \"id\": \"{userId}\", \"username\": \"{username}\" }}";

        var req = new UnityWebRequest($"{supabaseUrl}/rest/v1/profiles", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("Profile insert failed: " + req.downloadHandler.text);
    }

    IEnumerator RegisterRoutine(string username, string password, System.Action<bool, string> callback)
    {
        string email = username + "@fakeemail.local";
        string url = $"{supabaseUrl}/auth/v1/signup";
        string json = $"{{\"email\":\"{email}\",\"password\":\"{password}\"}}";

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            callback(false, req.downloadHandler.text);
            yield break;
        }

        var response = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
        StartCoroutine(InsertProfileRoutine(username));

        var profile = new UserProfile
        {
            id = response.user.id,
            username = username,
            accessToken = response.access_token,
            refreshToken = response.refresh_token
        };

        UserManager.Instance.SetUser(profile);
        PlayerPrefs.SetString("refresh_token", response.refresh_token);

        callback(true, null);
    }

    // ---------------- LOGOUT ----------------
    public void Logout()
    {
        accessToken = null;
        refreshToken = null;
        userId = null;
        UserManager.Instance.Logout();
    }

    // ---------------- ACHIEVEMENTS ----------------
    public void SendAchievementUnlock(string achievementCode)
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("Guest mode: achievements not sent.");
            return;
        }

        StartCoroutine(PostAchievement(achievementCode));
    }

    IEnumerator PostAchievement(string achievementCode)
    {
        string url = supabaseUrl + "/rest/v1/rpc/unlock_achievement";
        string json = $"{{ \"_achievement_id\": \"{achievementCode}\" }}";

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("Achievement unlock failed: " + req.downloadHandler.text);
        }
    }

    public void FetchAllAchievements(System.Action<AchievementDTO[]> callback)
    {
        StartCoroutine(FetchAllAchievementsRoutine(callback));
    }

    IEnumerator FetchAllAchievementsRoutine(System.Action<AchievementDTO[]> callback)
    {
        var req = UnityWebRequest.Get(
            $"{supabaseUrl}/rest/v1/achievements?select=id,name,description"
        );

        req.SetRequestHeader("apikey", apiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fetch achievements failed: " + req.downloadHandler.text);
            callback?.Invoke(null);
            yield break;
        }

        // JsonUtility cannot parse arrays directly
        string wrapped = $"{{\"items\":{req.downloadHandler.text}}}";
        var data = JsonUtility.FromJson<AchievementListWrapper>(wrapped);

        callback?.Invoke(data.items);
    }

    public void FetchUnlockedAchievements(System.Action<HashSet<string>> callback)
    {
        if (!IsLoggedIn)
        {
            callback?.Invoke(new HashSet<string>());
            return;
        }

        StartCoroutine(FetchUnlockedAchievementsRoutine(callback));
    }

    IEnumerator FetchUnlockedAchievementsRoutine(System.Action<HashSet<string>> callback)
    {
        var req = UnityWebRequest.Get(
            $"{supabaseUrl}/rest/v1/user_achievements?select=achievement_id"
        );

        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fetch user achievements failed: " + req.downloadHandler.text);
            callback?.Invoke(new HashSet<string>());
            yield break;
        }

        string wrapped = $"{{\"items\":{req.downloadHandler.text}}}";
        var data = JsonUtility.FromJson<UserAchievementWrapper>(wrapped);

        HashSet<string> unlocked = new();
        foreach (var a in data.items)
            unlocked.Add(a.achievement_id);

        callback?.Invoke(unlocked);
    }

    // ---------------- GUEST LOGIN ----------------
    public void PlayAsGuest(System.Action callback)
    {
        accessToken = null;
        refreshToken = null;
        userId = null;

        UserManager.Instance.SetUser(new UserProfile
        {
            username = "Guest"
        });

        callback?.Invoke();
    }
}
