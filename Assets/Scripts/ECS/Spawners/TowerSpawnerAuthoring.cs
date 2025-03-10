using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring for enemy spawner entity
[InternalBufferCapacity(2)]
public struct TowerBufferElement : IBufferElementData
{
    public Entity ItemEntity;
}
public struct TowerSpawner : IComponentData { }
public class TowerSpawnerAuthoring : MonoBehaviour
{
    public GameObject[] prefabs;
}

public class TowerSpawnerBaker : Baker<TowerSpawnerAuthoring>
{
    public override void Bake(TowerSpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        var buffer = AddBuffer<TowerBufferElement>(entity);
        for (int i = 0; i < authoring.prefabs.Length; i++)
        {
            buffer.Add(new TowerBufferElement
            {
                ItemEntity = GetEntity(authoring.prefabs[i], TransformUsageFlags.Dynamic)
            });
        }
        AddComponent<TowerSpawner>(entity);
    }
}
