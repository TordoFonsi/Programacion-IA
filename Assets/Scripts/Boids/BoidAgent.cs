using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private BoidFlocking flocking;
    private BoidHealth health;

    public bool IsDead =>
        health != null &&
        health.IsDead;

    private void Awake()
    {
        flocking = GetComponent<BoidFlocking>();
        health = GetComponent<BoidHealth>();
    }

    private void Update()
    {
        if (IsDead)
            return;

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
}