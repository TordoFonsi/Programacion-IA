using System.Collections.Generic;
using UnityEngine;

public enum SensorRole
{
    None,
    BoidNeighbours,
    BoidThreat,
    BoidInterest,
    HunterVision
}

[RequireComponent(typeof(SphereCollider))]
public class TriggerSensor : MonoBehaviour
{
    [SerializeField] private SensorRole role = SensorRole.None;
    [SerializeField] private float radius = 5f;
    [SerializeField] private LayerMask layerMask = ~0;
    [SerializeField] private Color gizmoColor = new Color(0.4f, 0.9f, 1f, 0.4f);

    private readonly List<Collider> detected = new List<Collider>();
    private SphereCollider trigger;

    public SensorRole Role => role;
    public float Radius => radius;
    public IReadOnlyList<Collider> Detected => detected;

    public int Count
    {
        get
        {
            Prune();
            return detected.Count;
        }
    }

    private void Awake()
    {
        ApplyRadius();
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.1f, radius);
        ApplyRadius();
    }

    private void OnDisable()
    {
        detected.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInMask(other)) return;
        if (!detected.Contains(other)) detected.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        detected.Remove(other);
    }

    public void Configure(SensorRole sensorRole, float sensorRadius, LayerMask mask)
    {
        role = sensorRole;
        radius = Mathf.Max(0.1f, sensorRadius);
        layerMask = mask;
        ApplyRadius();
    }

    public void SetRadius(float sensorRadius)
    {
        radius = Mathf.Max(0.1f, sensorRadius);
        ApplyRadius();
    }

    public bool Contains(Component target)
    {
        if (target == null) return false;
        Transform targetTransform = target.transform;

        for (int i = detected.Count - 1; i >= 0; i--)
        {
            Collider collider = detected[i];
            if (collider == null)
            {
                detected.RemoveAt(i);
                continue;
            }

            if (collider.transform == targetTransform) return true;
        }

        return false;
    }

    public T GetClosest<T>() where T : Component
    {
        T closest = null;
        float bestDistance = float.MaxValue;
        Vector3 origin = transform.position;

        for (int i = detected.Count - 1; i >= 0; i--)
        {
            Collider collider = detected[i];
            if (collider == null || !collider.gameObject.activeInHierarchy)
            {
                detected.RemoveAt(i);
                continue;
            }

            T candidate = collider.GetComponentInParent<T>();
            if (candidate == null) continue;

            float distance = (candidate.transform.position - origin).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            closest = candidate;
        }

        return closest;
    }

    public void Collect<T>(List<T> results) where T : Component
    {
        if (results == null) return;
        results.Clear();

        for (int i = detected.Count - 1; i >= 0; i--)
        {
            Collider collider = detected[i];
            if (collider == null || !collider.gameObject.activeInHierarchy)
            {
                detected.RemoveAt(i);
                continue;
            }

            T candidate = collider.GetComponentInParent<T>();
            if (candidate != null && !results.Contains(candidate)) results.Add(candidate);
        }
    }

    public void Prune()
    {
        for (int i = detected.Count - 1; i >= 0; i--)
        {
            Collider collider = detected[i];
            if (collider == null || !collider.gameObject.activeInHierarchy) detected.RemoveAt(i);
        }
    }

    private bool IsInMask(Collider other)
    {
        if (other == null) return false;
        return (layerMask.value & (1 << other.gameObject.layer)) != 0;
    }

    private void ApplyRadius()
    {
        if (trigger == null) trigger = GetComponent<SphereCollider>();
        if (trigger == null) return;

        trigger.isTrigger = true;
        trigger.radius = radius;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);

        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < detected.Count; i++)
        {
            Collider collider = detected[i];
            if (collider != null) Gizmos.DrawLine(transform.position, collider.transform.position);
        }
    }
}
