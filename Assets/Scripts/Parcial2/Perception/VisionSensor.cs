using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField] private float range = 10f;
    [SerializeField] private float fieldOfView = 100f;
    [SerializeField] private float closeRange = 3f;
    [SerializeField] private float eyeHeight = 1.2f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Stability")]
    // The current target keeps a slightly larger range and cone. Without this
    // margin a prey sitting right on the edge of the cone flickers in and out
    // every frame and the hunter jumps between Attack and Search.
    [SerializeField] private float targetRangeMargin = 1.25f;
    [SerializeField] private float targetAngleMargin = 12f;

    [Header("Memory")]
    [SerializeField] private float memoryDuration = 4f;

    private PreyAgent target;
    private Vector3 lastKnownPosition;
    private float memoryTimer;
    private bool alerted;

    public PreyAgent Target => target;
    public bool HasTarget => target != null;
    public bool IsVisible { get; private set; }
    public bool IsAlerted => alerted;
    public Vector3 LastKnownPosition => lastKnownPosition;
    public float Range => range;
    public float FieldOfView => fieldOfView;
    public float MemoryTimer => memoryTimer;

    public void Configure(float visionRange, float fov, float close, LayerMask obstacles)
    {
        range = visionRange;
        fieldOfView = fov;
        closeRange = close;
        obstacleMask = obstacles;
    }

    public bool Scan()
    {
        return Scan(Time.deltaTime);
    }

    public bool Scan(float deltaTime)
    {
        PreyAgent visible = FindVisible();

        if (visible != null)
        {
            target = visible;
            lastKnownPosition = visible.transform.position;
            memoryTimer = memoryDuration;
            alerted = true;
            IsVisible = true;
            return true;
        }

        IsVisible = false;

        if (target == null)
        {
            alerted = false;
            return false;
        }

        memoryTimer -= deltaTime;
        if (memoryTimer > 0f) return true;

        target = null;
        alerted = false;
        return false;
    }

    public void Forget()
    {
        target = null;
        memoryTimer = 0f;
        alerted = false;
        IsVisible = false;
    }

    private PreyAgent FindVisible()
    {
        Vector3 origin = EyePosition;
        PreyAgent best = null;
        float bestDistance = float.MaxValue;

        System.Collections.Generic.IReadOnlyList<PreyAgent> all = PreyRegistry.All;

        for (int i = all.Count - 1; i >= 0; i--)
        {
            PreyAgent prey = all[i];
            if (prey == null || !prey.IsActive || prey.IsDead) continue;

            bool current = prey == target;
            float allowedRange = current ? range * targetRangeMargin : range;
            float allowedHalfAngle = fieldOfView * 0.5f + (current ? targetAngleMargin : 0f);

            Vector3 point = prey.transform.position + Vector3.up * 0.5f;
            Vector3 offset = point - origin;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance > allowedRange || distance < 0.0001f) continue;

            if (distance > closeRange)
            {
                float angle = Vector3.Angle(transform.forward, offset);
                if (angle > allowedHalfAngle) continue;
            }

            if (Physics.Raycast(origin, offset.normalized, distance, obstacleMask)) continue;

            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = prey;
        }

        return best;
    }

    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    private void OnDrawGizmos()
    {
        if (!GizmoSettings.VisionCones) return;

        Gizmos.color = alerted
            ? new Color(1f, 0.25f, 0.2f, 0.75f)
            : new Color(1f, 0.9f, 0.2f, 0.35f);

        const int steps = 16;
        float half = fieldOfView * 0.5f;
        Vector3 previous = GroundPoint(-half);

        for (int i = 1; i <= steps; i++)
        {
            float angle = -half + fieldOfView * i / steps;
            Vector3 point = GroundPoint(angle);

            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.DrawLine(GroundOrigin, GroundPoint(-half));
        Gizmos.DrawLine(GroundOrigin, GroundPoint(half));

        if (memoryTimer <= 0f) return;

        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(lastKnownPosition + Vector3.up * 0.2f, 0.8f);
    }

    private Vector3 GroundOrigin => new Vector3(transform.position.x, 0.05f, transform.position.z);

    private Vector3 GroundPoint(float angle)
    {
        Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
        direction.y = 0f;

        Vector3 point = GroundOrigin + direction.normalized * range;
        point.y = 0.05f;
        return point;
    }
}
