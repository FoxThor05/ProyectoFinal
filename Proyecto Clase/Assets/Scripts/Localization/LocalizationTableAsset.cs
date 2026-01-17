using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Proyecto Clase/Localization Table", fileName = "LocalizationTable")]
public class LocalizationTableAsset : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;
        [TextArea(1, 4)] public string english;
        [TextArea(1, 4)] public string spanish;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, Entry> lookup;

    public IReadOnlyList<Entry> Entries => entries;

    public void RebuildLookup()
    {
        lookup = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            lookup[e.key.Trim()] = e;
        }
    }

    public bool TryGet(string key, out Entry entry)
    {
        if (lookup == null) RebuildLookup();
        return lookup.TryGetValue(key.Trim(), out entry);
    }

#if UNITY_EDITOR
    // Editor-only helper: add or update an entry
    public void Upsert(string key, string english, string spanishIfNew = "")
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        key = key.Trim();

        // Find existing
        foreach (var e in entries)
        {
            if (e == null) continue;
            if (string.Equals(e.key?.Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                // Only update English if currently empty (avoid stomping your work)
                if (string.IsNullOrEmpty(e.english))
                    e.english = english;

                return;
            }
        }

        // Add new
        entries.Add(new Entry
        {
            key = key,
            english = english,
            spanish = spanishIfNew
        });
    }
#endif
}
