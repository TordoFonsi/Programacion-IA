using System;
using System.Collections.Generic;
using UnityEngine;

public enum HunterMode
{
    Patrol,
    Hunt,
    Gather,
    Rest
}

[DisallowMultipleComponent]
[RequireComponent(typeof(SteeringBody))]
public class HunterFSM : MonoBehaviour
{
    [Header("World")]
    [SerializeField] private WorldBounds bounds;
    [SerializeField] private float boundsMargin = 3f;

    [Header("Patrol")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float waypointReachedDistance = 1.2f;
    [SerializeField] private float arriveSlowRadius = 3f;

    [Header("Perception")]
    [SerializeField] private float visionRadius = 10f;

    [Header("Attack")]
    [SerializeField] private float tba = 2f;
    [SerializeField] private float meleeRadius = 2f;
    [SerializeField] private float rangeRadius = 8f;
    [SerializeField] private int meleeDamage = 2;
    [SerializeField] private int rangedDamage = 1;

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float huntDrain = 10f;
    [SerializeField] private float restRecharge = 20f;

    [Header("Gather")]
    [SerializeField] private float gatherTime = 1.5f;
    [SerializeField] private float gatherDistance = 1.6f;

    [Header("Interest Objects")]
    [SerializeField] private GameObject interestPrefab;
    [SerializeField] private float interestInterval = 5f;
    [SerializeField] private int maxInterestObjects = 5;
    [SerializeField] private float interestSpawnRadius = 12f;

    private readonly List<InterestObject> interests = new List<InterestObject>();
    private SteeringBody steering;
    private TriggerSensor vision;
    private StateMachine machine;
    private float attackTimer;
    private float interestTimer;
    private int waypointIndex;

    public HunterMode StateId { get; private set; } = HunterMode.Patrol;
    public string CurrentStateName => StateId.ToString();

    public SteeringBody Steering => steering;
    public WorldBounds Bounds => bounds;
    public BoidAgent Target { get; private set; }

    public float VisionRadius => visionRadius;
    public float Energy { get; private set; }
    public float MaxEnergy => maxEnergy;
    public float ArriveSlowRadius => arriveSlowRadius;
    public float BoundsMargin => boundsMargin;
    public float WaypointReachedDistance => waypointReachedDistance;
    public float MeleeRadius => meleeRadius;
    public float RangeRadius => rangeRadius;
    public int MeleeDamage => meleeDamage;
    public int RangedDamage => rangedDamage;
    public float GatherTime => gatherTime;
    public float GatherDistance => gatherDistance;
    public float HuntDrain => huntDrain;
    public float RestRecharge => restRecharge;

    public event Action<Vector3> Attacked;

    private void Awake()
    {
        steering = GetComponent<SteeringBody>();
        Energy = maxEnergy;

        TriggerSensor[] sensors = GetComponentsInChildren<TriggerSensor>(true);
        for (int i = 0; i < sensors.Length; i++)
        {
            if (sensors[i].Role != SensorRole.HunterVision) continue;
            vision = sensors[i];
            break;
        }

        if (vision != null) vision.SetRadius(visionRadius);

        machine = new StateMachine();
        machine.RegisterState(HunterMode.Patrol, new HunterPatrolState(this, machine));
        machine.RegisterState(HunterMode.Hunt, new HunterHuntState(this, machine));
        machine.RegisterState(HunterMode.Gather, new HunterGatherBoidState(this, machine));
        machine.RegisterState(HunterMode.Rest, new HunterRestState(this, machine));

        machine.ChangeState(HunterMode.Patrol);
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;
        interestTimer += Time.deltaTime;

        HunterMode next = Evaluate();
        machine.ChangeState(next);
        StateId = next;
        machine.Update();
    }

    private HunterMode Evaluate()
    {
        if (StateId == HunterMode.Rest)
            return Energy >= maxEnergy ? HunterMode.Patrol : HunterMode.Rest;

        if (StateId == HunterMode.Hunt)
        {
            if (Energy <= 0f) return HunterMode.Rest;
            if (!IsTargetValid()) return HunterMode.Patrol;
            if (FindDeadBoid() != null) return HunterMode.Gather;
            return HunterMode.Hunt;
        }

        if (FindDeadBoid() != null) return HunterMode.Gather;
        if (CanAttack() && FindLiveBoid() != null) return HunterMode.Hunt;

        return HunterMode.Patrol;
    }

    public bool CanAttack()
    {
        return attackTimer >= tba;
    }

    public void ResetAttackTimer()
    {
        attackTimer = 0f;
    }

    public void AcquireTarget()
    {
        Target = FindLiveBoid();
    }

    public void ClearTarget()
    {
        Target = null;
    }

    public bool IsTargetValid()
    {
        if (Target == null || !Target.IsAlive) return false;
        if (bounds == null) return true;

        return bounds.Contains(Target.transform.position);
    }

    public BoidAgent FindLiveBoid()
    {
        return FindClosestBoid(true);
    }

    public BoidAgent FindDeadBoid()
    {
        return FindClosestBoid(false);
    }

    public void DrainEnergy(float amount)
    {
        Energy = Mathf.Max(0f, Energy - amount);
    }

    public void RechargeEnergy(float amount)
    {
        Energy = Mathf.Min(maxEnergy, Energy + amount);
    }

    public void Move(Vector3 force)
    {
        steering.Move(force, Time.deltaTime);
        steering.ClampTo(bounds);
    }

    public Transform CurrentWaypoint
    {
        get
        {
            if (waypoints == null || waypoints.Count == 0) return null;
            waypointIndex %= waypoints.Count;
            return waypoints[waypointIndex];
        }
    }

    public void AdvanceWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        waypointIndex = (waypointIndex + 1) % waypoints.Count;
    }

