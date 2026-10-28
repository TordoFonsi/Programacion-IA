using UnityEngine;

public enum CellType
{
    Solid,
    Floor,
    HuntingGround,
    Initial,
    SafeZone,
    Exit
}

public static class LevelLayout
{
    public const int Width = 100;
    public const int Height = 60;

    public static readonly RectInt InitialZone = new RectInt(1, 23, 11, 14);
    public static readonly RectInt ExitZone = new RectInt(88, 23, 11, 14);
    public static readonly RectInt HuntingGround = new RectInt(30, 23, 40, 14);
    public static readonly RectInt SafeZoneA = new RectInt(71, 40, 8, 8);
    public static readonly RectInt SafeZoneB = new RectInt(21, 12, 8, 8);

    public static CellType[,] Build()
    {
        CellType[,] cells = new CellType[Width, Height];

        Fill(cells, new RectInt(0, 0, Width, Height), CellType.Solid);
        Fill(cells, new RectInt(13, 3, 74, 54), CellType.Floor);
        Fill(cells, new RectInt(20, 10, 60, 40), CellType.Solid);

        Fill(cells, new RectInt(35, 40, 30, 10), CellType.Floor);
        Fill(cells, new RectInt(40, 43, 20, 5), CellType.Solid);
        Fill(cells, new RectInt(35, 10, 30, 10), CellType.Floor);
        Fill(cells, new RectInt(40, 12, 20, 5), CellType.Solid);

        Fill(cells, HuntingGround, CellType.HuntingGround);
        Fill(cells, new RectInt(19, 29, 11, 2), CellType.Floor);
        Fill(cells, new RectInt(70, 29, 10, 2), CellType.Floor);

        Fill(cells, SafeZoneA, CellType.SafeZone);
        Fill(cells, new RectInt(79, 43, 2, 2), CellType.SafeZone);
        Fill(cells, SafeZoneB, CellType.SafeZone);
        Fill(cells, new RectInt(19, 15, 2, 2), CellType.SafeZone);

        Fill(cells, InitialZone, CellType.Initial);
        Fill(cells, new RectInt(11, 29, 3, 2), CellType.Initial);
        Fill(cells, ExitZone, CellType.Exit);
        Fill(cells, new RectInt(86, 29, 3, 2), CellType.Exit);

        return cells;
    }

    private static void Fill(CellType[,] cells, RectInt rect, CellType type)
    {
        for (int x = rect.x; x < rect.x + rect.width; x++)
        for (int z = rect.y; z < rect.y + rect.height; z++)
            cells[x, z] = type;
    }
}
