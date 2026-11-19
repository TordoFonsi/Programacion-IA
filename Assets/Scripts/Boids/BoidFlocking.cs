using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BoidFlocking : MonoBehaviour
{
    [Header("Radii")]
    [SerializeField] private float neighbourRadius = 6f;
    [SerializeField] private float separationRadius = 2.2f;

    [Header("Weights")]
    [SerializeField] private float separationWeight = 2f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    private readonly List<SteeringBody> neighbours = new List<SteeringBody>();
    private readonly List<Transform> neighbourTransforms = new List<Transform>();

    public float NeighbourRadius => neighbourRadius;
    public float SeparationRadius => separationRadius;
    public float SeparationWeight => separationWeight;

    public Vector3 GetFlocking(SteeringBody steering, IReadOnlyList<Collider> detected)
    {
        Collect(steering, detected, neighbourRadius);

        Vector3 separation = steering.Separation(neighbourTransforms, separationRadius);
        Vector3 alignment = steering.Alignment(neighbours);
        Vector3 cohesion = steering.Cohesion(neighbourTransforms);

        return separation * separationWeight +
               alignment * alignmentWeight +
               cohesion * cohesionWeight;
    }

    public Vector3 GetSeparation(SteeringBody steering, IReadOnlyList<Collider> detected)
    {
        Collect(steering, detected, neighbourRadius);
        return steering.Separation(neighbourTransforms, separationRadius) * separationWeight;
    }

    // Puente temporal para el BoidAgent viejo; se elimina al reescribir su FSM.
    public bool IsEvading() => false;
    public Vector3 GetMovement() => Vector3.zero;

    private void Collect(SteeringBody steering, IReadOnlyList<Collider> detected, float radius)
    {
        neighbours.Clear();
        neighbourTransforms.Clear();

        if (steering == null || detected == null) return;

        Vector3 origin = steering.transform.position;
        float radiusSqr = radius * radius;

        for (int i = 0; i < detected.Count; i++)
        {
            Collider collider = detected[i];
            if (collider == null) continue;

            SteeringBody candidate = collider.GetComponentInParent<SteeringBody>();
            if (candidate == null || candidate == steering) continue;

            BoidAgent boid = collider.GetComponentInParent<BoidAgent>();
            if (boid != null && (boid.IsDead || boid.IsCollected)) continue;

            if ((candidate.transform.position - origin).sqrMagnitude > radiusSqr) continue;
            if (neighbours.Contains(candidate)) continue;

            neighbours.Add(candidate);
            neighbourTransforms.Add(candidate.transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, neighbourRadius);
    }
}
