using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class SecretOverlayReveal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap overlayTilemap;
    [SerializeField] private Collider2D overlayCollider; // TilemapCollider2D or CompositeCollider2D

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Fade")]
    [Range(0f, 1f)]
    [SerializeField] private float revealedAlpha = 0.12f;

    [Range(0.01f, 2f)]
    [SerializeField] private float fadeDuration = 0.25f;

    [Tooltip("Small grace period to prevent flicker if the player barely grazes the collider edge.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float exitGraceSeconds = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool touching;
    private float lastTouchTime = -999f;

    private float currentAlpha = 1f;
    private float targetAlpha = 1f;
    private Coroutine fadeRoutine;

    void Awake()
    {
        if (!overlayTilemap)
            overlayTilemap = GetComponent<Tilemap>();

        if (!overlayCollider)
            overlayCollider = GetComponent<Collider2D>();

        if (!overlayCollider)
            Debug.LogError("[SecretOverlayWholeFade] Missing overlayCollider. Add TilemapCollider2D/CompositeCollider2D and drag it here.");

        // Ensure we can tint per-cell colors if needed
        overlayTilemap.color = Color.white;
    }

    void OnEnable()
    {
        // Start fully opaque
        ApplyAlphaImmediate(1f);
        touching = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        touching = true;
        lastTouchTime = Time.time;

        SetTarget(revealedAlpha);

        if (debugLogs) Debug.Log("[SecretOverlayWholeFade] Trigger Enter -> Fade OUT overlay");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        touching = true;
        lastTouchTime = Time.time;

        // Keep target at revealed while staying
        SetTarget(revealedAlpha);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        touching = false;
        lastTouchTime = Time.time; // used for grace

        if (debugLogs) Debug.Log("[SecretOverlayWholeFade] Trigger Exit (pending grace) -> Fade IN overlay");
    }

    void Update()
    {
        // Grace-based restore to avoid flicker
        bool shouldBeRevealed = touching || (Time.time - lastTouchTime) <= exitGraceSeconds;

        float desired = shouldBeRevealed ? revealedAlpha : 1f;

        if (!Mathf.Approximately(desired, targetAlpha))
            SetTarget(desired);
    }

    void SetTarget(float alpha)
    {
        targetAlpha = alpha;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(alpha));
    }

    IEnumerator FadeTo(float alpha)
    {
        float start = currentAlpha;
        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeDuration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float a = Mathf.Lerp(start, alpha, k);
            ApplyAlphaImmediate(a);
            yield return null;
        }

        ApplyAlphaImmediate(alpha);
    }

    void ApplyAlphaImmediate(float alpha)
    {
        currentAlpha = alpha;

        // IMPORTANT:
        // Tilemap.color is a global tint (cheap). It SHOULD work.
        // BUT some pipelines / tile flags can block per-cell color changes;
        // global tint usually still works, but we’ll support both.
        var c = overlayTilemap.color;
        c.a = alpha;
        overlayTilemap.color = c;

        // If your tiles still ignore tint, uncomment this fallback (more expensive):
        // ApplyPerCellAlpha(alpha);
    }

    // Fallback if needed. This forces per-cell alpha and unlocks flags.
    void ApplyPerCellAlpha(float alpha)
    {
        overlayTilemap.CompressBounds();
        var bounds = overlayTilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!overlayTilemap.HasTile(pos)) continue;

            overlayTilemap.SetTileFlags(pos, TileFlags.None);

            Color col = overlayTilemap.GetColor(pos);
            col.a = alpha;
            overlayTilemap.SetColor(pos, col);
        }

        overlayTilemap.RefreshAllTiles();
    }
}
