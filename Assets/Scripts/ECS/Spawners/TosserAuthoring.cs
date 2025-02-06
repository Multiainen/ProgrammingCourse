using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring class for a new physical projectile
public class TosserAuthoring : MonoBehaviour
{

}

public class TosserAuthoringBaker : Baker<TosserAuthoring>
{
    public override void Bake(TosserAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new TagInitialize { });
        AddComponent(entity, new TagProjectile { despawnTimer = 5 });
        AddComponent(entity, new PhysSpawn { });
    }
}