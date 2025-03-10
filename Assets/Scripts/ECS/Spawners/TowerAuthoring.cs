using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring class for a new physical projectile
public class TowerAuthoring : MonoBehaviour
{

}

public class TowerAuthoringBaker : Baker<TowerAuthoring>
{
    public override void Bake(TowerAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new TagInitialize { });
        AddComponent(entity, new TowerComponent { });
        AddComponent(entity, new TowerID { });
        AddComponent(entity, new PositionComponent { });
    }
}