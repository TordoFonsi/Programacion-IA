using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SteeringBody))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(BoidFlocking))]
[RequireComponent(typeof(BoidFSM))]
public class BoidAgent : MonoBehaviour
{
    [Header("Perception")]
    [SerializeField] private float visionRadius = 7f;
    [SerializeField] private float interestRadius = 5f;

    [Header("Object")]
    [SerializeField] private float consumeDistance = 1.6f;
    [SerializeField] private float consumeInterval = 0.7f;
    [SerializeField] private int consumeDamage = 1;
    [SerializeField] private float arriveSlowRadius = 4f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 4f;
    [SerializeField] private float respawnMargin = 2f;

    [Header("Movement")]
    [SerializeField] private float boundsMargin = 3f;
    [SerializeField] private LayerMask boidLayer;

    [Header("World")]
    [SerializeField] private WorldBounds bounds;

    private SteeringBody steering;
    private Health health;
    private BoidFlocking flocking;
    private BoidFSM fsm;
    private Collider bodyCollider;
    private Renderer[] renderers;
    private TriggerSensor neighbourSensor;
    private TriggerSensor threatSensor;
    private TriggerSensor interestSensor;
    private bool collected;

    public bool IsDead => health != null && health.IsDead;
    public bool IsCollected => collected;
    public bool IsAlive => !IsDead && !IsCollected;

    public Health Health => health;
    public SteeringBody Steering => steering;
    public BoidFlocking Flocking => flocking;
    public BoidFSM Fsm => fsm;
    public WorldBounds Bounds => bounds;

    public float VisionRadius => visionRadius;
    public float InterestRadius => interestRadius;
    public float ConsumeDistance => consumeDistance;
    public float ConsumeInterval => consumeInterval;
    public int ConsumeDamage => consumeDamage;
    public float ArriveSlowRadius => arriveSlowRadius;
    public float RespawnDelay => respawnDelay;
    public float BoundsMargin => boundsMargin;

    public InterestObject CurrentInterest { get; set; }
    public string CurrentBehaviour => fsm != null ? fsm.CurrentStateName : "None";

    public IReadOnlyList<Collider> NeighbourColliders =>
        neighbourSensor != null ? neighbourSensor.Detected : null;

    public IReadOnlyList<Collider> InterestColliders =>
        interestSensor != null ? interestSensor.Detected : null;

    private void Awake()
    {
        steering = GetComponent<SteeringBody>();
        health = GetComponent<Health>();
        flocking = GetComponent<BoidFlocking>();
        fsm = GetComponent<BoidFSM>();
        bodyCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>(true);

        TriggerSensor[] sensors = GetComponentsInChildren<TriggerSensor>(true);
        for (int i = 0; i < sensors.Length; i++)
        {
            switch (sensors[i].Role)
            {
                case SensorRole.BoidNeighbours:
                    neighbourSensor = sensors[i];
                    break;
                case SensorRole.BoidThreat:
                    threatSensor = sensors[i];
                    break;
                case SensorRole.BoidInterest:
                    interestSensor = sensors[i];
                    break;
            }
        }

        if (neighbourSensor != null) neighbourSensor.SetRadius(flocking.NeighbourRadius);
        if (threatSensor != null) threatSensor.SetRadius(visionRadius);
        if (interestSensor != null) interestSensor.SetRadius(interestRadius);
    }

    private void Start()
    {
        if (IsAlive && steering.Velocity.sqrMagnitude < 0.0001f)
        {
            Vector3 direction = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            steering.SetVelocity(direction * steering.MaxSpeed * 0.5f);
        }
    }

    public SteeringBody GetThreat()
    {
        return threatSensor != null ? threatSensor.GetClosest<SteeringBody>() : null;
    }

    public InterestObject FindInterest()
    {
        if (interestSensor == null) return null;

        InterestObject closest = null;
        float best = float.MaxValue;
        Vector3 origin = transform.position;

        for (int i = 0; i < interestSensor.Detected.Count; i++)
        {
            Collider collider = interestSensor.Detected[i];
            if (collider == null) continue;

            InterestObject candidate = collider.GetComponentInParent<InterestObject>();
            if (candidate == null || candidate.IsDestroyed) continue;

            float distance = (candidate.transform.position - origin).sqrMagnitude;
            if (distance >= best) continue;

            best = distance;
            closest = candidate;
        }

        return closest;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        health.TakeDamage(amount);

        if (health.IsDead)
        {
            collected = false;
            CurrentInterest = null;
            steering.Stop();
        }
    }

    public void Collect()
    {
        if (collected || !IsDead) return;

        collected = true;
        CurrentInterest = null;
        steering.Stop();
    }

    public void Collected()
    {
        Collect();
    }

    public void RespawnAtRandom()
    {
        health.ResetHealth();
        collected = false;
        CurrentInterest = null;
        transform.position = FindSpawnPoint();
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        steering.Stop();
        steering.SetVelocity(transform.forward * steering.MaxSpeed * 0.5f);
        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].enabled = visible;

        if (bodyCollider != null) bodyCollider.enabled = visible;
        if (neighbourSensor != null) neighbourSensor.gameObject.SetActive(visible);
        if (threatSensor != null) threatSensor.gameObject.SetActive(visible);
        if (interestSensor != null) interestSensor.gameObject.SetActive(visible);
    }

    private Vector3 FindSpawnPoint()
    {
        if (bounds == null) return transform.position;

        for (int i = 0; i < 12; i++)
        {
            Vector3 candidate = bounds.RandomPoint(respawnMargin);
            if (!Physics.CheckSphere(candidate + Vector3.up * 0.5f, 0.8f, boidLayer)) return candidate;
        }

        return bounds.RandomPoint(respawnMargin);
    }

    private void OnValidate()
    {
        visionRadius = Mathf.Max(1f, visionRadius);
        interestRadius = Mathf.Max(1f, interestRadius);
        consumeDistance = Mathf.Max(0.5f, consumeDistance);
        consumeInterval = Mathf.Max(0.1f, consumeInterval);
        consumeDamage = Mathf.Max(1, consumeDamage);
        arriveSlowRadius = Mathf.Max(consumeDistance + 0.1f, arriveSlowRadius);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        respawnMargin = Mathf.Max(0f, respawnMargin);
        boundsMargin = Mathf.Max(0.5f, boundsMargin);
    }
}
