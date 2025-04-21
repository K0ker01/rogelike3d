using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    private Animator _animator;
    private CharacterController _controller;
    private bool _isAttacking;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (_isAttacking) return; // Блокировка управления при атаке

        HandleMovement();
        HandleAttack();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0, vertical).normalized;

        // Поворот персонажа
        if (moveInput.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Движение и анимация
        _controller.Move(moveInput * (moveSpeed * Time.deltaTime));
        _animator.SetFloat("Velocity", moveInput.magnitude); // 0 = Idle, 1 = Run
    }

    private void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0)) // ЛКМ
        {
            _isAttacking = true;
            _animator.SetTrigger("Attack");
        }
    }

    // Вызывается через Animation Event в конце анимации атаки
    public void EndAttack()
    {
        _isAttacking = false;
    }
}