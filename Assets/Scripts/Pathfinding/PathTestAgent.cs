using System.Collections.Generic;
using UnityEngine;

public class PathTestAgent : MonoBehaviour
{
    [SerializeField] private Graph graph;
    [SerializeField] private LineRenderer line;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform goalPoint;
    [SerializeField] private float speed = 6f;

    private readonly List<Node> path = new List<Node>();
    private readonly List<Node> visited = new List<Node>();
    private int index;

    public IReadOnlyList<Node> CurrentPath => path;
    public IReadOnlyList<Node> Visited => visited;

    public void Configure(Graph graphReference, LineRenderer lineReference, Transform start, Transform goal)
    {
        graph = graphReference;
        line = lineReference;
        startPoint = start;
        goalPoint = goal;
    }

    private void Start()
    {
        Recalculate();
    }

    private void Update()
    {
        if (path.Count == 0 || index >= path.Count) return;

        Vector3 target = path[index].WorldPosition;
        target.y = transform.position.y;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f) transform.forward = direction;

        if ((transform.position - target).sqrMagnitude > 0.01f) return;

        index++;
        if (index < path.Count) return;

        (startPoint, goalPoint) = (goalPoint, startPoint);
        Recalculate();
    }

    public void Recalculate()
    {
        if (graph == null || startPoint == null || goalPoint == null) return;

        graph.EnsureBuilt();

        Node start = graph.GetNearestNode(startPoint.position);
        Node goal = graph.GetNearestNode(goalPoint.position);

        visited.Clear();
        path.Clear();
        path.AddRange(AStar.FindPath(graph, start, goal, graph.IsWalkable, visited));
        index = 0;

        if (path.Count == 0)
        {
            Debug.LogWarning($"No path from {startPoint.name} to {goalPoint.name}.");
            DrawPath();
            return;
        }

        transform.position = new Vector3(path[0].WorldPosition.x, transform.position.y, path[0].WorldPosition.z);
        DrawPath();
    }

    private void DrawPath()
    {
        if (line == null) return;

        line.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            line.SetPosition(i, path[i].WorldPosition + Vector3.up * 0.15f);
    }
}
