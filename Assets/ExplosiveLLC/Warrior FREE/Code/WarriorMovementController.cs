using UnityEngine;

namespace WarriorAnim
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
         
            currentState = WarriorState.Idle;
        }

        #region Updates

        protected override void EarlyGlobalSuperUpdate()
        {
        }

        protected override void LateGlobalSuperUpdate()
        {
           transform.position += currentVelocity * warriorController.superCharacterController.deltaTime;

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
            if (warriorController.HasMoveInput() && warriorController.canMove)
            {
                currentState = WarriorState.Move;
                return;
            }
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, groundFriction * warriorController.superCharacterController.deltaTime);
        }

        private void Idle_ExitState()
        {
        }

        private void Move_SuperUpdate()
        {
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

        private void RotateTowardsMovementDir()
        {
            if (warriorController.moveInput != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(warriorController.moveInput), Time.deltaTime * rotationSpeed);
            }
        }
    }
}
