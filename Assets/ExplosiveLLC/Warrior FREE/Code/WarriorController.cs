using System.Collections;
using UnityEngine;

namespace WarriorAnim
{
    public class WarriorController : SuperStateMachine
    {
        [Header("Components")]
        public Warrior warrior;
        public GameObject target;
        public GameObject weapon;
        private Rigidbody rb;
        [HideInInspector] public SuperCharacterController superCharacterController;
        [HideInInspector] public WarriorMovementController warriorMovementController;
        [HideInInspector] public WarriorInputController warriorInputController;
        [HideInInspector] public WarriorInputSystemController warriorInputSystemController;
        [HideInInspector] public WarriorTiming warriorTiming;
        [HideInInspector] public Animator animator;
        [HideInInspector] public IKHands ikHands;

        [HideInInspector] public bool inputAttack;
        [HideInInspector] public float inputVertical = 0;
        [HideInInspector] public float inputHorizontal = 0;

        [HideInInspector] public Vector3 moveInput;

        private bool useInputSystem;

        public bool allowedInput { get { return _allowedInput; } }
        private bool _allowedInput = true;

        [HideInInspector] public bool isMoving;
        [HideInInspector] public bool useRootMotion = false;

        public bool canAction { get { return _canAction; } }
        private bool _canAction = true;

        public bool canMove { get { return _canMove; } }
        private bool _canMove = true;

        public float animationSpeed = 1;

        #region Initialization

        private void Awake()
        {
            superCharacterController = GetComponent<SuperCharacterController>();

            warriorMovementController = GetComponent<WarriorMovementController>();

            warriorTiming = gameObject.AddComponent<WarriorTiming>();
            warriorTiming.warriorController = this;

            ikHands = GetComponentInChildren<IKHands>();
            if (ikHands != null)
            {
                if (warrior == Warrior.TwoHanded
                    || warrior == Warrior.Hammer
                    || warrior == Warrior.Crossbow
                    || warrior == Warrior.Spearman)
                {
                    ikHands.canBeUsed = true;
                    ikHands.BlendIK(true, 0, 0.25f);
                }
            }

            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("ERROR: There is no Animator component for character.");
                Debug.Break();
            }
            else
            {
                animator.gameObject.AddComponent<WarriorCharacterAnimatorEvents>();
                animator.GetComponent<WarriorCharacterAnimatorEvents>().warriorController = this;
                animator.gameObject.AddComponent<AnimatorParentMove>();
                animator.GetComponent<AnimatorParentMove>().animator = animator;
                animator.GetComponent<AnimatorParentMove>().warriorController = this;
                animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            warriorInputController = GetComponent<WarriorInputController>();
            if (warriorInputController != null)
            {
                useInputSystem = false;
            }
            else
            {
                warriorInputSystemController = GetComponent<WarriorInputSystemController>();
                if (warriorInputSystemController != null) { useInputSystem = true; } else { Debug.LogError("No inputs!"); }
            }

            rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; }

            currentState = WarriorState.Idle;
        }

        #endregion

        #region Input

        private void GetInput()
        {
            if (allowedInput)
            {
                if (!useInputSystem)
                {
                    if (warriorInputController != null)
                    {
                        inputAttack = warriorInputController.inputAttack;
                        moveInput = warriorInputController.moveInput;
                    }
                }
                else
                {
                    if (warriorInputSystemController != null)
                    {
                        inputAttack = warriorInputSystemController.inputAttack;
                        moveInput = warriorInputSystemController.moveInput;
                    }
                }
            }
        }

        public bool HasMoveInput()
        {
            return moveInput != Vector3.zero;
        }

        public void AllowInput(bool b)
        {
            _allowedInput = b;
        }

        #endregion

        #region Updates

        private void Update()
        {
            GetInput();

            if (MaintainingGround() && canAction) { Attacking(); }

            UpdateAnimationSpeed();
        }

        private void UpdateAnimationSpeed()
        {
            SetAnimatorFloat("Animation Speed", animationSpeed);
        }

        #endregion

        #region Combat

        private void Attacking()
        {
            if (inputAttack) { Attack1(); }
        }

        public void Attack1()
        {
            SetAnimatorInt("Action", 1);
            SetAnimatorTrigger(AnimatorTrigger.AttackTrigger);
            Lock(true, true, true, 0, warriorTiming.TimingLock(warrior, "attack1"));
        }

        #endregion

        #region Locks

        public void Lock(bool lockMovement, bool lockAction, bool timed, float delayTime, float lockTime)
        {
            StopCoroutine("_Lock");
            StartCoroutine(_Lock(lockMovement, lockAction, timed, delayTime, lockTime));
        }

        public IEnumerator _Lock(bool lockMovement, bool lockAction, bool timed, float delayTime, float lockTime)
        {
            if (delayTime > 0) { yield return new WaitForSeconds(delayTime); }
            if (lockMovement) { LockMove(true); }
            if (lockAction) { LockAction(true); }
            if (timed)
            {
                if (lockTime > 0)
                {
                    yield return new WaitForSeconds(lockTime);
                    UnLock(lockMovement, lockAction);
                }
            }
        }

        public void LockMove(bool b)
        {
            if (b)
            {
                SetAnimatorBool("Moving", false);
                SetAnimatorRootMotion(true);
                _canMove = false;
                moveInput = Vector3.zero;
            }
            else
            {
                _canMove = true;
                SetAnimatorRootMotion(false);
            }
        }

        public void LockAction(bool b)
        {
            _canAction = !b;
        }

        private void UnLock(bool movement, bool actions)
        {
            if (movement) { LockMove(false); }
            if (actions) { _canAction = true; }
        }

        #endregion

        #region Misc

        public bool MaintainingGround()
        {
            return superCharacterController.currentGround.IsGrounded(true, 0.5f);
        }

        public void SetAnimatorTrigger(AnimatorTrigger trigger)
        {
            animator.SetInteger("Trigger Number", (int)trigger);
            animator.SetTrigger("Trigger");
        }

        public void SetAnimatorBool(string name, bool b)
        {
            animator.SetBool(name, b);
        }

        public void SetAnimatorFloat(string name, float f)
        {
            animator.SetFloat(name, f);
        }

        public void SetAnimatorInt(string name, int i)
        {
            animator.SetInteger(name, i);
        }

        public void SetAnimatorRootMotion(bool b)
        {
            useRootMotion = b;
        }

        public void ControllerDebug()
        {
            Debug.Log("CONTROLLER SETTINGS---------------------------");
            Debug.Log("useInputSystem: " + useInputSystem);
            Debug.Log("allowedInput: " + allowedInput);
            Debug.Log("isMoving: " + isMoving);
            Debug.Log("useRootMotion: " + useRootMotion);
            Debug.Log("canAction: " + canAction);
            Debug.Log("canMove: " + canMove);
            Debug.Log("animationSpeed: " + animationSpeed);
        }

        public void AnimatorDebug()
        {
            Debug.Log("ANIMATOR SETTINGS---------------------------");
            Debug.Log("Moving: " + animator.GetBool("Moving"));
            Debug.Log("Trigger Number: " + animator.GetInteger("Trigger Number"));
            Debug.Log("Velocity: " + animator.GetFloat("Velocity"));
        }

        #endregion
    }
}
