using UnityEngine;

[RequireComponent(typeof(SteeringBody))]
public class SteeringDemo : MonoBehaviour
{
    [SerializeField] private WorldBounds bounds;
    [SerializeField] private float slowDownDistance = 4f;
    [SerializeField] private float arriveRadius = 0.8f;

    private SteeringBody steering;
    private Vector3 target;

    private void Awake()
    {
        steering = GetComponent<SteeringBody>();
        PickTarget();

        if (steering.Velocity.sqrMagnitude < 0.0001f)
            steering.SetVelocity(transform.forward * steering.MaxSpeed * 0.5f);
    }

    private void Update()
    {
        Vector3 offset = target - transform.position;
        offset.y = 0f;

        Vector3 force = offset.magnitude > slowDownDistance
            ? steering.Seek(target)
            : steering.Arrive(target, arriveRadius, slowDownDistance);

        force += steering.Bounds(bounds, 3f);
        steering.Move(force, Time.deltaTime);
        steering.ClampTo(bounds);

        if (offset.magnitude <= arriveRadius) PickTarget();
    }

    private void PickTarget()
    {
        target = bounds != null
            ? bounds.RandomPoint(3f)
            : transform.position + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, target);
    }
}
