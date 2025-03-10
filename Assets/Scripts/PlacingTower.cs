using UnityEngine;

public class PlacingTower : MonoBehaviour
{
    public GameObject Tower;
    public ECSBridge bridge;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Mouse0))
        //{
        //    RaycastHit hit;
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //    if (Physics.Raycast(ray, out hit, int.MaxValue))
        //    {
        //        bridge.TowerList.Add(new TowerStats (hit.point, 0));
        //    }
        //}
        //else if (Input.GetKeyDown(KeyCode.Mouse1))
        //{
        //    RaycastHit hit;
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //    if (Physics.Raycast(ray, out hit, int.MaxValue))
        //    {
        //        bridge.TowerList.Add(new TowerStats(hit.point, 1));
        //    }
        //}
    }
}
