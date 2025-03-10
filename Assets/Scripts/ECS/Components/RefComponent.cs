using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// for getting reference to ECS bridge
public class RefComponent : MonoBehaviour
{
    public ECSBridge bridge;

    void Awake()
    {
        bridge = GameObject.Find("Root").GetComponent<ECSBridge>();
    }
}