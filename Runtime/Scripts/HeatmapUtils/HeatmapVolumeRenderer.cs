using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ---------------------------------------------------------------------------
// Temperature Color Stop
// ---------------------------------------------------------------------------

[Serializable]
public class TempColorStop
{
    [Tooltip("Temperature at this stop (same units as TempMin / TempMax)")]
    public float temperature = 0f;
    [ColorUsage(false, true)]
    public Color color = Color.blue;
    public TempColorStop() { }
    public TempColorStop(float temp, Color col) { temperature = temp; color = col; }
}

// ---------------------------------------------------------------------------
// HeatmapVolumeRenderer
// ---------------------------------------------------------------------------

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class HeatmapVolumeRenderer : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────── //

    [Header("Shader")]
    public Shader volumeShader;

    [Header("Temperature Range")]
    public float tempMin = 0f;
    public float tempMax = 100f;

    [Header("Color Gradient (2–8 stops, sorted by temperature)")]
    public List<TempColorStop> colorStops = new List<TempColorStop>
    {
        new TempColorStop(  0f, new Color(0.00f, 0.00f, 1.00f)),
        new TempColorStop( 20f, new Color(0.00f, 1.00f, 1.00f)),
        new TempColorStop( 40f, new Color(0.00f, 1.00f, 0.00f)),
        new TempColorStop( 60f, new Color(1.00f, 1.00f, 0.00f)),
        new TempColorStop( 80f, new Color(1.00f, 0.45f, 0.00f)),
        new TempColorStop(100f, new Color(1.00f, 0.00f, 0.00f)),
    };

    [Header("Raymarching Quality")]
    [Range(16, 128)] public int stepCount = 64;

    [Header("Volume Appearance")]
    [Range(0f, 5f)]  public float density        = 1.5f;
    [Range(0f, 1f)]  public float alphaThreshold = 0.01f;

    [Header("Edge Flow / Turbulence")]
    [Tooltip("Animation speed. 0 = frozen.")]
    [Range(0f, 5f)]  public float flowSpeed    = 0.6f;

    [Tooltip("Displacement magnitude in world units. 0 = no visible effect.")]
    [Range(0f, 5f)]  public float flowStrength = 0.35f;

    [Tooltip("Spatial frequency of the noise. High = small swirls.")]
    [Range(0.1f, 5f)] public float flowScale   = 2f;

    [Tooltip("FBM octaves (1-4). More = richer detail, more GPU cost.")]
    [Range(1, 5)]    public int   flowOctaves  = 3;

    [Tooltip("How far inward the turbulence reaches from the edge. 0 = surface only.")]
    [Range(0f, 5f)]  public float edgeBand     = 0.4f;

    [Tooltip("Main drift direction. Y=1 simulates natural heat-rise.")]
    public Vector3 flowDirection = new Vector3(0.2f, 1f, 0.1f);

    [Header("Base Temperature (background fill)")]
    [Tooltip("Temperature of the volume where no HeatSource is present. " +
             "Set within [TempMin, TempMax] — e.g. 50 for the green mid-point of a 0-100 range.")]
    public float baseTemperature = 50f;

    [Tooltip("Density multiplier for base-temperature regions (0 = invisible, 1 = full density). " +
             "Keep low (0.2–0.5) so hot spots stand out clearly.")]
    [Range(0f, 1f)]
    public float baseDensityScale = 0.3f;

    [Header("Temperature Smoothing")]
    [Tooltip("How fast displayed temperature chases the target value (degrees per second). " +
             "0 = instant snap, higher = slower transition.")]
    [Min(0f)]
    public float tempSmoothSpeed = 10f;

    [Header("Heat Source Discovery")]
    public bool autoRefreshSources = true;

    // ── Private ──────────────────────────────────────────────────────── //

    private Material     _mat;
    private MeshRenderer _rend;
    private HeatSource[] _sources = Array.Empty<HeatSource>();

    // Wall-clock based timer — works in Editor AND WebGL build
    // -1 means uninitialised; set on OnEnable or first FlowTime() call.
    // Stored as double so sub-millisecond precision survives long Editor sessions.
    [NonSerialized] private double _startRealtime = -1;

    // Cached shader property IDs
    private static readonly int ID_RaymarchPack  = Shader.PropertyToID("_RaymarchPack");
    private static readonly int ID_TempRangePack = Shader.PropertyToID("_TempRangePack");
    private static readonly int ID_FlowPack      = Shader.PropertyToID("_FlowPack");
    private static readonly int ID_FlowPack2     = Shader.PropertyToID("_FlowPack2");
    private static readonly int ID_FlowDir       = Shader.PropertyToID("_FlowDirection");
    private static readonly int ID_StopPack0     = Shader.PropertyToID("_TempStopPack0");
    private static readonly int ID_StopPack1     = Shader.PropertyToID("_TempStopPack1");
    private static readonly int ID_SrcCountPack  = Shader.PropertyToID("_HeatSourceCountPack");
    private static readonly int ID_BaseTempPack  = Shader.PropertyToID("_BaseTempPack");
    // _FlowTime 已改用 shader built-in _Time.y，不需要 C# 傳入

    private static readonly int[] ID_TempColor =
    {
        Shader.PropertyToID("_TempColor0"), Shader.PropertyToID("_TempColor1"),
        Shader.PropertyToID("_TempColor2"), Shader.PropertyToID("_TempColor3"),
        Shader.PropertyToID("_TempColor4"), Shader.PropertyToID("_TempColor5"),
        Shader.PropertyToID("_TempColor6"), Shader.PropertyToID("_TempColor7"),
    };

    private readonly Vector4[] _srcPos    = new Vector4[32];
    private readonly Vector4[] _srcParams = new Vector4[32];

    // Smoothed display temperature for each heat source.
    // Key = HeatSource instance, Value = current interpolated temperature.
    private readonly Dictionary<HeatSource, float> _smoothedTemp
        = new Dictionary<HeatSource, float>();

    // ── Lifecycle ─────────────────────────────────────────────────────── //

    private void OnEnable()
    {
        // Record wall-clock start so elapsed time is always valid
        _startRealtime = GetRealtime();

        EnsureComponents();
        RefreshHeatSources();

#if UNITY_EDITOR
        // Hook into Editor's update loop so the volume animates
        // even when NOT in Play Mode and the Scene view is idle.
        EditorApplication.update -= EditorTick;
        EditorApplication.update += EditorTick;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorTick;
#endif
        if (_mat != null && !Application.isPlaying)
            DestroyImmediate(_mat);
    }

