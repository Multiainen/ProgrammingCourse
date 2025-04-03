using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring for terrain objects
public class MapAuthoring : MonoBehaviour
{

}

public class MapAuthoringBaker : Baker<MapAuthoring>
{
    public override void Bake(MapAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new MapComponent { });
        AddComponent(entity, new MapRefComponent { });
        AddComponent(entity, new TowerProjectilesToSpawn { });
        AddComponent(entity, new ExplosionsToSpawn { });
        AddComponent(entity, new TowerDataRef { });
        AddComponent(entity, new GoldEarned { });
    }
}