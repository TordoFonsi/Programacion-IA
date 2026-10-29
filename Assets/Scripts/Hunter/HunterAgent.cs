using System.Collections.Generic;
using UnityEngine;

public enum HunterStateId
{
    Search,
    Attack,
    Gather,
    Exit,
    Flee,
    Dead
}

public class HunterAgent : SteeringAgent
{
    [Header("Life")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int goal = 3;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private int attackDamage = 1;

    [Header("Gather")]
    [SerializeField] private float gatherRange = 1.4f;
    [SerializeField] private float gatherTime = 1.5f;
    [SerializeField] private float gatherSearchRadius = 12f;

    [Header("Senses")]
    [SerializeField] private float visionRange = 14f;
    [SerializeField] private float fieldOfView = 100f;
    [SerializeField] private float fleeRadius = 8f;

    [Header("Pathing")]
    [SerializeField] private float waypointDistance = 0.35f;

    private Graph graph;
    private VisionSensor vision;
    private LineRenderer line;
    private StateMachine stateMachine;
    private Renderer body;
    private MaterialPropertyBlock block;
    private Color baseColor;

    private readonly List<Node> path = new List<Node>();
    private int pathIndex;
    private Vector3 pathGoal;
    private float attackTimer;
    private float gatherTimer;
    private int health;
    private bool escaped;

    public int Collected { get; private set; }
    public int Goal => goal;
    public int Health => health;
    public bool IsDead => health <= 0;
    public bool IsEscaped => escaped;
    public HunterStateId StateId { get; private set; } = HunterStateId.Search;
    public string CurrentStateName => StateId.ToString();
    public VisionSensor Vision => vision;
    public Graph Graph => graph;
    public IReadOnlyList<Node> CurrentPath => path;
    public float DeltaTime { get; private set; }
    public float SearchWait { get; set; }
    public Vector3 LastKnownPosition => vision != null ? vision.LastKnownPosition : transform.position;
    public float AttackRange => attackRange;
    public float GatherRange => gatherRange;
    public float GatherSearchRadius => gatherSearchRadius;
    public float FleeRadius => fleeRadius;
    public int StateChanges { get; private set; }

    public void Configure(Graph graphReference, Material material, LineRenderer pathLine, Color color, float speed, float force)
    {
        graph = graphReference;
        line = pathLine;
        baseColor = color;
        maxSpeed = speed;
        maxForce = force;

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

        health = maxHealth;
        Collected = 0;
        escaped = false;
        StateId = HunterStateId.Search;
    }

    private void Awake()
    {
        EnsureStates();
    }

    private void Start()
    {
        StartBrain();
    }

    public void StartBrain()
    {
        EnsureStates();
        stateMachine.ChangeState(HunterStateId.Search);
        StateId = HunterStateId.Search;
    }

    private void EnsureStates()
    {
        if (stateMachine != null) return;

        stateMachine = new StateMachine();
        stateMachine.RegisterState(HunterStateId.Search, new HunterSearchState(this, stateMachine));
        stateMachine.RegisterState(HunterStateId.Attack, new HunterAttackState(this, stateMachine));
        stateMachine.RegisterState(HunterStateId.Gather, new HunterGatherState(this, stateMachine));
        stateMachine.RegisterState(HunterStateId.Exit, new HunterExitState(this, stateMachine));
        stateMachine.RegisterState(HunterStateId.Flee, new HunterFleeState(this, stateMachine));
        stateMachine.RegisterState(HunterStateId.Dead, new HunterDeadState(this, stateMachine));
    }

    private void OnEnable()
    {
        ThreatRegistry.Register(transform);
        HunterRegistry.Register(this);
    }

    private void OnDisable()
    {
        ThreatRegistry.Unregister(transform);
        HunterRegistry.Unregister(this);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (graph == null) return;

        DeltaTime = deltaTime;
        attackTimer += deltaTime;
        if (vision != null) vision.Scan(deltaTime);

        if (IsDead && StateId != HunterStateId.Dead)
        {
            ChangeState(HunterStateId.Dead);
            return;
        }

        stateMachine.Update();
    }

    public void ChangeState(HunterStateId id)
    {
        if (StateId == id) return;

        StateChanges++;
        StateId = id;
        stateMachine.ChangeState(id);
    }

    public bool TryHigherPriority(HunterStateId current)
    {
        if (IsDead)
        {
            ChangeState(HunterStateId.Dead);
            return true;
        }

        if (ShouldFlee())
        {
            if (current == HunterStateId.Flee) return false;

            ChangeState(HunterStateId.Flee);
            return true;
        }

        if (current == HunterStateId.Flee)
        {
            ChangeState(ChooseFallback());
            return true;
        }

        if (current != HunterStateId.Exit && Collected >= goal)
        {
            ChangeState(HunterStateId.Exit);
            return true;
        }

        if (current != HunterStateId.Gather &&
            current != HunterStateId.Exit &&
            FindDeadPrey() != null)
        {
            ChangeState(HunterStateId.Gather);
            return true;
        }

        if (current != HunterStateId.Attack &&
            current != HunterStateId.Gather &&
            current != HunterStateId.Exit &&
            vision.IsVisible &&
            vision.Target != null)
        {
            ChangeState(HunterStateId.Attack);
            return true;
        }

        return false;
    }

    private HunterStateId ChooseFallback()
    {
        if (Collected >= goal) return HunterStateId.Exit;
        if (FindDeadPrey() != null) return HunterStateId.Gather;
        if (vision.IsVisible && vision.Target != null) return HunterStateId.Attack;
        return HunterStateId.Search;
    }

    public bool ShouldFlee()
    {
        return ThreatRegistry.FindNearest(transform.position, fleeRadius, transform) != null;
    }

    public PreyAgent FindDeadPrey()
    {
        PreyAgent best = null;
        float bestDistance = gatherSearchRadius * gatherSearchRadius;

        IReadOnlyList<PreyAgent> all = PreyRegistry.All;
        for (int i = 0; i < all.Count; i++)
        {
            PreyAgent prey = all[i];
            if (prey == null || !prey.IsDead || prey.IsCollected) continue;

            float distance = (prey.transform.position - transform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = prey;
        }

        return best;
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

        if (start == null || goalNode == null)
        {
            DrawPath();
            return false;
        }

        path.AddRange(AStar.FindPath(graph, start, goalNode, graph.IsWalkable));
        DrawPath();
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

    public bool TryAttack(PreyAgent target)
    {
        if (target == null || attackTimer < attackCooldown) return false;

        attackTimer = 0f;
        target.ApplyDamage(attackDamage, transform.position);
        return true;
    }

    public bool GatherTick(PreyAgent target)
    {
        if (target == null || !target.IsDead || target.IsCollected) return false;

        gatherTimer += DeltaTime;
        if (gatherTimer < gatherTime) return true;

        gatherTimer = 0f;
        if (!target.Collect()) return false;

        Collected++;
        return false;
    }

    public void ApplyDamage(int amount, Vector3 source)
    {
        if (IsDead) return;

        health -= amount;
        if (health > 0) return;

        health = 0;
        ChangeState(HunterStateId.Dead);
    }

    public void MarkEscaped()
    {
        escaped = true;
        Halt();
    }

    public void SetColor(Color color)
    {
        if (body == null || block == null) return;

        body.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        body.SetPropertyBlock(block);
    }

    public void ResetColor()
    {
        SetColor(baseColor);
    }

    public void SetGoal(int value)
    {
        goal = Mathf.Max(1, value);
    }

    public Vector3 ExitPosition => new Vector3(
        LevelLayout.ExitZone.x + LevelLayout.ExitZone.width * 0.5f,
        transform.position.y,
        LevelLayout.ExitZone.y + LevelLayout.ExitZone.height * 0.5f);

    public bool IsInsideExitZone()
    {
        Vector2Int cell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.z));

        return LevelLayout.ExitZone.Contains(cell);
    }

    public bool IsInsideSafeZone()
    {
        IReadOnlyList<Zone> zones = ZoneRegistry.Zones;
        for (int i = 0; i < zones.Count; i++)
        {
            Zone zone = zones[i];
            if (zone == null || zone.Type != ZoneType.SafeZone) continue;
            if (zone.Contains(transform.position)) return true;
        }

        return false;
    }

    public Vector3 NearestSafeZone()
    {
        Vector3 best = transform.position;
        float bestDistance = float.MaxValue;

        IReadOnlyList<Zone> zones = ZoneRegistry.Zones;
        for (int i = 0; i < zones.Count; i++)
        {
            Zone zone = zones[i];
            if (zone == null || zone.Type != ZoneType.SafeZone) continue;

            float distance = (zone.Center - transform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = zone.Center;
        }

        if (bestDistance < float.MaxValue) return best;

        Transform threat = ThreatRegistry.FindNearest(transform.position, 100f, transform);
        if (threat == null) return transform.position;

        Vector3 escape = transform.position - threat.position;
        escape.y = 0f;
        return transform.position + escape.normalized * 10f;
    }

    public Vector3 RandomSearchPoint()
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

        return nodes[Random.Range(0, nodes.Count)].WorldPosition;
    }

    private void DrawPath()
    {
        if (line == null) return;

        line.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            line.SetPosition(i, path[i].WorldPosition + Vector3.up * 0.2f);
    }

    private void OnDrawGizmos()
    {
        if (!GizmoSettings.CombatRadii) return;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, gatherRange);

        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
    }
}
