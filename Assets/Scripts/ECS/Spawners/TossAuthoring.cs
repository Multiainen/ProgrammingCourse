using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring for projectile spawner entity
public class TossSpawnerAuthoring : MonoBehaviour
{
    public GameObject prefab;
}

public class TossSpawnerBaker : Baker<TossSpawnerAuthoring>
{
    public override void Bake(TossSpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new TossSpawner
        {
            prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
        });
    }
}
public struct TossSpawner : IComponentData
{
    public Entity prefab;
}
