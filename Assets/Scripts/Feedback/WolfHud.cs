using UnityEngine;

public class WolfHud : MonoBehaviour
{
    [SerializeField] private WolfPack pack;
    [SerializeField] private HunterAgent hunter;
    private GUIStyle style;

    public void Configure(WolfPack packReference, HunterAgent hunterReference)
    {
        pack = packReference;
        hunter = hunterReference;
    }

    private void OnGUI()
    {
        if (pack == null) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            style.normal.textColor = Color.white;
        }

        int count = pack.Wolves.Count;
        float height = 52f + count * 17f;
        float top = Screen.height - height - 12f;

        GUI.Box(new Rect(12f, top, 340f, height), GUIContent.none);

        string alerts = pack.AlertCount > 0
            ? $"Manada - alertas: {pack.AlertCount}"
            : "Manada - sin alertas";

        GUI.Label(new Rect(24f, top + 6f, 320f, 18f), alerts, style);

        Vector3 alert = pack.LastAlertPosition;
        string lastAlert = pack.AlertCount > 0
            ? $"Ultima alerta: ({alert.x:0.0}, {alert.z:0.0})"
            : "Ultima alerta: -";

        GUI.Label(new Rect(24f, top + 24f, 320f, 18f), lastAlert, style);

        for (int i = 0; i < count; i++)
        {
            WolfAgent wolf = pack.Wolves[i];
            if (wolf == null) continue;

            string line = $"{wolf.WolfName}: {wolf.CurrentBehaviour}";

            if (wolf.HasAlert)
            {
                Vector3 last = wolf.LastKnownPosition;
                line += $" -> ({last.x:0.0}, {last.z:0.0})";
            }

            if (wolf.CurrentPath.Count > 1) line += $" [A* {wolf.CurrentPath.Count}]";

            GUI.Label(new Rect(24f, top + 42f + i * 17f, 320f, 17f), line, style);
        }

        if (hunter == null || !hunter.IsInsideSafeZone()) return;

        GUI.Label(new Rect(24f, top + 42f + count * 17f, 320f, 17f), "Cazador a salvo", style);
    }
}
