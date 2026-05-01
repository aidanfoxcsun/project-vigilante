using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BasicEnemy : MonoBehaviour, ITarget, IDamageable, IAttacker
{
    //-----------------------------------DECLARES--------------------------//
    public Transform player;
    private NavMeshAgent agent;
    private CharacterController characterController;
    private EnemyManager enemyManager;
    private Coroutine MovementCoroutine;
    private Coroutine PrepareAttackCoroutine;
    private Coroutine RetreatCoroutine;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private CounterIndicator counterIndicator;

    [Header("Enemy Stats")]
    public float health = 20;
    private float moveSpeed = 1;
    [SerializeField] private Vector3 moveDirection;
    // radius from center of the player that the enemy will stop at
    public float stoppingDistance;

    [Header("Enemy States")]
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isWaiting = true;
    [SerializeField] private bool isPreparingAttack;
    [SerializeField] private bool isRetreating;
    [SerializeField] private bool isStunned;
    private bool MovementCoroutineActive = false;

    public event System.Action<IAttacker> OnAttackSignaled;
    public event System.Action<IAttacker> OnAttackCanceled;

    //-----------------------------------UNITY LIFECYCLE--------------------------//

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        characterController = GetComponent<CharacterController>();
        enemyManager = FindFirstObjectByType<EnemyManager>();

        // Fallback: try to find Animator on this GameObject if not assigned in Inspector
        if (animator == null)
            animator = GetComponent<Animator>();

        StartCoroutine(RegisterWithManager());
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerCombat>()?.transform;
    }

    //-----------------------------------ENEMY MANAGER BOOLS--------------------------//
    public bool IsPreparingAttack()
    {
        return isPreparingAttack; 
    }

    public bool IsRetreating()
    {
        return isRetreating; 
    }

    //-----------------------------------INTERFACES--------------------------//

    public Transform GetTransform()
    {
        return transform;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log("Enemy took " + damage + " damage. Remaining health: " + health);
        if (health <= 0)
        {
            Death();
            Destroy(gameObject);
        }
    }


    // Called by an Animation Event at the start of the attack animation
    public void SignalAttack()
    {
        counterIndicator?.Show();
        OnAttackSignaled?.Invoke(this);
    }

    public void InterruptAttack()
    {
        // Cancel your attack animation / coroutine here
        animator.SetTrigger("Interrupted");
        counterIndicator?.Hide();
        OnAttackCanceled?.Invoke(this);
    }

    //-----------------------------------MOVEMENT STATES--------------------------//

    public void SetAttack()
    {
        if (isStunned) return;

        isWaiting = false;
        isRetreating = false;
        isPreparingAttack = true;

        if (PrepareAttackCoroutine != null)
            StopCoroutine(PrepareAttackCoroutine);

        PrepareAttackCoroutine = StartCoroutine(PrepareAttack());
    }

    public void SetRetreat()
    {
        isPreparingAttack = false;
        isRetreating = true;
        isWaiting = false;

        if (RetreatCoroutine != null)
            StopCoroutine(RetreatCoroutine);

        RetreatCoroutine = StartCoroutine(Retreat());
    }

    //-----------------------------------COROUTINES--------------------------//

    private IEnumerator RegisterWithManager()
    {
        yield return null; // Wait one frame
        enemyManager?.SetEnemyAvailiability(this, true);
    }

    private IEnumerator PrepareAttack()
    {
        SignalAttack();

        while (player != null &&
               Vector3.Distance(transform.position, player.position) > stoppingDistance)
        {
            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }

            yield return null;
        }

        if (player != null)
        {
            agent.isStopped = true;
            animator?.SetTrigger("Attack");
        }

        isPreparingAttack = false;

        enemyManager?.SetEnemyAvailiability(this, false);
    }

    private IEnumerator Retreat()
    {

        if (agent != null && player != null)
        {
            // Move away from the player by flipping the direction
            Vector3 retreatDirection = (transform.position - player.position).normalized;
            Vector3 retreatTarget = transform.position + retreatDirection * stoppingDistance * 2f;

            agent.isStopped = false;
            agent.SetDestination(retreatTarget);
        }

        // Wait until far enough away or a timeout elapses
        float timeout = 3f;
        float elapsed = 0f;

        yield return new WaitUntil(() =>
        {
            elapsed += Time.deltaTime;
            return elapsed >= timeout ||
                   player == null ||
                   Vector3.Distance(transform.position, player.position) > stoppingDistance * 2f;
        });

        isRetreating = false;
        isWaiting = true;

        // Mark available again so EnemyManager can pick this enemy in future rounds
        enemyManager?.SetEnemyAvailiability(this, true);
    }

    //-----------------------------------DEATH--------------------------//

    private void Death()
    {
        isPreparingAttack = false;
        isRetreating = false;

        enemyManager?.SetEnemyAvailiability(this, false);
        enemyManager?.NotifyEnemyDied(this);

        // Cancel any running coroutines so nothing fires after death
        if (MovementCoroutine != null) StopCoroutine(MovementCoroutine);
        if (PrepareAttackCoroutine != null) StopCoroutine(PrepareAttackCoroutine);
        if (RetreatCoroutine != null) StopCoroutine(RetreatCoroutine);

        // Make sure counter indicator is hidden and events fire cleanly
        counterIndicator?.Hide();
        OnAttackCanceled?.Invoke(this);

        // Disable agent so it stops moving before the object is destroyed
        if (agent != null) agent.enabled = false;

        Debug.Log($"{name} has died.");
        Destroy(gameObject);
    }
}
