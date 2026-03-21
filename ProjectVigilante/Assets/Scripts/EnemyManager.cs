using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public int livingEnemyCount;

    private BasicEnemy[] enemies;
    private EnemyStruct[] allEnemies;
    private List<int> enemyIndexes;

    private Coroutine AI_Loop_Coroutine;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemies = GetComponentsInChildren<BasicEnemy>();

        allEnemies = new EnemyStruct[enemies.Length];

        for (int i = 0; i < allEnemies.Length; i++)
        {
            allEnemies[i].basicEnemy = enemies[i];
            allEnemies[i].enemyAvailability = true;
        }


    }


}

//----------ENEMY CLASS------------//
[System.Serializable]
public struct EnemyStruct
{
    public BasicEnemy basicEnemy;
    public bool enemyAvailability;
}