#if UNITY_EDITOR
    // Called by EditorApplication.update — fires every Editor frame (~100 fps)
    // regardless of whether the Scene view has focus or is being interacted with.
    private void EditorTick()
    {
        if (this == null || !enabled || !gameObject.activeInHierarchy)
        {
            EditorApplication.update -= EditorTick;
            return;
        }
        if (Application.isPlaying) return; // Play Mode uses Update() below

        if (autoRefreshSources) RefreshHeatSources();
        UploadAllParameters();

        // QueuePlayerLoopUpdate makes Unity run one player-loop tick so
        // [ExecuteAlways] components actually see the new material properties.
        // RepaintAll then asks every Scene/Game view to redraw.
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
#endif

    // Update() handles Play Mode (and WebGL build where EditorApplication doesn't exist)
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return; // Editor handled by EditorTick
#endif
        if (autoRefreshSources) RefreshHeatSources();
        UploadAllParameters();
    }

    // ── Public API ────────────────────────────────────────────────────── //

    public void RefreshHeatSources()
    {
        // Build the volume's world-space AABB from the renderer bounds.
        // Works for any scale/rotation; no Physics or layer setup required.
        Bounds worldBounds = GetComponent<MeshRenderer>().bounds;

#if UNITY_2023_1_OR_NEWER
        var all = FindObjectsByType<HeatSource>(FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<HeatSource>();
#endif
        // Count first to allocate exactly once.
        int count = 0;
        foreach (var hs in all)
            if (worldBounds.Contains(hs.WorldPosition)) count++;

        if (_sources == null || _sources.Length != count)
            _sources = new HeatSource[count];

        int idx = 0;
        foreach (var hs in all)
            if (worldBounds.Contains(hs.WorldPosition))
                _sources[idx++] = hs;
    }

    public void ResetFlowTime()
        => _startRealtime = GetRealtime();

    // ── Internal ──────────────────────────────────────────────────────── //

    // Platform-safe wall-clock elapsed seconds
    // Works in Editor, Play Mode, and WebGL build
    private float FlowTime()
    {
        if (_startRealtime < 0) _startRealtime = GetRealtime();
        return (float)(GetRealtime() - _startRealtime);
    }

    private static double GetRealtime()
    {
#if UNITY_EDITOR
        // EditorApplication.timeSinceStartup advances even when not playing
        return EditorApplication.timeSinceStartup;
#else
        return Time.realtimeSinceStartupAsDouble;
#endif
    }

    private void EnsureComponents()
    {
        _rend = GetComponent<MeshRenderer>();

        if (_mat == null)
        {
            Shader sh = volumeShader != null
                ? volumeShader
                : Shader.Find("Custom/HeatmapVolume");

            if (sh == null)
            {
                Debug.LogError("[HeatmapVolumeRenderer] Shader 'Custom/HeatmapVolume' not found.");
                enabled = false;
                return;
            }
            _mat = new Material(sh) { name = "HeatmapVolume_Mat" };
            _rend.sharedMaterial = _mat;
        }

        var mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null) mf.sharedMesh = BuildUnitCube();
    }

    private void UploadAllParameters()
    {
        if (_mat == null) { EnsureComponents(); return; }

        // ── Raymarching ─────────────────────────────────────────────────
        _mat.SetVector(ID_RaymarchPack, new Vector4(stepCount, density, alphaThreshold, 0f));

        // ── Temperature range ────────────────────────────────────────────
        var sorted = new List<TempColorStop>(colorStops);
        sorted.Sort((a, b) => a.temperature.CompareTo(b.temperature));
        int n = Mathf.Clamp(sorted.Count, 2, 8);
        _mat.SetVector(ID_TempRangePack, new Vector4(tempMin, tempMax, n, 0f));

        // ── Flow — FlowTime from wall-clock, guaranteed to advance ───────
        // _FlowPack.x 不使用（時間改由 shader built-in _Time.y 處理）
        _mat.SetVector(ID_FlowPack, new Vector4(0f, flowSpeed, flowStrength, flowScale));
        _mat.SetVector(ID_FlowPack2, new Vector4(flowOctaves, edgeBand, 0f, 0f));
        _mat.SetVector(ID_FlowDir,   new Vector4(flowDirection.x, flowDirection.y,
                                                  flowDirection.z, 0f));

        // ── Colour stops ─────────────────────────────────────────────────
        float range = Mathf.Max(tempMax - tempMin, 0.0001f);
        float[] ns  = new float[8];
        for (int i = 0; i < 8; i++)
        {
            int si = Mathf.Min(i, n - 1);
            ns[i] = Mathf.Clamp01((sorted[si].temperature - tempMin) / range);
            _mat.SetColor(ID_TempColor[i], sorted[si].color);
        }
        _mat.SetVector(ID_StopPack0, new Vector4(ns[0], ns[1], ns[2], ns[3]));
        _mat.SetVector(ID_StopPack1, new Vector4(ns[4], ns[5], ns[6], ns[7]));

        // ── Base temperature ──────────────────────────────────────────────
        _mat.SetVector(ID_BaseTempPack, new Vector4(baseTemperature, baseDensityScale, 0f, 0f));

        // ── Heat sources ──────────────────────────────────────────────────
        int cnt = Mathf.Min(_sources.Length, 32);
        _mat.SetVector(ID_SrcCountPack, new Vector4(cnt, 0f, 0f, 0f));

        float dt = Application.isPlaying ? Time.deltaTime : (float)(1.0 / 60.0);

        for (int i = 0; i < cnt; i++)
        {
            var hs = _sources[i];
            var wp = hs.WorldPosition;

            // Initialise smoothed temp on first sight of this source.
            if (!_smoothedTemp.TryGetValue(hs, out float current))
                current = hs.temperature;

            // Smooth towards the target temperature.
            float smoothed = (tempSmoothSpeed <= 0f)
                ? hs.temperature
                : Mathf.MoveTowards(current, hs.temperature, tempSmoothSpeed * dt);

            _smoothedTemp[hs] = smoothed;

            _srcPos[i]    = new Vector4(wp.x, wp.y, wp.z, smoothed);
            _srcParams[i] = new Vector4(hs.radius, hs.falloff, 0f, 0f);
        }

        // Remove stale entries (sources that left the volume this frame).
        if (_smoothedTemp.Count > cnt)
        {
            var active = new System.Collections.Generic.HashSet<HeatSource>(_sources);
            var toRemove = new System.Collections.Generic.List<HeatSource>();
            foreach (var key in _smoothedTemp.Keys)
                if (!active.Contains(key)) toRemove.Add(key);
            foreach (var key in toRemove) _smoothedTemp.Remove(key);
        }

        _mat.SetVectorArray("_HeatSourcePositions", _srcPos);
        _mat.SetVectorArray("_HeatSourceParams",    _srcParams);
    }

    private static Mesh BuildUnitCube()
    {
        var m = new Mesh { name = "HeatmapVolumeCube" };
        m.vertices = new Vector3[]
        {
            new(-0.5f,-0.5f,-0.5f), new(0.5f,-0.5f,-0.5f),
            new(0.5f, 0.5f,-0.5f), new(-0.5f, 0.5f,-0.5f),
            new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f),
            new(0.5f,-0.5f, 0.5f), new(-0.5f,-0.5f, 0.5f),
        };
        m.triangles = new int[]
        {
            0,2,1, 0,3,2,   2,3,4, 2,4,5,
            1,2,5, 1,5,6,   0,7,4, 0,4,3,
            5,4,7, 5,7,6,   0,6,7, 0,1,6,
        };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color  = new Color(1f, 0.6f, 0.1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.color  = new Color(0.4f, 0.9f, 1f, 0.7f);
        Gizmos.matrix = Matrix4x4.identity;
        Vector3 c = transform.position;
        Vector3 d = flowDirection.normalized * 0.8f;
        Gizmos.DrawLine(c, c + d);
        Gizmos.DrawSphere(c + d, 0.08f);
    }
}
