using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// aspect of an enemy currently in simulated motion, as determined by the Enemy Motion component
public readonly partial struct AspectDwarfMotion : IAspect
{
    private readonly RefRW<DwarfData> dwarfData;
    private readonly RefRO<DwarfTarget> target;
    private readonly RefRO<EnemyID> id;
    private readonly RefRW<ManualMotion> motion;

    public float3 GetMotion(){ return motion.ValueRO.value; }
    public bool GetGrounded() { return motion.ValueRO.grounded;}
    public float GetHP() { return dwarfData.ValueRO.health; }
    public int2 GetPrevTarget() { return target.ValueRO.prevKey; }
    public int2 GetTarget() { return target.ValueRO.key; }

    public void AdjustMotion(float3 m) { motion.ValueRW.value += m; }
    public void SetMotion(float3 m) { motion.ValueRW.value = m; }
    public void MultiplyMotion(float m) { motion.ValueRW.value *= m; }
    public void SetGrounded(bool m) { motion.ValueRW.grounded = m;}
    public void AdjustHP(float m) { dwarfData.ValueRW.health += m; }
    public void SetHP(float m) { dwarfData.ValueRW.health = m; }
}
