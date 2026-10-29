using System.Collections.Generic;
using UnityEngine;

public class WolfAgent : SteeringAgent
{
    [Header("Combat")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.1f;
    [SerializeField] private int attackDamage = 1;

    [Header("Senses")]
    [SerializeField] private float visionRange = 12f;
    [SerializeField] private float fieldOfView = 120f;
    [SerializeField] private float aggroRadius = 10f;

    [Header("Pathing")]
    [SerializeField] private float waypointDistance = 0.4f;

    private Graph graph;
    private VisionSensor vision;
    private Renderer body;
    private MaterialPropertyBlock block;
    private Color baseColor;

    private readonly List<Node> path = new List<Node>();
    private int pathIndex;
    private Vector3 pathGoal;
    private Vector3 lastSafePosition;
    private float attackTimer;
    private float waitTimer;

    public float DeltaTime { get; private set; }
    public string CurrentBehaviour { get; private set; } = "Patrol";
    public PreyAgent PreyTarget { get; private set; }
    public HunterAgent HunterTarget { get; private set; }
    public IReadOnlyList<Node> CurrentPath => path;
    public float AggroRadius => aggroRadius;
    public float AttackRange => attackRange;

    public void Configure(Graph graphReference, Material material, Color color, float speed, float force)
    {
        graph = graphReference;
        baseColor = color;
        maxSpeed = speed;
        maxForce = force;
        lastSafePosition = transform.position;

        body = GetComponentInChildren<Renderer>();
        if (body != null)
        {
            body.sharedMaterial = material;
            block = new MaterialPropertyBlock();
            SetColor(baseColor);
        }

        vision = GetComponent<VisionSensor>();
        if (vision == null) vision = gameObject.AddComponent<VisionSensor>();
        vision.Configure(visionRange, fieldOfView, 3f, ~0);
    }

    private void OnEnable()
    {
        ThreatRegistry.Register(transform);
    }

    private void OnDisable()
    {
        ThreatRegistry.Unregister(transform);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (graph == null || vision == null) return;

        DeltaTime = deltaTime;
        attackTimer += deltaTime;
        vision.Scan(deltaTime);

        ChooseTarget();

        if (HunterTarget != null) ChaseHunter();
        else if (PreyTarget != null) ChasePrey();
        else Patrol();

        KeepOutOfSafeZone();
    }

    private void ChooseTarget()
    {
        PreyTarget = vision.IsVisible ? vision.Target : null;
        HunterTarget = HunterRegistry.FindNearest(transform.position, aggroRadius);

        if (PreyTarget != null && !PreyTarget.IsActive) PreyTarget = null;
        if (HunterTarget != null && HunterTarget.IsDead) HunterTarget = null;

        if (PreyTarget == null || HunterTarget == null) return;

        float preyDistance = (PreyTarget.transform.position - transform.position).sqrMagnitude;
        float hunterDistance = (HunterTarget.transform.position - transform.position).sqrMagnitude;

        if (hunterDistance < preyDistance) PreyTarget = null;
        else HunterTarget = null;
    }

    private void ChaseHunter()
    {
        CurrentBehaviour = "Chase Hunter";

        Vector3 offset = HunterTarget.transform.position - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= attackRange)
        {
            Halt();
            Face(HunterTarget.transform.position);
            CurrentBehaviour = "Attack Hunter";

            if (attackTimer < attackCooldown) return;

            attackTimer = 0f;
            HunterTarget.ApplyDamage(attackDamage, transform.position);
            return;
        }

        SetDestination(HunterTarget.transform.position);
        FollowPath(DeltaTime);
    }

    private void ChasePrey()
    {
        CurrentBehaviour = "Chase Prey";

        Vector3 offset = PreyTarget.transform.position - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= attackRange)
        {
            Halt();
            Face(PreyTarget.transform.position);
            CurrentBehaviour = "Attack Prey";

            if (attackTimer < attackCooldown) return;

            attackTimer = 0f;
            PreyTarget.ApplyDamage(attackDamage, transform.position);
            return;
        }

        SetDestination(PreyTarget.transform.position);
        FollowPath(DeltaTime);
    }

    private void Patrol()
    {
        CurrentBehaviour = "Patrol";

        if (HasArrived)
        {
            Halt();
            waitTimer -= DeltaTime;
            if (waitTimer > 0f) return;

            SetDestination(RandomPatrolPoint());
            waitTimer = 1.2f;
            return;
        }

        FollowPath(DeltaTime);
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
        if (HasArrived)
        {
            Halt();
            return;
        }

        Vector3 target = path[pathIndex].WorldPosition;
        target.y = transform.position.y;

        Vector3 offset = target - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude <= waypointDistance * waypointDistance)
        {
            pathIndex++;
            if (HasArrived)
            {
                Halt();
                return;
            }

            target = path[pathIndex].WorldPosition;
            target.y = transform.position.y;
            offset = target - transform.position;
            offset.y = 0f;
        }

        Vector3 desired = offset.normalized * maxSpeed;
        ApplyAcceleration(Steer(desired), deltaTime);
    }

    public void Halt()
    {
        Stop();
    }

    public void Face(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) transform.forward = direction;
    }

    public Vector3 RandomPatrolPoint()
    {
        graph.EnsureBuilt();

        IReadOnlyList<Node> nodes = graph.AllNodes;
        if (nodes.Count == 0) return transform.position;

        for (int attempt = 0; attempt < 24; attempt++)
        {
            Node node = nodes[Random.Range(0, nodes.Count)];
            if (node.CellType == CellType.HuntingGround) return node.WorldPosition;
        }

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Node node = nodes[Random.Range(0, nodes.Count)];
            if (node.CellType != CellType.SafeZone) return node.WorldPosition;
        }

        return transform.position;
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

    private void KeepOutOfSafeZone()
    {
        if (!IsInsideSafeZone())
        {
            lastSafePosition = transform.position;
            return;
        }

        transform.position = lastSafePosition;
        Velocity = Vector3.zero;
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
        if (!GizmoSettings.CombatRadii) return;

        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);

        Gizmos.color = new Color(0.9f, 0.15f, 0.1f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
