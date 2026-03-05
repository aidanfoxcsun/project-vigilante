using UnityEngine;

public class BasicEnemy : MonoBehaviour, ITarget, IDamageable
{
    public float health = 20;
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
}
