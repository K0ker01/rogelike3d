using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    private float lastAttackTime;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time > lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    private void Attack()
    {
        // ������ �������� �����
        animator.SetTrigger("Attack");

        // �������� ���������
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange, enemyLayer))
        {
            if (hit.collider.TryGetComponent<HealthSystem>(out var health))
            {
                health.TakeDamage(damage);
                Debug.Log($"������ ����: {damage}");
            }
        }

        lastAttackTime = Time.time;
    }

    // ������������ ������� ����� � ���������
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * attackRange);
    }
}