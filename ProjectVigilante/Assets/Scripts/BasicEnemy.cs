using UnityEngine;
using System.Collections;

public class BasicEnemy : MonoBehaviour, ITarget, IDamageable, IAttacker
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

    // Inside your enemy class � simplified example

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
}
