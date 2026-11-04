using System.Collections.Generic;
using UnityEngine;

public abstract class SteeringAgent : MonoBehaviour
{
    [SerializeField] protected float maxSpeed = 6f;
    [SerializeField] protected float maxForce = 18f;
    [SerializeField] protected float collisionRadius = 0.4f;

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

        Vector3 displacement = Velocity * deltaTime;
        float distance = displacement.magnitude;

        if (distance > 0.0001f)
        {
            // Sub-step the move so a big deltaTime cannot tunnel through a wall.
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 0.25f));
            Vector3 stepDelta = displacement / steps;

            for (int i = 0; i < steps; i++)
            {
                Vector3 current = transform.position;
                Vector3 next = current + stepDelta;

                if (LevelCollision.IsWalkable(next, collisionRadius))
                {
                    transform.position = next;
                    continue;
                }

                // Slide along the wall: try each axis on its own.
                Vector3 nextX = new Vector3(next.x, current.y, current.z);
                Vector3 nextZ = new Vector3(current.x, current.y, next.z);
                bool moved = false;

                if (Mathf.Abs(stepDelta.x) > 0.0001f &&
                    LevelCollision.IsWalkable(nextX, collisionRadius))
                {
                    transform.position = nextX;
                    Velocity = new Vector3(Velocity.x, 0f, 0f);
                    moved = true;
                }
                else if (Mathf.Abs(stepDelta.z) > 0.0001f &&
                         LevelCollision.IsWalkable(nextZ, collisionRadius))
                {
                    transform.position = nextZ;
                    Velocity = new Vector3(0f, 0f, Velocity.z);
                    moved = true;
                }

                if (!moved)
                {
                    Velocity = Vector3.zero;
                    break;
                }

                stepDelta = Velocity * (deltaTime / steps);
            }
        }

        if (Velocity.sqrMagnitude > 0.01f) transform.forward = Velocity;
    }

    protected void Stop()
    {
        Velocity = Vector3.zero;
    }

    /// <summary>
    /// Follows the polyline formed by the graph nodes. It steers towards a
    /// point on the line ahead of the agent instead of towards the node itself,
    /// so an agent that is slightly off the drawn line merges onto it instead
    /// of orbiting the node. The speed drops on sharp turns, but never to zero
    /// unless the path is actually finished.
    /// </summary>
    protected bool FollowNodes(IReadOnlyList<Node> nodes, ref int index, float reach, float deltaTime)
    {
        if (nodes == null || nodes.Count == 0 || index >= nodes.Count)
        {
            Stop();
            return true;
        }

        Vector3 position = transform.position;
        position.y = 0f;
        reach = Mathf.Max(reach, 0.1f);

        // Advance over nodes the agent has already passed.
        while (index < nodes.Count - 1)
        {
            Vector3 a = nodes[index].WorldPosition;
            Vector3 b = nodes[index + 1].WorldPosition;
            a.y = position.y;
            b.y = position.y;

            Vector3 segment = b - a;
            segment.y = 0f;
            float length = segment.magnitude;
            if (length < 0.0001f)
            {
                index++;
                continue;
            }

            Vector3 direction = segment / length;
            float along = Vector3.Dot(position - a, direction);

            if (along < length - reach * 0.5f) break;
            index++;
        }

        // The path is finished: mark the index past the end so HasArrived
        // reports true and the state can pick the next destination.
        if (index >= nodes.Count - 1)
        {
            index = nodes.Count;
            Stop();
            return true;
        }

        Vector3 start = nodes[index].WorldPosition;
        Vector3 end = nodes[index + 1].WorldPosition;
        start.y = position.y;
        end.y = position.y;

        Vector3 currentSegment = end - start;
        currentSegment.y = 0f;
        float currentLength = currentSegment.magnitude;
        if (currentLength < 0.0001f)
        {
            index++;
            return false;
        }

        Vector3 currentDirection = currentSegment / currentLength;
        float projection = Mathf.Clamp(Vector3.Dot(position - start, currentDirection), 0f, currentLength);
        Vector3 closest = start + currentDirection * projection;

        Vector3 toClosest = closest - position;
        toClosest.y = 0f;
        float offLine = toClosest.magnitude;

        float lookAhead = Mathf.Clamp(maxSpeed * 0.3f, 0.9f, 1.8f);
        Vector3 target = LookAheadPoint(nodes, index, position, lookAhead);

        Vector3 desired = target - position;
        desired.y = 0f;

        // When the agent is off the line, blend towards the closest point so it
        // merges smoothly instead of swinging around the node.
        if (offLine > 0.35f)
        {
            Vector3 merge = closest - position;
            merge.y = 0f;
            float blend = Mathf.Clamp01(offLine / 1.5f) * 0.7f;
            desired = Vector3.Lerp(desired, merge, blend);
        }

        if (desired.sqrMagnitude < 0.0001f)
        {
            Stop();
            return false;
        }

        Vector3 desiredDirection = desired.normalized;

        float turn = Velocity.sqrMagnitude > 0.01f
            ? Vector3.Angle(Velocity, desiredDirection) / 180f
            : 0f;

        float speed = maxSpeed * Mathf.Lerp(1f, 0.55f, turn);
        if (offLine > 0.35f) speed *= 0.8f;

        ApplyAcceleration(Steer(desiredDirection * speed), deltaTime);
        return false;
    }

    /// <summary>
    /// Walks the polyline from the agent's projection on the current segment
    /// and returns the point at the requested distance ahead. This is what
    /// makes corners smooth: the target can already be on the next segment.
    /// </summary>
    private static Vector3 LookAheadPoint(
        IReadOnlyList<Node> nodes,
        int index,
        Vector3 position,
        float distance)
    {
        Vector3 start = nodes[index].WorldPosition;
        Vector3 end = nodes[index + 1].WorldPosition;
        start.y = position.y;
        end.y = position.y;

        Vector3 segment = end - start;
        segment.y = 0f;
        float length = segment.magnitude;

        Vector3 point;
        if (length < 0.0001f)
        {
            point = end;
        }
        else
        {
            Vector3 direction = segment / length;
            float projection = Mathf.Clamp(Vector3.Dot(position - start, direction), 0f, length);
            point = start + direction * projection;
        }

        float remaining = distance;

        for (int i = index; i < nodes.Count - 1; i++)
        {
            Vector3 next = nodes[i + 1].WorldPosition;
            next.y = position.y;

            float step = Vector3.Distance(point, next);
            if (step >= remaining)
            {
                return Vector3.MoveTowards(point, next, remaining);
            }

            remaining -= step;
            point = next;
        }

        return point;
    }
}
