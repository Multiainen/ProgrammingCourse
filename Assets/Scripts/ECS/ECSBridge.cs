using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ECSBridge : MonoBehaviour
{
    public int[] enemySpawnCount = new int[0]; // total remaining enemy spawn count for this wave by enemy type
    public float[] enemySpawnRate = new float[0]; // current spawn rate of enemies by type per frame
    public List<int2> addEnemyList = new List<int2>(); // queued enemies to spawn and their designated IDs
    public EnemyStats[] enemyType; // 0 = basic dwarf
    public Dictionary<int2, List<int2>> openNodes = new Dictionary<int2, List<int2>>(); // currently open next waypoint nodes for each waypoint
    public Dictionary<int2, int2[]> possibleNodes = new Dictionary<int2, int2[]>(); // all possible next waypoint nodes for each waypoint (even currently blocked ones)
    public Dictionary<int2, int2> fallbackNodes = new Dictionary<int2, int2>(); // fallback return waypoint node for each waypoint (can't be blocked)
    public HashSet<int2> endNodes; // destination waypoint nodes of routes
    public int2 ultimateNode; // abstract post-destination node, should be transform of goal object so it can be used for orientation
    public int2[] startNodes; // possible start nodes to spawn enemies at

    public int3[] collisionFilters; // collision filters for each collision layer
    public List<ProjectileData> addProjectileList = new List<ProjectileData>(); // projectiles designated to be spawned
    private float timer = 0;
    private DwarfManager dwarfManager; // managed system to bridge into DOTS

    void Start()
    {
        EntityManager _manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        ResMgr.entityManager = _manager;

        // assign this class reference to managed entity
        var finder = _manager.CreateEntityQuery(typeof(ManagedRoot));
        Entity root = finder.GetSingletonEntity();
        _manager.GetComponentData<ManagedRoot>(root).bridge = this;
    }

    void Update()
    {
        // spawn enemies at set rate if any more enemies need to be spawned for this wave
        for (int i = 0; i < enemySpawnCount.Length; i++)
        {
            if (enemySpawnCount[i] > 0)
            {
                // if spawn rate is at least 1 per frame, spawn that amount; else use spawn rate as percentile chance to spawn a single enemy
                if (enemySpawnRate[i] >= 1)
                {
                    for (int j = 0; j < enemySpawnRate[i]; j++)
                        addEnemyList.Add(new int2(i, ResMgr.GenID()));
                    enemySpawnCount[i] -= (int)enemySpawnRate[i];
                }
                else if (UnityEngine.Random.value < enemySpawnRate[i])
                {
                    addEnemyList.Add(new int2(i, ResMgr.GenID()));
                    enemySpawnCount[i]--;
                }
            }
        }

        ProjectileTest();

    }

    // test function to spawn projectiles in a fixed position and shoot them at a lane of enemies
    private void ProjectileTest()
    {
        timer += Time.deltaTime;
        if (timer > 1)
        {
            addProjectileList.Add(new ProjectileData(new float3(76, 5, 50), new float3(UnityEngine.Random.Range(-.6f, .6f), 0, 15), 0));
            timer = 0;
        }
    }
}

// spawn values for projectiles
public struct ProjectileData
{
    public float3 loc; // spawn location
    public float3 force; // velocity to spawn with
    public int type; // type of projectile

    public ProjectileData(float3 loc, float3 force, int type)
    {
        this.loc = loc;
        this.force = force;
     this.type = type;
    }
}

// stat values of each enemy type
[System.Serializable]
public struct EnemyStats
{
    public int hp;
    public float speed;

    public EnemyStats(int hp, float speed)
    {
        this.hp = hp;
        this.speed = speed;
    }
}
