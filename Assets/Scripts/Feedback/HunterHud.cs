using UnityEngine;

public class HunterHud : MonoBehaviour
{
    [SerializeField] private HunterAgent hunter;
    [SerializeField] private Transform lastKnownMarker;
    private GUIStyle style;

    public void Configure(HunterAgent hunterReference, Transform marker)
    {
        hunter = hunterReference;
        lastKnownMarker = marker;
    }

    private void Update()
    {
        if (hunter == null || lastKnownMarker == null) return;

        VisionSensor vision = hunter.Vision;
        bool show = vision != null && !vision.IsVisible && vision.MemoryTimer > 0f;

        if (lastKnownMarker.gameObject.activeSelf != show) lastKnownMarker.gameObject.SetActive(show);
        if (!show) return;

        lastKnownMarker.position = hunter.LastKnownPosition + Vector3.up * 0.35f;
        lastKnownMarker.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
    }

    private void OnGUI()
    {
        if (hunter == null) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            style.normal.textColor = Color.white;
        }

        string detection = "Sin objetivo";
        if (hunter.Vision != null && hunter.Vision.IsVisible) detection = "Detectado";
        else if (hunter.Vision != null && hunter.Vision.HasTarget) detection = "Alerta";

        string state = "Muerto";
        if (!hunter.IsDead) state = hunter.IsEscaped ? "Escapó" : hunter.CurrentStateName;

        Vector3 last = hunter.LastKnownPosition;
        string lastPosition = hunter.Vision != null && hunter.Vision.HasTarget
            ? $"({last.x:0.0}, {last.z:0.0})"
            : "(sin datos)";

        GUI.Box(new Rect(12f, 12f, 300f, 160f), GUIContent.none);
        GUI.Label(new Rect(24f, 20f, 280f, 22f), $"Estado: {state}", style);
        GUI.Label(new Rect(24f, 42f, 280f, 22f), $"Vida: {hunter.Health}", style);
        GUI.Label(new Rect(24f, 64f, 280f, 22f), $"Recolectados: {hunter.Collected}/{hunter.Goal}", style);
        GUI.Label(new Rect(24f, 86f, 280f, 22f), $"Detección: {detection}", style);
        GUI.Label(new Rect(24f, 108f, 280f, 22f), $"Última posición: {lastPosition}", style);
        GUI.Label(new Rect(24f, 130f, 280f, 22f), $"Camino A*: {hunter.CurrentPath.Count} nodos", style);
    }
}
