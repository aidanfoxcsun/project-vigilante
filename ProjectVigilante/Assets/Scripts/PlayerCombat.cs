using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerCombat : MonoBehaviour
{
    // Check Radius around player for enemies (ITargets). When attack is triggered, choose the closest and most front enemy to attack.
    public float attackRadius = 10f;
    public float forwardBias = 0.75f; // How much more 'pull' enemies in front of the player have when determining which target to attack.
    public float endDistance = 1f;
    public float attackDamage = 10f; //damage of player
    public float jumpArcHeight = 2f; //height of the arc when player zips to target

    [Header("Launcher Settings")]
    public float launchForce = 10f; // How hard to hit them upwards
    public float launchForwardPunch = 2f; // Slight forward nudge so they fly away from you

    private bool attacking = false;

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;

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

        if (gamepad == null || attacking) return;

        if (gamepad.buttonWest.wasPressedThisFrame && targetsInRange.Count > 0)
        {
            StartCoroutine(ZipToTarget(GetClosestTarget(targetsInRange)));
            Debug.Log("Attacking target: " + GetClosestTarget(targetsInRange).name);
        }

    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, attackRadius))
        {
            return hit.transform == target;
        }
        return false;
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
        if(!HasLineOfSight(target))
        {
            Debug.Log("No line of sight to target. Aborting attack.");
            yield break;
        }

        attacking = true;
        if (target == null) yield break; // Safety check

        Vector3 startPos = transform.position;

        // Calculate the actual landing point: 
        // target position + vector pointing from target TO player * endDistance
        Vector3 directionFromTargetToPlayer = (startPos - target.position).normalized;
        Vector3 finalLandingPoint = target.position + (directionFromTargetToPlayer * endDistance);

        // Make sure we always face the target's center
        transform.rotation = Quaternion.LookRotation((target.position - transform.position).normalized);

        float elapsedTime = 0f;
        float distance = Vector3.Distance(startPos, finalLandingPoint);
        float duration = distance / 20f; // Dynamic speed

        // Ensure we don't have duration of zero if standing on the landing point
        if (duration <= 0) duration = 0.1f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration; // 0 to 1

            // Use a slight "S" curve for smoother motion (optional but looks nice)
            // normalizedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

            // Lerp toward the PRE-CALCULATED static landing point, not the target itself
            Vector3 currentLerpPos = Vector3.Lerp(startPos, finalLandingPoint, normalizedTime);

            // Apply the Arc (sine wave)
            float arcHeight = Mathf.Sin(normalizedTime * Mathf.PI) * jumpArcHeight * Mathf.Log(distance);
            currentLerpPos.y += arcHeight;

            transform.position = currentLerpPos;

            // We only rotate at the start of the jump to keep the snap minimal

            yield return null;
        }

        // Ensure we land exactly at the landing point
        transform.position = finalLandingPoint;

        // --- Damage logic (requires target still exist) ---
        if (target != null)
        {
            // 1. Deal Damage
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null) damageable.TakeDamage(attackDamage);

            // 2. Launch the Enemy
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                // Calculate launch vector: Straight up + a bit of forward "oomph"
                Vector3 launchDir = Vector3.up * launchForce + transform.forward * launchForwardPunch;

                // ForceMode.Impulse is best for instant bursts (ignoring mass if you want)
                targetRb.AddForce(launchDir, ForceMode.Impulse);

                Debug.Log($"Launched {target.name} into the air!");
            }
        }
        attacking = false;
    }
}
