using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

// component for getting ECS bridge class reference
public class ManagedRoot : IComponentData
{
    public GameObject prefab;
    public ECSBridge bridge;
}