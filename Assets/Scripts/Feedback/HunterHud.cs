using UnityEngine;

/// <summary>
/// Compact hunter feedback. Only the data the statement asks for is shown:
/// state, current action, collections, detection, last known position, A*
/// path and chosen safe zone. Rows appear only when they have something to
/// say, so the panel stays out of the way.
/// </summary>
public class HunterHud : MonoBehaviour
{
    [SerializeField] private HunterAgent hunter;
    [SerializeField] private Transform lastKnownMarker;

    private const float PanelWidth = 340f;
    private const float RowHeight = 19f;

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

        bool dead = hunter.IsDead;
        string state = dead
            ? $"Muerto ({hunter.RespawnRemaining:0.0}s)"
            : hunter.CurrentStateName;

        string detection = DetectionLabel();
        bool showDetection = detection != null;

        bool showLast = !dead && hunter.Vision != null &&
                        hunter.Vision.HasTarget && !hunter.Vision.IsVisible;
        Vector3 last = hunter.LastKnownPosition;

        int pathNodes = hunter.CurrentPath.Count;
        bool showPath = pathNodes > 0;
        bool showSafe = hunter.StateId == HunterStateId.Flee && hunter.HasSafeZoneTarget;
        bool gathering = hunter.StateId == HunterStateId.Gather;

        // Rows that are always visible: state, action and collections.
        int rows = 3;
        if (showDetection) rows++;
        if (showLast) rows++;
        if (showPath) rows++;
        if (showSafe) rows++;

        float height = 8f + rows * RowHeight + (gathering ? 12f : 0f) + 24f;

        float x = 12f;
        float top = 12f;

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(new Rect(x, top, PanelWidth, height), Texture2D.whiteTexture);
        GUI.color = previous;

        float labelX = x + 12f;
        float y = top + 8f;

        // State, with the same colour the hunter body has in the scene.
        Swatch(new Rect(labelX, y + 4f, 12f, 12f), HunterAgent.StateColor(hunter.StateId));

        GUI.Label(new Rect(labelX + 19f, y, PanelWidth - 30f, RowHeight),
            $"Estado: {state}", titleStyle);
        y += RowHeight;

        GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
            $"Accion: {hunter.ActionLabel}", style);
        y += RowHeight;

        GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
            $"Recolectados: {hunter.Collected}/{hunter.Goal}", style);
        y += RowHeight;

        if (showDetection)
        {
            GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
                $"Deteccion: {detection}", style);
            y += RowHeight;
        }

        if (showLast)
        {
            GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
                $"Ultima posicion conocida: ({last.x:0.0}, {last.z:0.0})", style);
            y += RowHeight;
        }

        if (showPath)
        {
            GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
                $"Camino A*: {pathNodes} nodos", style);
            y += RowHeight;
        }

        if (showSafe)
        {
            GUI.Label(new Rect(labelX, y, PanelWidth - 24f, RowHeight),
                $"Zona segura: {hunter.SafeZoneTargetName}", style);
            y += RowHeight;
        }

        if (gathering)
        {
            DrawGatherBar(labelX, y + 3f, PanelWidth - 24f);
            y += 12f;
        }

        GUI.Label(new Rect(labelX, y + 4f, PanelWidth - 24f, 16f),
            "Tecla L: leyenda de colores", smallStyle);
    }

    private string DetectionLabel()
    {
        if (hunter.DetectedWolf != null) return $"Lobo {hunter.DetectedWolf.WolfName}";

        if (hunter.Vision == null) return null;
        if (hunter.Vision.IsVisible) return "Boid detectado";
        if (hunter.Vision.HasTarget) return "Alerta (memoria)";
        return null;
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
