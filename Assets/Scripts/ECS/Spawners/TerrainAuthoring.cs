using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// authoring for terrain objects
public class TerrainAuthoring : MonoBehaviour
{

}

public class TerrainAuthoringBaker : Baker<TerrainAuthoring>
{
    public override void Bake(TerrainAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new TagTerrain { });
    }
}