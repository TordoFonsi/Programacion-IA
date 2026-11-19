using UnityEngine;

public class Evade : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 6f;

    public Vector3 GetDirection(
        Vector3 threatPosition)
    {
        Vector3 direction =
            transform.position -
            threatPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return Vector3.zero;

        return direction.normalized * maxSpeed;
    }
}