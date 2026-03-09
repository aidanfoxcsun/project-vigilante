using UnityEngine;

public class BasicEnemy : MonoBehaviour, ITarget
{
    public Transform GetTransform()
    {
        return transform;
    }
}
