using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    // Check Radius around player for enemies (ITargets). When attack is triggered, choose the closest and most front enemy to attack.
    public float attackRadius = 10f;
    public float forwardBias = 0.75f; // How much more 'pull' enemies in front of the player have when determining which target to attack.
    public float endDistance = 1f;
    public float attackDamage = 10f; //damage of player

    private void Update()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);
        List<ITarget> targetsInRange = new List<ITarget>();

        foreach (Collider hitCollider in hitColliders)
        {
            ITarget target = hitCollider.GetComponent<ITarget>();
            if (target != null)
            {
                targetsInRange.Add(target);

            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && targetsInRange.Count > 0)
        {
            StartCoroutine(ZipToTarget(GetClosestTarget(targetsInRange)));
            Debug.Log("Attacking target: " + GetClosestTarget(targetsInRange).name);
        }

    }

    private Transform GetClosestTarget(List<ITarget> targets)
    {
        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        Vector3 forward = transform.forward;

        foreach (ITarget target in targets)
        {
            float distance = Vector3.Distance(transform.position, target.GetTransform().position);
            Vector3 toTarget = (target.GetTransform().position - transform.position).normalized;

            // Apply forward bias
            distance *= 1f - forwardBias * Mathf.Max(0, Vector3.Dot(forward, toTarget));

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target.GetTransform();
            }
        }
        return closestTarget;
    }

    private IEnumerator ZipToTarget(Transform target)
    {
        while (Vector3.Distance(transform.position, target.position) > endDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * 20f);
            yield return null;
        }
        
        // Damage logic
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(attackDamage);
        }
    }
}
