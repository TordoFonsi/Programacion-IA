using System.Collections.Generic;
using UnityEngine;

public class GraphVisualizer : MonoBehaviour
{
    [SerializeField] private Graph graph;

    private static readonly Color NodeColor = new Color(0.2f, 0.9f, 0.4f, 0.45f);

    public void Configure(Graph graphReference)
    {
        graph = graphReference;
    }

    private void OnDrawGizmos()
    {
        if (graph == null || !GizmoSettings.Graph) return;

        graph.EnsureBuilt();
        DrawNodes(graph.AllNodes);
    }

    private static void DrawNodes(IReadOnlyList<Node> nodes)
    {
        Gizmos.color = NodeColor;
        Vector3 scale = new Vector3(0.18f, 0.02f, 0.18f);

        for (int i = 0; i < nodes.Count; i++)
            Gizmos.DrawCube(nodes[i].WorldPosition + Vector3.up * 0.05f, scale);
    }
}
