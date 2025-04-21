using UnityEngine;

namespace WarriorAnimsFREE
{
    public class WarriorMovementController : SuperStateMachine
    {
        [Header("Components")]
        private WarriorController warriorController;

        [Header("Movement")]
        public float movementAcceleration = 90.0f;
        public float runSpeed = 6f;
        private readonly float rotationSpeed = 40f;
        public float groundFriction = 50f;
        [HideInInspector] public Vector3 currentVelocity;

        [HideInInspector] public Vector3 lookDirection { get; private set; }

        private void Start()
        {
            warriorController = GetComponent<WarriorController>();
            // Set currentState to idle on startup.
            currentState = WarriorState.Idle;
        }

        #region Updates

        protected override void EarlyGlobalSuperUpdate()
        {
        }

        protected override void LateGlobalSuperUpdate()
        {
            // Move the player by our velocity every frame.
            transform.position += currentVelocity * warriorController.superCharacterController.deltaTime;

            // If alive and is moving, set animator.
            if (warriorController.canMove)
            {
                if (currentVelocity.magnitude > 0 && warriorController.HasMoveInput())
                {
                    warriorController.isMoving = true;
                    warriorController.SetAnimatorBool("Moving", true);
                    warriorController.SetAnimatorFloat("Velocity", currentVelocity.magnitude);
                }
                else
                {
                    warriorController.isMoving = false;
                    warriorController.SetAnimatorBool("Moving", false);
                    warriorController.SetAnimatorFloat("Velocity", 0);
                }
            }

            RotateTowardsMovementDir();

            // Update animator with local movement values.
            warriorController.SetAnimatorFloat("Velocity", transform.InverseTransformDirection(currentVelocity).z);
        }

        #endregion

        #region States

        private void Idle_EnterState()
        {
            warriorController.superCharacterController.EnableSlopeLimit();
            warriorController.superCharacterController.EnableClamping();
            warriorController.SetAnimatorBool("Moving", false);
        }

        private void Idle_SuperUpdate()
        {
            // Move if there is input.
            if (warriorController.HasMoveInput() && warriorController.canMove)
            {
                currentState = WarriorState.Move;
                return;
            }
            // Apply friction to slow to a halt.
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, groundFriction * warriorController.superCharacterController.deltaTime);
        }

        private void Idle_ExitState()
        {
        }

        private void Move_SuperUpdate()
        {
            // Set speed determined by movement type.
            if (warriorController.HasMoveInput() && warriorController.canMove)
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, warriorController.moveInput * runSpeed, movementAcceleration * warriorController.superCharacterController.deltaTime);
            }
            else
            {
                currentState = WarriorState.Idle;
            }
        }

        #endregion

        /// <summary>
        /// Rotate towards the direction the Warrior is moving.
        /// </summary>
        private void RotateTowardsMovementDir()
        {
            if (warriorController.moveInput != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(warriorController.moveInput), Time.deltaTime * rotationSpeed);
            }
        }
    }
}
