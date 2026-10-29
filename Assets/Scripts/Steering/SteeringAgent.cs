using UnityEngine;

public abstract class SteeringAgent : MonoBehaviour
{
    [SerializeField] protected float maxSpeed = 6f;
    [SerializeField] protected float maxForce = 18f;

    public Vector3 Velocity { get; protected set; }
    public float MaxSpeed => maxSpeed;
    public float MaxForce => maxForce;

    public Vector3 Steer(Vector3 desiredVelocity)
    {
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector3 Seek(Vector3 target)
    {
        Vector3 offset = target - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.0001f) return Steer(Vector3.zero);

        return Steer(offset.normalized * maxSpeed);
    }

    public Vector3 Flee(Vector3 target)
    {
        Vector3 offset = transform.position - target;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.0001f) return Steer(Vector3.zero);

        return Steer(offset.normalized * maxSpeed);
    }

    public Vector3 Arrive(Vector3 target, float slowingDistance)
    {
        Vector3 offset = target - transform.position;
        offset.y = 0f;

        float distance = offset.magnitude;
        if (distance < 0.001f) return Steer(Vector3.zero);

        float desiredSpeed = maxSpeed * Mathf.Clamp01(distance / slowingDistance);
        return Steer(offset.normalized * desiredSpeed);
    }

    protected void ApplyAcceleration(Vector3 acceleration)
    {
        ApplyAcceleration(acceleration, Time.deltaTime);
    }

    protected void ApplyAcceleration(Vector3 acceleration, float deltaTime)
    {
        Velocity += acceleration * deltaTime;
        Velocity = Vector3.ClampMagnitude(Velocity, maxSpeed);
        Velocity = new Vector3(Velocity.x, 0f, Velocity.z);

        transform.position += Velocity * deltaTime;

        if (Velocity.sqrMagnitude > 0.01f) transform.forward = Velocity;
    }

    protected void Stop()
    {
        Velocity = Vector3.zero;
    }
}
