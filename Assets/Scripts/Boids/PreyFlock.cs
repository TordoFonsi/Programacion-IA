using System.Collections.Generic;
using UnityEngine;

public class PreyFlock : MonoBehaviour
{
    [SerializeField] private RectInt area = LevelLayout.HuntingGround;
    [SerializeField] private Material preyMaterial;
    [SerializeField] private int count = 6;
    [SerializeField] private float speed = 4.5f;
    [SerializeField] private float force = 16f;

    [Header("Flocking")]
    [SerializeField] private float neighbourRadius = 5f;
    [SerializeField] private float separationRadius = 1.8f;
    [SerializeField] private float separationWeight = 2f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    [Header("Confinement")]
    [SerializeField] private float borderDistance = 3f;
    [SerializeField] private float borderWeight = 4f;

    [Header("Threat")]
    [SerializeField] private float threatWeight = 6f;

    private readonly List<PreyAgent> agents = new List<PreyAgent>();
    private readonly List<PreyAgent> tickBuffer = new List<PreyAgent>();

    public IReadOnlyList<PreyAgent> Agents => agents;
    public RectInt Area => area;

    public void Configure(RectInt huntingArea, Material material, int amount, float preySpeed, float preyForce)
    {
        area = huntingArea;
        preyMaterial = material;
        count = amount;
        speed = preySpeed;
        force = preyForce;
    }

    public void Spawn()
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Prey_{i}";
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
            go.transform.position = GetSpawnPosition();

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            PreyAgent agent = go.AddComponent<PreyAgent>();
            agent.Configure(this, preyMaterial, new Color(0.97f, 0.96f, 0.92f), speed, force);

            agents.Add(agent);
            PreyRegistry.Register(agent);
        }
    }

    private void Update()
    {
        Step(Time.deltaTime);
    }

    public void Step(float deltaTime)
    {
        tickBuffer.Clear();
        for (int i = 0; i < agents.Count; i++)
            if (agents[i] != null && agents[i].IsActive) tickBuffer.Add(agents[i]);

        for (int i = 0; i < tickBuffer.Count; i++)
        {
            PreyAgent agent = tickBuffer[i];
            agent.Tick(ComputeSteering(agent), deltaTime);
        }
    }

    private Vector3 ComputeSteering(PreyAgent agent)
    {
        Vector3 position = agent.transform.position;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int neighbours = 0;
        int close = 0;

        for (int i = 0; i < tickBuffer.Count; i++)
        {
            PreyAgent other = tickBuffer[i];
            if (other == agent) continue;

            Vector3 offset = other.transform.position - position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance > agent.NeighbourRadius || distance < 0.0001f) continue;

            neighbours++;
            alignment += other.Velocity;
            cohesion += other.transform.position;

            if (distance > agent.SeparationRadius) continue;

            close++;
            separation -= offset / (distance * distance);
        }

        Vector3 desired = Vector3.zero;

        if (neighbours > 0)
        {
            desired += separation.normalized * separationWeight;
            desired += (alignment / neighbours).normalized * alignmentWeight;
            desired += ((cohesion / neighbours) - position).normalized * cohesionWeight;
        }

        desired += BorderForce(position) * borderWeight;

        Transform threat = ThreatRegistry.FindNearest(position, agent.ThreatRadius);
        if (threat != null)
        {
            Vector3 escape = position - threat.position;
            escape.y = 0f;
            desired += escape.normalized * threatWeight;
            agent.SetBehaviour("Evading");
        }
        else if (close > 0)
        {
            agent.SetBehaviour("Separating");
        }
        else
        {
            agent.SetBehaviour("Flocking");
        }

        if (desired.sqrMagnitude < 0.0001f) return agent.Arrive(Center, area.width * 0.5f);

        return agent.Steer(desired.normalized * agent.MaxSpeed);
    }

    private Vector3 BorderForce(Vector3 position)
    {
        float minX = area.x + borderDistance;
        float maxX = area.x + area.width - borderDistance;
        float minZ = area.y + borderDistance;
        float maxZ = area.y + area.height - borderDistance;

        Vector3 force = Vector3.zero;

        if (position.x < minX) force.x += (minX - position.x) / borderDistance;
        if (position.x > maxX) force.x -= (position.x - maxX) / borderDistance;
        if (position.z < minZ) force.z += (minZ - position.z) / borderDistance;
        if (position.z > maxZ) force.z -= (position.z - maxZ) / borderDistance;

        return force;
    }

    public Vector3 Clamp(Vector3 position)
    {
        const float margin = 0.6f;

        position.x = Mathf.Clamp(position.x, area.x + margin, area.x + area.width - margin);
        position.z = Mathf.Clamp(position.z, area.y + margin, area.y + area.height - margin);
        return position;
    }

    public Vector3 GetSpawnPosition()
    {
        const float margin = 2f;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(area.x + margin, area.x + area.width - margin),
                0.5f,
                Random.Range(area.y + margin, area.y + area.height - margin));

            bool free = true;
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i] == null || !agents[i].IsActive) continue;
                if ((agents[i].transform.position - candidate).sqrMagnitude > 4f) continue;

                free = false;
                break;
            }

            if (free) return candidate;
        }

        return Center + Vector3.up * 0.5f;
    }

    private Vector3 Center => new Vector3(area.x + area.width * 0.5f, 0.5f, area.y + area.height * 0.5f);

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.65f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(Center, new Vector3(area.width, 0.2f, area.height));

        Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.4f);
        Gizmos.DrawWireCube(
            Center,
            new Vector3(area.width - borderDistance * 2f, 0.2f, area.height - borderDistance * 2f));
    }
}
