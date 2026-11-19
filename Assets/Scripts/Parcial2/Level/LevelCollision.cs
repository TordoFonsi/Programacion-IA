using UnityEngine;

/// <summary>
/// Grid collision for the agents. The level is made of 1x1 cells, so testing
/// the footprint against the cell grid is cheaper and more reliable than
/// physics colliders, and it also stops diagonal moves from cutting through
/// the corner of a wall.
/// </summary>
public static class LevelCollision
{
    private static CellType[,] cells;

    private static CellType[,] Cells
    {
        get
        {
            if (cells == null) cells = LevelLayout.Build();
            return cells;
        }
    }

    public static bool IsWalkable(Vector3 position, float radius)
    {
        int minX = Mathf.FloorToInt(position.x - radius);
        int maxX = Mathf.FloorToInt(position.x + radius);
        int minZ = Mathf.FloorToInt(position.z - radius);
        int maxZ = Mathf.FloorToInt(position.z + radius);

        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
        {
            if (x < 0 || x >= LevelLayout.Width || z < 0 || z >= LevelLayout.Height) return false;
            if (Cells[x, z] == CellType.Solid) return false;
        }

        return true;
    }
}
