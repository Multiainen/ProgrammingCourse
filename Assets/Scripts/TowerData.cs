using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class TowerData 
{
    public readonly static float[] hp = new float[] { 100, 100, 100 };
    public readonly static float[] cooldown = new float[] { 3, .7f, 7 };
    public readonly static int[] projectile = new int[] { 0, 1, 2 };

    public readonly static float[] projectileMass = new float[] { .3f, .01f, 1.2f, 1 };
    public readonly static float[] projectileSharpness = new float[] { 2, 300, 1, 1 };
    public readonly static float[] projectileRadius = new float[] { .1f, .05f, 1, .5f };
    public readonly static int[] projectileBehaviour = new int[] { 0, 0, 0, 1 };
    // range x = min range, y = max range
    public readonly static float2[] range = new float2[] { new float2(3, 20), new float2(3, 15), new float2(3, 30), new float2(5000, 10000), new float2 (5000, 10000) }; 
}
