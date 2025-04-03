using Unity.Entities;
using UnityEngine;

public readonly partial struct AspectAddGold : IAspect
{
    private readonly RefRW<GoldEarned> gold;

    public int AddGold()
    {
        int ret = gold.ValueRO.value;
        gold.ValueRW.value = 0;
        return ret;
    }
}
