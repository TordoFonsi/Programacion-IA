using UnityEngine;

public class Arrive : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float slowRadius = 5f;
    [SerializeField] private float stopRadius = 1f;

    public Vector3 GetDirection(Vector3 targetPosition)
    {
        Vector3 direction =
            targetPosition -
            transform.position;

        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= stopRadius)
            return Vector3.zero;

        float speed = maxSpeed;

        if (distance < slowRadius)
            speed =
                maxSpeed *
                (distance / slowRadius);

        return direction.normalized * speed;
    }
}