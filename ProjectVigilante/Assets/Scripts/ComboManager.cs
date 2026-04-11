using UnityEngine;
using TMPro; // Assuming you're using TextMeshPro for UI

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance;

    [Header("Settings")]
    [SerializeField] private float comboExpiryTime = 2.0f;

    private int currentCombo = 0;
    private float timer;
    private bool isTimerActive;

    public int CurrentCombo => currentCombo;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (isTimerActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                ResetCombo();
            }
        }
    }

    public void RegisterHit()
    {
        currentCombo++;

        // 1. Trigger the crunchy hitstop
        HitstopManager.Instance.TriggerHitstop(0.05f);

        // 2. Update the UI with a pulse
        FindFirstObjectByType<ComboUI>().UpdateComboUI(currentCombo);

        timer = comboExpiryTime; // Reset the clock
        isTimerActive = true;

        // Visual feedback
        Debug.Log($"Combo: {currentCombo}!");
    }

    public void ResetCombo()
    {
        currentCombo = 0;
        isTimerActive = false;
        FindFirstObjectByType<ComboUI>().UpdateComboUI(currentCombo);
    }
}