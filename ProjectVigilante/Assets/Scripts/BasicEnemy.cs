using UnityEngine;
using System.Collections;

public class BasicEnemy : MonoBehaviour, ITarget, IDamageable
{
    //-----------------------------------DECLARES--------------------------//
    public Transform player;
    private UnityEngine.AI.NavMeshAgent agent;
    private CharacterController characterController;
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

    [Header("Enemy States")]
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isWaiting = true;
    [SerializeField] private bool isPreparingAttack;
    [SerializeField] private bool isRetreating;
    [SerializeField] private bool isStunned;
    private bool MovementCoroutineActive = false;

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

    //---------------------------------MOVEMENT--------------------------//
    void Start()
    {
        // comment out when manager under dev. //
        enemyManager = GetComponentInParent<EnemyManager>();
        // ----------------------------------- //
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        characterController = GetComponent<CharacterController>();
        agent.stoppingDistance = 0f;
        stoppingDistance = Random.Range(4.0f, 10.0f);

        if (!MovementCoroutineActive)
        {
            MovementCoroutine = StartCoroutine(EncircleEnemyMovement());
        }
    }

    IEnumerator EncircleEnemyMovement()
    {
        MovementCoroutineActive = true;
        yield return new WaitUntil(() => isWaiting == true && Vector3.Distance(transform.position, player.position) <= stoppingDistance);

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

        MovementCoroutine = StartCoroutine(EncircleEnemyMovement());

    }

    void Update()
    {
        if (player != null)
        {
            Vector3 flatEnemyPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatPlayerPos = new Vector3(player.position.x, 0, player.position.z);
            float distanceToPlayer = Vector3.Distance(flatEnemyPos, flatPlayerPos);

            if (distanceToPlayer > stoppingDistance)
            {
                Debug.Log("Enemy tracking player");
                Vector3 toEnemy = (flatEnemyPos - flatPlayerPos).normalized;

                if (toEnemy == Vector3.zero)
                    toEnemy = transform.forward;

                Vector3 targetPos = player.position + toEnemy * Mathf.Max(0f, stoppingDistance - agent.radius);

                agent.isStopped = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
                agent.SetDestination(targetPos);

                isWaiting = false;
            }
            else
            {
                Debug.Log("Enemy in range");
                enemyManager.SetEnemyAvailiability(this, true);
                // keeps looking at player
                transform.LookAt(player);

                // sets agent stuff to false when within stopping distance
                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
                isWaiting = true;
                MoveEnemy(moveDirection);
                agent.nextPosition = transform.position;
            }

        }

        ////activates movement coroutine when enemy is within range of stopping distance
        //if (Vector3.Distance(transform.position, player.position) <= stoppingDistance && !MovementCoroutineActive && isWaiting)
        //{
        //    StartCoroutine(EncircleEnemyMovement());
        //}
    }

    void Death()
    {
        StopCoroutinesEnemy();
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
            stoppingDistance = 8f;
            Debug.Log("Retreating!");

            yield return new WaitUntil(() => Vector3.Distance(transform.position, player.position) > 6f);
            Debug.Log("Retreat successful");
            isRetreating = false;
            StopMoving();

            isWaiting = true;
            MovementCoroutine = StartCoroutine(EncircleEnemyMovement());
            agent.radius = 1.0f;
            stoppingDistance = Random.Range(4.0f, 10.0f);

        }
    }


    //--------------------------ATTACK LOGIC---------------------------//
    public void SetAttack()
    {
        isWaiting = false;
        Debug.Log("Preparing to attack...");
        PrepareAttackCoroutine = StartCoroutine(PrepAttack());

        IEnumerator PrepAttack()
        {
            PrepareAttack(true);
            yield return new WaitForSeconds(0.2f);
            Debug.Log("Attacking!");
            agent.radius = 0.5f;
            stoppingDistance = 1.5f;
            moveDirection = Vector3.forward;
            isMoving = true;
        }
    }

    //-------------------------COUNTER MECHANIC LOGIC BOOL----------------------//
    void PrepareAttack (bool active)
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
            return;

        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 pDir = Quaternion.AngleAxis(90, Vector3.up) * dir; //so enemy appears to circle player
        Vector3 movedir = Vector3.zero;

        Vector3 finalDirection = Vector3.zero;

        if (direction == Vector3.forward)
            finalDirection = dir;
        if (direction == Vector3.right || direction == Vector3.left)
        {
            moveSpeed /= 1.5f;
            finalDirection = (pDir * direction.normalized.x);
        }
        if (direction == -Vector3.forward)
            finalDirection = -transform.forward;

        movedir += finalDirection * moveSpeed * Time.deltaTime;

        characterController.Move(movedir);
        if (!isPreparingAttack) return;


        //attack logic
        if (Vector3.Distance(transform.position, player.position) < 2)
        {
            StopMoving();
            Debug.Log("Player has been attacked");
            PrepareAttack(false);
        }
    }


    public void StopMoving()
    {
        isMoving = false;
        moveDirection = Vector3.zero;
        //characterController.Move(moveDirection);
    }

    void StopCoroutinesEnemy()
    {
        PrepareAttack(false);

        if (isRetreating)
        {
            if (RetreatCoroutine != null)
            {
                StopCoroutine(RetreatCoroutine);
                Debug.Log("Retreat Coroutine stopped");
            }
        }

        if (PrepareAttackCoroutine != null)
        {
            StopCoroutine(PrepareAttackCoroutine);
            Debug.Log("Attack Coroutine stopped");
        }

        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
            Debug.Log("Movement Coroutine stopped");
        }
    }
}
