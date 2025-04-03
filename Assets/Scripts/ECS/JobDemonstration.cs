using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;


public class JobDemonstration : MonoBehaviour
{
    Keyboard kb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (kb == null) kb = Keyboard.current;
        else
        {
            if (kb.kKey.wasReleasedThisFrame)
            {

            }
            if (kb.lKey.wasReleasedThisFrame)
            {
                NativeArray<float> results = new NativeArray<float>(20000000, Allocator.TempJob);
                int resultsLength = results.Length;
                int sectionLength = 10000;
                var timer = System.Diagnostics.Stopwatch.StartNew();

                for (int i = 0; i < resultsLength; i++)
                    results[i] = math.sqrt(i) * math.sin(i) * math.tan(i) * math.cos(i);

                timer.Stop();
                Debug.Log("Regular: " + timer.ElapsedMilliseconds);
                timer.Restart();
                JobHandle jobHandle = new CountStuffJob
                {
                    results = results,
                    resultsLength = resultsLength
                }.Schedule();
                jobHandle.Complete();
                timer.Stop();
                Debug.Log("Job: " + timer.ElapsedMilliseconds);
                timer.Restart();
                jobHandle = new CountStuffBurstJob
                {
                    results = results,
                    resultsLength = resultsLength
                }.Schedule();
                jobHandle.Complete();
                timer.Stop();
                Debug.Log("Burst Job: " + timer.ElapsedMilliseconds);
                timer.Restart();
                jobHandle = new CountStuffParallelJob
                {
                    results = results,
                    resultsLength = resultsLength,
                    sectionLength = sectionLength
                }.Schedule(resultsLength / sectionLength, 32);
                jobHandle.Complete();
                timer.Stop();
                Debug.Log("Parallel Job: " + timer.ElapsedMilliseconds);
                timer.Restart();
                jobHandle = new CountStuffParallelBurstJob
                {
                    results = results,
                    resultsLength = resultsLength,
                    sectionLength = sectionLength
                }.Schedule(resultsLength / sectionLength, 32);
                jobHandle.Complete();
                timer.Stop();
                Debug.Log("Parallel Burst Job: " + timer.ElapsedMilliseconds);

                results.Dispose();
            }
        }
    }
}

public partial struct CountStuffJob : IJob
{
    public NativeArray<float> results;
    public int resultsLength;
    public void Execute()
    {
        for (int i = 0; i < resultsLength; i++)
            results[i] = math.sqrt(i) * math.sin(i) * math.tan(i) * math.cos(i);
    }
}

[BurstCompile]
public partial struct CountStuffBurstJob : IJob
{
    public NativeArray<float> results;
    public int resultsLength;
    public void Execute()
    {
        for (int i = 0; i < resultsLength; i++)
            results[i] = math.sqrt(i) * math.sin(i) * math.tan(i) * math.cos(i);
    }
}

public partial struct CountStuffParallelJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction] public NativeArray<float> results;
    public int resultsLength;
    public int sectionLength;
    public void Execute(int index)
    {
        for (int i = sectionLength * index; i < (sectionLength + 1) * index; i++)
            results[i] = math.sqrt(i) * math.sin(i) * math.tan(i) * math.cos(i);
    }
}

[BurstCompile]
public partial struct CountStuffParallelBurstJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction] public NativeArray<float> results;
    public int resultsLength;
    public int sectionLength;
    public void Execute(int index)
    {
        for (int i = sectionLength * index; i < (sectionLength + 1) * index; i++)
            results[i] = math.sqrt(i) * math.sin(i) * math.tan(i) * math.cos(i);
    }
}
