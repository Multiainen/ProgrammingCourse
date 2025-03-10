using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring for enemy spawner entity
[InternalBufferCapacity(2)]
public struct HumonBufferElement : IBufferElementData
{
    public Entity ItemEntity;
}
public struct HumonSpawner : IComponentData { }
public class HumonSpawnerAuthoring : MonoBehaviour
{
    public GameObject[] prefabs;
}

public class HumonSpawnerBaker : Baker<HumonSpawnerAuthoring>
{
    public override void Bake(HumonSpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        var buffer = AddBuffer<HumonBufferElement>(entity);
        for (int i = 0; i < authoring.prefabs.Length; i++)
        {
            buffer.Add(new HumonBufferElement
            {
                ItemEntity = GetEntity(authoring.prefabs[i], TransformUsageFlags.Dynamic)
            });
        }
        AddComponent<HumonSpawner>(entity);
    }
}
