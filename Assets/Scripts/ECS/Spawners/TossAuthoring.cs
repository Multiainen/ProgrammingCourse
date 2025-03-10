using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring for projectile spawner entity
[InternalBufferCapacity(2)]
public struct TossBufferElement : IBufferElementData
{
    public Entity ItemEntity;
}
public struct TossSpawner : IComponentData { }
public class TossAuthoring : MonoBehaviour
{
    public GameObject[] prefabs;
}

public class TossSpawnerBaker : Baker<TossAuthoring>
{
    public override void Bake(TossAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        var buffer = AddBuffer<TossBufferElement>(entity);
        for (int i = 0; i < authoring.prefabs.Length; i++)
        {
            buffer.Add(new TossBufferElement
            {
                ItemEntity = GetEntity(authoring.prefabs[i], TransformUsageFlags.Dynamic)
            });
        }
        AddComponent<TossSpawner>(entity);
    }
}

