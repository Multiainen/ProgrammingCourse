using UnityEngine;
using UnityEngine.VFX;

public class UpdateVFXCam : MonoBehaviour
{
    public VisualEffect vfx;
    bool launched = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        vfx.SendEvent("Launch");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
