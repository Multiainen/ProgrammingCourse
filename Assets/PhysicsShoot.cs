using Unity.Entities.UniversalDelegates;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PhysicsShoot : MonoBehaviour
{
    public ECSBridge ECSBridge;
    public float timer = 0;
    public Transform target;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 1)
        {
            ECSBridge.addProjectileList.Add(new ProjectileData(new float3(transform.position.x, transform.position.y+4f, transform.position.z), new float3(target.position.x, target.position.y, target.position.z), 0));
            timer = 0;
        }

    }

}
