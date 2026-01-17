using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    [Tooltip("If true, this component will do nothing if it's under a LocalizationManager ignoreRoot.")]
    [SerializeField] private bool respectIgnoreRoots = true;

    private TMP_Text tmp;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        Apply();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Apply;
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Apply;
    }

    public void Apply()
    {
        if (!tmp) tmp = GetComponent<TMP_Text>();
        if (LocalizationManager.Instance == null) return;

        if (respectIgnoreRoots && LocalizationManager.Instance.IsUnderIgnoreRoot(transform))
            return;

        tmp.text = LocalizationManager.Instance.Get(key, tmp.text);
    }

    public string Key => key;

#if UNITY_EDITOR
    // Used by the auto-applier to set keys safely
    public void EditorSetKey(string newKey)
    {
        key = newKey;
    }
#endif
}
