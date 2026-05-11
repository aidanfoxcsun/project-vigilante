using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public static HealthUI Instance;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateHealthUI(float current, float max)
    {
        healthSlider.value = current / max; // 0–1 range

        // Tint red if health is below 30%
        if (current / max < 0.3f)
        {
            fillImage.color = Color.Lerp(Color.white, Color.red, 1 - (current / max) / 0.3f);
        }
        else
        {
            fillImage.color = Color.white;
        }
    }
}