using UnityEngine;

public enum PoliceStates
{
    Idle,
    Patrol,
    Attack,
    Gather
}

public class FSMAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Patrol")]
    [SerializeField] private PatrolData dataPatrol;

    [Header("Perception")]
    [SerializeField] private float perceptionRadius = 15f;
    [SerializeField] private LayerMask boidLayer;

    [Header("Attack")]
    [SerializeField] private float tba = 3f;
    [SerializeField] private float rangeAttackRadius = 10f;
    [SerializeField] private float meleeAttackRadius = 2f;
    [SerializeField] private int attackDamage = 25;

    [Header("Gather")]
    [SerializeField] private float gatherTime = 2f;

    [Header("Interest Objects")]
    [SerializeField] private GameObject interestObjectPrefab;
    [SerializeField] private float interestSpawnInterval = 8f;
    [SerializeField] private int maxInterestObjects = 5;
    [SerializeField] private Transform spawnArea;
    [SerializeField]
    private Vector3 spawnAreaSize =
        new Vector3(20f, 0f, 20f);

    private StateMachine stateMachine;
    private float attackTimer;
    private float interestTimer;

    public float Speed => speed;
    public float PerceptionRadius => perceptionRadius;
    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public int AttackDamage => attackDamage;
    public float GatherTime => gatherTime;
    public LayerMask BoidLayer => boidLayer;

    public GameObject InterestObjectPrefab =>
        interestObjectPrefab;

    public float InterestSpawnInterval =>
        interestSpawnInterval;

    public int MaxInterestObjects =>
        maxInterestObjects;

    public Transform SpawnArea =>
        spawnArea;

    public Vector3 SpawnAreaSize =>
        spawnAreaSize;

    private void Awake()
    {
        stateMachine = new StateMachine();

        IdleState idleState =
            new IdleState(stateMachine);

        PatrolState patrolState =
            new PatrolState(
                this,
                dataPatrol,
                stateMachine);

        AttackState attackState =
            new AttackState(
                this,
                stateMachine);

        GatherState gatherState =
            new GatherState(
                this,
                stateMachine);

        stateMachine.RegisterState(
            PoliceStates.Idle,
            idleState);

        stateMachine.RegisterState(
            PoliceStates.Patrol,
            patrolState);

        stateMachine.RegisterState(
            PoliceStates.Attack,
            attackState);

        stateMachine.RegisterState(
            PoliceStates.Gather,
            gatherState);

        stateMachine.ChangeState(
            PoliceStates.Idle);
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;
        interestTimer += Time.deltaTime;

        stateMachine.Update();
    }

    public bool CanAttack()
    {
        return attackTimer >= tba;
    }

    public void ResetAttackTimer()
    {
        attackTimer = 0f;
    }

    public bool HasBoidInRange()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                perceptionRadius,
                boidLayer);

        foreach (Collider hit in hits)
        {
            BoidAgent boid =
                hit.GetComponent<BoidAgent>();

            if (boid == null)
                continue;

            if (boid.IsDead ||
                boid.IsCollected)
                continue;

            return true;
        }

        return false;
    }

    public bool HasDeadBoidInRange()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                perceptionRadius,
                boidLayer);

        foreach (Collider hit in hits)
        {
            BoidAgent boid =
                hit.GetComponent<BoidAgent>();

            if (boid == null)
                continue;

            if (boid.IsDead &&
                !boid.IsCollected)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanSpawnInterestObject()
    {
        if (interestTimer <
            interestSpawnInterval)
        {
            return false;
        }

        GameObject[] objects =
            GameObject.FindGameObjectsWithTag(
                "InterestObject");

        if (objects.Length >=
            maxInterestObjects)
        {
            return false;
        }

        interestTimer = 0f;

        return true;
    }

    public Vector3 GetRandomSpawnPosition()
    {
        Vector3 center =
            spawnArea != null
                ? spawnArea.position
                : transform.position;

        float x =
            Random.Range(
                -spawnAreaSize.x / 2f,
                spawnAreaSize.x / 2f);

        float z =
            Random.Range(
                -spawnAreaSize.z / 2f,
                spawnAreaSize.z / 2f);

        return center +
            new Vector3(x, 0f, z);
    }

    public string CurrentStateName =>
        stateMachine.CurrentState != null
            ? stateMachine.CurrentState.GetType().Name
            : "None";
}