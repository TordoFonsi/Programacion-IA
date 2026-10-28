using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] private bool buildOnAwake = true;

    private Node[,] nodes;
    private readonly List<Node> allNodes = new List<Node>();

    public IReadOnlyList<Node> AllNodes => allNodes;
    public int Width => LevelLayout.Width;
    public int Height => LevelLayout.Height;

    private void Awake()
    {
        if (buildOnAwake) Build();
    }

    public void EnsureBuilt()
    {
        if (nodes == null) Build();
    }

    public void Build()
    {
        CellType[,] cells = LevelLayout.Build();
        nodes = new Node[LevelLayout.Width, LevelLayout.Height];
        allNodes.Clear();

        for (int x = 0; x < LevelLayout.Width; x++)
        for (int z = 0; z < LevelLayout.Height; z++)
        {
            if (cells[x, z] == CellType.Solid) continue;

            Vector2Int coordinates = new Vector2Int(x, z);
            Node node = new Node(coordinates, cells[x, z], new Vector3(x + 0.5f, 0f, z + 0.5f));
            nodes[x, z] = node;
            allNodes.Add(node);
        }
    }

    public Node GetNode(Vector2Int coordinates)
    {
        if (coordinates.x < 0 || coordinates.x >= LevelLayout.Width) return null;
        if (coordinates.y < 0 || coordinates.y >= LevelLayout.Height) return null;
        return nodes[coordinates.x, coordinates.y];
    }

    public Node GetNode(Vector3 worldPosition)
    {
        return GetNode(new Vector2Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.z)));
    }

    public Node GetNearestNode(Vector3 worldPosition)
    {
        Node node = GetNode(worldPosition);
        if (node != null) return node;

        int centerX = Mathf.FloorToInt(worldPosition.x);
        int centerZ = Mathf.FloorToInt(worldPosition.z);
        int maxRadius = Mathf.Max(LevelLayout.Width, LevelLayout.Height);

        for (int radius = 1; radius < maxRadius; radius++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                if (Mathf.Abs(x - centerX) != radius && Mathf.Abs(z - centerZ) != radius) continue;

                Node candidate = GetNode(new Vector2Int(x, z));
                if (candidate != null) return candidate;
            }
        }

        return null;
    }

    public IEnumerable<Node> GetNeighbors(Node node)
    {
        Vector2Int coordinates = node.Coordinates;

        Node up = GetNode(new Vector2Int(coordinates.x, coordinates.y + 1));
        if (up != null) yield return up;

        Node right = GetNode(new Vector2Int(coordinates.x + 1, coordinates.y));
        if (right != null) yield return right;

        Node down = GetNode(new Vector2Int(coordinates.x, coordinates.y - 1));
        if (down != null) yield return down;

        Node left = GetNode(new Vector2Int(coordinates.x - 1, coordinates.y));
        if (left != null) yield return left;
    }

    public bool IsWalkable(Node node) => node != null;

    public bool IsWolfWalkable(Node node) => node != null && node.CellType != CellType.SafeZone;
}
