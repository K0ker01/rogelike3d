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

    // ������� ��� UI � ��������
    public event System.Action<int> OnHealthChanged;
    public event System.Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0) return;

        // �������� ������������ ����� ��������� �����
        if (Time.time < lastDamageTime + invincibilityTime) return;

        currentHealth -= damage;
        lastDamageTime = Time.time;

        // ����������� ��������
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // ���������� �����������
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

        // ��������� ���������� ��� ������
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

        // ��� ������
        if (TryGetComponent<EnemyAI>(out var enemy)) enemy.enabled = false;

        // ��� ������
        if (TryGetComponent<PlayerController>(out var player)) player.enabled = false;

        // ���������� ������ ����� 3 ������� (����� �������� �� �������� ������)
        Destroy(gameObject, 3f);
    }

    // ��� �������������� ��������
    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }
}