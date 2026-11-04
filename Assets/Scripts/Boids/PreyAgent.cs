using UnityEngine;

public class PreyAgent : SteeringAgent
{
    [Header("Life")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private float corpseTime = 12f;
    [SerializeField] private float respawnTime = 5f;

    [Header("Radii")]
    [SerializeField] private float neighbourRadius = 5f;
    [SerializeField] private float separationRadius = 1.8f;
    [SerializeField] private float threatRadius = 8f;

    private PreyFlock flock;
    private Renderer body;
    private MaterialPropertyBlock block;
    private Color aliveColor;
    private int health;
    private float timer;
    private bool dead;
    private bool collected;

    public bool IsDead => dead;
    public bool IsCollected => collected;
    public bool IsActive => !dead && !collected;
    public float NeighbourRadius => neighbourRadius;
    public float SeparationRadius => separationRadius;
    public float ThreatRadius => threatRadius;
    public string CurrentBehaviour { get; private set; } = "Flocking";

    public void Configure(PreyFlock owner, Material material, Color color, float speed, float force)
    {
        flock = owner;
        maxSpeed = speed;
        maxForce = force;
        aliveColor = color;

        body = GetComponentInChildren<Renderer>();
        if (body != null)
        {
            body.sharedMaterial = material;
            block = new MaterialPropertyBlock();
            ApplyColor(aliveColor);
        }

        health = maxHealth;
        dead = false;
        collected = false;
        timer = 0f;
        CurrentBehaviour = "Flocking";
    }

    private void Update()
    {
        if (IsActive) return;

        timer -= Time.deltaTime;
        if (timer <= 0f) Respawn();
    }

    private void OnDestroy()
    {
        PreyRegistry.Unregister(this);
    }

    public void Tick(Vector3 acceleration)
    {
        Tick(acceleration, Time.deltaTime);
    }

    public void Tick(Vector3 acceleration, float deltaTime)
    {
        if (!IsActive) return;

        ApplyAcceleration(acceleration, deltaTime);
        KeepInsideArea();
    }

    public void ApplyDamage(int amount, Vector3 source)
    {
        if (!IsActive) return;

        health -= amount;
        if (health <= 0)
        {
            Die();
            return;
        }

        CurrentBehaviour = "Fleeing";
        Velocity = (transform.position - source).normalized * maxSpeed;
    }

    public void Die()
    {
        if (!IsActive) return;

        dead = true;
        collected = false;
        timer = corpseTime;
        CurrentBehaviour = "Dead";
        Velocity = Vector3.zero;
        ApplyColor(LevelPalette.PreyDead);
    }

    public bool Collect()
    {
        if (!dead || collected) return false;

        collected = true;
        timer = respawnTime;
        CurrentBehaviour = "Collected";

        if (body != null) body.enabled = false;
        return true;
    }

    public void SetBehaviour(string behaviour)
    {
        if (IsActive) CurrentBehaviour = behaviour;
    }

    private void Respawn()
    {
        if (flock != null) transform.position = flock.GetSpawnPosition();

        Velocity = Vector3.zero;
        health = maxHealth;
        dead = false;
        collected = false;
        timer = 0f;
        CurrentBehaviour = "Flocking";

        if (body != null)
        {
            body.enabled = true;
            ApplyColor(aliveColor);
        }
    }

    private void KeepInsideArea()
    {
        if (flock == null) return;

        Vector3 position = transform.position;
        Vector3 clamped = flock.Clamp(position);

        if (clamped == position) return;

        transform.position = clamped;
        Velocity = new Vector3(Velocity.x * 0.5f, 0f, Velocity.z * 0.5f);
    }

    private void ApplyColor(Color color)
    {
        if (body == null || block == null) return;

        body.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        body.SetPropertyBlock(block);
    }

    private void OnDrawGizmos()
    {
        if (!GizmoSettings.BoidRadii) return;

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, neighbourRadius);

        Gizmos.color = new Color(1f, 0.45f, 0.35f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, threatRadius);
    }
}
