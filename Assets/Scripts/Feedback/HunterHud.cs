using UnityEngine;

/// <summary>
/// Hunter feedback. Shows the state (with its colour), what the hunter is
/// doing right now (melee or ranged attack, gathering progress, flee target),
/// the current objective and the A* / safe zone information.
/// </summary>
public class HunterHud : MonoBehaviour
{
    [SerializeField] private HunterAgent hunter;
    [SerializeField] private Transform lastKnownMarker;

    private const float PanelWidth = 430f;
    private const float RowHeight = 19f;
    private const float PanelHeight = 216f;

    private GUIStyle style;
    private GUIStyle titleStyle;
    private GUIStyle smallStyle;

    public void Configure(HunterAgent hunterReference, Transform marker)
    {
        hunter = hunterReference;
        lastKnownMarker = marker;
    }

    private void Update()
    {
        if (hunter == null || lastKnownMarker == null) return;

        VisionSensor vision = hunter.Vision;
        bool show = !hunter.IsDead && vision != null && !vision.IsVisible && vision.MemoryTimer > 0f;

        if (lastKnownMarker.gameObject.activeSelf != show) lastKnownMarker.gameObject.SetActive(show);
        if (!show) return;

        lastKnownMarker.position = hunter.LastKnownPosition + Vector3.up * 0.35f;
        lastKnownMarker.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
    }

    private void OnGUI()
    {
        if (hunter == null) return;

        EnsureStyles();

        float x = 12f;
        float top = 12f;

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(new Rect(x, top, PanelWidth, PanelHeight), Texture2D.whiteTexture);
        GUI.color = previous;

        float labelX = x + 12f;
        float y = top + 8f;
        float column = PanelWidth * 0.5f;

        // State, with the same colour the hunter body has in the scene.
        Swatch(new Rect(labelX, y + 4f, 12f, 12f), HunterAgent.StateColor(hunter.StateId));

        string state = hunter.IsDead
            ? $"Muerto ({hunter.RespawnRemaining:0.0}s)"
            : hunter.CurrentStateName;

        GUI.Label(new Rect(labelX + 19f, y, PanelWidth - 30f, RowHeight),
            $"Estado: {state}", titleStyle);
        y += RowHeight;

        GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
            $"Accion: {hunter.ActionLabel}", style);
        y += RowHeight;

        GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
            $"Objetivo: {hunter.TargetLabel}", style);
        y += RowHeight;

        TwoColumns(labelX, y, column,
            $"Vida: {hunter.Health}",
            $"Recolectados: {hunter.Collected}/{hunter.Goal}");
        y += RowHeight;

        string detection = "Sin objetivo";
        if (hunter.DetectedWolf != null) detection = $"Lobo {hunter.DetectedWolf.WolfName}";
        else if (hunter.Vision != null && hunter.Vision.IsVisible) detection = "Boid detectado";
        else if (hunter.Vision != null && hunter.Vision.HasTarget) detection = "Alerta (memoria)";

        TwoColumns(labelX, y, column,
            $"Ciclos completados: {hunter.RunsCompleted}",
            $"Deteccion: {detection}");
        y += RowHeight;

        Vector3 last = hunter.LastKnownPosition;
        string lastPosition = hunter.Vision != null && hunter.Vision.HasTarget
            ? $"({last.x:0.0}, {last.z:0.0})"
            : "(sin datos)";

        GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
            $"Ultima posicion conocida: {lastPosition}", style);
        y += RowHeight;

        string safeZone = hunter.HasSafeZoneTarget ? hunter.SafeZoneTargetName : "-";

        TwoColumns(labelX, y, column,
            $"Camino A*: {hunter.CurrentPath.Count} nodos",
            $"Zona segura: {safeZone}");
        y += RowHeight;

        GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
            $"Rangos: melee {hunter.MeleeAttackRange:0.0} u / rango {hunter.RangeAttackRange:0.0} u", style);
        y += RowHeight + 2f;

        DrawGatherBar(labelX, y, PanelWidth - 24f);

        GUI.Label(new Rect(labelX, y + 13f, PanelWidth - 24f, 16f),
            "Tecla L: leyenda de colores", smallStyle);
    }

    private void DrawGatherBar(float x, float y, float width)
    {
        const float barHeight = 8f;

        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.18f);
        GUI.DrawTexture(new Rect(x, y, width, barHeight), Texture2D.whiteTexture);

        bool gathering = hunter.StateId == HunterStateId.Gather;
        float progress = gathering ? hunter.GatherProgress : 0f;

        if (progress > 0f)
        {
            GUI.color = HunterAgent.GatherColor;
            GUI.DrawTexture(new Rect(x, y, width * progress, barHeight), Texture2D.whiteTexture);
        }

        GUI.color = previous;
    }

    private void TwoColumns(float x, float y, float column, string left, string right)
    {
        GUI.Label(new Rect(x, y, column - 8f, RowHeight), left, style);
        GUI.Label(new Rect(x + column, y, column - 8f, RowHeight), right, style);
    }

    private void Swatch(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void EnsureStyles()
    {
        if (style != null) return;

        style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        style.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        titleStyle = new GUIStyle(style) { fontSize = 15, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(style) { fontSize = 11, fontStyle = FontStyle.Italic };
        smallStyle.normal.textColor = new Color(0.70f, 0.70f, 0.70f);
    }
}
