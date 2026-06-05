using UnityEngine;

/// <summary>
/// Represents a single heat source point in the volumetric heatmap.
/// Place multiple HeatSource objects inside a HeatmapVolume's bounds.
/// </summary>
[ExecuteAlways]
public class HeatSource : MonoBehaviour
{
    [Header("Heat Properties")]
    [Tooltip("Temperature value of this source (raw unit, e.g. Celsius)")]
    [Min(0f)]
    public float temperature = 50f;

    [Tooltip("Radius of influence (world units). Beyond this distance, heat drops to zero.")]
    [Min(0.01f)]
    public float radius = 2f;

    [Tooltip("Falloff exponent. Higher = sharper falloff at the edge.")]
    [Range(0.5f, 8f)]
    public float falloff = 2f;

    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(1f, 0.4f, 0f, 0.35f);

    // Read by HeatmapVolumeRenderer
    public Vector3 WorldPosition => transform.position;

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.12f);

        // Influence sphere (wireframe)
        Color wire = gizmoColor;
        wire.a = 0.18f;
        Gizmos.color = wire;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
        Gizmos.DrawSphere(transform.position, 0.18f);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (radius * 0.12f + 0.3f),
            $"{temperature:F1}°  r={radius:F1}"
        );
#endif
    }

    public void SetTemperature(float temp) => temperature = Mathf.Max(0f, temp);
}
