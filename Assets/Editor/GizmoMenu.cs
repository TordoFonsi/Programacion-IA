using UnityEditor;
using UnityEngine;

public static class GizmoMenu
{
    private const string Root = "Tools/Level/Gizmos/";

    [MenuItem(Root + "Boid Radii", false, 100)]
    private static void ToggleBoidRadii()
    {
        GizmoSettings.BoidRadii = !GizmoSettings.BoidRadii;
        SceneView.RepaintAll();
    }

    [MenuItem(Root + "Boid Radii", true)]
    private static bool ValidateBoidRadii()
    {
        Menu.SetChecked(Root + "Boid Radii", GizmoSettings.BoidRadii);
        return true;
    }

    [MenuItem(Root + "Vision Cones", false, 101)]
    private static void ToggleVisionCones()
    {
        GizmoSettings.VisionCones = !GizmoSettings.VisionCones;
        SceneView.RepaintAll();
    }

    [MenuItem(Root + "Vision Cones", true)]
    private static bool ValidateVisionCones()
    {
        Menu.SetChecked(Root + "Vision Cones", GizmoSettings.VisionCones);
        return true;
    }

    [MenuItem(Root + "Combat Radii", false, 102)]
    private static void ToggleCombatRadii()
    {
        GizmoSettings.CombatRadii = !GizmoSettings.CombatRadii;
        SceneView.RepaintAll();
    }

    [MenuItem(Root + "Combat Radii", true)]
    private static bool ValidateCombatRadii()
    {
        Menu.SetChecked(Root + "Combat Radii", GizmoSettings.CombatRadii);
        return true;
    }

    [MenuItem(Root + "Zones", false, 103)]
    private static void ToggleZones()
    {
        GizmoSettings.Zones = !GizmoSettings.Zones;
        SceneView.RepaintAll();
    }

    [MenuItem(Root + "Zones", true)]
    private static bool ValidateZones()
    {
        Menu.SetChecked(Root + "Zones", GizmoSettings.Zones);
        return true;
    }

    [MenuItem(Root + "Graph Nodes", false, 104)]
    private static void ToggleGraph()
    {
        GizmoSettings.Graph = !GizmoSettings.Graph;
        SceneView.RepaintAll();
    }

    [MenuItem(Root + "Graph Nodes", true)]
    private static bool ValidateGraph()
    {
        Menu.SetChecked(Root + "Graph Nodes", GizmoSettings.Graph);
        return true;
    }
}
