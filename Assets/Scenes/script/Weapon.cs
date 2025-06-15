using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private float knockbackForce = 0f; // ����� ��������� ������������
    [SerializeField] private string enemyTag = "Enemy";

    private Collider weaponCollider;
    private Rigidbody parentRigidbody;

    private void Awake()
    {
        weaponCollider = GetComponent<Collider>();
        weaponCollider.isTrigger = true; // ����������� trigger-�����

        parentRigidbody = GetComponentInParent<Rigidbody>();
        if (parentRigidbody != null)
        {
            parentRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    // �������� ��������� ������ �� ����� �����
    public void EnableDamage()
    {
        weaponCollider.enabled = true;
    }

    public void DisableDamage()
    {
        weaponCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(enemyTag))
        {
            if (other.TryGetComponent<HealthSystem>(out var health))
            {
                health.TakeDamage(damage);

                // ������������ ������������
                if (knockbackForce > 0 && other.TryGetComponent<Rigidbody>(out var enemyRb))
                {
                    Vector3 direction = (other.transform.position - transform.position).normalized;
                    enemyRb.AddForce(direction * knockbackForce, ForceMode.Impulse);
                }
            }
        }
    }
}