    public void TrySpawnInterest()
    {
        if (interestPrefab == null || interestTimer < interestInterval) return;

        PruneInterests();
        if (interests.Count >= maxInterestObjects) return;

        interestTimer = 0f;
        Vector3 point = SpawnPoint();

        GameObject spawned = Instantiate(interestPrefab, point, Quaternion.identity);
        InterestObject interest = spawned.GetComponent<InterestObject>();
        if (interest != null) interests.Add(interest);
    }

    public void NotifyAttack(Vector3 targetPosition)
    {
        Attacked?.Invoke(targetPosition);
    }

    private BoidAgent FindClosestBoid(bool alive)
    {
        if (vision == null) return null;

        BoidAgent closest = null;
        float bestDistance = float.MaxValue;
        Vector3 origin = transform.position;

        for (int i = vision.Detected.Count - 1; i >= 0; i--)
        {
            Collider collider = vision.Detected[i];
            if (collider == null) continue;

            BoidAgent candidate = collider.GetComponentInParent<BoidAgent>();
            if (candidate == null) continue;
            if (alive && !candidate.IsAlive) continue;
            if (!alive && (!candidate.IsDead || candidate.IsCollected)) continue;

            float distance = (candidate.transform.position - origin).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            closest = candidate;
        }

        return closest;
    }

    private Vector3 SpawnPoint()
    {
        Vector3 point = bounds != null
            ? bounds.RandomPointNear(transform.position, interestSpawnRadius, 2f)
            : transform.position;

        point.y = 0.5f;
        return point;
    }

    private void PruneInterests()
    {
        for (int i = interests.Count - 1; i >= 0; i--)
            if (interests[i] == null) interests.RemoveAt(i);
    }

    private void OnValidate()
    {
        boundsMargin = Mathf.Max(0.5f, boundsMargin);
        waypointReachedDistance = Mathf.Max(0.1f, waypointReachedDistance);
        arriveSlowRadius = Mathf.Max(0.5f, arriveSlowRadius);
        visionRadius = Mathf.Max(1f, visionRadius);
        tba = Mathf.Max(0.1f, tba);
        meleeRadius = Mathf.Max(0.5f, meleeRadius);
        rangeRadius = Mathf.Max(meleeRadius + 0.1f, rangeRadius);
        meleeDamage = Mathf.Max(1, meleeDamage);
        rangedDamage = Mathf.Max(1, rangedDamage);
        maxEnergy = Mathf.Max(1f, maxEnergy);
        huntDrain = Mathf.Max(0f, huntDrain);
        restRecharge = Mathf.Max(0f, restRecharge);
        gatherTime = Mathf.Max(0.1f, gatherTime);
        gatherDistance = Mathf.Max(0.5f, gatherDistance);
        interestInterval = Mathf.Max(0.5f, interestInterval);
        maxInterestObjects = Mathf.Max(1, maxInterestObjects);
        interestSpawnRadius = Mathf.Max(1f, interestSpawnRadius);
    }
}
