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

    public static readonly Color Path = new Color(1f, 0.60f, 0.20f);
    public static readonly Color LastKnown = new Color(1f, 0.40f, 0.10f);
    public static readonly Color Detection = new Color(1f, 0.85f, 0.25f);
    public static readonly Color Background = new Color(0.08f, 0.09f, 0.11f);
}
