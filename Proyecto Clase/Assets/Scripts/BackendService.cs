using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
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

    // ---------------- DTOs ----------------
    [System.Serializable]
    public class AchievementListWrapper
    {
        public AchievementDTO[] items;
    }

    [System.Serializable]
    public class UserAchievementWrapper
    {
        public UserAchievementDTO[] items;
    }

    // ---------------- AUTH ----------------
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

    // RPC payload for insert_profile(username)
    [System.Serializable]
    private class InsertProfilePayload
    {
        public string _username;
        public InsertProfilePayload(string username) => _username = username;
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

        // Ensure profile exists (safe on conflict)
        yield return StartCoroutine(EnsureProfileRoutine(username));

        callback(true, null);
    }

    // ---------------- REGISTER ----------------
    public void Register(string username, string password, System.Action<bool, string> callback)
    {
        StartCoroutine(RegisterRoutine(username, password, callback));
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

        yield return StartCoroutine(EnsureProfileRoutine(username));

        callback(true, null);
    }

    // ---------------- PROFILE UPSERT (RPC) ----------------
    IEnumerator EnsureProfileRoutine(string username)
    {
        if (!IsLoggedIn)
            yield break;

        string url = $"{supabaseUrl}/rest/v1/rpc/insert_profile";
        string json = JsonUtility.ToJson(new InsertProfilePayload(username));

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("EnsureProfile RPC failed: " + req.downloadHandler.text);
        }
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
        // UPDATED: includes icon_key
        var req = UnityWebRequest.Get(
            $"{supabaseUrl}/rest/v1/achievements?select=id,name,description,rarity,icon_key"
        );

        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Accept", "application/json");
        if (IsLoggedIn)
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fetch achievements failed: " + req.downloadHandler.text);
            callback?.Invoke(null);
            yield break;
        }

        string wrapped = $"{{\"items\":{req.downloadHandler.text}}}";
        var data = JsonUtility.FromJson<AchievementListWrapper>(wrapped);

        callback?.Invoke(data?.items ?? new AchievementDTO[0]);
    }

    public void FetchUnlockedAchievements(System.Action<string[]> callback)
    {
        if (!IsLoggedIn)
        {
            callback?.Invoke(new string[0]);
            return;
        }

        StartCoroutine(FetchUnlockedAchievementsRoutine(callback));
    }

    IEnumerator FetchUnlockedAchievementsRoutine(System.Action<string[]> callback)
    {
        var req = UnityWebRequest.Get(
            $"{supabaseUrl}/rest/v1/user_achievements?select=achievement_id"
        );

        req.SetRequestHeader("apikey", apiKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fetch user achievements failed: " + req.downloadHandler.text);
            callback?.Invoke(new string[0]);
            yield break;
        }

        string wrapped = $"{{\"items\":{req.downloadHandler.text}}}";
        var data = JsonUtility.FromJson<UserAchievementWrapper>(wrapped);

        string[] unlockedIds = new string[data.items.Length];
        for (int i = 0; i < data.items.Length; i++)
            unlockedIds[i] = data.items[i].achievement_id;

        callback?.Invoke(unlockedIds);
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
