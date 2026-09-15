using System.Collections;
using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float respawnTime = 5f;

    [Header("Movement Area")]
    [SerializeField] private Transform movementArea;
    [SerializeField]
    private Vector3 movementAreaSize =
        new Vector3(25f, 0f, 25f);

    [Header("Recovery")]
    [SerializeField] private float recoveryRadius = 12f;

    [Header("Border Avoidance")]
    [SerializeField] private float borderAvoidanceDistance = 3f;
    [SerializeField] private float borderAvoidanceWeight = 3f;

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

    private Vector3 deathPosition;

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

            KeepInsideMovementArea();

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

        Vector3 borderMovement =
            GetBorderAvoidance();

        movement +=
            borderMovement *
            borderAvoidanceWeight;

        if (movement.sqrMagnitude <= 0.01f)
        {
            movement =
                GetRecoveryMovement();
        }

        if (movement.sqrMagnitude <= 0.01f)
            return;

        movement.y = 0f;

        transform.position +=
            movement.normalized *
            speed *
            Time.deltaTime;

        transform.forward =
            movement.normalized;

        KeepInsideMovementArea();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || IsCollected)
            return;

        health.TakeDamage(damage);

        if (IsDead)
        {
            deathPosition =
                transform.position;

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
            deathPosition;

        health.ResetHealth();

        isCollected = false;

        currentBehaviour = "Flocking";

        SetVisible(true);

        flocking.enabled = true;
    }

    private Vector3 GetRecoveryMovement()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                recoveryRadius);

        Vector3 center =
            Vector3.zero;

        int count = 0;

        foreach (Collider hit in hits)
        {
            BoidAgent boid =
                hit.GetComponent<BoidAgent>();

            if (boid == null)
                continue;

            if (boid == this)
                continue;

            if (boid.IsDead ||
                boid.IsCollected)
                continue;

            center +=
                boid.transform.position;

            count++;
        }

        if (count == 0)
            return Vector3.zero;

        center /=
            count;

        Vector3 direction =
            center -
            transform.position;

        direction.y = 0f;

        return direction;
    }

    private Vector3 GetBorderAvoidance()
    {
        if (movementArea == null)
            return Vector3.zero;

        Vector3 center =
            movementArea.position;

        Vector3 position =
            transform.position;

        float minX =
            center.x -
            movementAreaSize.x / 2f;

        float maxX =
            center.x +
            movementAreaSize.x / 2f;

        float minZ =
            center.z -
            movementAreaSize.z / 2f;

        float maxZ =
            center.z +
            movementAreaSize.z / 2f;

        Vector3 direction =
            Vector3.zero;

        if (position.x - minX <
            borderAvoidanceDistance)
        {
            float strength =
                1f -
                (position.x - minX) /
                borderAvoidanceDistance;

            direction.x +=
                strength;
        }

        if (maxX - position.x <
            borderAvoidanceDistance)
        {
            float strength =
                1f -
                (maxX - position.x) /
                borderAvoidanceDistance;

            direction.x -=
                strength;
        }

        if (position.z - minZ <
            borderAvoidanceDistance)
        {
            float strength =
                1f -
                (position.z - minZ) /
                borderAvoidanceDistance;

            direction.z +=
                strength;
        }

        if (maxZ - position.z <
            borderAvoidanceDistance)
        {
            float strength =
                1f -
                (maxZ - position.z) /
                borderAvoidanceDistance;

            direction.z -=
                strength;
        }

        return direction;
    }

    private void KeepInsideMovementArea()
    {
        if (movementArea == null)
            return;

        Vector3 center =
            movementArea.position;

        Vector3 position =
            transform.position;

        float minX =
            center.x -
            movementAreaSize.x / 2f;

        float maxX =
            center.x +
            movementAreaSize.x / 2f;

        float minZ =
            center.z -
            movementAreaSize.z / 2f;

        float maxZ =
            center.z +
            movementAreaSize.z / 2f;

        position.x =
            Mathf.Clamp(
                position.x,
                minX,
                maxX);

        position.z =
            Mathf.Clamp(
                position.z,
                minZ,
                maxZ);

        transform.position =
            position;
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