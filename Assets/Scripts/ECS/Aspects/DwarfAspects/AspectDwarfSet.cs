using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// aspect for an enemy that needs to be teleported to a certain position, as determined by the Set Pos tag
public readonly partial struct AspectDwarfSet : IAspect
{
    private readonly RefRW<DwarfTarget> target;
    private readonly RefRW<EnemyID> id;
    private readonly RefRO<TagSetPos> tagSet;

    public void SetPos(ref LocalTransform transformAspect, MapComponent map)
    {
        //float2 pos = mapRef.paths[mapRef.pathIndices[target.ValueRO.curPath] + 21];
        //transformAspect.Position = new float3(pos.x, 2.5f, pos.y);

        target.ValueRW.step = map.pathStartStep[target.ValueRO.curPath];
    }

    public void SetID(int id)
    {
        this.id.ValueRW.value = id;
    }
}
