using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    //-----------------------------------DECLARES--------------------------//
    public int livingEnemyCount;

    private BasicEnemy[] enemies;
    public EnemyStruct[] allEnemies;
    private List<int> enemyIndexes;

    private Coroutine AI_Loop_Coroutine;

    //-----------------------------------SETUP----------------------------------//
    void Start()
    {
        enemies = GetComponentsInChildren<BasicEnemy>();

        allEnemies = new EnemyStruct[enemies.Length];

        for (int i = 0; i < allEnemies.Length; i++)
        {
            allEnemies[i].basicEnemy = enemies[i];
            allEnemies[i].enemyAvailability = false;
        }

        BeginAI();
    }

    public void BeginAI()
    {
        Debug.Log("Beginning AI loop");
        AI_Loop_Coroutine = StartCoroutine(AI_Loop(null));
    }

    //-----------------------------------MAIN LOGIC LOOP--------------------------//
    IEnumerator AI_Loop (BasicEnemy enemy)
    {
        Debug.Log("Loop started");
        if (LivingEnemyCount() == 0)
        {
            StopCoroutine(AI_Loop(null));
            yield break;
        }

        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        BasicEnemy attackingEnemy = RandomEnemyExcludingOne(enemy);

        if (attackingEnemy == null)
        {
            Debug.Log("First time attacking, choosing random enemy");
            attackingEnemy = RandomEnemy();
        }

        if (attackingEnemy == null)
        {
            Debug.Log("Attacking enemy not found!");
            AI_Loop_Coroutine = StartCoroutine(AI_Loop(null));
            yield break;
        }

        yield return new WaitUntil(() => attackingEnemy.IsRetreating() == false);

        Debug.Log(attackingEnemy + "assigned to attack!");
        attackingEnemy.SetAttack();

        yield return new WaitUntil(() => attackingEnemy.IsPreparingAttack() == false);

        Debug.Log(attackingEnemy + "assigned to retreat!");
        attackingEnemy.SetRetreat();

        yield return new WaitForSeconds(Random.Range(0, 1.5f));

        if (LivingEnemyCount() > 0)
        {
            Debug.Log("Restarting loop");
            AI_Loop_Coroutine = StartCoroutine(AI_Loop(attackingEnemy));
        }
    }

    //------------------------------METHODS--------------------------//
    // Picks random enemy from list
    public BasicEnemy RandomEnemy()
    {
        enemyIndexes = new List<int>();

        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].enemyAvailability)
                enemyIndexes.Add(i);
        }

        if (enemyIndexes.Count == 0)
            return null;

        BasicEnemy randomEnemy;
        int randomIndex = Random.Range(0, enemyIndexes.Count);
        randomEnemy = allEnemies[enemyIndexes[randomIndex]].basicEnemy;

        return randomEnemy;
    }

    // Picks random enemy from list while ignoring previously attacking enemy (same enemy wont attack twice)
    public BasicEnemy RandomEnemyExcludingOne(BasicEnemy exclude)
    {
        enemyIndexes = new List<int>();

        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].enemyAvailability && allEnemies[i].basicEnemy != exclude)
                enemyIndexes.Add(i);
        }

        if (enemyIndexes.Count == 0)
            return null;

        BasicEnemy randomEnemy;
        int randomIndex = Random.Range(0, enemyIndexes.Count);
        randomEnemy = allEnemies[enemyIndexes[randomIndex]].basicEnemy;

        return randomEnemy;
    }

    // To check if all enemies in a group are alive
    public int LivingEnemyCount()
    {
        int count = 0;
        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].basicEnemy != null)
                count++; 

        }
        livingEnemyCount = count;
        return count;
    }

    // To set whether enemies are available to prepare attack
    public void SetEnemyAvailiability (BasicEnemy enemy, bool state)
    {
        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].basicEnemy == enemy) 
                allEnemies[i].enemyAvailability = state;
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