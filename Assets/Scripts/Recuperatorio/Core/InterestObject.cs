using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class InterestObject : MonoBehaviour
{
    private Health health;

    public bool IsDestroyed => health == null || health.IsDead;
    public bool IsAvailable => health != null && !health.IsDead;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.Died += HandleDied;
    }

    private void OnDestroy()
    {
        if (health != null) health.Died -= HandleDied;
    }

    public void TakeDamage(int amount)
    {
        if (health == null) health = GetComponent<Health>();
        if (health != null) health.TakeDamage(amount);
    }

    private void HandleDied()
    {
        Destroy(gameObject);
    }
}
