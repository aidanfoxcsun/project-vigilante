using UnityEngine;
using System.Collections;

public class HitstopManager : MonoBehaviour
{
    public static HitstopManager Instance;

    private void Awake() => Instance = this;

    public void TriggerHitstop(float duration = 0.05f)
    {
        StartCoroutine(DoHitstop(duration));
    }

    private IEnumerator DoHitstop(float duration)
    {
        Time.timeScale = 0f;
        // Realtime is immune to timeScale changes
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }
}
