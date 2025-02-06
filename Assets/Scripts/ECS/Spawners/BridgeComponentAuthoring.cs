using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

// authoring for DOTS object bridging over managed and unmanaged code
public struct BridgeComponent : IComponentData
{

}
public class BridgeComponentAuthoring : MonoBehaviour
{
    public GameObject prefab;
}

public class BridgeComponentBaker : Baker<BridgeComponentAuthoring>
{
    public override void Bake(BridgeComponentAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new BridgeComponent { });



        ManagedRoot managedRoot = new ManagedRoot();
        managedRoot.prefab = authoring.prefab;
        managedRoot.bridge = GameObject.Find("Root").GetComponent<ECSBridge>();
        AddComponentObject(entity, managedRoot);
    }
}
