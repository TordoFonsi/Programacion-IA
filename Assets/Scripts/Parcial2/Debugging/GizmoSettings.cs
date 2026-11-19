using UnityEngine;

/// <summary>
/// Toggles for the debug gizmos drawn by the Parcial 2 agents.
/// They are stored in EditorPrefs so the choice survives domain reloads and
/// can be changed from Tools/Level/Gizmos without touching any component.
/// </summary>
public static class GizmoSettings
{
    private const string Prefix = "parcial2.gizmos.";

    public static bool BoidRadii
    {
        get => Get("boids", false);
        set => Set("boids", value);
    }

    public static bool VisionCones
    {
        get => Get("vision", true);
        set => Set("vision", value);
    }

    public static bool CombatRadii
    {
        get => Get("combat", false);
        set => Set("combat", value);
    }

    public static bool Zones
    {
        get => Get("zones", true);
        set => Set("zones", value);
    }

    public static bool Graph
    {
        get => Get("graph", false);
        set => Set("graph", value);
    }

#if UNITY_EDITOR
    private static bool Get(string key, bool fallback)
    {
        return UnityEditor.EditorPrefs.GetBool(Prefix + key, fallback);
    }

    private static void Set(string key, bool value)
    {
        UnityEditor.EditorPrefs.SetBool(Prefix + key, value);
    }
#else
    private static bool Get(string key, bool fallback)
    {
        return fallback;
    }

    private static void Set(string key, bool value)
    {
    }
#endif
}
