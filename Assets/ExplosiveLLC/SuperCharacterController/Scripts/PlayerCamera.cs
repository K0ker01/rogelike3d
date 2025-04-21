using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public float distance = 5.0f;
    public float height = 2.0f;
    public float rotationSpeed = 2.0f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 70f;

    [Header("Dependencies")]
    [SerializeField] private GameObject playerTarget;

    private Transform target;
    private PlayerInputController input;
    private PlayerMachine machine;
    private SuperCharacterController controller;
    private float yRotation;

    private void Start()
    {
        if (!playerTarget)
        {
            Debug.LogError("PlayerTarget not assigned!", this);
            enabled = false;
            return;
        }

        target = playerTarget.transform;
        input = playerTarget.GetComponent<PlayerInputController>();
        machine = playerTarget.GetComponent<PlayerMachine>();
        controller = playerTarget.GetComponent<SuperCharacterController>();

        if (!input || !machine || !controller)
        {
            Debug.LogError("Missing required components on PlayerTarget!", this);
            enabled = false;
        }

        // Инициализируем начальный поворот
        yRotation = transform.eulerAngles.x;
    }

    private void LateUpdate()
    {
        if (!target) return;

        // Обработка ввода
        yRotation -= input.Current.MouseInput.y * rotationSpeed;
        yRotation = Mathf.Clamp(yRotation, minVerticalAngle, maxVerticalAngle);

        // Вычисляем направление камеры
        Vector3 cameraForward = machine.lookDirection.normalized;
        Vector3 cameraRight = Vector3.Cross(controller.up, cameraForward).normalized;
        Vector3 cameraUp = Vector3.Cross(cameraForward, cameraRight);

        // Позиционирование камеры
        Quaternion rotation = Quaternion.LookRotation(cameraForward, controller.up);
        rotation *= Quaternion.AngleAxis(yRotation, cameraRight);

        transform.rotation = rotation;
        transform.position = target.position + (rotation * Vector3.back * distance) + (controller.up * height);
    }
}