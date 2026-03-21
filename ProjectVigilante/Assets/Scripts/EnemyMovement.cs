using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{

    public Transform player;
    private UnityEngine.AI.NavMeshAgent agent;
    private CharacterController characterController;

    // radius from center of the player that the enemy will stop at
    public float stoppingDistance;
    [SerializeField] private Vector3 moveDirection;

    // movement stats
    private float moveSpeed = 1;

    // States
    private bool MovementCoroutineActive = false;
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isWaiting = true;

    private Coroutine MovementCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

    // Update is called once per frame
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

    void MoveEnemy(Vector3 direction)
    {
        moveSpeed = 1;

        if (direction == Vector3.forward)
            moveSpeed = 5;
        if (direction == -Vector3.forward)
            moveSpeed = 2f;

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
    }


    public void StopMoving()
    {
        isMoving = false;
        moveDirection = Vector3.zero;
    }

}