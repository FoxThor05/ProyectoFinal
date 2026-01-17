#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalizationAutoApply
{
    [MenuItem("Tools/Proyecto Clase/Localization/Auto-Apply LocalizedText In Scene")]
    public static void AutoApplyInScene()
    {
        var manager = Object.FindObjectOfType<LocalizationManager>();
        if (manager == null)
        {
            Debug.LogError("[LocalizationAutoApply] No LocalizationManager found in the scene.");
            return;
        }

        if (manager.Table == null)
        {
            Debug.LogError("[LocalizationAutoApply] LocalizationManager.Table is not assigned. Create a LocalizationTable asset and assign it.");
            return;
        }

        var table = manager.Table;

        var allTMP = Object.FindObjectsOfType<TMP_Text>(true);

        int added = 0;
        int skipped = 0;
        int updatedTable = 0;

        Undo.RecordObject(table, "Localization table update");

        foreach (var tmp in allTMP)
        {
            if (tmp == null) continue;

            // Skip ignored roots
            if (manager.IsUnderIgnoreRoot(tmp.transform))
            {
                skipped++;
                continue;
            }

            // Skip empty text (often layout placeholders)
            var currentText = tmp.text?.Trim();
            if (string.IsNullOrEmpty(currentText))
            {
                skipped++;
                continue;
            }

            // Ensure component exists
            var lt = tmp.GetComponent<LocalizedText>();
            if (lt == null)
            {
                lt = Undo.AddComponent<LocalizedText>(tmp.gameObject);
                added++;
            }

            // Stable key based on hierarchy path (sceneName/objectPath)
            string key = BuildKeyFor(tmp.gameObject);

            // Set key (editor-only setter)
            Undo.RecordObject(lt, "Set LocalizedText key");
            lt.EditorSetKey(key);

            // Populate table: english = current text, spanish blank (you fill later)
            table.Upsert(key, currentText, "");
            updatedTable++;
        }

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();

        Debug.Log($"[LocalizationAutoApply] Scene: {SceneManager.GetActiveScene().name} | TMP found: {allTMP.Length} | Added LocalizedText: {added} | Skipped: {skipped} | Table upserts: {updatedTable}");
    }

    static string BuildKeyFor(GameObject go)
    {
        // Example key: "MenuInicial.Canvas.MainMenu.NewGameButton.Label"
        string scene = SceneManager.GetActiveScene().name;

        string path = go.name;
        var t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "." + path;
        }

        // Clean it a bit
        path = path.Replace(" ", "_");

        return $"{scene}.{path}";
    }
}
#endif
