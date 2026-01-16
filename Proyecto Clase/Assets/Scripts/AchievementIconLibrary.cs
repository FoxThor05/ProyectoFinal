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

    [SerializeField] private List<Entry> entries = new();

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

        map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.iconKey) || e.icon == null) continue;
            map[e.iconKey] = e.icon;
        }
    }

    public Sprite GetIcon(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey) || map == null) return null;
        return map.TryGetValue(iconKey, out var s) ? s : null;
    }
}
