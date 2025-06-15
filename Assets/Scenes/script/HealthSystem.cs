using UnityEngine;
using System;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityTime = 0.5f;
    [SerializeField] private GameObject deathEffect;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    // Все события для гибкости
    public event Action<int> OnHealthChanged; // Основное событие для UI
    public event Action<DamageInfo> OnDamageTaken; // Для дополнительных эффектов
    public event Action<HealthSystem> OnDeath;
    public event Action<int> OnHealed;

    private bool isInvincible;
    private float lastDamageTime;

    public struct DamageInfo
    {
        public int damageAmount;
        public GameObject damageSource;
        public Vector3 hitPoint;
    }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage, GameObject damageSource = null)
    {
        if (isInvincible || CurrentHealth <= 0) return;
        if (Time.time < lastDamageTime + invincibilityTime) return;

        CurrentHealth -= damage;
        lastDamageTime = Time.time;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        // Вызываем оба события
        OnHealthChanged?.Invoke(CurrentHealth);
        OnDamageTaken?.Invoke(new DamageInfo
        {
            damageAmount = damage,
            damageSource = damageSource,
            hitPoint = transform.position
        });

        if (CurrentHealth <= 0) Die();
    }

    private IEnumerator InvincibilityFrame()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    private void Die()
    {
        OnDeath?.Invoke(this);

        var colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = false;

        if (TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        if (TryGetComponent<EnemyAI>(out var enemy)) enemy.enabled = false;
        if (TryGetComponent<PlayerController>(out var player)) player.enabled = false;

        Destroy(gameObject, 0f);
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth);
        OnHealed?.Invoke(amount);
    }
}