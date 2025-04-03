using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShootOnClick : MonoBehaviour
{
    Mouse mouse;
    ECSBridge bridge;
    float pressTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bridge = GameObject.Find("Root").GetComponent<ECSBridge>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mouse == null) mouse = Mouse.current;
        else
        {
            //if (mouse.leftButton.wasPressedThisFrame)
            //    pressTime = Time.time;
            //else if (mouse.leftButton.wasReleasedThisFrame)
            //{
            //    RaycastHit hit;
            //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //    if (Physics.Raycast(ray, out hit, int.MaxValue))
            //    {
            //        Vector3 dir = new float3(ray.direction.x, 0, ray.direction.z);
            //        bridge.AddProjectile(new float3(Camera.main.transform.position.x, 10, Camera.main.transform.position.z), dir * (25 + 20 * (Time.time - pressTime)), 0);
            //    }
            //}
        }
    }
}
