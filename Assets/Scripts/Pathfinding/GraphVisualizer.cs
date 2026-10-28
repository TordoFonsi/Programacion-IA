using System.Collections.Generic;
using UnityEngine;

public class GraphVisualizer : MonoBehaviour
{
    [SerializeField] private Graph graph;
    [SerializeField] private PathTestAgent agent;
    [SerializeField] private bool showNodes;
    [SerializeField] private bool showSearch;

    private static readonly Color NodeColor = new Color(0.2f, 0.9f, 0.4f, 0.45f);
    private static readonly Color SearchColor = new Color(0.2f, 0.5f, 1f, 0.35f);
    private static readonly Color PathColor = new Color(1f, 0.85f, 0.2f);

    public void Configure(Graph graphReference, PathTestAgent agentReference)
    {
        graph = graphReference;
        agent = agentReference;
    }

    private void OnDrawGizmos()
    {
        if (graph == null) return;
        graph.EnsureBuilt();

        if (showNodes) DrawNodes(graph.AllNodes, NodeColor, 0.18f);
        if (showSearch && agent != null) DrawNodes(agent.Visited, SearchColor, 0.3f);
        if (agent != null) DrawPath(agent.CurrentPath);
    }

    private static void DrawNodes(IReadOnlyList<Node> nodes, Color color, float size)
    {
        Gizmos.color = color;
        Vector3 scale = new Vector3(size, 0.02f, size);

        for (int i = 0; i < nodes.Count; i++)
            Gizmos.DrawCube(nodes[i].WorldPosition + Vector3.up * 0.05f, scale);
    }

    private static void DrawPath(IReadOnlyList<Node> path)
    {
        if (path == null || path.Count < 2) return;

        Gizmos.color = PathColor;
        for (int i = 0; i < path.Count - 1; i++)
            Gizmos.DrawLine(path[i].WorldPosition + Vector3.up * 0.12f, path[i + 1].WorldPosition + Vector3.up * 0.12f);
    }
}
