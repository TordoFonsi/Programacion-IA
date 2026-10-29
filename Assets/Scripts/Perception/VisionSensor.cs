using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField] private float range = 14f;
    [SerializeField] private float fieldOfView = 100f;
    [SerializeField] private float closeRange = 3f;
    [SerializeField] private float eyeHeight = 1.2f;
    [SerializeField] private LayerMask obstacleMask = ~0;

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

        memoryTimer -= Time.deltaTime;
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
            if (prey == null || !prey.IsActive) continue;

            Vector3 point = prey.transform.position + Vector3.up * 0.5f;
            Vector3 offset = point - origin;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance > range || distance < 0.0001f) continue;

            if (distance > closeRange)
            {
                float angle = Vector3.Angle(transform.forward, offset);
                if (angle > fieldOfView * 0.5f) continue;
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
        Vector3 origin = EyePosition;
        Gizmos.color = alerted
            ? new Color(1f, 0.25f, 0.2f, 0.9f)
            : new Color(1f, 0.9f, 0.2f, 0.6f);

        Gizmos.DrawWireSphere(origin, range);

        const int steps = 20;
        float half = fieldOfView * 0.5f;
        Vector3 previous = origin;

        for (int i = 0; i <= steps; i++)
        {
            float angle = -half + fieldOfView * i / steps;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Vector3 point = origin + direction * range;

            Gizmos.DrawLine(origin, point);
            if (i > 0) Gizmos.DrawLine(previous, point);
            previous = point;
        }

        if (memoryTimer <= 0f) return;

        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(lastKnownPosition + Vector3.up * 0.2f, 0.8f);
    }
}
