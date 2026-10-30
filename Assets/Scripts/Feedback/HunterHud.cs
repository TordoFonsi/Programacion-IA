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
        bool show = !hunter.IsDead && vision != null && !vision.IsVisible && vision.MemoryTimer > 0f;

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

        string state = hunter.IsDead
            ? $"Muerto ({hunter.RespawnRemaining:0.0}s)"
            : hunter.CurrentStateName;

        string detection = "Sin objetivo";
        if (hunter.DetectedWolf != null) detection = $"Lobo {hunter.DetectedWolf.WolfName}";
        else if (hunter.Vision != null && hunter.Vision.IsVisible) detection = "Boid detectado";
        else if (hunter.Vision != null && hunter.Vision.HasTarget) detection = "Alerta (memoria)";

        Vector3 last = hunter.LastKnownPosition;
        string lastPosition = hunter.Vision != null && hunter.Vision.HasTarget
            ? $"({last.x:0.0}, {last.z:0.0})"
            : "(sin datos)";

        string safeZone = hunter.HasSafeZoneTarget
            ? hunter.SafeZoneTargetName
            : "-";

        GUI.Box(new Rect(12f, 12f, 320f, 205f), GUIContent.none);
        GUI.Label(new Rect(24f, 20f, 300f, 22f), $"Estado: {state}", style);
        GUI.Label(new Rect(24f, 42f, 300f, 22f), $"Vida: {hunter.Health}", style);
        GUI.Label(new Rect(24f, 64f, 300f, 22f), $"Recolectados: {hunter.Collected}/{hunter.Goal}", style);
        GUI.Label(new Rect(24f, 86f, 300f, 22f), $"Ciclos completados: {hunter.RunsCompleted}", style);
        GUI.Label(new Rect(24f, 108f, 300f, 22f), $"Deteccion: {detection}", style);
        GUI.Label(new Rect(24f, 130f, 300f, 22f), $"Ultima posicion: {lastPosition}", style);
        GUI.Label(new Rect(24f, 152f, 300f, 22f), $"Camino A*: {hunter.CurrentPath.Count} nodos", style);
        GUI.Label(new Rect(24f, 174f, 300f, 22f), $"Zona segura: {safeZone}", style);
    }
}
