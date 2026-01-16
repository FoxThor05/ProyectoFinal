using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementIconLibrary : MonoBehaviour
{
    public static AchievementIconLibrary Instance { get; private set; }

    [Serializable]
    public class Entry
    {
        public string iconKey;
        public Sprite icon;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<string, Sprite> map;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildMap();
    }

    void OnValidate()
    {
        // Keep dictionary in sync while editing (and also while playing).
        if (Instance == null || Instance == this)
            BuildMap();
    }

    void BuildMap()
    {
        map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in entries)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.iconKey)) continue;

            map[e.iconKey.Trim()] = e.icon; // icon can be null until you assign it
        }
    }

    public Sprite GetIcon(string iconKey)
    {
        if (string.IsNullOrWhiteSpace(iconKey) || map == null)
            return null;

        return map.TryGetValue(iconKey.Trim(), out var s) ? s : null;
    }
}
