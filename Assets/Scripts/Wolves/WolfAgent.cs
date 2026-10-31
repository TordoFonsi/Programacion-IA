using System.Collections.Generic;
using UnityEngine;

public class WolfAgent : SteeringAgent
{
    [Header("Combat")]
    [SerializeField] private float captureRadius = 1.6f;

    [Header("Senses")]
    [SerializeField] private float visionRange = 14f;
    [SerializeField] private float fieldOfView = 110f;
    [SerializeField] private float closeRange = 3f;
    [SerializeField] private float eyeHeight = 1.1f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Patrol")]
    [SerializeField] private Vector3[] patrolRoute;
    [SerializeField] private float waypointWait = 1f;
    [SerializeField] private float waypointDistance = 0.5f;

    [Header("Search")]
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float repathInterval = 0.5f;
    [SerializeField] private float captureCooldown = 8f;

    private Graph graph;
    private WolfPack pack;
    private LineRenderer pathLine;
    private Renderer body;
    private MaterialPropertyBlock block;
    private Color baseColor;

    private readonly List<Node> path = new List<Node>();
    private int pathIndex;
    private Vector3 pathGoal;
    private int patrolIndex;
    private int patrolDirection = 1;
    private float waitTimer;
    private float searchTimer;
    private float repathTimer;
    private bool hasAlert;
    private Vector3 lastSafePosition;
    private float huntCooldown;

    public float DeltaTime { get; private set; }
    public string CurrentBehaviour { get; private set; } = "Patrol";

    public static readonly Color PatrolColor = new Color(0.55f, 0.58f, 0.62f);
    public static readonly Color AlertColor = new Color(0.95f, 0.55f, 0.15f);
    public static readonly Color SearchColor = new Color(0.85f, 0.35f, 0.75f);
    public static readonly Color ChaseColor = new Color(0.90f, 0.15f, 0.15f);

    public static Color BehaviorColor(string behaviour)
    {
        switch (behaviour)
        {
            case "Chase": return ChaseColor;
            case "Alert": return AlertColor;
            case "Search": return SearchColor;
            default: return PatrolColor;
        }
    }

    public HunterAgent Target { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public bool HasAlert => hasAlert;
    public bool SeesTarget { get; private set; }
    public IReadOnlyList<Node> CurrentPath => path;
    public float VisionRange => visionRange;
    public float FieldOfView => fieldOfView;
    public float CaptureRadius => captureRadius;
    public string WolfName => name;

    public void Configure(
        Graph graphReference,
        Material material,
        Color color,
        float speed,
        float force,
        Vector3[] route,
        LineRenderer line = null)
    {
        graph = graphReference;
        baseColor = color;
        maxSpeed = speed;
        maxForce = force;
        patrolRoute = route;
        patrolIndex = 0;
        patrolDirection = 1;
        pathLine = line;
        lastSafePosition = transform.position;

        body = GetComponent<MeshRenderer>();
        if (body != null)
        {
            body.sharedMaterial = material;
            block = new MaterialPropertyBlock();
            SetColor(baseColor);
        }

        DrawPath();
    }

    public void SetPack(WolfPack wolfPack)
    {
        pack = wolfPack;
    }

    private void OnEnable()
    {
        ThreatRegistry.Register(transform);
        WolfRegistry.Register(this);
    }

    private void OnDisable()
    {
        ThreatRegistry.Unregister(transform);
        WolfRegistry.Unregister(this);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (graph == null) return;

        DeltaTime = deltaTime;
        repathTimer -= deltaTime;
        huntCooldown = Mathf.Max(0f, huntCooldown - deltaTime);

        // After a capture the wolf ignores the hunter for a while, otherwise the
        // pack camps the respawn point and kills it again and again.
        HunterAgent seen = null;
        SeesTarget = huntCooldown <= 0f && TrySeeHunter(out seen);

        if (seen != null)
        {
            bool newTarget = Target != seen;

            Target = seen;
            LastKnownPosition = seen.transform.position;
            hasAlert = true;
            searchTimer = searchDuration;

            if (newTarget && pack != null) pack.Alert(LastKnownPosition, this);
        }

        if (Target != null && Target.IsDead) LoseTarget();
        else if (hasAlert && IsHunterProtected()) LoseTarget();

        if (Target != null && SeesTarget) Chase();
        else if (hasAlert) MoveToLastKnown();
        else Patrol();

        KeepOutOfProtectedZones();
        TryCapture();
        DrawPath();
    }

    public void ReceiveAlert(Vector3 position)
    {
        if (huntCooldown > 0f) return;

        LastKnownPosition = position;
        hasAlert = true;
        searchTimer = searchDuration;
        path.Clear();
        pathIndex = 0;
        repathTimer = 0f;
    }

    private void LoseTarget()
    {
        Target = null;
        hasAlert = false;
        SeesTarget = false;
        searchTimer = 0f;
        path.Clear();
        pathIndex = 0;
        SetColor(baseColor);
        DrawPath();
    }

    private void Chase()
    {
        CurrentBehaviour = "Chase";
        SetColor(ChaseColor);

        Vector3 offset = Target.transform.position - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude <= waypointDistance * waypointDistance)
        {
            Halt();
            return;
        }

        if (HasLineOfSight(Target.transform.position + Vector3.up * 0.6f))
        {
            path.Clear();
            pathIndex = 0;
            ApplyAcceleration(Steer(offset.normalized * maxSpeed), DeltaTime);
            return;
        }

        if (repathTimer <= 0f || path.Count == 0)
        {
            SetDestination(Target.transform.position);
            repathTimer = repathInterval;
        }

        FollowPath(DeltaTime);
    }

