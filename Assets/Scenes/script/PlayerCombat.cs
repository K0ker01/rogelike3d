using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Pool; // Для пулинга объектов

[RequireComponent(typeof(Animator))] // Гарантирует наличие Animator
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Damage Display")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private float textDisplayTime = 1f;
    [SerializeField] private Vector3 textOffset = new Vector3(0, 2f, 0);

    private float lastAttackTime;
    private Camera mainCamera;
    private ObjectPool<GameObject> damageTextPool; // Пул для текста урона

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        // Инициализация пула для текста урона
        damageTextPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(damageTextPrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj)
        );
    }

    private void Update()
    {
        if (CanAttack() && Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    private bool CanAttack()
    {
        return Time.time > lastAttackTime + attackCooldown;
    }

    private void Attack()
    {
        lastAttackTime = Time.time;
        TriggerAttackAnimation();
        PerformAttack();
    }

    private void TriggerAttackAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }
    }

    private void PerformAttack()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange, enemyLayer))
        {
            ProcessHit(hit);
        }
    }

    private void ProcessHit(RaycastHit hit)
    {
        if (hit.collider.TryGetComponent<HealthSystem>(out var health))
        {
            health.TakeDamage(damage);
            DisplayDamageText(damage, hit.point, hit.collider.transform);
            LogDamage(hit.collider.name);
        }
    }

    private void DisplayDamageText(int damageAmount, Vector3 hitPoint, Transform target)
    {
        if (damageTextPrefab == null) return;

        GameObject textObj = damageTextPool.Get();
        textObj.transform.position = target.position + textOffset;

        var textMesh = textObj.GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = damageAmount.ToString();
            textMesh.transform.LookAt(2 * textMesh.transform.position - mainCamera.transform.position);
        }

        StartCoroutine(ReleaseTextAfterDelay(textObj, textDisplayTime));
    }

    private IEnumerator ReleaseTextAfterDelay(GameObject textObj, float delay)
    {
        yield return new WaitForSeconds(delay);
        damageTextPool.Release(textObj);
    }

    private void LogDamage(string enemyName)
    {
        Debug.Log($"Нанесён урон {damage} врагу {enemyName}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * attackRange);
    }

    private void OnDestroy()
    {
        damageTextPool?.Clear();
    }
}