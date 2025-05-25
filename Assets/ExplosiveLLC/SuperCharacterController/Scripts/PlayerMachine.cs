using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SuperCharacterController))]
[RequireComponent(typeof(PlayerInputController))]
public class PlayerMachine : SuperStateMachine
{
    public Transform AnimatedMesh;

    public float WalkSpeed = 4.0f;
    public float WalkAcceleration = 30.0f;
    public float Gravity = 25.0f;

    public float RollSpeed = 10f;
    public float RollDuration = 0.5f;

    enum PlayerStates { Idle, Walk, Roll, Fall }

    private SuperCharacterController controller;
    private Vector3 moveDirection;
    public Vector3 lookDirection { get; private set; }
    private PlayerInputController input;

    private float rollTimer;
    private Vector3 rollDirection;

    void Start()
    {
        input = GetComponent<PlayerInputController>();
        controller = GetComponent<SuperCharacterController>();
        lookDirection = transform.forward;
        currentState = PlayerStates.Idle;
    }

    protected override void EarlyGlobalSuperUpdate()
    {
        lookDirection = Quaternion.AngleAxis(input.Current.MouseInput.x * (controller.deltaTime / Time.deltaTime), controller.up) * lookDirection;
    }

    protected override void LateGlobalSuperUpdate()
    {
        transform.position += moveDirection * controller.deltaTime;
        AnimatedMesh.rotation = Quaternion.LookRotation(lookDirection, controller.up);
    }

    private bool AcquiringGround()
    {
        return controller.currentGround.IsGrounded(false, 0.01f);
    }

    private bool MaintainingGround()
    {
        return controller.currentGround.IsGrounded(true, 0.5f);
    }

    public void RotateGravity(Vector3 up)
    {
        lookDirection = Quaternion.FromToRotation(transform.up, up) * lookDirection;
    }

    private Vector3 LocalMovement()
    {
        Vector3 right = Vector3.Cross(controller.up, lookDirection);
        Vector3 local = Vector3.zero;

        if (input.Current.MoveInput.x != 0)
            local += right * input.Current.MoveInput.x;

        if (input.Current.MoveInput.z != 0)
            local += lookDirection * input.Current.MoveInput.z;

        return local.normalized;
    }

    // -------- STATES --------

    void Idle_EnterState()
    {
        controller.EnableSlopeLimit();
        controller.EnableClamping();
    }

    void Idle_SuperUpdate()
    {
        if (!MaintainingGround())
        {
            currentState = PlayerStates.Fall;
            return;
        }

        if (input.Current.RollInput && input.Current.MoveInput != Vector3.zero)
        {
            rollDirection = LocalMovement();
            rollTimer = RollDuration;
            currentState = PlayerStates.Roll;
            return;
        }

        if (input.Current.MoveInput != Vector3.zero)
        {
            currentState = PlayerStates.Walk;
            return;
        }

        moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, 10f * controller.deltaTime);
    }

    void Walk_SuperUpdate()
    {
        if (!MaintainingGround())
        {
            currentState = PlayerStates.Fall;
            return;
        }

        if (input.Current.RollInput && input.Current.MoveInput != Vector3.zero)
        {
            rollDirection = LocalMovement();
            rollTimer = RollDuration;
            currentState = PlayerStates.Roll;
            return;
        }

        if (input.Current.MoveInput != Vector3.zero)
        {
            moveDirection = Vector3.MoveTowards(moveDirection, LocalMovement() * WalkSpeed, WalkAcceleration * controller.deltaTime);
        }
        else
        {
            currentState = PlayerStates.Idle;
        }
    }

    void Roll_EnterState()
    {
        controller.DisableClamping();
        controller.DisableSlopeLimit();
    }

    void Roll_SuperUpdate()
    {
        rollTimer -= controller.deltaTime;
        moveDirection = rollDirection * RollSpeed;

        if (rollTimer <= 0 || input.Current.MoveInput == Vector3.zero)
        {
            currentState = PlayerStates.Idle;
        }
    }

    void Fall_EnterState()
    {
        controller.DisableClamping();
        controller.DisableSlopeLimit();
    }

    void Fall_SuperUpdate()
    {
        if (AcquiringGround())
        {
            moveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
            currentState = PlayerStates.Idle;
            return;
        }

        moveDirection -= controller.up * Gravity * controller.deltaTime;
    }
}
