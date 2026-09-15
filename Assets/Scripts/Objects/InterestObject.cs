using UnityEngine;

public class InterestObject : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public bool IsDestroyed =>
        currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDestroyed)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            Destroy(gameObject);
        }
    }
}