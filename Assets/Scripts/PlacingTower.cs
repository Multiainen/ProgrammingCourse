using UnityEngine;
using UnityEngine.InputSystem;

public class PlacingTower : MonoBehaviour
{
    public GameObject Tower;
    public ECSBridge bridge;
    Mouse mouse;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (mouse == null) mouse = Mouse.current;
        //if (Input.GetKeyDown(KeyCode.Mouse0))
        //{
        //    RaycastHit hit;
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //    if (Physics.Raycast(ray, out hit, int.MaxValue))
        //    {
        //        bridge.TowerList.Add(new TowerStats (hit.point, 0));
        //    }
        //}
        else if (mouse.rightButton.wasReleasedThisFrame)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, int.MaxValue))
            {
                bridge.TowerList.Add(new TowerStats(hit.point, 0));
            }
        }
    }
}
