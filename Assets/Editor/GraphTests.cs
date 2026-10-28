using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GraphTests
{
    [MenuItem("Tools/Level/Test Graph")]
    public static void Run()
    {
        GameObject host = new GameObject("Graph Test");
        Graph graph = host.AddComponent<Graph>();
        graph.Build();

        Node initial = graph.GetNode(new Vector3(6.5f, 0f, 30f));
        Node exit = graph.GetNode(new Vector3(93.5f, 0f, 30f));
        Node coto = graph.GetNode(new Vector3(50f, 0f, 30f));
        Node safeA = graph.GetNode(new Vector3(75f, 0f, 44f));
        Node safeB = graph.GetNode(new Vector3(25f, 0f, 16f));

        Debug.Log($"Graph nodes: {graph.AllNodes.Count}");

        Test("Initial -> Exit", graph, initial, exit, graph.IsWalkable, true);
        Test("Initial -> Coto", graph, initial, coto, graph.IsWalkable, true);
        Test("Coto -> SafeA", graph, coto, safeA, graph.IsWalkable, true);
        Test("Initial -> SafeB", graph, initial, safeB, graph.IsWalkable, true);
        Test("Wolf Initial -> SafeA", graph, initial, safeA, graph.IsWolfWalkable, false);
        Test("Wolf Coto -> Exit", graph, coto, exit, graph.IsWolfWalkable, true);

        UnityEngine.Object.DestroyImmediate(host);
    }

    private static void Test(string name, Graph graph, Node start, Node goal, Func<Node, bool> canTraverse, bool expectPath)
    {
        List<Node> path = AStar.FindPath(graph, start, goal, canTraverse);
        bool hasPath = path.Count > 0;

        bool contiguous = true;
        for (int i = 1; i < path.Count; i++)
        {
            int distance = Mathf.Abs(path[i].Coordinates.x - path[i - 1].Coordinates.x) +
                           Mathf.Abs(path[i].Coordinates.y - path[i - 1].Coordinates.y);

            if (distance != 1) contiguous = false;
        }

        bool ok = hasPath == expectPath && contiguous;
        string expected = expectPath ? "path" : "blocked";
        Debug.Log($"{name}: {(ok ? "OK" : "FAIL")} | path={path.Count} expected={expected}");
    }
}
