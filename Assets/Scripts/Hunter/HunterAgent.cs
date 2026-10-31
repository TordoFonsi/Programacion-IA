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
    [SerializeField] private int requiredGathers = 3;
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private float safeZoneWaitTime = 3f;

    [Header("Combat")]
    [SerializeField] private float meleeAttackRange = 1.6f;
    [SerializeField] private float rangeAttackRange = 6f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private int attackDamage = 1;

    [Header("Gather")]
    [SerializeField] private float gatherRange = 1.4f;
    [SerializeField] private float gatherTime = 1.5f;

    [Header("Senses")]
    [SerializeField] private float visionRange = 14f;
    [SerializeField] private float fieldOfView = 100f;
    [SerializeField] private float eyeHeight = 1.2f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Pathing")]
    [SerializeField] private float waypointDistance = 0.35f;

    [Header("Interest Objects")]
    [SerializeField] private float interestSpawnInterval = 6f;
    [SerializeField] private int maxInterestObjects = 5;
    [SerializeField] private Material interestMaterial;

    private Graph graph;
    private VisionSensor vision;
    private LineRenderer line;
    private LineRenderer attackLine;
    private float attackFlashTimer;
    private StateMachine stateMachine;
    private Renderer body;
    private MaterialPropertyBlock block;
    private Color baseColor;

    private readonly List<Node> path = new List<Node>();
    private readonly List<GameObject> interestObjects = new List<GameObject>();
    private Transform interestRoot;
    private int pathIndex;
    private Vector3 pathGoal;
    private float attackTimer;
    private float gatherTimer;
    private float interestTimer;
    private float respawnTimer;
    private int health;
    private bool dead;

    public int Collected { get; private set; }
    public int Goal => requiredGathers;
    public int Health => health;
    public bool IsDead => dead;
    public float RespawnRemaining => dead ? Mathf.Max(0f, respawnTimer) : 0f;
    public int RunsCompleted { get; private set; }
    public HunterStateId StateId { get; private set; } = HunterStateId.Search;
    public string CurrentStateName => StateId.ToString();
    public VisionSensor Vision => vision;
    public Graph Graph => graph;
    public IReadOnlyList<Node> CurrentPath => path;
    public float DeltaTime { get; private set; }
    public float SearchWait { get; set; }
    public Vector3 LastKnownPosition => vision != null ? vision.LastKnownPosition : transform.position;
    public float MeleeAttackRange => meleeAttackRange;
    public float RangeAttackRange => rangeAttackRange;
    public float VisionRadius => visionRange;
    public float GatherRange => gatherRange;
    public float RespawnTime => respawnTime;
    public float SafeZoneWaitTime => safeZoneWaitTime;
    public int StateChanges { get; private set; }
    public WolfAgent DetectedWolf { get; private set; }

    public static readonly Color SearchColor = new Color(0.35f, 0.80f, 1.00f);
    public static readonly Color AttackColor = new Color(0.95f, 0.20f, 0.20f);
    public static readonly Color GatherColor = new Color(0.30f, 0.85f, 0.35f);
    public static readonly Color ExitColor = new Color(0.30f, 0.50f, 0.95f);
    public static readonly Color FleeColor = new Color(1.00f, 0.85f, 0.20f);
    public static readonly Color DeadColor = new Color(0.30f, 0.27f, 0.25f);

    public static Color StateColor(HunterStateId id)
    {
        switch (id)
        {
            case HunterStateId.Attack: return AttackColor;
            case HunterStateId.Gather: return GatherColor;
            case HunterStateId.Exit: return ExitColor;
            case HunterStateId.Flee: return FleeColor;
            case HunterStateId.Dead: return DeadColor;
            default: return SearchColor;
        }
    }

    /// <summary>
    /// What the hunter is doing right now, in words. Used by the HUD so the
    /// player can tell a melee attack from a ranged one, a gather in progress,
    /// a flee towards a specific safe zone, and so on.
    /// </summary>
    public string ActionLabel
    {
        get
        {
            switch (StateId)
            {
                case HunterStateId.Dead:
                    return $"Muerto - reaparece en {RespawnRemaining:0.0} s";

                case HunterStateId.Flee:
                    if (IsInsideSafeZone())
                    {
                        float wait = Mathf.Max(0f, safeZoneWaitTime - FleeWait);
                        return $"Refugiado en {SafeZoneTargetName} - sale en {wait:0.0} s";
                    }

                    return "Huyendo a zona segura";

                case HunterStateId.Exit:
                    return "Yendo a la salida";

                case HunterStateId.Gather:
                    return $"Recolectando boid ({GatherProgress * 100f:0}%)";

                case HunterStateId.Attack:
                    return AttackMode;

                default:
                    return DetectedWolf != null ? "Alerta: lobo a la vista" : "Explorando el coto";
            }
        }
    }

    public string TargetLabel
    {
        get
        {
            if (DetectedWolf != null)
            {
                float distance = Vector3.Distance(transform.position, DetectedWolf.transform.position);
                return $"Lobo {DetectedWolf.WolfName} ({distance:0.0} u)";
            }

            if (vision != null && vision.Target != null && vision.Target.IsActive)
            {
                float distance = Vector3.Distance(transform.position, vision.Target.transform.position);
                return $"Boid {vision.Target.name} ({distance:0.0} u)";
            }

            if (vision != null && vision.HasTarget) return "Boid (memoria)";

            return "Sin objetivo";
        }
    }

    public float GatherProgress => gatherTime > 0.01f ? Mathf.Clamp01(gatherTimer / gatherTime) : 1f;
    public string AttackMode { get; private set; } = "-";
    public float FleeWait { get; set; }

    public void SetAttackMode(string mode)
    {
        AttackMode = mode;
    }
    public Vector3 SafeZoneTarget { get; private set; }
    public string SafeZoneTargetName { get; private set; } = "-";
    public bool HasSafeZoneTarget => SafeZoneTargetName != "-";

    public void Configure(
        Graph graphReference,
        Material material,
        LineRenderer pathLine,
        Color color,
        float speed,
        float force,
        Material interestMaterialReference = null)
    {
        graph = graphReference;
        line = pathLine;
        baseColor = color;
        maxSpeed = speed;
        maxForce = force;
        if (interestMaterialReference != null) interestMaterial = interestMaterialReference;

        body = GetComponent<MeshRenderer>();
        if (body != null)
        {
            body.sharedMaterial = material;
            block = new MaterialPropertyBlock();
            SetColor(baseColor);
        }

        vision = GetComponent<VisionSensor>();
        if (vision == null) vision = gameObject.AddComponent<VisionSensor>();
        vision.Configure(visionRange, fieldOfView, 3f, obstacleMask);

        EnsureAttackLine();
        ApplyStateColor(HunterStateId.Search);

        health = maxHealth;
        dead = false;
        Collected = 0;
        RunsCompleted = 0;
        StateId = HunterStateId.Search;
        SafeZoneTarget = transform.position;
        SafeZoneTargetName = "-";
        ClearPath();
        SetVisible(true);
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
        StateId = HunterStateId.Search;
        stateMachine.ChangeState(HunterStateId.Search);
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
        TickAttackFlash(Time.deltaTime);
    }

    private void TickAttackFlash(float deltaTime)
    {
        if (attackFlashTimer <= 0f) return;

        attackFlashTimer -= deltaTime;
        if (attackFlashTimer > 0f) return;

        attackFlashTimer = 0f;
        if (attackLine != null) attackLine.enabled = false;
    }

    public void Tick(float deltaTime)
    {
        if (graph == null) return;

        DeltaTime = deltaTime;

        if (dead)
        {
            respawnTimer -= deltaTime;
            if (respawnTimer <= 0f) Respawn();
            return;
        }

        attackTimer += deltaTime;
        if (vision != null) vision.Scan(deltaTime);

        EnsureStates();
        stateMachine.Update();
    }

    public void ChangeState(HunterStateId id)
    {
        EnsureStates();
        if (StateId == id) return;

        StateChanges++;
        StateId = id;
        ApplyStateColor(id);
        stateMachine.ChangeState(id);
    }

    private void ApplyStateColor(HunterStateId id)
    {
        SetColor(StateColor(id));
    }

    /// <summary>
    /// Short lived line from the hunter to its target, so a melee hit and a
    /// ranged hit are both visible in the Game view.
    /// </summary>
    public void FlashAttack(Vector3 target, bool melee)
    {
        if (attackLine == null) return;

        Color color = melee
            ? new Color(1f, 0.25f, 0.20f, 0.95f)
            : new Color(1f, 0.65f, 0.15f, 0.90f);

        attackLine.startColor = color;
        attackLine.endColor = color;

        // URP/Unlit takes the colour from the material, not from the line
        // gradient, so the flash needs its own material (sharing the path
        // material would repaint the whole A* line).
        if (attackLine.sharedMaterial != null)
        {
            attackLine.sharedMaterial.SetColor("_BaseColor", color);
            attackLine.sharedMaterial.SetColor("_Color", color);
        }

        attackLine.positionCount = 2;
        attackLine.SetPosition(0, transform.position + Vector3.up * 0.9f);
        attackLine.SetPosition(1, target + Vector3.up * 0.5f);
        attackLine.enabled = true;

        attackFlashTimer = 0.3f;
    }

    private void EnsureAttackLine()
    {
        if (attackLine != null || line == null) return;

        GameObject go = new GameObject("AttackFlash");
        go.transform.SetParent(transform, false);

        attackLine = go.AddComponent<LineRenderer>();
        attackLine.useWorldSpace = true;
        attackLine.widthMultiplier = 0.16f;
        attackLine.numCapVertices = 2;
        attackLine.positionCount = 0;
        attackLine.sharedMaterial = new Material(line.sharedMaterial);
        attackLine.sharedMaterial.name = "AttackFlash";
        attackLine.enabled = false;
    }

    public bool TryHigherPriority(HunterStateId current)
    {
        if (IsDead)
        {
            if (current == HunterStateId.Dead) return false;

            ChangeState(HunterStateId.Dead);
            return true;
        }

        // Flee owns its exit: it must be able to wait inside a safe zone even if
        // a wolf is still visible from outside.
        if (current == HunterStateId.Flee) return false;

        if (ShouldFlee())
        {
            ChangeState(HunterStateId.Flee);
            return true;
        }

        if (current != HunterStateId.Exit && Collected >= requiredGathers)
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
            CanAttack() &&
            HasLivingPreyInSight())
        {
            ChangeState(HunterStateId.Attack);
            return true;
        }

        return false;
    }

    public HunterStateId PostFleeState()
    {
        return Collected >= requiredGathers ? HunterStateId.Exit : HunterStateId.Search;
    }

    public bool CanAttack()
    {
        return attackTimer >= attackCooldown;
    }

    public bool HasLivingPreyInSight()
    {
        if (vision == null || !vision.IsVisible || vision.Target == null) return false;
        return vision.Target.IsActive;
    }

    public bool ShouldFlee()
    {
        DetectedWolf = FindVisibleWolf();
        if (DetectedWolf == null) return false;

        // The hunter is already untouchable in its spawn or in a safe zone, so
        // fleeing from there would only drag it back into the open.
        return !IsProtected();
    }

    private WolfAgent FindVisibleWolf()
    {
        IReadOnlyList<WolfAgent> all = WolfRegistry.All;
        Vector3 origin = EyePosition;
        WolfAgent best = null;
        float bestDistance = visionRange * visionRange;

        for (int i = all.Count - 1; i >= 0; i--)
        {
            WolfAgent wolf = all[i];
            if (wolf == null) continue;

            Vector3 point = wolf.transform.position + Vector3.up * 0.6f;
            Vector3 offset = point - origin;
            float distance = offset.magnitude;
            if (distance > visionRange || distance < 0.0001f) continue;

            if (Physics.Raycast(origin, offset / distance, distance, obstacleMask)) continue;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = wolf;
        }

        return best;
    }

    public PreyAgent FindDeadPrey()
    {
        PreyAgent best = null;
        float bestDistance = visionRange * visionRange;
        Vector3 origin = EyePosition;

        IReadOnlyList<PreyAgent> all = PreyRegistry.All;
        for (int i = 0; i < all.Count; i++)
        {
            PreyAgent prey = all[i];
            if (prey == null || !prey.IsDead || prey.IsCollected) continue;

            Vector3 point = prey.transform.position + Vector3.up * 0.5f;
            Vector3 offset = point - origin;
            float distance = offset.magnitude;
            if (distance > visionRange) continue;
            if (distance > 0.0001f && Physics.Raycast(origin, offset / distance, distance, obstacleMask)) continue;
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
        FollowNodes(path, ref pathIndex, waypointDistance, deltaTime);
    }

    public void Halt()
    {
        Stop();
    }

    public void ClearPath()
    {
        path.Clear();
        pathIndex = 0;
        DrawPath();
    }

    public void Face(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) transform.forward = direction;
    }

    /// <summary>
    /// Pursuit steering used only for the final leg towards a boid that is
    /// already inside the hunter's line of sight.
    /// </summary>
    public void PursueTarget(PreyAgent target)
    {
        if (target == null) return;

        Vector3 position = target.transform.position;
        float distance = Vector3.Distance(transform.position, position);
        float prediction = maxSpeed > 0.01f ? distance / maxSpeed : 0f;
        Vector3 future = position + target.Velocity * prediction;

        Vector3 offset = future - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude < 0.0001f)
        {
            Halt();
            return;
        }

        ApplyAcceleration(Steer(offset.normalized * maxSpeed), DeltaTime);
        Face(future);
    }

    public bool TryAttack(PreyAgent target)
    {
        if (target == null || target.IsDead || target.IsCollected) return false;
        if (!CanAttack()) return false;

        attackTimer = 0f;
        target.ApplyDamage(attackDamage, transform.position);
        return true;
    }

    public void ResetGatherTimer()
    {
        gatherTimer = 0f;
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
        if (dead) return;

        health -= amount;
        if (health > 0) return;

        Kill();
    }

    public void Kill()
    {
        if (dead) return;

        dead = true;
        health = 0;
        respawnTimer = respawnTime;
        Velocity = Vector3.zero;
        ClearPath();
        if (vision != null) vision.Forget();
        SetVisible(false);
        ChangeState(HunterStateId.Dead);
    }

    private void Respawn()
    {
        dead = false;
        health = maxHealth;
        Collected = 0;
        transform.position = InitialSpawn;
        Velocity = Vector3.zero;
        ClearPath();
        if (vision != null) vision.Forget();
        SafeZoneTarget = transform.position;
        SafeZoneTargetName = "-";
        SetVisible(true);
        ResetColor();
        ChangeState(HunterStateId.Search);
    }

    /// <summary>
    /// Reached the exit: the run is completed, the hunter reappears in the
    /// initial zone with the gather counter reset and starts searching again.
    /// </summary>
    public void CompleteRun()
    {
        RunsCompleted++;
        Collected = 0;
        transform.position = InitialSpawn;
        Velocity = Vector3.zero;
        ClearPath();
        if (vision != null) vision.Forget();
        SafeZoneTarget = transform.position;
        SafeZoneTargetName = "-";
        ResetColor();
        ChangeState(HunterStateId.Search);
    }

    private void SetVisible(bool visible)
    {
        if (body != null) body.enabled = visible;
        if (line != null) line.enabled = visible;
        if (attackLine != null && !visible) attackLine.enabled = false;
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
        requiredGathers = Mathf.Max(1, value);
    }

    public Vector3 InitialSpawn => new Vector3(
        LevelLayout.InitialZone.x + LevelLayout.InitialZone.width * 0.5f,
        1f,
        LevelLayout.InitialZone.y + LevelLayout.InitialZone.height * 0.5f);

    public Vector3 ExitPosition => new Vector3(
        LevelLayout.ExitZone.x + LevelLayout.ExitZone.width * 0.5f,
        transform.position.y,
        LevelLayout.ExitZone.y + LevelLayout.ExitZone.height * 0.5f);

    public Vector3 HuntingGroundCenter => new Vector3(
        LevelLayout.HuntingGround.x + LevelLayout.HuntingGround.width * 0.5f,
        transform.position.y,
        LevelLayout.HuntingGround.y + LevelLayout.HuntingGround.height * 0.5f);

    public bool IsInsideExitZone()
    {
        return LevelLayout.ExitZone.Contains(CellPosition());
    }

    public bool IsInsideHuntingGround()
    {
        return LevelLayout.HuntingGround.Contains(CellPosition());
    }

    public bool IsInsideSafeZone()
    {
        Vector2Int cell = CellPosition();

        if (graph != null)
        {
            graph.EnsureBuilt();
            Node node = graph.GetNode(cell);
            if (node != null && node.CellType == CellType.SafeZone) return true;
        }

        IReadOnlyList<Zone> zones = ZoneRegistry.Zones;
        for (int i = 0; i < zones.Count; i++)
        {
            Zone zone = zones[i];
            if (zone == null || zone.Type != ZoneType.SafeZone) continue;
            if (zone.Contains(transform.position)) return true;
        }

        return LevelLayout.SafeZoneA.Contains(cell) || LevelLayout.SafeZoneB.Contains(cell);
    }

    public bool IsInsideInitialZone()
    {
        Vector2Int cell = CellPosition();

        if (graph != null)
        {
            graph.EnsureBuilt();
            Node node = graph.GetNode(cell);
            if (node != null && node.CellType == CellType.Initial) return true;
        }

        return LevelLayout.InitialZone.Contains(cell);
    }

    /// <summary>
    /// Zones where the hunter cannot be seen nor captured: the safe zones and
    /// its own spawn. Without protecting the spawn the pack camps the respawn
    /// point and the run never restarts.
    /// </summary>
    public bool IsProtected()
    {
        return IsInsideSafeZone() || IsInsideInitialZone();
    }

    /// <summary>
    /// Nearest safe zone measured by the length of the A* path, as required by
    /// the statement (not by straight-line distance).
    /// </summary>
    public Vector3 NearestSafeZone()
    {
        graph.EnsureBuilt();
        Node start = graph.GetNearestNode(transform.position);

        Vector3 best = transform.position;
        string bestName = "-";
        int bestLength = int.MaxValue;

        IReadOnlyList<Zone> zones = ZoneRegistry.Zones;
        bool found = false;

        for (int i = 0; i < zones.Count; i++)
        {
            Zone zone = zones[i];
            if (zone == null || zone.Type != ZoneType.SafeZone) continue;

            found = true;
            ConsiderSafeZone(zone.Center, zone.name, start, ref best, ref bestName, ref bestLength);
        }

        if (!found)
        {
            ConsiderSafeZone(RectCenter(LevelLayout.SafeZoneA), "SafeZoneA", start, ref best, ref bestName, ref bestLength);
            ConsiderSafeZone(RectCenter(LevelLayout.SafeZoneB), "SafeZoneB", start, ref best, ref bestName, ref bestLength);
        }

        SafeZoneTarget = best;
        SafeZoneTargetName = bestName;
        return best;
    }

    private void ConsiderSafeZone(
        Vector3 center,
        string zoneName,
        Node start,
        ref Vector3 best,
        ref string bestName,
        ref int bestLength)
    {
        Node goal = graph.GetNearestNode(center);
        if (start == null || goal == null) return;

        List<Node> route = AStar.FindPath(graph, start, goal, graph.IsWalkable);
        if (route.Count == 0 || route.Count >= bestLength) return;

        bestLength = route.Count;
        best = center;
        bestName = zoneName;
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

    public int ActiveInterestObjects
    {
        get
        {
            for (int i = interestObjects.Count - 1; i >= 0; i--)
                if (interestObjects[i] == null) interestObjects.RemoveAt(i);

            return interestObjects.Count;
        }
    }

    /// <summary>
    /// While searching inside the hunting ground, spawns an interest object
    /// every interval as long as fewer than five are active.
    /// </summary>
    public void TickInterest(float deltaTime)
    {
        interestTimer += deltaTime;
        if (interestTimer < interestSpawnInterval) return;

        interestTimer = 0f;
        if (!IsInsideHuntingGround()) return;
        if (ActiveInterestObjects >= maxInterestObjects) return;

        Vector3 point = RandomHuntingPoint();
        SpawnInterestObject(point);
    }

    private Vector3 RandomHuntingPoint()
    {
        graph.EnsureBuilt();

        IReadOnlyList<Node> nodes = graph.AllNodes;
        for (int attempt = 0; attempt < 32; attempt++)
        {
            Node node = nodes[Random.Range(0, nodes.Count)];
            if (node.CellType == CellType.HuntingGround) return node.WorldPosition;
        }

        return HuntingGroundCenter;
    }

    private void SpawnInterestObject(Vector3 point)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "InterestObject";
        go.transform.position = point + Vector3.up * 0.4f;
        go.transform.localScale = Vector3.one * 0.5f;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) DestroyNow(collider);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && interestMaterial != null) renderer.sharedMaterial = interestMaterial;

        go.AddComponent<InterestObject>();

        if (interestRoot == null)
        {
            GameObject rootObject = new GameObject("InterestObjects");
            interestRoot = rootObject.transform;
            interestRoot.SetParent(transform.parent, false);
        }

        go.transform.SetParent(interestRoot, true);
        interestObjects.Add(go);
    }

    public void ClearInterestObjects()
    {
        for (int i = 0; i < interestObjects.Count; i++)
            if (interestObjects[i] != null) DestroyNow(interestObjects[i]);

        interestObjects.Clear();

        if (interestRoot != null) DestroyNow(interestRoot.gameObject);
        interestRoot = null;
    }

    private static void DestroyNow(Object target)
    {
        if (target == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(target);
            return;
        }
#endif
        Object.Destroy(target);
    }

    private Vector2Int CellPosition()
    {
        return new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.z));
    }

    private Vector3 RectCenter(RectInt rect)
    {
        return new Vector3(
            rect.x + rect.width * 0.5f,
            transform.position.y,
            rect.y + rect.height * 0.5f);
    }

    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

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

        Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, rangeAttackRange);
    }
}
