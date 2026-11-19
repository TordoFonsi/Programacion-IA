using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 2;

    private int currentHealth;

    public int Max => maxHealth;
    public int Current => currentHealth;
    public bool IsDead => currentHealth <= 0;

    public event Action<int, int> Changed;
    public event Action Damaged;
    public event Action Died;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        Damaged?.Invoke();
        Changed?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0) Died?.Invoke();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Changed?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        Changed?.Invoke(currentHealth, maxHealth);
    }
}
