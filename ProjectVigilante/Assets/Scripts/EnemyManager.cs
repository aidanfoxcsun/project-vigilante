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

        // Stop any existing loop before starting a new one
        if (AI_Loop_Coroutine != null)
            StopCoroutine(AI_Loop_Coroutine);

        AI_Loop_Coroutine = StartCoroutine(AI_Loop(null));
    }

    //-----------------------------------MAIN LOGIC LOOP--------------------------//
    IEnumerator AI_Loop(BasicEnemy lastAttacker)
    {
        Debug.Log("Loop started");

        if (LivingEnemyCount() == 0)
        {
            Debug.Log("No living enemies. Stopping AI loop.");
            yield break; // Just yield break — don't call StopCoroutine(AI_Loop(null)), that starts a NEW coroutine
        }

        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        // Try to pick someone other than the last attacker, fall back to any enemy
        BasicEnemy attackingEnemy = RandomEnemyExcludingOne(lastAttacker) ?? RandomEnemy();

        if (attackingEnemy == null)
        {
            Debug.Log("No available enemy found. Retrying loop.");
            AI_Loop_Coroutine = StartCoroutine(AI_Loop(null));
            yield break;
        }

        // Wait until the chosen enemy has finished retreating, with a dead-enemy guard
        yield return new WaitUntil(() =>
            attackingEnemy == null ||
            !attackingEnemy.IsRetreating());

        // Enemy may have died while we were waiting
        if (attackingEnemy == null || !IsEnemyAlive(attackingEnemy))
        {
            Debug.Log("Chosen enemy died while waiting to attack. Restarting loop.");
            AI_Loop_Coroutine = StartCoroutine(AI_Loop(null));
            yield break;
        }

        Debug.Log($"{attackingEnemy.name} assigned to attack!");
        attackingEnemy.SetAttack();

        // Wait until the attack wind-up finishes, with a dead-enemy guard
        yield return new WaitUntil(() =>
            attackingEnemy == null ||
            !attackingEnemy.IsPreparingAttack());

        if (attackingEnemy == null || !IsEnemyAlive(attackingEnemy))
        {
            Debug.Log("Attacking enemy died mid-attack. Restarting loop.");
            AI_Loop_Coroutine = StartCoroutine(AI_Loop(null));
            yield break;
        }

        Debug.Log($"{attackingEnemy.name} assigned to retreat!");
        attackingEnemy.SetRetreat();

        yield return new WaitForSeconds(Random.Range(0f, 1.5f));

        if (LivingEnemyCount() > 0)
        {
            Debug.Log("Restarting loop.");
            AI_Loop_Coroutine = StartCoroutine(AI_Loop(attackingEnemy));
        }
    }

    //------------------------------METHODS--------------------------//

    // Checks whether a specific enemy is still alive in allEnemies
    public bool IsEnemyAlive(BasicEnemy enemy)
    {
        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].basicEnemy == enemy)
                return true;
        }
        return false;
    }

    // Picks a random available enemy
    public BasicEnemy RandomEnemy()
    {
        enemyIndexes = new List<int>();

        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].enemyAvailability)
                enemyIndexes.Add(i);
        }

        if (enemyIndexes.Count == 0) return null;

        int randomIndex = Random.Range(0, enemyIndexes.Count);
        return allEnemies[enemyIndexes[randomIndex]].basicEnemy;
    }

    // Picks a random available enemy excluding the last attacker
    public BasicEnemy RandomEnemyExcludingOne(BasicEnemy exclude)
    {
        enemyIndexes = new List<int>();

        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].enemyAvailability && allEnemies[i].basicEnemy != exclude)
                enemyIndexes.Add(i);
        }

        if (enemyIndexes.Count == 0) return null;

        int randomIndex = Random.Range(0, enemyIndexes.Count);
        return allEnemies[enemyIndexes[randomIndex]].basicEnemy;
    }

    // Returns living enemy count and updates the public field
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

    // Called by BasicEnemy.Death() to remove it from the array slot
    public void NotifyEnemyDied(BasicEnemy enemy)
    {
        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].basicEnemy == enemy)
            {
                allEnemies[i].basicEnemy = null;
                allEnemies[i].enemyAvailability = false;
                return;
            }
        }
    }

    public void SetEnemyAvailiability(BasicEnemy enemy, bool state)
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