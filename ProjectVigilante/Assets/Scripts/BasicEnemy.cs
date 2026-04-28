using UnityEngine;

public class BasicEnemy : MonoBehaviour, ITarget, IDamageable, IAttacker
{
    public float health = 20;
    [SerializeField] private CounterIndicator counterIndicator;
    [SerializeField] private Animator animator;
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
            Destroy(gameObject);
        }
    }

    // Inside your enemy class — simplified example

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
