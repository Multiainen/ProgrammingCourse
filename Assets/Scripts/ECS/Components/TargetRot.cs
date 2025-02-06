using Unity.Entities;
using UnityEngine;

// enemy rotation component
public struct TargetRot : IComponentData
{
    public int target, cur;
}
