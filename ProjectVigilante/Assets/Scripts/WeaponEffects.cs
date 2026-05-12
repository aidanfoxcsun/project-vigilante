using UnityEngine;
using System.Collections;

public class WeaponEffects : MonoBehaviour
{
    [SerializeField] private ParticleSystem strikeParticles;

    public void PlayStrikeEffect()
    {
        strikeParticles.Stop();
        StartCoroutine(PlayStrikeEffectDelayed());
    }

    IEnumerator PlayStrikeEffectDelayed()
    {
        yield return new WaitForSeconds(0.2f);
        strikeParticles.Play();
    }
}