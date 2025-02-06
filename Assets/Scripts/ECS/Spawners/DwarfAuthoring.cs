using GPUECSAnimationBaker.Engine.AnimatorSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// enemy authoring class
public class DwarfAuthoring : MonoBehaviour
{

}

public class DwarfAuthoringBaker : Baker<DwarfAuthoring>
{
    public override void Bake(DwarfAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new TagInitialize { });
        AddComponent(entity, new EnemyID { });
        AddComponent(entity, new DwarfTarget { });
        AddComponent(entity, new PhysTarget { });
        AddComponent(entity, new DwarfData { });
        AddComponent(entity, new TargetRot { });
    }
}