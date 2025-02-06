using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

// alternate way to convert prefabs into entities by assigning their required components and values manually
public class Entitize : MonoBehaviour
{
    public Entity Init(RenderMeshArray r, BatchMeshID meshIndex, BatchMaterialID matIndex)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        Entity entity = entityManager.CreateEntity();
        var desc = new RenderMeshDescription(
            shadowCastingMode: ShadowCastingMode.On,
            receiveShadows: true,
            renderingLayerMask: 1
        );
        var renderMeshArray = r;
        RenderMeshUtility.AddComponents(
            entity, entityManager,
            desc,
            renderMeshArray,
            new MaterialMeshInfo
            {
                MaterialID = matIndex,
                MeshID = meshIndex
            }
        );
        entityManager.SetComponentData(entity, new LocalToWorld
        {
            Value = transform.localToWorldMatrix
        });
        return entity;
    }
}
