#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WeatherCardPrefabGenerator
{
    [MenuItem("Tools/Proyecto Clase/Generate Weather Card UI")]
    public static void Generate()
    {
        var root = new GameObject("WeatherCard", typeof(RectTransform));
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 90);

        root.AddComponent<Image>();

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;

        // Icon
        var iconGO = CreateChild(root.transform, "Icon");
        var icon = iconGO.AddComponent<Image>();
        iconGO.GetComponent<RectTransform>().sizeDelta = new Vector2(48, 48);

        // Text column
        var textCol = CreateChild(root.transform, "TextColumn");
        var vlg = textCol.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;

        var time = CreateTMP(textCol.transform, "Time", "12:34", 26);
        var date = CreateTMP(textCol.transform, "Date", "Monday, 01 Jan", 14);

        // Script
        var ui = root.AddComponent<WeatherCardUI>();
        var so = new SerializedObject(ui);

        so.FindProperty("timeText").objectReferenceValue = time;
        so.FindProperty("dateText").objectReferenceValue = date;
        so.FindProperty("weatherIcon").objectReferenceValue = icon;

        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/UI/WeatherCard.prefab");
        Object.DestroyImmediate(root);

        Debug.Log("WeatherCard prefab generated at Assets/Prefabs/UI/WeatherCard.prefab");
    }

    static GameObject CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static TMP_Text CreateTMP(Transform parent, string name, string text, int size)
    {
        var go = CreateChild(parent, name);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        return tmp;
    }
}
#endif
