using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

// generic static management operations
public static class ResMgr
{
    private static int nextID = 1;
    public static readonly int mapHeight = 1000;
    public static readonly int mapWidth = 1000;
    public static float generalSeed;

    public static int[] resources = new int[] { 100, 0, 1, 400, 80, 25 };
    public static int[] resourceTrickle = new int[] { 0, 0, 0, 0, 0, 0 };
    public static int[][] towerCost = new int[][]
    {
        new int[] { 20, 5, 0 },
        new int[] { 30, 20, 0 },
        new int[] { 150, 25, 25 },
        new int[] { 100, 0, 0 },
        new int[] { 100, 0, 0 },
    };
    public static int curWave = 0;
    //public static NativeList<Entity> reTagList = new NativeList<Entity>();
    //public static EntityManager entityManager;

    public static float2[][] resDepots; // resource deposit locations on map, per resource
    public static bool spawnResources;

    public static List<float2>[][] towerLocations;

    public static readonly string[] soundBank = new string[]
    {
        "Explosion",
        "Catapult_Fire",
        "Rock_Break",
        "Water_Splash",
        "Catapult_Fire_2",
        "Catapult_Fire_3",
    };
    public static List<AudioStats>[] soundsPlaying;

    // return unique ID value
    public static int GenID()
    {
        return nextID++;
    }
}
