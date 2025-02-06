using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

// testing class for projectile spawn VFX
public class ProjectileTest : MonoBehaviour
{
    public float spawnChance; // chance to trigger spawn event
    public int spawnMinCount; // min amount to spawn if triggered
    public int spawnMaxCount; // max amount to spawn if triggered
    public VisualEffect vfx;

    private GraphicsBuffer projectileBuffer;

    void Start()
    {
        
    }

    void Update()
    {
        RandomProjectileSpawn();
    }

    // spawn projectiles randomly based on inspector parameters
    private void RandomProjectileSpawn()
    {
        if (UnityEngine.Random.value < spawnChance)
        {
            int spawnCount = UnityEngine.Random.Range(spawnMinCount, spawnMaxCount);

            // create graphics buffer to transfer data into VFX
            TransformData[] bufferData = new TransformData[spawnCount];
            projectileBuffer?.Release();
            projectileBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, spawnCount, Marshal.SizeOf(typeof(TransformData)));

            // assign buffer data, send to VFX and launch spawn event
            for (int i = 0; i < spawnCount; i++)
            {
                bufferData[i] = new TransformData(new Vector3(UnityEngine.Random.Range(-50, 50), 3, UnityEngine.Random.Range(-50, 50)), new Vector3(UnityEngine.Random.Range(-10, 10), 3, UnityEngine.Random.Range(-10, 10)));
            }
            projectileBuffer.SetData(bufferData);
            vfx.SetGraphicsBuffer("LaunchData", projectileBuffer);
            vfx.SendEvent("Launch");
        }
    }

    // VFX buffer struct with spawning info
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    struct TransformData
    {
        public Vector3 Position; // position to spawn in
        public Vector3 Direction; // directional velocity on spawn

        public TransformData(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }
    }
}
