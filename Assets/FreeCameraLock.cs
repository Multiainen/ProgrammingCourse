using UnityEngine;
using UnityEngine.VFX;

public class FreeCameraLock : MonoBehaviour
{
    public VisualEffect effect;
    public Vector3 target;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
