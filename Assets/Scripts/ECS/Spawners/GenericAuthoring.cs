using Unity.Entities;
using UnityEngine;

public class GenericAuthoring : MonoBehaviour
{

}

public class GenericBaker : Baker<GenericAuthoring>
{
    public override void Bake(GenericAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new TagInitialize { });
        AddComponent(entity, new TagGeneric { });
    }
}
