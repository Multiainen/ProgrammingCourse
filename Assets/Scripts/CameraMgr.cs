using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering;

public class CameraMgr : MonoBehaviour
{
    public ECSBridge bridge;
    public UIMgr ui;
    public Camera cam;
    public Transform camFocus;
    public Transform reticule;

    public float overheadSensitivity = .5f;
    public float personalSensitivity = .3f;
    public float panSensitivity = .2f;
    public float zoomSpeed = 1;
    public float maxZoom = 120;
    public float minZoom = 5;
    public int cameraMode = 0; // 0: standard overhead, 1: top down, 2: 1st person
    Mouse mouse;
    Keyboard kb;
    public Vector2 camFocusRotation = new Vector2(70, 0);
    public Vector2 reticuleRotation = Vector2.zero;
    public Vector3 camPos;
    public Vector3 fpsTarget;
    public Vector3 rotTarget;
    bool rotateTowardsTarget;
    Vector3 camRotation = Vector3.zero;
    float camPosMomentum;
    public int approachCamPos;
    float curZoom = 30;
    float pressTime;
    float shotCooldown;
    float recoilRecoverSpeed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camPos = camFocus.transform.position;
    }

    private void FixedUpdate()
    {
        if (approachCamPos == 1) // gradual camera approach towards target
        {
            float distance = (fpsTarget - cam.transform.localPosition).magnitude;
            if (camPosMomentum > distance * 3)
            {
                camPosMomentum = distance * 3;
                if (distance < 3)
                    camPosMomentum += (3 - distance) * 15 * Time.fixedDeltaTime;
            }
            else
            {
                camPosMomentum += distance * Time.fixedDeltaTime;
                camPosMomentum *= 1 - (Time.fixedDeltaTime * .25f);
            }
            if (rotateTowardsTarget)
            {
                camRotation += (rotTarget - camRotation) * camPosMomentum / distance * Time.fixedDeltaTime;
                cam.transform.localRotation = Quaternion.Euler(camRotation);
            }
            cam.transform.localPosition += (fpsTarget - cam.transform.localPosition).normalized * camPosMomentum * Time.fixedDeltaTime;
            if (distance < .02f)
            {
                approachCamPos = 0;
                cam.transform.localPosition = fpsTarget;
                if (rotateTowardsTarget)
                    cam.transform.localRotation = Quaternion.Euler(rotTarget);
            }
        }
    }

    private void Update()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (mouse == null || kb == null)
        {
            mouse = Mouse.current;
            kb = Keyboard.current;
        }
        else
        {
            switch (cameraMode)
            {
                case 0: // overhead
                    //if (mouse.middleButton.isPressed)
                    //{
                    //    camFocusRotation += new Vector2(mouse.delta.value.y, mouse.delta.value.x) * overheadSensitivity;
                    //    if (camFocusRotation.x < 25) camFocusRotation = new Vector2(25, camFocusRotation.y);
                    //    else if (camFocusRotation.x > 80) camFocusRotation = new Vector2(80, camFocusRotation.y);
                    //    camFocus.rotation = Quaternion.Euler(camFocusRotation);
                    //}
                    if (mouse.rightButton.isPressed)
                    {
                        if (kb.ctrlKey.isPressed)
                        {
                            camFocusRotation += new Vector2(0, mouse.delta.value.x) * overheadSensitivity;
                            camFocus.rotation = Quaternion.Euler(camFocusRotation);
                        }
                        else
                            camFocus.transform.position += new Vector3(mouse.delta.value.x, 0, mouse.delta.value.y) * panSensitivity;
                    }
                    curZoom -= mouse.scroll.value.y;
                    if (curZoom > maxZoom) curZoom = maxZoom;
                    else if (curZoom < minZoom) curZoom = minZoom;
                    cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, cam.transform.localPosition.y, -curZoom);
                    break;
                case 1: // top down
                    if (mouse.rightButton.isPressed)
                        camFocus.transform.position += new Vector3(mouse.delta.value.x, 0, mouse.delta.value.y) * panSensitivity;
                    curZoom -= mouse.scroll.value.y;
                    if (curZoom > maxZoom) curZoom = maxZoom;
                    else if (curZoom < minZoom) curZoom = minZoom;
                    cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, cam.transform.localPosition.y, -curZoom);
                    break;
                case 2: // first person
                        reticuleRotation += new Vector2(-mouse.delta.value.y, mouse.delta.value.x) * personalSensitivity;
                        if (reticuleRotation.x < -90) reticuleRotation = new Vector2(-90, reticuleRotation.y);
                        else if (reticuleRotation.x > 90) reticuleRotation = new Vector2(90, reticuleRotation.y);
                        reticule.rotation = Quaternion.Euler(reticuleRotation);
                    if (approachCamPos < 1)
                    {
                        shotCooldown -= Time.deltaTime;
                        if (shotCooldown < 0)
                        {
                            if (mouse.leftButton.wasPressedThisFrame)
                                pressTime = Time.time;
                            else if (mouse.leftButton.isPressed && pressTime > 1)
                                cam.transform.localPosition -= new Vector3(0, 0, Time.deltaTime * .5f);
                            else if (mouse.leftButton.wasReleasedThisFrame)
                            {
                                bridge.AddProjectile(reticule.position + reticule.rotation * new float3(0, .6f, 0), reticule.forward * (10 + 10 * (Time.time - pressTime)), 3, 1);
                                shotCooldown = 1;
                                pressTime = 0;
                                recoilRecoverSpeed = -cam.transform.localPosition.z - .7f;
                                FMODUnity.RuntimeManager.PlayOneShot("event:/Catapult_Fire");
                            }
                            else
                                cam.transform.localPosition = new Vector3(0, 2.1f, -.7f);
                        }
                        else
                        {
                            cam.transform.localPosition += new Vector3(0, 0, Time.deltaTime * recoilRecoverSpeed);
                        }
                    }
                    break;
                default:
                    break;
            }
            if (kb.zKey.wasReleasedThisFrame)
                SwitchCamMode(0);
            if (kb.xKey.wasReleasedThisFrame)
                SwitchCamMode(1);
            if (kb.cKey.wasReleasedThisFrame)
                SwitchCamMode(2);
            if (kb.digit1Key.wasReleasedThisFrame)
                ui.SelectTower(0);
            if (kb.digit2Key.wasReleasedThisFrame)
                ui.SelectTower(1);
            if (kb.digit3Key.wasReleasedThisFrame)
                ui.SelectTower(2);
            if (kb.digit4Key.wasReleasedThisFrame)
                ui.SelectTower(3);
            if (kb.digit5Key.wasReleasedThisFrame)
                ui.SelectTower(4);
            if (kb.tKey.wasReleasedThisFrame)
                ui.SelectTower(-1);
            if (kb.escapeKey.wasReleasedThisFrame)
                ui.ToggleTutorial();
        }
    }

    public void SwitchCamMode(int mode)
    {
        if (cameraMode == mode) return;
        if (cameraMode == 2 || mode == 2) approachCamPos = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        cam.transform.SetParent(camFocus);
        rotTarget = Vector3.zero;
        switch (mode)
        {
            case 0: // overhead
                camFocusRotation = new Vector2(60, camFocusRotation.y);
                camFocus.rotation = Quaternion.Euler(camFocusRotation);
                if (cameraMode == 2)
                {
                    fpsTarget = new Vector3(0, 0, -curZoom);
                    camRotation = cam.transform.localRotation.eulerAngles;
                    rotateTowardsTarget = true;
                }
                else
                    cam.transform.localPosition = new Vector3(0, 0, -curZoom);
                break;
            case 1: // top down
                camFocusRotation = new Vector2(90, 0);
                camFocus.rotation = Quaternion.Euler(camFocusRotation);
                if (cameraMode == 2)
                {
                    fpsTarget = new Vector3(0, 0, -curZoom);
                    camRotation = cam.transform.localRotation.eulerAngles;
                    rotateTowardsTarget = true;
                }
                else
                    cam.transform.localPosition = new Vector3(0, 0, -curZoom);
                break;
            case 2: // first person
                reticuleRotation = camFocusRotation;
                reticule.rotation = Quaternion.Euler(reticuleRotation);
                cam.transform.SetParent(reticule);
                rotateTowardsTarget = false;
                fpsTarget = new Vector3(0, 2.1f, -.7f);
                ui.SelectTower(-1);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            default:
                break;
        }
        cameraMode = mode;
    }
}