    private void MoveToLastKnown()
    {
        CurrentBehaviour = "Alert";
        SetColor(AlertColor);

        Vector3 offset = LastKnownPosition - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= 1.5f || (path.Count > 0 && HasArrived))
        {
            Halt();
            Search();
            return;
        }

        if (HasLineOfSight(LastKnownPosition + Vector3.up * 0.5f))
        {
            path.Clear();
            pathIndex = 0;
            ApplyAcceleration(Steer(offset.normalized * maxSpeed), DeltaTime);
            return;
        }

        if (repathTimer <= 0f || path.Count == 0)
        {
            SetDestination(LastKnownPosition);
            repathTimer = repathInterval;
        }

        FollowPath(DeltaTime);
    }

    private void Search()
    {
        CurrentBehaviour = "Search";
        SetColor(SearchColor);

        searchTimer -= DeltaTime;
        transform.Rotate(Vector3.up, 90f * DeltaTime, Space.World);

        if (searchTimer > 0f) return;

        LoseTarget();
    }

    private void Patrol()
    {
        CurrentBehaviour = "Patrol";
        SetColor(PatrolColor);

        if (patrolRoute == null || patrolRoute.Length == 0) return;

        if (waitTimer > 0f)
        {
            Halt();
            waitTimer -= DeltaTime;
            return;
        }

        Vector3 waypoint = patrolRoute[patrolIndex];
        Vector3 offset = waypoint - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude <= waypointDistance * waypointDistance)
        {
            BeginWait();
            return;
        }

        if (HasLineOfSight(waypoint + Vector3.up * 0.5f))
        {
            path.Clear();
            pathIndex = 0;
            ApplyAcceleration(Steer(offset.normalized * maxSpeed), DeltaTime);
            return;
        }

        if (repathTimer <= 0f || path.Count == 0)
        {
            SetDestination(waypoint);
            repathTimer = repathInterval;
        }

        if (path.Count > 0 && HasArrived)
        {
            BeginWait();
            return;
        }

        FollowPath(DeltaTime);
    }

    private void BeginWait()
    {
        waitTimer = waypointWait;
        path.Clear();
        pathIndex = 0;

        if (patrolRoute.Length == 1) return;

        patrolIndex += patrolDirection;

        if (patrolIndex >= patrolRoute.Length)
        {
            patrolDirection = -1;
            patrolIndex = patrolRoute.Length - 2;
            return;
        }

        if (patrolIndex < 0)
        {
            patrolDirection = 1;
            patrolIndex = 1;
        }
    }

    private void TryCapture()
    {
        if (Target == null || Target.IsDead || Target.IsProtected()) return;

        Vector3 point = Target.transform.position + Vector3.up * 0.6f;
        Vector3 offset = Target.transform.position - transform.position;
        offset.y = 0f;
        if (offset.magnitude > captureRadius) return;
        if (!SeesTarget && !HasLineOfSight(point)) return;

        Target.Kill();
        huntCooldown = captureCooldown;
        LoseTarget();
    }

    public bool SetDestination(Vector3 worldPosition)
    {
        if (path.Count > 0 && (worldPosition - pathGoal).sqrMagnitude < 1f) return true;

        graph.EnsureBuilt();
        Node start = graph.GetNearestNode(transform.position);
        Node goalNode = graph.GetNearestNode(worldPosition);

        path.Clear();
        pathIndex = 0;
        pathGoal = worldPosition;

        if (start == null || goalNode == null) return false;

        path.AddRange(AStar.FindPath(graph, start, goalNode, graph.IsWolfWalkable));
        return path.Count > 0;
    }

    public bool HasArrived => path.Count == 0 || pathIndex >= path.Count;

    public void FollowPath(float deltaTime)
    {
        FollowNodes(path, ref pathIndex, waypointDistance, deltaTime);
    }

    public void Halt()
    {
        Stop();
    }

    private bool TrySeeHunter(out HunterAgent hunter)
    {
        hunter = null;

        IReadOnlyList<HunterAgent> all = HunterRegistry.All;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;

        for (int i = 0; i < all.Count; i++)
        {
            HunterAgent candidate = all[i];
            if (candidate == null || candidate.IsDead) continue;
            if (candidate.IsProtected()) continue;

            Vector3 point = candidate.transform.position + Vector3.up * 0.6f;
            Vector3 offset = point - origin;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance > visionRange || distance < 0.0001f) continue;

            if (distance > closeRange)
            {
                float angle = Vector3.Angle(transform.forward, offset);
                if (angle > fieldOfView * 0.5f) continue;
            }

            if (Physics.Raycast(origin, offset.normalized, distance, obstacleMask)) continue;

            hunter = candidate;
            return true;
        }

        return false;
    }

    private bool IsHunterProtected()
    {
        IReadOnlyList<HunterAgent> all = HunterRegistry.All;
        for (int i = 0; i < all.Count; i++)
        {
            HunterAgent hunter = all[i];
            if (hunter == null || hunter.IsDead) continue;
            if (hunter.IsProtected()) return true;
        }

        return false;
    }

    private bool HasLineOfSight(Vector3 point)
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 offset = point - origin;
        float distance = offset.magnitude;

        if (distance < 0.01f) return true;

        return !Physics.Raycast(origin, offset / distance, distance, obstacleMask);
    }

    public bool IsInsideSafeZone()
    {
        Vector2Int cell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.z));

        if (graph != null)
        {
            Node node = graph.GetNode(cell);
            if (node != null) return node.CellType == CellType.SafeZone;
        }

        return LevelLayout.SafeZoneA.Contains(cell) || LevelLayout.SafeZoneB.Contains(cell);
    }

    /// <summary>
    /// Safe zones and the hunter spawn are off limits for the pack.
    /// </summary>
    private bool IsInsideBlockedZone()
    {
        Vector2Int cell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.z));

        if (graph != null)
        {
            Node node = graph.GetNode(cell);
            if (node != null)
                return node.CellType == CellType.SafeZone || node.CellType == CellType.Initial;
        }

        return LevelLayout.SafeZoneA.Contains(cell) ||
               LevelLayout.SafeZoneB.Contains(cell) ||
               LevelLayout.InitialZone.Contains(cell);
    }

    private void KeepOutOfProtectedZones()
    {
        if (!IsInsideBlockedZone())
        {
            lastSafePosition = transform.position;
            return;
        }

        transform.position = lastSafePosition;
        Velocity = Vector3.zero;
        path.Clear();
        pathIndex = 0;
    }

    private void DrawPath()
    {
        if (pathLine == null) return;

        bool show = hasAlert && path.Count > 1;
        pathLine.enabled = show;

        if (!show)
        {
            pathLine.positionCount = 0;
            return;
        }

        pathLine.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            pathLine.SetPosition(i, path[i].WorldPosition + Vector3.up * 0.15f);
    }

    public void SetColor(Color color)
    {
        if (body == null || block == null) return;

        body.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        body.SetPropertyBlock(block);
    }

    private void OnDrawGizmos()
    {
        if (!GizmoSettings.VisionCones) return;

        DrawVisionCone();
    }

    private void DrawVisionCone()
    {
        const int steps = 14;
        float half = fieldOfView * 0.5f;

        Gizmos.color = Target != null
            ? new Color(1f, 0.25f, 0.2f, 0.75f)
            : new Color(1f, 0.55f, 0.1f, 0.35f);

        Vector3 previous = GroundPoint(-half);

        for (int i = 1; i <= steps; i++)
        {
            float angle = -half + fieldOfView * i / steps;
            Vector3 point = GroundPoint(angle);

            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.DrawLine(GroundOrigin, GroundPoint(-half));
        Gizmos.DrawLine(GroundOrigin, GroundPoint(half));

        if (!hasAlert) return;

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(LastKnownPosition + Vector3.up * 0.3f, 0.7f);
    }

    private Vector3 GroundOrigin => new Vector3(transform.position.x, 0.05f, transform.position.z);

    private Vector3 GroundPoint(float angle)
    {
        Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
        direction.y = 0f;

        Vector3 point = GroundOrigin + direction.normalized * visionRange;
        point.y = 0.05f;
        return point;
    }

    private void OnDrawGizmosSelected()
    {
        if (!GizmoSettings.CombatRadii) return;

        Gizmos.color = new Color(0.9f, 0.15f, 0.1f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}
