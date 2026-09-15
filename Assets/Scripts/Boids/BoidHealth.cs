using UnityEngine;

public class BoidHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public bool IsDead =>
        currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
}