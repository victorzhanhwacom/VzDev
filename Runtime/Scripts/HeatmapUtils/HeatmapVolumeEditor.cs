#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(HeatmapVolumeRenderer))]
public class HeatmapVolumeEditor : Editor
{
    private const int RAMP_HEIGHT  = 22;
    private const int RAMP_SAMPLES = 256;
    private Texture2D _rampTex;

    // Foldout states
    private bool _showFlow = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var rend = (HeatmapVolumeRenderer)target;

        // ── Gradient preview ─────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Temperature Gradient Preview", EditorStyles.boldLabel);
        DrawRampPreview(rend);

        // ── Flow presets ─────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Flow Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Calm Haze"))    ApplyFlowPreset(rend, 0.2f, 0.12f, 1.5f, 2, 0.25f, new Vector3(0,1,0));
        if (GUILayout.Button("Heat Plume"))   ApplyFlowPreset(rend, 0.7f, 0.40f, 2.5f, 3, 0.45f, new Vector3(0.2f,1,0.1f));
        if (GUILayout.Button("Turbulent"))    ApplyFlowPreset(rend, 1.5f, 0.90f, 4.0f, 4, 0.70f, new Vector3(0.5f,1,0.5f));
        if (GUILayout.Button("Frozen"))       ApplyFlowPreset(rend, 0.0f, 0.00f, 2.0f, 2, 0.30f, new Vector3(0,1,0));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // ── Color presets ────────────────────────────────────────────────
        EditorGUILayout.LabelField("Color Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Thermal"))  ApplyColorPreset(rend, PresetThermal());
        if (GUILayout.Button("Magma"))    ApplyColorPreset(rend, PresetMagma());
        if (GUILayout.Button("Plasma"))   ApplyColorPreset(rend, PresetPlasma());
        if (GUILayout.Button("Ice→Fire")) ApplyColorPreset(rend, PresetIceFire());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Refresh Heat Sources"))
            rend.RefreshHeatSources();
    }

    // ── Gradient preview ─────────────────────────────────────────────────

    private void DrawRampPreview(HeatmapVolumeRenderer rend)
    {
        var stops = new List<TempColorStop>(rend.colorStops);
        stops.Sort((a, b) => a.temperature.CompareTo(b.temperature));
        if (stops.Count < 2) return;

        if (_rampTex == null)
            _rampTex = new Texture2D(RAMP_SAMPLES, 1, TextureFormat.RGBA32, false)
                { wrapMode = TextureWrapMode.Clamp };

        float range = Mathf.Max(rend.tempMax - rend.tempMin, 0.0001f);

        for (int x = 0; x < RAMP_SAMPLES; x++)
        {
            float nt = x / (float)(RAMP_SAMPLES - 1);
            _rampTex.SetPixel(x, 0, EvalGradient(stops, rend.tempMin, range, nt));
        }
        _rampTex.Apply();

        Rect r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
            GUILayout.Height(RAMP_HEIGHT), GUILayout.ExpandWidth(true));
        GUI.DrawTexture(r, _rampTex, ScaleMode.StretchToFill);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{rend.tempMin:G4}°", GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{(rend.tempMin + rend.tempMax) * 0.5f:G4}°", GUILayout.Width(60));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{rend.tempMax:G4}°", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
    }

    private Color EvalGradient(List<TempColorStop> stops, float tMin, float range, float nt)
    {
        if (nt <= 0f) return stops[0].color;
        float ns(int i) => Mathf.Clamp01((stops[i].temperature - tMin) / range);
        for (int i = 1; i < stops.Count; i++)
        {
            if (nt <= ns(i))
            {
                float t = Mathf.InverseLerp(ns(i-1), ns(i), nt);
                return Color.Lerp(stops[i-1].color, stops[i].color, t);
            }
        }
        return stops[stops.Count - 1].color;
    }

    // ── Flow preset helper ───────────────────────────────────────────────

    private void ApplyFlowPreset(HeatmapVolumeRenderer rend,
        float speed, float strength, float scale, int octaves,
        float edgeBand, Vector3 dir)
    {
        Undo.RecordObject(rend, "Apply Flow Preset");
        rend.flowSpeed    = speed;
        rend.flowStrength = strength;
        rend.flowScale    = scale;
        rend.flowOctaves  = octaves;
        rend.edgeBand     = edgeBand;
        rend.flowDirection = dir;
        EditorUtility.SetDirty(rend);
    }

    // ── Color presets ────────────────────────────────────────────────────

    private void ApplyColorPreset(HeatmapVolumeRenderer rend, List<TempColorStop> preset)
    {
        Undo.RecordObject(rend, "Apply Color Preset");
        rend.colorStops = preset;
        EditorUtility.SetDirty(rend);
    }

    private List<TempColorStop> PresetThermal() => new()
    {
        new(  0f, new Color(0.02f,0.02f,0.20f)),
        new( 16f, new Color(0.05f,0.10f,0.50f)),
        new( 33f, new Color(0.08f,0.45f,0.70f)),
        new( 50f, new Color(0.22f,0.78f,0.50f)),
        new( 67f, new Color(0.85f,0.85f,0.08f)),
        new( 83f, new Color(0.95f,0.40f,0.05f)),
        new(100f, new Color(1.00f,0.00f,0.00f)),
    };

    private List<TempColorStop> PresetMagma() => new()
    {
        new(  0f, new Color(0.00f,0.00f,0.02f)),
        new( 25f, new Color(0.24f,0.08f,0.29f)),
        new( 50f, new Color(0.63f,0.18f,0.33f)),
        new( 75f, new Color(0.98f,0.54f,0.26f)),
        new(100f, new Color(0.99f,0.98f,0.75f)),
    };

    private List<TempColorStop> PresetPlasma() => new()
    {
        new(  0f, new Color(0.05f,0.03f,0.53f)),
        new( 33f, new Color(0.58f,0.10f,0.60f)),
        new( 67f, new Color(0.94f,0.38f,0.24f)),
        new(100f, new Color(0.94f,0.98f,0.13f)),
    };

    private List<TempColorStop> PresetIceFire() => new()
    {
        new(  0f, new Color(0.60f,0.90f,1.00f)),
        new( 25f, new Color(0.10f,0.40f,0.90f)),
        new( 45f, new Color(0.02f,0.02f,0.25f)),
        new( 55f, new Color(0.30f,0.00f,0.00f)),
        new( 75f, new Color(1.00f,0.35f,0.00f)),
        new(100f, new Color(1.00f,0.95f,0.30f)),
    };
}
#endif
