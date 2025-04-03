using Unity.Entities;
using UnityEngine;

public class ExplosionAuthoring : MonoBehaviour
{

}

public class ExplosionAuthoringBaker : Baker<ExplosionAuthoring>
{
    public override void Bake(ExplosionAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new ExplosionComponent { });
    }
}