using System;
using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
    public static List<Node> FindPath(Graph graph, Node start, Node goal, Func<Node, bool> canTraverse, List<Node> visited = null)
    {
        List<Node> empty = new List<Node>();
        if (graph == null || start == null || goal == null || canTraverse == null) return empty;
        if (!canTraverse(start) || !canTraverse(goal)) return empty;

        PriorityQueue<Node> frontier = new PriorityQueue<Node>();
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        Dictionary<Node, float> costSoFar = new Dictionary<Node, float>();
        HashSet<Node> processed = new HashSet<Node>();

        frontier.Enqueue(start, 0f);
        cameFrom[start] = null;
        costSoFar[start] = 0f;

        while (frontier.Count > 0)
        {
            Node current = frontier.Dequeue();
            if (!processed.Add(current)) continue;

            visited?.Add(current);
            if (current == goal) return Reconstruct(cameFrom, current);

            foreach (Node neighbor in graph.GetNeighbors(current))
            {
                if (!canTraverse(neighbor)) continue;

                float newCost = costSoFar[current] + neighbor.Cost;
                if (costSoFar.TryGetValue(neighbor, out float previous) && newCost >= previous) continue;

                costSoFar[neighbor] = newCost;
                cameFrom[neighbor] = current;
                frontier.Enqueue(neighbor, newCost + Heuristic(neighbor, goal));
                processed.Remove(neighbor);
            }
        }

        return empty;
    }

    public static float Heuristic(Node from, Node to)
    {
        return Mathf.Abs(from.Coordinates.x - to.Coordinates.x) + Mathf.Abs(from.Coordinates.y - to.Coordinates.y);
    }

    private static List<Node> Reconstruct(Dictionary<Node, Node> cameFrom, Node current)
    {
        List<Node> path = new List<Node>();

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }
}
