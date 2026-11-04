using UnityEngine;

public class WolfHud : MonoBehaviour
{
    [SerializeField] private WolfPack pack;
    [SerializeField] private HunterAgent hunter;

    private GUIStyle style;
    private GUIStyle titleStyle;

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
            style.normal.textColor = new Color(0.90f, 0.90f, 0.90f);

            titleStyle = new GUIStyle(style) { fontSize = 14, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = Color.white;
        }

        int count = pack.Wolves.Count;
        float height = 58f + count * 17f;
        float top = Screen.height - height - 12f;
        float x = 12f;
        float width = 360f;

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(new Rect(x, top, width, height), Texture2D.whiteTexture);
        GUI.color = previous;

        string alerts = pack.AlertCount > 0
            ? $"MANADA  -  alertas: {pack.AlertCount}"
            : "MANADA  -  sin alertas";

        GUI.Label(new Rect(x + 12f, top + 6f, width - 24f, 18f), alerts, titleStyle);

        Vector3 alert = pack.LastAlertPosition;
        string lastAlert = pack.AlertCount > 0
            ? $"Ultima alerta: ({alert.x:0.0}, {alert.z:0.0})"
            : "Ultima alerta: -";

        GUI.Label(new Rect(x + 12f, top + 24f, width - 24f, 18f), lastAlert, style);

        for (int i = 0; i < count; i++)
        {
            WolfAgent wolf = pack.Wolves[i];
            if (wolf == null) continue;

            float rowY = top + 44f + i * 17f;

            Swatch(new Rect(x + 12f, rowY + 3f, 11f, 11f), WolfAgent.BehaviorColor(wolf.CurrentBehaviour));

            string line = $"{wolf.WolfName}: {wolf.CurrentBehaviour}";

            if (wolf.HasAlert)
            {
                Vector3 last = wolf.LastKnownPosition;
                line += $" -> ({last.x:0.0}, {last.z:0.0})";
            }

            if (wolf.CurrentPath.Count > 1) line += $"  [A* {wolf.CurrentPath.Count}]";

            GUI.Label(new Rect(x + 29f, rowY, width - 41f, 17f), line, style);
        }

        if (hunter == null || !hunter.IsInsideSafeZone()) return;

        GUI.Label(new Rect(x + 12f, top + 44f + count * 17f, width - 24f, 17f), "Cazador a salvo", style);
    }

    private void Swatch(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
