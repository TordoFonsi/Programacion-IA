using UnityEngine;

/// <summary>
/// Single source of truth for the colours used by the level and by the
/// legend, so the HUD can never drift away from what is on screen.
/// </summary>
public static class LevelPalette
{
    public static readonly Color Floor = new Color(0.85f, 0.82f, 0.74f);
    public static readonly Color Wall = new Color(0.18f, 0.19f, 0.22f);
    public static readonly Color HuntingGround = new Color(0.95f, 0.72f, 0.35f);
    public static readonly Color InitialZone = new Color(0.45f, 0.75f, 0.55f);
    public static readonly Color SafeZone = new Color(0.65f, 0.45f, 0.80f);
    public static readonly Color ExitZone = new Color(0.40f, 0.65f, 0.90f);

    public static readonly Color Prey = new Color(0.97f, 0.96f, 0.92f);
    public static readonly Color PreyDead = new Color(0.35f, 0.32f, 0.30f);
    public static readonly Color PreyCollected = new Color(0.55f, 0.55f, 0.55f);

    public static readonly Color Path = new Color(1f, 0.90f, 0.30f);
    public static readonly Color LastKnown = new Color(1f, 0.40f, 0.10f);
    public static readonly Color Detection = new Color(1f, 0.85f, 0.25f);
    public static readonly Color Background = new Color(0.08f, 0.09f, 0.11f);

    // Colours of the four wolf patrol routes (same as the reference map).
    public static readonly Color RouteL1 = new Color(0.90f, 0.20f, 0.20f);
    public static readonly Color RouteL2 = new Color(0.95f, 0.30f, 0.75f);
    public static readonly Color RouteL3 = new Color(1.00f, 0.55f, 0.10f);
    public static readonly Color RouteL4 = new Color(0.15f, 0.75f, 0.95f);

    public static Color RouteColor(int index)
    {
        switch (((index % 4) + 4) % 4)
        {
            case 0: return RouteL1;
            case 1: return RouteL2;
            case 2: return RouteL3;
            default: return RouteL4;
        }
    }
}
