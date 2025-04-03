using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring for enemy spawner entity
[InternalBufferCapacity(5)]
public struct FoliageBufferElement : IBufferElementData
{
    public Entity ItemEntity;
}
public struct FoliageSpawner : IComponentData { }
public class FoliageSpawnerAuthoring : MonoBehaviour
{
    public GameObject[] prefabs;
}

public class FoliageSpawnerBaker : Baker<FoliageSpawnerAuthoring>
{
    public override void Bake(FoliageSpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        var buffer = AddBuffer<FoliageBufferElement>(entity);
        for (int i = 0; i < authoring.prefabs.Length; i++)
        {
            buffer.Add(new FoliageBufferElement
            {
                ItemEntity = GetEntity(authoring.prefabs[i], TransformUsageFlags.Dynamic)
            });
        }
        AddComponent<FoliageSpawner>(entity);
    }
}
