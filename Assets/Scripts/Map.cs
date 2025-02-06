using JetBrains.Annotations;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// map/level management
public class Map : MonoBehaviour
{
    public Level[] levels; // all levels in the game
    public int curLevel; // index of current level
    ECSBridge bridge;

    void Start()
    {
        bridge = GetComponent<ECSBridge>();
        LoadLevel(0);
    }

    void Update()
    {
        
    }

    // set up requested level from level data
    public void LoadLevel(int level)
    {
        curLevel = level;
        bridge.endNodes = new HashSet<int2>();
        for (int i = 0; i < levels[level].endNodes.Length; i++)
            bridge.endNodes.Add(levels[level].endNodes[i]);
        bridge.startNodes = levels[level].startNodes;
        bridge.fallbackNodes = new Dictionary<int2, int2>();
        bridge.openNodes = new Dictionary<int2, List<int2>>();
        bridge.possibleNodes = new Dictionary<int2, int2[]>();
        List<int2> open;
        for (int i = 0; i < levels[level].possibleNodesKeys.Length; i++)
        {
            open = new List<int2>();
            bridge.fallbackNodes.Add(levels[level].possibleNodesKeys[i], levels[level].fallbackNodes[i]);
            bridge.possibleNodes.Add(levels[level].possibleNodesKeys[i], levels[level].possibleNodes[i].array);
            for (int j = 0; j < levels[level].possibleNodes[i].array.Length; j++)
                open.Add(levels[level].possibleNodes[i].array[j]);
            bridge.openNodes.Add(levels[level].possibleNodesKeys[i], open);
        }
        bridge.enemySpawnCount = new int[levels[level].enemySpawnCounts.Length];
        bridge.enemySpawnRate = new float[levels[level].enemySpawnRates.Length];
        for (int i = 0; i < bridge.enemySpawnCount.Length; i++)
        {
            bridge.enemySpawnCount[i] = levels[level].enemySpawnCounts[i];
            bridge.enemySpawnRate[i] = levels[level].enemySpawnRates[i];
        }
    }
}

// level data
[System.Serializable]
public class Level
{
    public int2[] possibleNodesKeys; // waypoint nodes which also act as keys for possible/fallback nodes
    public Int2Array[] possibleNodes; // possible nodes from each node
    public int2[] fallbackNodes; // fallback node from each node
    public int2[] endNodes; // final destination nodes
    public int2[] startNodes; // possible start (spawning) nodes
    public int[] enemySpawnCounts; // enemy spawn counts by type
    public float[] enemySpawnRates; // enemy spawn rates per frame by type
}

// helper class to make jagged int2 arrays accessible from inspector
[System.Serializable]
public class Int2Array
{
    public int2[] array;
}
