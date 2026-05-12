using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthUI : MonoBehaviour
{
    public static HealthUI Instance;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;

    [Header("Tiers (descending order)")]
    [SerializeField] private float[] tiers = { 100f, 75f, 50f, 30f };

    [Header("Regen Settings")]
    [SerializeField] private float combatCooldown = 3f;   // seconds of no damage before regen starts
    [SerializeField] private float regenRate = 5f;    // HP per second during regen
    [SerializeField] private float regenTickRate = 0.1f;  // how often regen applies (seconds)

    private float currentHealth;
    private float maxHealth;

    private Coroutine regenCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    private float previousHealth;

    public void UpdateHealthUI(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;

        float pct = current / max;
        healthSlider.value = pct; // 0–1 range

        // Tint red if health is below 30%
        if (pct < 0.3f)
        {
            fillImage.color = Color.Lerp(Color.red, Color.white, pct / 0.3f);
        }
        else
        {
            fillImage.color = Color.white;
        }

        if(current < previousHealth)
        {
            // Restart the regen countdown every time damage is taken
            if (regenCoroutine != null)
                StopCoroutine(regenCoroutine);
            regenCoroutine = StartCoroutine(RegenAfterCooldown());
        }

        previousHealth = currentHealth;
    }

    private float GetRegenCeiling()
    {
        // tiers[] is descending (100, 75, 50, 30)
        // Walk from lowest to highest — return the first tier ABOVE current health
        for (int i = tiers.Length - 1; i >= 0; i--)
        {
            if (currentHealth < tiers[i])
                return tiers[i];
        }
        return maxHealth; // already at or above all tiers
    }

    private IEnumerator RegenAfterCooldown()
    {
        // Wait for the combat cooldown — if UpdateHealthUI fires again
        // during this window, this coroutine gets cancelled and restarted,
        // so the timer naturally resets on every hit.
        yield return new WaitForSeconds(combatCooldown);

        float ceiling = GetRegenCeiling();

        // Already at or above the ceiling (e.g. full health) — nothing to do
        if (currentHealth >= ceiling) yield break;

        // Tick regen until we hit the ceiling
        while (currentHealth < ceiling)
        {
            float newHealth = Mathf.Min(currentHealth + regenRate * regenTickRate, ceiling);

            // Push the new value back to PlayerCombat so its currentHealth
            // stays in sync — PlayerCombat then calls UpdateHealthUI back
            PlayerCombat.Instance.Heal(newHealth - currentHealth);

            yield return new WaitForSeconds(regenTickRate);
        }
    }


}