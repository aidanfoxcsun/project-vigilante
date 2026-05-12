using UnityEngine;

public class CombatInteractionIndicator : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        // Apply a constant slight jostle to the indicator to make it more noticeable
        float jostleAmount = 0.05f; // Adjust this value to increase/decrease the jostle intensity
        float jostleX = Mathf.Sin(Time.time * 5f) * jostleAmount; // Jostle speed and intensity
        float jostleY = Mathf.Cos(Time.time * 5f) * jostleAmount; // Jostle speed and intensity
        transform.localPosition = new Vector3(jostleX, jostleY, transform.localPosition.z);

    }
}
