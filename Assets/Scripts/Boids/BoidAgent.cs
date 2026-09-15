using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    [Header("Interest")]
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private int interactionDamage = 20;
    [SerializeField] private float interactionInterval = 1f;

    private BoidFlocking flocking;
    private BoidHealth health;
    private Arrive arrive;
    private float interactionTimer;

    public bool IsDead =>
        health != null &&
        health.IsDead;

    private void Awake()
    {
        flocking = GetComponent<BoidFlocking>();
        health = GetComponent<BoidHealth>();
        arrive = GetComponent<Arrive>();
    }

    private void Update()
    {
        if (IsDead)
            return;

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
        if (IsDead)
            return;

        health.TakeDamage(damage);

        if (IsDead)
        {
            flocking.enabled = false;

            Debug.Log(
                name +
                " ha muerto.");
        }
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