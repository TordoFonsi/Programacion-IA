using UnityEngine;

public class Node
{
    public Vector2Int Coordinates { get; }
    public Vector3 WorldPosition { get; }
    public CellType CellType { get; }
    public int Cost { get; set; } = 1;

    public Node(Vector2Int coordinates, CellType cellType, Vector3 worldPosition)
    {
        Coordinates = coordinates;
        CellType = cellType;
        WorldPosition = worldPosition;
    }
}
