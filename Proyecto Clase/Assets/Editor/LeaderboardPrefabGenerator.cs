#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class LeaderboardPrefabGenerator
{
    private const string DefaultPrefabPath = "Assets/Prefabs/UI/LeaderboardItem.prefab";

    [MenuItem("Tools/Proyecto Clase/Generate Leaderboard Row Prefab")]
    public static void GenerateLeaderboardRowPrefab()
    {
        // Ensure folders exist
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/UI");

        // Root
        var root = new GameObject("LeaderboardItem", typeof(RectTransform));
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(900f, 90f);

        // Background (optional)
        var bg = root.AddComponent<Image>();
        bg.raycastTarget = true;

        // Layout: fixed row height for stability
        var le = root.AddComponent<LayoutElement>();
        le.preferredHeight = 90f;
        le.flexibleHeight = 0f;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 8, 8);
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Avatar
        var avatarGO = CreateUIChild(root.transform, "Avatar", typeof(Image), typeof(LayoutElement));
        var avatarImg = avatarGO.GetComponent<Image>();
        avatarImg.raycastTarget = false;
        avatarImg.preserveAspect = true;

        var avatarLE = avatarGO.GetComponent<LayoutElement>();
        avatarLE.preferredWidth = 48f;
        avatarLE.preferredHeight = 48f;
        avatarLE.flexibleWidth = 0f;
        avatarLE.flexibleHeight = 0f;

        // Right column
        var rightColumn = CreateUIChild(root.transform, "RightColumn", typeof(VerticalLayoutGroup));
        var rightVLG = rightColumn.GetComponent<VerticalLayoutGroup>();
        rightVLG.spacing = 6f;
        rightVLG.childAlignment = TextAnchor.UpperLeft;
        rightVLG.childControlWidth = true;
        rightVLG.childControlHeight = true;
        rightVLG.childForceExpandWidth = false;
        rightVLG.childForceExpandHeight = false;

        // Make right column take remaining width
        var rightLE = rightColumn.AddComponent<LayoutElement>();
        rightLE.flexibleWidth = 1f;

        // Top row (username + score)
        var topRow = CreateUIChild(rightColumn.transform, "TopRow", typeof(HorizontalLayoutGroup));
        var topHLG = topRow.GetComponent<HorizontalLayoutGroup>();
        topHLG.spacing = 8f;
        topHLG.childAlignment = TextAnchor.MiddleLeft;
        topHLG.childControlWidth = true;
        topHLG.childControlHeight = true;
        topHLG.childForceExpandWidth = false;
        topHLG.childForceExpandHeight = false;

        // Username TMP
        var usernameGO = CreateTMP(topRow.transform, "Username");
        var usernameTMP = usernameGO.GetComponent<TMP_Text>();
        usernameTMP.text = "Username";
        usernameTMP.overflowMode = TextOverflowModes.Ellipsis;
        usernameTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var usernameLE = usernameGO.AddComponent<LayoutElement>();
        usernameLE.flexibleWidth = 1f;

        // Score TMP
        var scoreGO = CreateTMP(topRow.transform, "Score");
        var scoreTMP = scoreGO.GetComponent<TMP_Text>();
        scoreTMP.text = "0";
        scoreTMP.alignment = TextAlignmentOptions.MidlineRight;

        var scoreLE = scoreGO.AddComponent<LayoutElement>();
        scoreLE.preferredWidth = 80f;
        scoreLE.flexibleWidth = 0f;

        // Achievement icons strip
        var strip = CreateUIChild(rightColumn.transform, "AchievementsStrip", typeof(HorizontalLayoutGroup));
        var stripHLG = strip.GetComponent<HorizontalLayoutGroup>();
        stripHLG.spacing = 6f;
        stripHLG.childAlignment = TextAnchor.MiddleLeft;
        stripHLG.childControlWidth = true;
        stripHLG.childControlHeight = true;
        stripHLG.childForceExpandWidth = false;
        stripHLG.childForceExpandHeight = false;

        // Create 9 icon slots
        var iconImages = new List<Image>(9);
        for (int i = 1; i <= 9; i++)
        {
            var iconGO = CreateUIChild(strip.transform, $"Icon{i}", typeof(Image), typeof(LayoutElement));
            var img = iconGO.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;

            var iconLE = iconGO.GetComponent<LayoutElement>();
            iconLE.preferredWidth = 20f;
            iconLE.preferredHeight = 20f;
            iconLE.flexibleWidth = 0f;
            iconLE.flexibleHeight = 0f;

            iconImages.Add(img);
        }

        // Add your row script and auto-wire references
        var rowScript = root.AddComponent<LeaderboardItemUI>();

        var so = new SerializedObject(rowScript);
        so.FindProperty("avatarImage").objectReferenceValue = avatarImg;
        so.FindProperty("usernameText").objectReferenceValue = usernameTMP;
        so.FindProperty("scoreText").objectReferenceValue = scoreTMP;

        var iconsProp = so.FindProperty("achievementIconSlots");
        iconsProp.arraySize = iconImages.Count;
        for (int i = 0; i < iconImages.Count; i++)
            iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = iconImages[i];

        so.ApplyModifiedPropertiesWithoutUndo();

        // Save prefab
        var prefabPath = DefaultPrefabPath;
        var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        if (saved != null)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = saved;
            Debug.Log($"Leaderboard row prefab generated at: {prefabPath}");
        }
        else
        {
            Debug.LogError("Failed to save LeaderboardItem prefab.");
        }
    }

    private static GameObject CreateUIChild(Transform parent, string name, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        foreach (var t in components)
            go.AddComponent(t);

        // Basic sane defaults for RectTransform
        var rt = go.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;

        return go;
    }

    private static GameObject CreateTMP(Transform parent, string name)
    {
        var go = CreateUIChild(parent, name, typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 24f);
        return go;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
