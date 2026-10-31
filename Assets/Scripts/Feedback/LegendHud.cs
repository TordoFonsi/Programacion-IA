using UnityEngine;

/// <summary>
/// Colour legend for the Game view: every colour used by the level, the
/// hunter states and the wolf behaviours is explained here, and the rows that
/// are active right now are highlighted.
/// </summary>
public class LegendHud : MonoBehaviour
{
    [SerializeField] private HunterAgent hunter;
    [SerializeField] private WolfPack pack;
    [SerializeField] private bool visible = true;

    private float panelWidth = 372f;
    private const float RowHeight = 18f;
    private const float HeaderHeight = 21f;
    private const float SwatchSize = 11f;

    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle rowStyle;
    private GUIStyle activeRowStyle;
    private GUIStyle hintStyle;

    public void Configure(HunterAgent hunterReference, WolfPack packReference)
    {
        hunter = hunterReference;
        pack = packReference;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame)
        {
            visible = !visible;
        }
#endif
    }

    private void OnGUI()
    {
        if (!visible) return;

        EnsureStyles();

        panelWidth = Mathf.Clamp(Screen.width * 0.20f, 300f, 400f);

        float height = PanelHeight();
        float x = Screen.width - panelWidth - 12f;
        float top = 12f;

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(new Rect(x, top, panelWidth, height), Texture2D.whiteTexture);
        GUI.color = previous;

        float y = top + 8f;
        GUI.Label(new Rect(x + 12f, y, panelWidth - 24f, 20f), "LEYENDA DE COLORES", titleStyle);
        y += 24f;

        // ---------------------------------------------------------------- map
        y = Header(x, y, "MAPA");

        float column = (panelWidth - 24f) / 2f;
        y = Row2(x, y, column,
            LevelPalette.Floor, "Piso",
            LevelPalette.HuntingGround, "Coto de caza");
        y = Row2(x, y, column,
            LevelPalette.Wall, "Muro",
            LevelPalette.InitialZone, "Zona inicial");
        y = Row2(x, y, column,
            LevelPalette.SafeZone, "Zona segura",
            LevelPalette.ExitZone, "Salida");

        // ------------------------------------------------------------- hunter
        y = Header(x, y, "CAZADOR  (el color del cuerpo es su estado)");

        y = Row(x, y, HunterAgent.SearchColor, "Search", "explorando el coto",
            hunter != null && hunter.StateId == HunterStateId.Search);
        y = Row(x, y, HunterAgent.AttackColor, "Attack", "atacando boid (melee o rango)",
            hunter != null && hunter.StateId == HunterStateId.Attack);
        y = Row(x, y, HunterAgent.GatherColor, "Gather", "recolectando boid muerto",
            hunter != null && hunter.StateId == HunterStateId.Gather);
        y = Row(x, y, HunterAgent.ExitColor, "Exit", "yendo a la salida",
            hunter != null && hunter.StateId == HunterStateId.Exit);
        y = Row(x, y, HunterAgent.FleeColor, "Flee", "huyendo a una zona segura",
            hunter != null && hunter.StateId == HunterStateId.Flee);
        y = Row(x, y, HunterAgent.DeadColor, "Dead", "muerto, esperando respawn",
            hunter != null && hunter.StateId == HunterStateId.Dead);

        // -------------------------------------------------------------- wolves
        y = Header(x, y, "LOBOS  (el color del cuerpo es su comportamiento)");

        float quarter = (panelWidth - 24f) / 4f;
        y = Row4(x, y, quarter,
            WolfAgent.PatrolColor, "Patrol", IsWolfIn("Patrol"),
            WolfAgent.AlertColor, "Alert", IsWolfIn("Alert"),
            WolfAgent.SearchColor, "Search", IsWolfIn("Search"),
            WolfAgent.ChaseColor, "Chase", IsWolfIn("Chase"));

        y = Row2(x, y, column,
            LevelPalette.Path, "Camino A*",
            LevelPalette.LastKnown, "Ultima posicion conocida");

        // --------------------------------------------------------------- boids
        y = Header(x, y, "BOIDS Y MARCAS");

        y = Row2(x, y, column,
            LevelPalette.Prey, "Boid vivo",
            LevelPalette.PreyDead, "Boid muerto");
        y = Row2(x, y, column,
            LevelPalette.Background, "Recolectado: desaparece",
            LevelPalette.Detection, "Icono de deteccion");

        GUI.Label(new Rect(x + 12f, y + 2f, panelWidth - 24f, 18f),
            "Tecla L: ocultar / mostrar la leyenda", hintStyle);
    }

    private float PanelHeight()
    {
        return 24f + HeaderHeight + 3f * RowHeight
             + HeaderHeight + 6f * RowHeight
             + HeaderHeight + 2f * RowHeight
             + HeaderHeight + 2f * RowHeight
             + 26f;
    }

    private bool IsWolfIn(string behaviour)
    {
        if (pack == null) return false;

        for (int i = 0; i < pack.Wolves.Count; i++)
        {
            WolfAgent wolf = pack.Wolves[i];
            if (wolf != null && wolf.CurrentBehaviour == behaviour) return true;
        }

        return false;
    }

    private float Header(float x, float y, string text)
    {
        GUI.Label(new Rect(x + 12f, y, panelWidth - 24f, HeaderHeight), text, headerStyle);
        return y + HeaderHeight;
    }

    private float Row(float x, float y, Color color, string label, string description, bool active)
    {
        Swatch(new Rect(x + 14f, y + 3f, SwatchSize, SwatchSize), color);

        GUIStyle style = active ? activeRowStyle : rowStyle;
        string prefix = active ? "> " : "  ";
        GUI.Label(new Rect(x + 32f, y, panelWidth - 44f, RowHeight),
            $"{prefix}{label}  -  {description}", style);

        return y + RowHeight;
    }

    private float Row2(float x, float y, float column,
        Color colorA, string labelA,
        Color colorB, string labelB)
    {
        Swatch(new Rect(x + 14f, y + 3f, SwatchSize, SwatchSize), colorA);
        GUI.Label(new Rect(x + 32f, y, column - 20f, RowHeight), labelA, rowStyle);

        Swatch(new Rect(x + 14f + column, y + 3f, SwatchSize, SwatchSize), colorB);
        GUI.Label(new Rect(x + 32f + column, y, column - 20f, RowHeight), labelB, rowStyle);

        return y + RowHeight;
    }

    private float Row4(float x, float y, float column,
        Color colorA, string labelA, bool activeA,
        Color colorB, string labelB, bool activeB,
        Color colorC, string labelC, bool activeC,
        Color colorD, string labelD, bool activeD)
    {
        Row4Cell(x + 14f, y, column, colorA, labelA, activeA);
        Row4Cell(x + 14f + column, y, column, colorB, labelB, activeB);
        Row4Cell(x + 14f + column * 2f, y, column, colorC, labelC, activeC);
        Row4Cell(x + 14f + column * 3f, y, column, colorD, labelD, activeD);

        return y + RowHeight;
    }

    private void Row4Cell(float x, float y, float column, Color color, string label, bool active)
    {
        Swatch(new Rect(x, y + 3f, SwatchSize, SwatchSize), color);
        GUI.Label(new Rect(x + 15f, y, column - 15f, RowHeight), label, active ? activeRowStyle : rowStyle);
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
        if (rowStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.white;

        headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
        headerStyle.normal.textColor = new Color(0.75f, 0.82f, 0.95f);

        rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        rowStyle.normal.textColor = new Color(0.86f, 0.86f, 0.86f);

        activeRowStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
        activeRowStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic };
        hintStyle.normal.textColor = new Color(0.70f, 0.70f, 0.70f);
    }
}
