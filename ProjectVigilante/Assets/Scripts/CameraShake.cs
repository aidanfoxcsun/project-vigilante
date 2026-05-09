using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [SerializeField] private CinemachineImpulseSource impulseSource;

    void Awake()
    {
        Instance = this;
    }

    public void Shake(float force)
    {
        impulseSource.GenerateImpulse(force);
    }
}