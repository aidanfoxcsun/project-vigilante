using UnityEngine;

public class SpawnEnemies : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // enables EnemyManager on gameobject first
            GetComponent<EnemyManager>().enabled = true;
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
                child.GetComponent<BasicEnemy>().enabled = true;
            }
        }

        GetComponent<Collider>().enabled = false;
    }
}
