using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class FloorAuthoring : MonoBehaviour
{

}

public class FloorAuthoringBaker : Baker<FloorAuthoring>
{
    public override void Bake(FloorAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new TagFloor { });
    }
}