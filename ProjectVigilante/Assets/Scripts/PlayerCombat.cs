using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRadius = 10f;
    public float forwardBias = 0.75f;
    public float endDistance = 1f;
    public float attackDamage = 10f;
    public float jumpArcHeight = 2f;

    [Header("Launcher Settings")]
    public float launchForce = 10f;
    public float launchForwardPunch = 2f;

    [Header("Counter Settings")]
    // How long after SignalAttack the player can press counter.
    public float counterWindowDuration = 1.2f;
    // Damage multiplier applied to a successful counter hit.
    public float counterDamageMultiplier = 1.5f;

    // private state 
    private bool attacking;
    private bool countering;

    // The enemy currently offering a counter opportunity.
    private IAttacker pendingCounterAttacker;
    private Transform pendingCounterTransform;
    private Coroutine counterWindowCoroutine;

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);
        List<ITarget> targetsInRange = new List<ITarget>();

        foreach (Collider col in hitColliders)
        {
            ITarget t = col.GetComponent<ITarget>();
            if (t != null) targetsInRange.Add(t);

            // Subscribe to any new IAttacker enemies that enter range.
            IAttacker attacker = col.GetComponent<IAttacker>();
            if (attacker != null)
                SubscribeToAttacker(attacker);
        }

        // Counter input (takes priority over a normal attack) 
        bool counterPressed = (gamepad != null)
            ? gamepad.buttonNorth.wasPressedThisFrame          // Triangle / Y
            : Input.GetKeyDown(KeyCode.Q);

        if (counterPressed && pendingCounterAttacker != null)
        {
            StartCoroutine(PerformCounter());
            return;
        }

        // Normal attack input
        if (attacking || countering) return;

        bool attackPressed = (gamepad != null)
            ? gamepad.buttonWest.wasPressedThisFrame
            : Input.GetKeyDown(KeyCode.Space);

        if (attackPressed && targetsInRange.Count > 0)
        {
            Transform best = GetClosestTarget(targetsInRange);
            StartCoroutine(ZipToTarget(best, isCounter: false));
        }

    }

    private readonly HashSet<IAttacker> subscribedAttackers = new();

    private void SubscribeToAttacker(IAttacker attacker)
    {
        if (subscribedAttackers.Contains(attacker)) return;
        subscribedAttackers.Add(attacker);
        attacker.OnAttackSignaled += HandleAttackSignaled;
        attacker.OnAttackCanceled += HandleAttackCanceled;
    }

    private void HandleAttackSignaled(IAttacker attacker)
    {
        // Only track the nearest / most urgent threat.
        Transform t = (attacker as MonoBehaviour)?.transform;
        if (t == null) return;

        // If a counter window is already open, only replace if this enemy is closer.
        if (pendingCounterAttacker != null)
        {
            float existingDist = Vector3.Distance(transform.position, pendingCounterTransform.position);
            float newDist = Vector3.Distance(transform.position, t.position);
            if (newDist >= existingDist) return;
        }

        // Cancel any existing window and open a fresh one.
        if (counterWindowCoroutine != null)
            StopCoroutine(counterWindowCoroutine);

        pendingCounterAttacker = attacker;
        pendingCounterTransform = t;
        counterWindowCoroutine = StartCoroutine(CounterWindowTimer());

        Debug.Log($"[Counter] Window opened — threat: {t.name}");
    }

    private void HandleAttackCanceled(IAttacker attacker)
    {
        if (attacker != pendingCounterAttacker) return;
        CloseCounterWindow();
    }

    private IEnumerator CounterWindowTimer()
    {
        yield return new WaitForSeconds(counterWindowDuration);
        CloseCounterWindow();
        Debug.Log("[Counter] Window expired.");
    }

    private void CloseCounterWindow()
    {
        if (counterWindowCoroutine != null)
        {
            StopCoroutine(counterWindowCoroutine);
            counterWindowCoroutine = null;
        }
        pendingCounterAttacker = null;
        pendingCounterTransform = null;
    }

    // Counter coroutine

    private IEnumerator PerformCounter()
    {
        if (pendingCounterAttacker == null) yield break;

        // Snapshot target before clearing state.
        IAttacker attacker = pendingCounterAttacker;
        Transform target = pendingCounterTransform;

        CloseCounterWindow();
        countering = true;
        attacking = false; // Interrupt any normal attack in progress.

        // Interrupt the enemy's wind-up so their attack never lands.
        attacker.InterruptAttack();

        Debug.Log($"[Counter] Countering {target.name}!");

        // Reuse ZipToTarget with the counter flag so we can apply the bonus.
        yield return StartCoroutine(ZipToTarget(target, isCounter: true));

        countering = false;
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

    private IEnumerator ZipToTarget(Transform target, bool isCounter)
    {
        if (!HasLineOfSight(target))
        {
            Debug.Log("[Combat] No line of sight. Aborting.");
            yield break;
        }

        attacking = !isCounter; // Counters set `countering` instead.

        if (target == null) yield break;

        Vector3 startPos = transform.position;
        Vector3 dirFromTargetToPlayer = (startPos - target.position).normalized;
        Vector3 finalLandingPoint = target.position + dirFromTargetToPlayer * endDistance;

        transform.rotation = Quaternion.LookRotation((target.position - transform.position).normalized);

        float distance = Vector3.Distance(startPos, finalLandingPoint);
        float duration = Mathf.Max(distance / 20f, 0.1f);

        // Counters are snappier — halve the travel time for that reactive feel.
        if (isCounter) duration *= 0.5f;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            Vector3 lerpPos = Vector3.Lerp(startPos, finalLandingPoint, t);
            float arcScale = isCounter ? jumpArcHeight * 0.4f : jumpArcHeight;
            lerpPos.y += Mathf.Sin(t * Mathf.PI) * arcScale * Mathf.Log(Mathf.Max(distance, 1f));
            transform.position = lerpPos;
            yield return null;
        }

        transform.position = finalLandingPoint;

        if (target != null)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float damage = isCounter
                    ? attackDamage * counterDamageMultiplier
                    : attackDamage;

                damageable.TakeDamage(damage);

                if (ComboManager.Instance != null)
                    ComboManager.Instance.RegisterHit();

                if (isCounter)
                    Debug.Log($"[Counter] Hit! {damage} damage (×{counterDamageMultiplier}).");
            }

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 launchDir = Vector3.up * launchForce + transform.forward * launchForwardPunch;
                rb.AddForce(launchDir, ForceMode.Impulse);
            }
        }

        attacking = false;
        countering = false;
    }

    private void OnDestroy()
    {
        foreach (IAttacker a in subscribedAttackers)
        {
            a.OnAttackSignaled -= HandleAttackSignaled;
            a.OnAttackCanceled -= HandleAttackCanceled;
        }
    }
}
