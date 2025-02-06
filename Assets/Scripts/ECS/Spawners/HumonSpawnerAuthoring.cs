using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring for enemy spawner entity
public struct HumonSpawner : IComponentData
{
    public Entity prefab;
}
public class HumonSpawnerAuthoring : MonoBehaviour
{
    public GameObject prefab;
}

public class HumonSpawnerBaker : Baker<HumonSpawnerAuthoring>
{
    public override void Bake(HumonSpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new HumonSpawner
        {
            prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
        });
    }
}
