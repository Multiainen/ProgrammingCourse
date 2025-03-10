using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.LowLevelPhysics;
using UnityEngine.Rendering;

// alternate way to convert prefabs into entities by assigning their required components and values manually
public static class Entitize
{
    public static Entity Init(RenderMeshArray r, int meshIndex, int matIndex, Transform transform, ShadowCastingMode shadows = ShadowCastingMode.On)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        Entity entity = entityManager.CreateEntity();
        var desc = new RenderMeshDescription(
            shadowCastingMode: shadows,
            receiveShadows: true,
            renderingLayerMask: 1
        );
        RenderMeshUtility.AddComponents(
            entity, entityManager,
            desc,
            r,
            MaterialMeshInfo.FromRenderMeshArrayIndices(matIndex, meshIndex)
        );
        entityManager.SetComponentData(entity, new LocalToWorld
        {
            Value = transform.localToWorldMatrix
        });
        return entity;
    }

    public static Entity InitCollider(RenderMeshArray r, int meshIndex, int matIndex, Transform transform,
        BlobAssetReference<Unity.Physics.Collider> collider, int objectType = 0, ColliderType colliderType = ColliderType.Terrain, ShadowCastingMode shadows = ShadowCastingMode.On)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        Entity entity = entityManager.CreateEntity();
        var desc = new RenderMeshDescription(
            shadowCastingMode: shadows,
            receiveShadows: true,
            renderingLayerMask: 1
        );
        RenderMeshUtility.AddComponents(
            entity, entityManager,
            desc,
            r,
            MaterialMeshInfo.FromRenderMeshArrayIndices(matIndex, meshIndex)
        );
        entityManager.SetComponentData(entity, new LocalToWorld
        {
            Value = transform.localToWorldMatrix
        });

        entityManager.AddComponent<PhysicsWorldIndex>(entity);
        entityManager.AddComponent<PhysicsCollider>(entity);
        entityManager.SetComponentData(entity, new PhysicsCollider
        {
            Value = collider
        });
        entityManager.AddSharedComponentManaged(entity, new PhysicsWorldIndex
        {
            Value = 0
        });
        return entity;
    }

    public static Entity InitColliderOnly(Transform transform,
    BlobAssetReference<Unity.Physics.Collider> collider, int objectType = 0, ColliderType colliderType = ColliderType.Terrain, ShadowCastingMode shadows = ShadowCastingMode.On)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        Entity entity = entityManager.CreateEntity();
        entityManager.AddComponent<LocalToWorld>(entity);
        entityManager.SetComponentData(entity, new LocalToWorld
        {
            Value = transform.localToWorldMatrix
        });

        entityManager.AddComponent<PhysicsWorldIndex>(entity);
        entityManager.AddComponent<PhysicsCollider>(entity);
        entityManager.SetComponentData(entity, new PhysicsCollider
        {
            Value = collider
        });
        entityManager.AddSharedComponentManaged(entity, new PhysicsWorldIndex
        {
            Value = 0
        });
        return entity;
    }
}
