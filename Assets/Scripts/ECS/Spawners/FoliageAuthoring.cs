using Unity.Entities;
using UnityEngine;

public class FoliageAuthoring : MonoBehaviour
{

}

public class FoliageAuthoringBaker : Baker<FoliageAuthoring>
{
    public override void Bake(FoliageAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new TagInitialize { });
        AddComponent(entity, new TagFoliage { });
    }
}
