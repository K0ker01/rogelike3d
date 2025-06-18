using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; 
    public float detectionRange = 10f;
    public float attackRange = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private bool isAttacking = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.stoppingDistance = attackRange - 0.1f;
        agent.updateRotation = true;
        agent.updatePosition = true;
    }

    private void Update()
    {
        if (target == null || !agent.isOnNavMesh) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            if (!isAttacking)
            {
                StartAttack();
            }
            RotateTowardsTarget();
        }
        else if (distance <= detectionRange)
        {
            if (!isAttacking)
            {
                agent.SetDestination(target.position);
                animator.SetBool("Moving", true);
                animator.SetFloat("Animation Speed", agent.velocity.magnitude);
            }
        }
        else
        {
            StopMovement();
        }

    }

    private void StartAttack()
    {
        isAttacking = true;
        agent.ResetPath();

        animator.SetBool("Moving", false);
        animator.SetInteger("Trigger Number", 2); 
        animator.SetTrigger("Trigger");
    }

    private void StopMovement()
    {
        agent.ResetPath();
        animator.SetBool("Moving", false);
        animator.SetFloat("Animation Speed", 0f);
    }

    private void RotateTowardsTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    public void DealDamage()
    {
        if (target.TryGetComponent<HealthSystem>(out var health))
        {
            health.TakeDamage(15); 
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    public void FootR()
    {
       /* Debug.Log("");*/
    }

    public void FootL()
    {
     /*   Debug.Log("");*/
    }
}