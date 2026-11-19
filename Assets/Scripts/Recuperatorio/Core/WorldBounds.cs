using UnityEngine;

[DisallowMultipleComponent]
public class WorldBounds : MonoBehaviour
{
    [SerializeField] private Vector3 size = new Vector3(44f, 1f, 36f);
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 1f, 0.6f);

    public Vector3 Center => transform.position;
    public Vector3 Size => size;

    public Vector3 Min => Center - size * 0.5f;
    public Vector3 Max => Center + size * 0.5f;

    public bool Contains(Vector3 position, float margin = 0f)
    {
        Vector3 min = Min + Vector3.one * margin;
        Vector3 max = Max - Vector3.one * margin;
        return position.x >= min.x && position.x <= max.x &&
               position.z >= min.z && position.z <= max.z;
    }

    public Vector3 Clamp(Vector3 position, float margin = 0f)
    {
        Vector3 min = Min + Vector3.one * margin;
        Vector3 max = Max - Vector3.one * margin;

        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.z = Mathf.Clamp(position.z, min.z, max.z);
        return position;
    }

    public Vector3 RandomPoint(float margin = 0f)
    {
        Vector3 min = Min + Vector3.one * margin;
        Vector3 max = Max - Vector3.one * margin;

        return new Vector3(
            Random.Range(min.x, max.x),
            Center.y,
            Random.Range(min.z, max.z));
    }

    public Vector3 RandomPointNear(Vector3 origin, float radius, float margin = 1f)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 point = origin + new Vector3(offset.x, 0f, offset.y);
        return Clamp(point, margin);
    }

    public Vector3 AvoidanceVelocity(Vector3 position, float margin, float speed)
    {
        if (margin <= 0f) return Vector3.zero;

        Vector3 min = Min;
        Vector3 max = Max;
        Vector3 push = Vector3.zero;

        float left = position.x - min.x;
        if (left < margin) push.x += 1f - Mathf.Clamp01(left / margin);

        float right = max.x - position.x;
        if (right < margin) push.x -= 1f - Mathf.Clamp01(right / margin);

        float back = position.z - min.z;
        if (back < margin) push.z += 1f - Mathf.Clamp01(back / margin);

        float front = max.z - position.z;
        if (front < margin) push.z -= 1f - Mathf.Clamp01(front / margin);

        if (push.sqrMagnitude < 0.0001f) return Vector3.zero;
        return push.normalized * speed;
    }

    public Vector3 ClampVelocity(Vector3 velocity, Vector3 position)
    {
        Vector3 min = Min;
        Vector3 max = Max;

        if (position.x <= min.x + 0.01f && velocity.x < 0f) velocity.x = 0f;
        if (position.x >= max.x - 0.01f && velocity.x > 0f) velocity.x = 0f;
        if (position.z <= min.z + 0.01f && velocity.z < 0f) velocity.z = 0f;
        if (position.z >= max.z - 0.01f && velocity.z > 0f) velocity.z = 0f;

        return new Vector3(velocity.x, 0f, velocity.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(Center, new Vector3(size.x, 0.1f, size.z));
    }
}
