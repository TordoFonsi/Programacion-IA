using System.Collections;
using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float respawnTime = 5f;

    [Header("Interest")]
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private int interactionDamage = 20;
    [SerializeField] private float interactionInterval = 1f;

    private BoidFlocking flocking;
    private BoidHealth health;
    private Arrive arrive;
    private Renderer[] renderers;
    private Collider[] colliders;
    private bool isCollected;
    private float interactionTimer;

    private string currentBehaviour = "Flocking";

    public bool IsDead =>
        health != null &&
        health.IsDead;

    public bool IsCollected =>
        isCollected;

    public string CurrentBehaviour =>
        currentBehaviour;

    private void Awake()
    {
        flocking = GetComponent<BoidFlocking>();
        health = GetComponent<BoidHealth>();
        arrive = GetComponent<Arrive>();

        renderers =
            GetComponentsInChildren<Renderer>();

        colliders =
            GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (IsDead)
        {
            currentBehaviour = "Dead";
            return;
        }

        if (IsCollected)
        {
            currentBehaviour = "Collected";
            return;
        }

        InterestObject interest =
            FindNearbyInterestObject();

        if (interest != null)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    interest.transform.position);

            if (distance <= interactionDistance)
            {
                currentBehaviour = "Arrive";

                interactionTimer +=
                    Time.deltaTime;

                if (interactionTimer >=
                    interactionInterval)
                {
                    interest.TakeDamage(
                        interactionDamage);

                    interactionTimer = 0f;
                }

                return;
            }

            currentBehaviour = "Arrive";

            Vector3 arriveMovement =
                arrive.GetDirection(
                    interest.transform.position);

            if (arriveMovement.sqrMagnitude >
                0.01f)
            {
                transform.position +=
                    arriveMovement.normalized *
                    speed *
                    Time.deltaTime;

                transform.forward =
                    arriveMovement.normalized;
            }

            return;
        }

        if (flocking.IsEvading())
        {
            currentBehaviour = "Evade";
        }
        else
        {
            currentBehaviour = "Flocking";
        }

        Vector3 movement =
            flocking.GetMovement();

        if (movement.sqrMagnitude <= 0.01f)
            return;

        movement.y = 0f;

        transform.position +=
            movement.normalized *
            speed *
            Time.deltaTime;

        transform.forward =
            movement.normalized;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || IsCollected)
            return;

        health.TakeDamage(damage);

        if (IsDead)
        {
            flocking.enabled = false;

            currentBehaviour = "Dead";

            Debug.Log(
                name +
                " ha muerto.");
        }
    }

    public void Collected()
    {
        if (isCollected)
            return;

        isCollected = true;

        flocking.enabled = false;

        currentBehaviour = "Collected";

        SetVisible(false);

        StartCoroutine(
            RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(
            respawnTime);

        transform.position =
            new Vector3(
                Random.Range(-15f, 15f),
                transform.position.y,
                Random.Range(-15f, 15f));

        health.ResetHealth();

        isCollected = false;

        currentBehaviour = "Flocking";

        SetVisible(true);

        flocking.enabled = true;
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer renderer in renderers)
            renderer.enabled = visible;

        foreach (Collider collider in colliders)
            collider.enabled = visible;
    }

    private InterestObject FindNearbyInterestObject()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                interactionDistance);

        foreach (Collider hit in hits)
        {
            InterestObject interest =
                hit.GetComponent<InterestObject>();

            if (interest != null &&
                !interest.IsDestroyed)
            {
                return interest;
            }
        }

        return null;
    }
}