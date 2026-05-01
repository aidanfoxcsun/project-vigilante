using UnityEngine;
using TMPro;
using System.Collections;

public class ComboUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private float pulseScale = 1.5f;
    [SerializeField] private float pulseDuration = 0.1f;

    private Vector3 originalScale;
    private Coroutine pulseRoutine;

    private void Start()
    {
        originalScale = comboText.transform.localScale;
    }

    public void UpdateComboUI(int count)
    {
        comboText.text = "x" + count;

        // Restart the pulse if one is already running
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseEffect());
    }

    private IEnumerator PulseEffect()
    {
        // 1. Instant Scale Up
        // We can make the pulse bigger based on combo milestones
        comboText.transform.localScale = originalScale * pulseScale;

        // 2. Smoothly Scale Down
        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            comboText.transform.localScale = Vector3.Lerp(
                originalScale * pulseScale,
                originalScale,
                elapsed / pulseDuration
            );
            yield return null;
        }

        comboText.transform.localScale = originalScale;
    }
}