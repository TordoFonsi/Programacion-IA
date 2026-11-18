using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SteeringBody : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxForce = 14f;
    [SerializeField] private float turnSpeed = 360f;

    public Vector3 Velocity { get; private set; }
    public float MaxSpeed => maxSpeed;
    public float MaxForce => maxForce;

    public Vector3 Steer(Vector3 desiredVelocity)
    {
        desiredVelocity.y = 0f;
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector3 Seek(Vector3 target)
    {
        Vector3 offset = Flat(target - transform.position);
        if (offset.sqrMagnitude < 0.0001f) return Steer(Vector3.zero);
        return Steer(offset.normalized * maxSpeed);
    }

    public Vector3 Flee(Vector3 target)
    {
        Vector3 offset = Flat(transform.position - target);
        if (offset.sqrMagnitude < 0.0001f) return Steer(Vector3.zero);
        return Steer(offset.normalized * maxSpeed);
    }

    public Vector3 Arrive(Vector3 target, float stopDistance, float slowRadius)
    {
        Vector3 offset = Flat(target - transform.position);
        float distance = offset.magnitude;
        if (distance <= stopDistance || distance < 0.0001f) return Steer(Vector3.zero);

        float range = Mathf.Max(0.01f, slowRadius - stopDistance);
        float desiredSpeed = maxSpeed * Mathf.Clamp01((distance - stopDistance) / range);
        return Steer(offset.normalized * desiredSpeed);
    }

    public Vector3 Pursuit(Vector3 target, Vector3 targetVelocity)
    {
        float distance = Flat(target - transform.position).magnitude;
        float eta = maxSpeed > 0.01f ? distance / maxSpeed : 0f;
        return Seek(target + Flat(targetVelocity) * eta);
    }

    public Vector3 Evade(Vector3 threat, Vector3 threatVelocity)
    {
        float distance = Flat(threat - transform.position).magnitude;
        float eta = maxSpeed > 0.01f ? distance / maxSpeed : 0f;
        return Flee(threat + Flat(threatVelocity) * eta);
    }

    public Vector3 Separation(IReadOnlyList<Transform> neighbours, float radius)
    {
        if (neighbours == null || radius <= 0f) return Vector3.zero;

        Vector3 push = Vector3.zero;
        int count = 0;

        for (int i = 0; i < neighbours.Count; i++)
        {
            Transform neighbour = neighbours[i];
            if (neighbour == null) continue;

            Vector3 offset = Flat(transform.position - neighbour.position);
            float distance = offset.magnitude;
            if (distance < 0.0001f || distance > radius) continue;

            push += offset.normalized * (1f - distance / radius);
            count++;
        }

        if (count == 0 || push.sqrMagnitude < 0.0001f) return Vector3.zero;
        return Steer(push.normalized * maxSpeed);
    }

    public Vector3 Alignment(IReadOnlyList<SteeringBody> neighbours)
    {
        if (neighbours == null) return Vector3.zero;

        Vector3 average = Vector3.zero;
        int count = 0;

        for (int i = 0; i < neighbours.Count; i++)
        {
            SteeringBody neighbour = neighbours[i];
            if (neighbour == null) continue;

            average += neighbour.Velocity;
            count++;
        }

        if (count == 0) return Vector3.zero;
        average = Flat(average / count);
        if (average.sqrMagnitude < 0.0001f) return Vector3.zero;
        return Steer(average.normalized * maxSpeed);
    }

    public Vector3 Cohesion(IReadOnlyList<Transform> neighbours)
    {
        if (neighbours == null) return Vector3.zero;

        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < neighbours.Count; i++)
        {
            Transform neighbour = neighbours[i];
            if (neighbour == null) continue;

            center += neighbour.position;
            count++;
        }

        if (count == 0) return Vector3.zero;
        return Seek(center / count);
    }

    public Vector3 Bounds(WorldBounds bounds, float margin)
    {
        if (bounds == null) return Vector3.zero;
        Vector3 desired = bounds.AvoidanceVelocity(transform.position, margin, maxSpeed);
        return desired.sqrMagnitude < 0.0001f ? Vector3.zero : Steer(desired);
    }

    public void Move(Vector3 acceleration, float deltaTime)
    {
        acceleration.y = 0f;
        Velocity += Vector3.ClampMagnitude(acceleration, maxForce) * deltaTime;
        Velocity = Vector3.ClampMagnitude(Velocity, maxSpeed);

        if (Velocity.sqrMagnitude < 0.0001f)
        {
            Velocity = Vector3.zero;
            return;
        }

        transform.position += Velocity * deltaTime;
        FaceVelocity(deltaTime);
    }

    public void ClampTo(WorldBounds bounds)
    {
        if (bounds == null) return;

        Vector3 position = transform.position;
        Vector3 clamped = bounds.Clamp(position, 0.6f);
        if ((clamped - position).sqrMagnitude < 0.0001f) return;

        transform.position = clamped;
        Velocity = bounds.ClampVelocity(Velocity, clamped);
    }

    public void Stop()
    {
        Velocity = Vector3.zero;
    }

    public void SetVelocity(Vector3 velocity)
    {
        Velocity = Vector3.ClampMagnitude(Flat(velocity), maxSpeed);
        if (Velocity.sqrMagnitude > 0.0001f) transform.forward = Velocity;
    }

    private void FaceVelocity(float deltaTime)
    {
        Vector3 direction = Flat(Velocity);
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * deltaTime);
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }
}
