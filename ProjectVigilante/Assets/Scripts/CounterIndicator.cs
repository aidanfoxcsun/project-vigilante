// CounterIndicator.cs
// Attach to a world-space Canvas prefab parented to the enemy.
// Call Show() / Hide() from the enemy when signaling / canceling.
using UnityEngine;
using UnityEngine.UI;

public class CounterIndicator : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.05f;

    private Vector3 baseLocalPos;
    private bool visible;

    private void Awake() => baseLocalPos = transform.localPosition;

    private void Update()
    {
        if (!visible) return;
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = baseLocalPos + Vector3.up * offset;
    }

    public void Show()
    {
        visible = true;
        icon.enabled = true;
    }

    public void Hide()
    {
        visible = false;
        icon.enabled = false;
    }
}