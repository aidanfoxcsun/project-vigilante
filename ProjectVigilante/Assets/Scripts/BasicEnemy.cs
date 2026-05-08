using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BasicEnemy : MonoBehaviour, ITarget, IDamageable, IAttacker
{
    //-----------------------------------DECLARES--------------------------//
    public Transform player;
    private NavMeshAgent agent;
    private EnemyManager enemyManager;
    private Coroutine MovementCoroutine;
    private Coroutine PrepareAttackCoroutine;
    private Coroutine RetreatCoroutine;

    [Header("Enemy Stats")]
    public float health = 20;
    private float moveSpeed = 1;
    [SerializeField] private Vector3 moveDirection;
    // radius from center of the player that the enemy will stop at
    public float stoppingDistance;

    [Header("NavMesh Movement")]
    [SerializeField] private float trackingDestinationRefreshTime = 0.15f;
    [SerializeField] private float encircleDestinationRefreshTime = 0.25f;
    [SerializeField] private float attackDestinationRefreshTime = 0.1f;
    [SerializeField] private float retreatDestinationRefreshTime = 0.25f;
    [SerializeField] private float destinationReachedDistance = 0.35f;
    [SerializeField] private float destinationRepathDistance = 0.75f;
    [SerializeField] private float navMeshSampleRadius = 1.25f;
    [SerializeField] private float maxNavMeshHeightDifference = 1.5f;
    [SerializeField] private float engageDistanceBuffer = 0.75f;
    [SerializeField] private float trackingInnerBuffer = 1.0f;
    [SerializeField] private float trackingAngleStep = 30f;
    [SerializeField] private int trackingSampleAttempts = 4;

    [Header("Encircle Movement")]
    [SerializeField] private float encircleAngleStep = 25f;
    [SerializeField] private int encircleSampleAttempts = 8;
    [SerializeField] private float minimumEncircleRadius = 2f;

    [Header("Attack Movement")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackStoppingDistance = 1.5f;
    [SerializeField] private float attackAgentRadius = 0.5f;

    [Header("Retreat Movement")]
    [SerializeField] private float retreatStoppingDistance = 8f;
    [SerializeField] private float retreatSuccessDistance = 6f;
    [SerializeField] private float retreatDestinationStep = 2f;
    [SerializeField] private int retreatSampleAttempts = 5;

    [Header("Enemy States")]
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isWaiting = true;
    [SerializeField] private bool isPreparingAttack;
    [SerializeField] private bool isRetreating;
    [SerializeField] private bool isStunned;
    private bool MovementCoroutineActive = false;

    private float defaultAgentSpeed;
    private float defaultAgentRadius;
    private float lastDestinationSetTime = -999f;
    private Vector3 currentDestination;
    private bool hasDestination;

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
    public event System.Action<IAttacker> OnAttackSignaled;
    public event System.Action<IAttacker> OnAttackCanceled;


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

    // Inside your enemy class   simplified example

    // Called by an Animation Event at the start of the attack animation
    public void SignalAttack()
    {
        //counterIndicator?.Show();
        OnAttackSignaled?.Invoke(this);
    }

    public void InterruptAttack()
    {
        // Cancel your attack animation / coroutine here
        //animator.SetTrigger("Interrupted");
        //counterIndicator?.Hide();

        // Stop the attack startup coroutine too, otherwise it can turn attacking back on after the counter.
        if (PrepareAttackCoroutine != null)
        {
            StopCoroutine(PrepareAttackCoroutine);
            PrepareAttackCoroutine = null;
        }

        // Reset the local attack state so EnemyManager does not wait forever after a counter.
        PrepareAttack(false);
        OnAttackCanceled?.Invoke(this);
    }

    //---------------------------------MOVEMENT--------------------------//
    void Start()
    {
        // comment out when manager under dev. //
        enemyManager = GetComponentInParent<EnemyManager>();
        // ----------------------------------- //
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError(name + " needs a NavMeshAgent for BasicEnemy movement.");
            enabled = false;
            return;
        }

        defaultAgentSpeed = agent.speed;
        defaultAgentRadius = agent.radius;

        // The script picks destinations that are already offset from the player, so the agent's own stopping distance should stay at 0.
        agent.stoppingDistance = 0f;
        agent.updatePosition = true;
        agent.updateRotation = true;

        stoppingDistance = Random.Range(4.0f, 10.0f);

        StartEncircleCoroutine();
    }

    IEnumerator EncircleEnemyMovement()
    {
        MovementCoroutineActive = true;

        while (true)
        {
            // Only roll a new idle/encircle choice while the enemy is close enough to be engaged with the player.
            yield return new WaitUntil(() => player != null && isWaiting == true && FlatDistanceToPlayer() <= stoppingDistance && !isPreparingAttack && !isRetreating);

            Debug.Log("Encricle Coroutine started");
            int randomChance = Random.Range(0, 2);

            if (randomChance == 1)
            {
                int randomDirection = Random.Range(0, 2);
                moveDirection = randomDirection == 0 ? Vector3.left : Vector3.right;
                isMoving = true;
            }

            else
            {
                StopMoving();
            }

            yield return new WaitForSeconds(2f);
        }
    }

    void Update()
    {
        if (player == null || agent == null)
            return;

        if (isStunned)
        {
            StopAgentMovement();
            return;
        }

        float distanceToPlayer = FlatDistanceToPlayer();

        if (distanceToPlayer > stoppingDistance + engageDistanceBuffer)
        {
            TrackPlayerAtStoppingDistance();
        }
        else
        {
            EngagePlayerAtCloseRange();
        }
    }

    private void TrackPlayerAtStoppingDistance()
    {
        Debug.Log("Enemy tracking player");

        if (enemyManager != null)
            enemyManager.SetEnemyAvailiability(this, false);

        isWaiting = false;

        // While outside the desired range, the enemy goes back to normal NavMesh tracking.
        agent.speed = defaultAgentSpeed;
        agent.radius = isPreparingAttack ? attackAgentRadius : agent.radius;
        agent.updatePosition = true;
        agent.updateRotation = true;

        Vector3 fromPlayerToEnemy = FlatVector(transform.position - player.position);

        if (fromPlayerToEnemy.sqrMagnitude < 0.001f)
            fromPlayerToEnemy = -FlatForward();

        fromPlayerToEnemy.Normalize();

        // This recreates the old tracking behavior: move toward a point near the player instead of directly onto the player.
        TrySetTrackingDestination(fromPlayerToEnemy);
    }

    private void EngagePlayerAtCloseRange()
    {
        Debug.Log("Enemy in range");

        if (enemyManager != null)
            enemyManager.SetEnemyAvailiability(this, !isPreparingAttack && !isRetreating);

        // The NavMeshAgent still moves the enemy, but rotation is handled manually so the enemy keeps looking at the player.
        agent.updatePosition = true;
        agent.updateRotation = false;
        FacePlayerFlat();

        isWaiting = true;
        MoveEnemy(moveDirection);
    }

    void Death()
    {
        StopCoroutinesEnemy();

        if (enemyManager != null)
            enemyManager.SetEnemyAvailiability(this, false);

    }

    //--------------------------RETREAT LOGIC---------------------------//

    public void SetRetreat()
    {
        StopCoroutinesEnemy();
        Debug.Log("Preparing to retreat...");
        RetreatCoroutine = StartCoroutine(PrepRetreat());

        IEnumerator PrepRetreat()
        {
            yield return new WaitForSeconds(1.4f);
            isRetreating = true;
            moveDirection = -Vector3.forward;
            isMoving = true;
            stoppingDistance = retreatStoppingDistance;
            Debug.Log("Retreating!");

            // Retreat ends once the enemy has created enough flat distance from the player.
            yield return new WaitUntil(() => FlatDistanceToPlayer() > retreatSuccessDistance || !isRetreating);
            Debug.Log("Retreat successful");
            isRetreating = false;
            StopMoving();

            isWaiting = true;
            StartEncircleCoroutine();
            agent.radius = defaultAgentRadius;
            stoppingDistance = Random.Range(4.0f, 10.0f);

        }
    }


    //--------------------------ATTACK LOGIC---------------------------//
    public void SetAttack()
    {
        StopEncircleCoroutine();
        isWaiting = false;
        Debug.Log("Preparing to attack...");
        PrepareAttackCoroutine = StartCoroutine(PrepAttack());

        IEnumerator PrepAttack()
        {
            PrepareAttack(true);
            yield return new WaitForSeconds(0.2f);
            Debug.Log("Attacking!");
            agent.radius = attackAgentRadius;
            stoppingDistance = attackStoppingDistance;
            moveDirection = Vector3.forward;
            isMoving = true;
        }
    }

    //-------------------------COUNTER MECHANIC LOGIC BOOL----------------------//
    void PrepareAttack(bool active)
    {
        isPreparingAttack = active;

        if (active)
        {
            //counter animations
        }

        else
        {
            {
                StopMoving();
                //counter animations
            }
        }
    }

    void MoveEnemy(Vector3 direction)
    {
        moveSpeed = 1;

        if (direction == Vector3.forward)
            moveSpeed = 5;
        if (direction == -Vector3.forward)
            moveSpeed = 2f;

        //animator stuff here?
        //-------//

        //-------//

        if (!isMoving)
        {
            StopAgentMovement();
            return;
        }

        // The original logic used Vector3.forward to mean attack/advance, left/right to mean encircle, and -forward to mean retreat.
        // This keeps that same meaning, but each state now chooses a NavMesh destination instead of using CharacterController.Move().
        agent.speed = moveSpeed;

        bool destinationFound = false;

        if (direction == Vector3.forward)
            destinationFound = SetAttackDestination();
        else if (direction == Vector3.right || direction == Vector3.left)
            destinationFound = SetEncircleDestination(direction);
        else if (direction == -Vector3.forward)
            destinationFound = SetRetreatDestination();

        if (!destinationFound)
        {
            // If the current direction would leave the NavMesh, stop instead of allowing world-space drifting or floating.
            StopAgentMovement();
        }

        if (!isPreparingAttack) return;


        //attack logic
        if (FlatDistanceToPlayer() < attackRange)
        {
            StopMoving();
            Debug.Log("Player has been attacked");
            PrepareAttack(false);
        }
    }

    //----------------------------------------HELPER STUFF------------------------------------//
    private bool SetAttackDestination()
    {
        Vector3 fromPlayerToEnemy = FlatVector(transform.position - player.position);

        if (fromPlayerToEnemy.sqrMagnitude < 0.001f)
            fromPlayerToEnemy = -FlatForward();

        fromPlayerToEnemy.Normalize();

        // Attack movement still approaches the player, but it targets a small offset point instead of the player's exact center.
        float desiredRadius = Mathf.Max(agent.radius + 0.25f, attackStoppingDistance * 0.5f);
        Vector3 desiredPosition = player.position + fromPlayerToEnemy * desiredRadius;

        return TrySetAgentDestination(desiredPosition, attackDestinationRefreshTime);
    }

    private bool SetEncircleDestination(Vector3 direction)
    {
        Vector3 fromPlayerToEnemy = FlatVector(transform.position - player.position);

        if (fromPlayerToEnemy.sqrMagnitude < 0.001f)
            fromPlayerToEnemy = -FlatForward();

        fromPlayerToEnemy.Normalize();

        float side = direction.x >= 0f ? 1f : -1f;
        float radius = Mathf.Max(minimumEncircleRadius, stoppingDistance - agent.radius);

        // First try to continue circling in the selected direction.
        if (TrySetEncircleDestinationWithSide(fromPlayerToEnemy, radius, side))
            return true;

        // If that side is blocked by a cliff/edge/obstacle, try reversing direction so the enemy keeps moving on the NavMesh.
        if (TrySetEncircleDestinationWithSide(fromPlayerToEnemy, radius, -side))
        {
            moveDirection = direction == Vector3.left ? Vector3.right : Vector3.left;
            return true;
        }

        return false;
    }

    private bool TrySetEncircleDestinationWithSide(Vector3 fromPlayerToEnemy, float radius, float side)
    {
        for (int i = 1; i <= encircleSampleAttempts; i++)
        {
            float angle = encircleAngleStep * i * side;
            Vector3 orbitDirection = Quaternion.AngleAxis(angle, Vector3.up) * fromPlayerToEnemy;
            Vector3 desiredPosition = player.position + orbitDirection.normalized * radius;

            if (TrySetAgentDestination(desiredPosition, encircleDestinationRefreshTime))
                return true;
        }

        return false;
    }

    private bool SetRetreatDestination()
    {
        Vector3 awayFromPlayer = FlatVector(transform.position - player.position);

        if (awayFromPlayer.sqrMagnitude < 0.001f)
            awayFromPlayer = -FlatForward();

        awayFromPlayer.Normalize();

        // Try several retreat points. The first is straight back, then wider angles if straight back is blocked.
        for (int distanceAttempt = 1; distanceAttempt <= retreatSampleAttempts; distanceAttempt++)
        {
            float distance = retreatDestinationStep * distanceAttempt;

            if (TrySetRetreatDestinationAtAngle(awayFromPlayer, 0f, distance))
                return true;

            if (TrySetRetreatDestinationAtAngle(awayFromPlayer, 35f, distance))
                return true;

            if (TrySetRetreatDestinationAtAngle(awayFromPlayer, -35f, distance))
                return true;

            if (TrySetRetreatDestinationAtAngle(awayFromPlayer, 70f, distance))
                return true;

            if (TrySetRetreatDestinationAtAngle(awayFromPlayer, -70f, distance))
                return true;
        }

        Debug.LogWarning(name + " could not find a valid retreat destination on the NavMesh.");
        isRetreating = false;
        return false;
    }

    private bool TrySetRetreatDestinationAtAngle(Vector3 awayFromPlayer, float angle, float distance)
    {
        Vector3 retreatDirection = Quaternion.AngleAxis(angle, Vector3.up) * awayFromPlayer;
        Vector3 desiredPosition = transform.position + retreatDirection.normalized * distance;
        return TrySetAgentDestination(desiredPosition, retreatDestinationRefreshTime);
    }

    private bool TrySetAgentDestination(Vector3 desiredPosition, float refreshTime)
    {
        if (!CanUseAgent())
            return false;

        bool reachedDestination = !agent.pathPending && agent.hasPath && agent.remainingDistance <= destinationReachedDistance;
        bool destinationMovedEnough = !hasDestination || FlatDistance(currentDestination, desiredPosition) >= destinationRepathDistance;
        bool refreshTimerReady = Time.time - lastDestinationSetTime >= refreshTime;

        // Reuse the current path briefly instead of spamming SetDestination every frame.
        if (hasDestination && agent.hasPath && !reachedDestination && !destinationMovedEnough && !refreshTimerReady)
            return true;

        if (!TryGetValidNavMeshPoint(desiredPosition, out Vector3 navMeshPosition))
            return false;

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(navMeshPosition, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        agent.isStopped = false;
        agent.updatePosition = true;
        agent.SetDestination(navMeshPosition);

        currentDestination = navMeshPosition;
        hasDestination = true;
        lastDestinationSetTime = Time.time;
        return true;
    }

    private bool TryGetValidNavMeshPoint(Vector3 desiredPosition, out Vector3 navMeshPosition)
    {
        navMeshPosition = desiredPosition;

        if (!CanUseAgent())
            return false;

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(desiredPosition, out hit, navMeshSampleRadius, agent.areaMask))
            return false;

        // This height guard prevents elevated enemies from snapping to a lower NavMesh surface beneath them.
        if (Mathf.Abs(hit.position.y - desiredPosition.y) > maxNavMeshHeightDifference)
            return false;

        navMeshPosition = hit.position;
        return true;
    }

    private bool TrySetTrackingDestination(Vector3 fromPlayerToEnemy)
    {
        // Aim inside stoppingDistance instead of exactly on the edge. This makes it much more likely
        // that reaching the tracking destination will immediately transition into engagement behavior.
        float desiredRadius = Mathf.Max(agent.radius + 0.25f, stoppingDistance - agent.radius - trackingInnerBuffer);
        float maxUsefulDistance = stoppingDistance + engageDistanceBuffer;

        if (TrySetTrackingDestinationAtAngle(fromPlayerToEnemy, 0f, desiredRadius, maxUsefulDistance))
            return true;

        // If the straight-line ring point is not useful, try nearby points around the player.
        // This helps when the original point samples to an edge, obstacle, or slightly wrong NavMesh polygon.
        for (int i = 1; i <= trackingSampleAttempts; i++)
        {
            float angle = trackingAngleStep * i;

            if (TrySetTrackingDestinationAtAngle(fromPlayerToEnemy, angle, desiredRadius, maxUsefulDistance))
                return true;

            if (TrySetTrackingDestinationAtAngle(fromPlayerToEnemy, -angle, desiredRadius, maxUsefulDistance))
                return true;
        }

        // Last fallback: go to a closer valid point instead of standing still outside the engagement range.
        float closerRadius = Mathf.Max(agent.radius + 0.25f, stoppingDistance * 0.5f);
        return TrySetTrackingDestinationAtAngle(fromPlayerToEnemy, 0f, closerRadius, maxUsefulDistance);
    }

    private bool TrySetTrackingDestinationAtAngle(Vector3 fromPlayerToEnemy, float angle, float radius, float maxFlatDistanceFromPlayer)
    {
        Vector3 rotatedDirection = Quaternion.AngleAxis(angle, Vector3.up) * fromPlayerToEnemy;
        rotatedDirection.y = 0f;

        if (rotatedDirection.sqrMagnitude < 0.001f)
            return false;

        rotatedDirection.Normalize();

        Vector3 desiredPosition = player.position + rotatedDirection * radius;

        if (!TryGetValidNavMeshPoint(desiredPosition, out Vector3 navMeshPosition))
            return false;

        if (FlatDistance(navMeshPosition, player.position) > maxFlatDistanceFromPlayer)
            return false;

        return TrySetAgentDestination(navMeshPosition, trackingDestinationRefreshTime);
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private Vector3 FlatVector(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    private Vector3 FlatForward()
    {
        Vector3 forward = FlatVector(transform.forward);

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private float FlatDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        return FlatDistance(transform.position, player.position);
    }

    private void FacePlayerFlat()
    {
        Vector3 directionToPlayer = FlatVector(player.position - transform.position);

        if (directionToPlayer.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(directionToPlayer.normalized);
    }

    private void StopAgentMovement()
    {
        if (!CanUseAgent())
            return;

        agent.isStopped = true;
        agent.ResetPath();
        hasDestination = false;
    }


    public void StopMoving()
    {
        isMoving = false;
        moveDirection = Vector3.zero;
        StopAgentMovement();
    }

    private void StartEncircleCoroutine()
    {
        if (!MovementCoroutineActive)
        {
            MovementCoroutine = StartCoroutine(EncircleEnemyMovement());
        }
    }

    private void StopEncircleCoroutine()
    {
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
            MovementCoroutine = null;
            Debug.Log("Movement Coroutine stopped");
        }

        MovementCoroutineActive = false;
    }

    void StopCoroutinesEnemy()
    {
        PrepareAttack(false);

        if (RetreatCoroutine != null)
        {
            StopCoroutine(RetreatCoroutine);
            RetreatCoroutine = null;
            Debug.Log("Retreat Coroutine stopped");
        }

        if (PrepareAttackCoroutine != null)
        {
            StopCoroutine(PrepareAttackCoroutine);
            PrepareAttackCoroutine = null;
            Debug.Log("Attack Coroutine stopped");
        }

        StopEncircleCoroutine();
    }
}