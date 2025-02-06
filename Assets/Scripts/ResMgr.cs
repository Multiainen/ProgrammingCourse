using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

// generic static management operations
public static class ResMgr
{
    private static int nextID = 1;
    //public static NativeList<Entity> reTagList = new NativeList<Entity>();
    //public static EntityManager entityManager;

    // return unique ID value
    public static int GenID()
    {
        return nextID++;
    }
}
