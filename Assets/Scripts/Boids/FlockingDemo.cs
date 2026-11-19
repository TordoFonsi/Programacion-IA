using UnityEngine;

[RequireComponent(typeof(SteeringBody))]
[RequireComponent(typeof(BoidFlocking))]
public class FlockingDemo : MonoBehaviour
{
    [SerializeField] private WorldBounds bounds;

    private SteeringBody steering;
    private BoidFlocking flocking;
    private TriggerSensor neighbours;

    private void Awake()
    {
        steering = GetComponent<SteeringBody>();
        flocking = GetComponent<BoidFlocking>();

        TriggerSensor[] sensors = GetComponentsInChildren<TriggerSensor>(true);
        for (int i = 0; i < sensors.Length; i++)
        {
            if (sensors[i].Role != SensorRole.BoidNeighbours) continue;
            neighbours = sensors[i];
            neighbours.SetRadius(flocking.NeighbourRadius);
            break;
        }

        if (steering.Velocity.sqrMagnitude < 0.0001f)
        {
            Vector3 direction = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            steering.SetVelocity(direction * steering.MaxSpeed * 0.6f);
        }
    }

    private void Update()
    {
        Vector3 force = flocking.GetFlocking(steering, neighbours != null ? neighbours.Detected : null);
        force += steering.Bounds(bounds, 3f);
        steering.Move(force, Time.deltaTime);
        steering.ClampTo(bounds);
    }
}
