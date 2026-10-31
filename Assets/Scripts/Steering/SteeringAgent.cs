using System.Collections.Generic;
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

    /// <summary>
    /// Follows a list of graph nodes. The reach radius grows with the step,
    /// the direction looks ahead to the next node and the speed drops on sharp
    /// turns, so the agent turns instead of orbiting a node it can never land
    /// on at full speed.
    /// </summary>
    protected bool FollowNodes(IReadOnlyList<Node> nodes, ref int index, float reach, float deltaTime)
    {
        if (nodes == null || nodes.Count == 0 || index >= nodes.Count)
        {
            Stop();
            return true;
        }

        Vector3 position = transform.position;
        reach = Mathf.Max(reach, maxSpeed * deltaTime * 1.5f);

        while (index < nodes.Count)
        {
            Vector3 nodePosition = nodes[index].WorldPosition;
            Vector3 delta = nodePosition - position;
            delta.y = 0f;

            if (delta.sqrMagnitude > reach * reach) break;

            index++;
        }

        if (index >= nodes.Count)
        {
            Stop();
            return true;
        }

        Vector3 target = nodes[index].WorldPosition;
        target.y = position.y;

        Vector3 offset = target - position;
        offset.y = 0f;

        if (index + 1 < nodes.Count && offset.magnitude < 2f)
        {
            Vector3 next = nodes[index + 1].WorldPosition;
            next.y = position.y;

            float blend = 1f - Mathf.Clamp01(offset.magnitude / 2f);
            offset = Vector3.Lerp(offset, next - position, blend * 0.6f);
            offset.y = 0f;
        }

        if (offset.sqrMagnitude < 0.0001f)
        {
            Stop();
            return false;
        }

        Vector3 direction = offset.normalized;

        float turn = Velocity.sqrMagnitude > 0.01f
            ? Vector3.Angle(Velocity, direction) / 180f
            : 0f;

        float speed = maxSpeed * Mathf.Lerp(1f, 0.45f, turn);

        if (index == nodes.Count - 1) speed *= Mathf.Clamp01(offset.magnitude / 1.5f);

        ApplyAcceleration(Steer(direction * speed), deltaTime);
        return false;
    }
}
