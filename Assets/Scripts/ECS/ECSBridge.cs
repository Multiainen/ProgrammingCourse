using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.UIElements;
using FMOD;
using FMODUnity;
using UnityEngine.VFX;
using System.Runtime.InteropServices;

public class ECSBridge : MonoBehaviour
{
    public UIMgr ui;
    public VisualEffect explosionVFX;
    public RaycastHit mouseRC;
    public bool mouseOnpath;
    public bool gamePaused;

    public int[] enemySpawnCount = new int[0]; // total remaining enemy spawn count for this wave by enemy type
    private int[] enemySpawnLeft;
    private float waveTimer;
    public int curWave = 0;
    public float[] enemySpawnRate = new float[0]; // current spawn rate of enemies by type per frame
    public List<int2> addEnemyList = new List<int2>(); // queued enemies to spawn and their designated IDs
    public EnemyStats[] enemyType; // 0 = basic dwarf
    public Dictionary<int2, List<int2>> openNodes = new Dictionary<int2, List<int2>>(); // currently open next waypoint nodes for each waypoint
    public Dictionary<int2, int2[]> possibleNodes = new Dictionary<int2, int2[]>(); // all possible next waypoint nodes for each waypoint (even currently blocked ones)
    public Dictionary<int2, int2> fallbackNodes = new Dictionary<int2, int2>(); // fallback return waypoint node for each waypoint (can't be blocked)
    public HashSet<int2> endNodes; // destination waypoint nodes of routes
    public int2 ultimateNode; // abstract post-destination node, should be transform of goal object so it can be used for orientation
    public int2[] startNodes; // possible start nodes to spawn enemies at
    public int targetIsland;
    public int[] spawnPaths;

    public int mapChunksX, mapChunksY, mapChunkSize;

    public int3[] collisionFilters; // collision filters for each collision layer
    public List<ProjectileData> addProjectileList = new List<ProjectileData>(); // projectiles designated to be spawned
    public List<ProjectileData> addExplosiveProjectileList = new List<ProjectileData>(); // explosive projectiles designated to be spawned
    public List<ExplosionData> spawnExplosionList = new List<ExplosionData>(); // explosions designated to be spawned in VFX graph
    public List<TowerStats> TowerList = new List<TowerStats>(); // towers designated to be spawned
    public List<TowerStats> OtherBuildingsList = new List<TowerStats>(); // non-tower buildings designated to be spawned

    public RenderMeshArray renderMeshArray;
    public Mesh[] terrainMeshes;
    public Material terrainMat;
    public Material pathMat;
    private float timer = 0;
    private DwarfManager dwarfManager; // managed system to bridge into DOTS

    void Start()
    {
        targetIsland = 0;
        spawnPaths = new int[] { 0, 1, 2 };
        enemySpawnLeft = new int[enemySpawnCount.Length];
    }

    void Update()
    {
        if (Time.timeScale < .5f) gamePaused = true;
        else
        {
            gamePaused = false;
            if (enemySpawnLeft[0] < 1)
            {
                waveTimer -= Time.deltaTime;
                if (waveTimer < 0)
                {
                    waveTimer = 30;
                    for (int i = 0; i < enemySpawnCount.Length; i++)
                    {
                        enemySpawnCount[i] = enemySpawnCount[i] * 3 / 2;
                        if (enemySpawnCount[i] % 10 != 0) enemySpawnCount[i] -= enemySpawnCount[i] % 10;
                        enemySpawnLeft[i] = enemySpawnCount[i];
                        enemySpawnRate[i] *= 1.3f;
                        ui.UpdateDisplay(1, enemySpawnLeft[i]);
                    }

                    curWave++;
                    ui.UpdateDisplay(2, curWave);
                }
                else
                    ui.UpdateDisplay(2, "(" + (int)waveTimer + ") " + curWave);
            }
            // spawn enemies at set rate if any more enemies need to be spawned for this wave
            for (int i = 0; i < enemySpawnLeft.Length; i++)
            {
                if (enemySpawnLeft[i] > 0)
                {
                    // if spawn rate is at least 1 per frame, spawn that amount; else use spawn rate as percentile chance to spawn a single enemy
                    if (enemySpawnRate[i] >= 1)
                    {
                        for (int j = 0; j < enemySpawnRate[i]; j++)
                            addEnemyList.Add(new int2(i, ResMgr.GenID()));
                        enemySpawnLeft[i] -= (int)enemySpawnRate[i];
                    }
                    else if (UnityEngine.Random.value < enemySpawnRate[i])
                    {
                        addEnemyList.Add(new int2(i, ResMgr.GenID()));
                        enemySpawnLeft[i]--;
                    }
                    ui.UpdateDisplay(1, enemySpawnLeft[0]);
                }
            }
        }
    }

    private void LateUpdate()
    {
        SpawnExplosions();
    }

    public void StopTime()
    {
        Time.timeScale = 0;
    }

    public void AddProjectile(float3 loc, float3 force, int type, int behaviourType = 0)
    {
        addProjectileList.Add(new ProjectileData(loc, force, type, behaviourType));
    }

    private void SpawnExplosions()
    {
        if (spawnExplosionList.Count < 1) return;
        GraphicsBuffer buffer;

        ExplosionVFXData[] bufferData = new ExplosionVFXData[spawnExplosionList.Count];
        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, spawnExplosionList.Count, Marshal.SizeOf(typeof(ExplosionVFXData)));

        // assign buffer data, send to VFX and launch spawn event
        for (int i = 0; i < spawnExplosionList.Count; i++)
        {
            bufferData[i] = new ExplosionVFXData(spawnExplosionList[i].pos, 10);
        }
        buffer.SetData(bufferData);
        explosionVFX.SetGraphicsBuffer("SpawnData", buffer);
        explosionVFX.SendEvent("Launch");
        spawnExplosionList.Clear();
    }
}

// spawn values for projectiles
public struct ProjectileData
{
    public float3 loc; // spawn location
    public float3 force; // velocity to spawn with
    public int type; // type of projectile
    public int behaviourType; // behaviour of objective (0 = regular, 1 = explosive)

    public ProjectileData(float3 loc, float3 force, int type, int behaviourType)
    {
        this.loc = loc;
        this.force = force;
        this.type = type;
        this.behaviourType = behaviourType;

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

public struct TowerStats
{
    public float3 pos;
    public int type;

    public TowerStats(float3 pos, int type)
    {
        this.pos = pos;
        this.type = type;
    }
}

public struct AudioStats
{
    public float3 pos;
    public float time;

    public AudioStats(float3 pos, float time)
    {
        this.pos = pos;
        this.time = time;
    }
}
