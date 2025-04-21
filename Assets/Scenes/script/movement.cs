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
        if (_isAttacking) return; // ���������� ���������� ��� �����

        HandleMovement();
        HandleAttack();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0, vertical).normalized;

        // ������� ���������
        if (moveInput.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // �������� � ��������
        _controller.Move(moveInput * (moveSpeed * Time.deltaTime));
        _animator.SetFloat("Velocity", moveInput.magnitude); // 0 = Idle, 1 = Run
    }

    private void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0)) // ���
        {
            _isAttacking = true;
            _animator.SetTrigger("Attack");
        }
    }

    // ���������� ����� Animation Event � ����� �������� �����
    public void EndAttack()
    {
        _isAttacking = false;
    }
}