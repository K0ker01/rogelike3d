using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityTime = 0.5f;
    public int MaxHealth => maxHealth;
    private int currentHealth;
    private bool isInvincible;
    private float lastDamageTime;

    // События для UI и эффектов
    public event System.Action<int> OnHealthChanged;
    public event System.Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0) return;

        // Проверка неуязвимости после получения урона
        if (Time.time < lastDamageTime + invincibilityTime) return;

        currentHealth -= damage;
        lastDamageTime = Time.time;

        // Ограничение здоровья
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Оповещение подписчиков
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrame());
        }
    }

    private IEnumerator InvincibilityFrame()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    private void Die()
    {
        OnDeath?.Invoke();

        // Отключаем компоненты при смерти
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

        // Для врагов
        if (TryGetComponent<EnemyAI>(out var enemy)) enemy.enabled = false;

        // Для игрока
        if (TryGetComponent<PlayerController>(out var player)) player.enabled = false;

        // Уничтожаем объект через 3 секунды (можно заменить на анимацию смерти)
        Destroy(gameObject, 3f);
    }

    // Для восстановления здоровья
    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }
}