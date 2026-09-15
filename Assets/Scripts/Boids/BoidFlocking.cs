using UnityEngine;

public class BoidFlocking : MonoBehaviour
{
    [Header("Neighbour Sensor")]
    [SerializeField] private float neighbourRadius = 8f;
    [SerializeField] private float separationRadius = 3f;
    [SerializeField] private LayerMask boidLayer;

    [Header("Weights")]
    [SerializeField] private float separationWeight = 2f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    [Header("Hunter")]
    [SerializeField] private float hunterRadius = 10f;
    [SerializeField] private LayerMask hunterLayer;
    [SerializeField] private Evade evade;

    public Vector3 GetMovement()
    {
        Collider[] hunterHits =
            Physics.OverlapSphere(
                transform.position,
                hunterRadius,
                hunterLayer);

        if (hunterHits.Length > 0)
        {
            return evade.GetDirection(
                hunterHits[0].transform.position);
        }

        BoidAgent[] neighbours =
            FindNeighbours();

        Vector3 separation =
            GetSeparation(neighbours);

        Vector3 alignment =
            GetAlignment(neighbours);

        Vector3 cohesion =
            GetCohesion(neighbours);

        return
            separation * separationWeight +
            alignment * alignmentWeight +
            cohesion * cohesionWeight;
    }

    private BoidAgent[] FindNeighbours()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                neighbourRadius,
                boidLayer);

        BoidAgent[] result =
            new BoidAgent[hits.Length];

        int count = 0;

        foreach (Collider hit in hits)
        {
            BoidAgent boid =
                hit.GetComponent<BoidAgent>();

            if (boid == null)
                continue;

            if (boid.gameObject == gameObject)
                continue;

            if (boid.IsDead)
                continue;

            result[count] = boid;
            count++;
        }

        System.Array.Resize(
            ref result,
            count);

        return result;
    }

    private Vector3 GetSeparation(
        BoidAgent[] neighbours)
    {
        Vector3 result =
            Vector3.zero;

        int count = 0;

        foreach (BoidAgent neighbour in neighbours)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    neighbour.transform.position);

            if (distance <= separationRadius &&
                distance > 0.01f)
            {
                Vector3 direction =
                    transform.position -
                    neighbour.transform.position;

                result +=
                    direction.normalized /
                    distance;

                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        return result / count;
    }

    private Vector3 GetAlignment(
        BoidAgent[] neighbours)
    {
        if (neighbours.Length == 0)
            return Vector3.zero;

        Vector3 average =
            Vector3.zero;

        foreach (BoidAgent neighbour in neighbours)
            average += neighbour.transform.forward;

        return average /
               neighbours.Length;
    }

    private Vector3 GetCohesion(
        BoidAgent[] neighbours)
    {
        if (neighbours.Length == 0)
            return Vector3.zero;

        Vector3 center =
            Vector3.zero;

        foreach (BoidAgent neighbour in neighbours)
            center +=
                neighbour.transform.position;

        center /=
            neighbours.Length;

        return center -
               transform.position;
    }
}