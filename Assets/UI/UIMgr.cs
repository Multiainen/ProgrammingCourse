using System.Collections.Generic;
using TMPro;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.VFX;

public class UIMgr : MonoBehaviour
{
    public ECSBridge bridge;
    public GameObject inGameCanvas;
    public GameObject mainMenu;
    public Image towerBar;
    public Image towerInfoBar;
    public Button[] towerButtons;
    public TextMeshProUGUI towerInfoText;
    public TextMeshProUGUI[] resourceAmt;

    public GameObject gameOverScreen;
    public GameObject tutorial;

    public int selectedTower;
    public Transform towerRangeIndicator;
    public VisualEffect towerRangeVFX;
    public MeshRenderer selectedTowerPreview;
    public MeshFilter selectedTowerMesh;

    public EventSystem eventSystem;
    public GraphicRaycaster raycaster;

    private Mouse mouse;
    private bool placeable;

    public Material previewGood;
    public Material previewBad;
    public Mesh[] previewMeshes;

    private readonly string[] towerInfoDescs = new string[]
    {
        "Basic tower tossing small rocks<br><br>20 I 5 W",
        "Sturdy tower shooting lots of arrows<br><br>30 I 20 W",
        "Catapult tower launching boulders<br><br>150 I 25 W 25 S",
        "Gathers wood from nearby thickets<br><br>100 I",
        "Gathers stone from nearby deposits<br><br>100 I",
    };
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        ResMgr.generalSeed = UnityEngine.Random.Range(0f, 1000f);
        ResMgr.towerLocations = new List<float2>[ResMgr.mapWidth / 10][];
        for (int i = 0; i < ResMgr.towerLocations.Length; i++)
        {
            ResMgr.towerLocations[i] = new List<float2>[ResMgr.mapHeight / 10];
            for (int j = 0; j < ResMgr.towerLocations[i].Length; j++)
                ResMgr.towerLocations[i][j] = new List<float2>();
        }
        ResMgr.soundsPlaying = new List<AudioStats>[ResMgr.soundBank.Length];
        for (int i = 0; i < ResMgr.soundsPlaying.Length; i++)
            ResMgr.soundsPlaying[i] = new List<AudioStats>();
    }
    void Start()
    {
        for (int i = 0; i < resourceAmt.Length; i++)
            resourceAmt[i].text = "" + ResMgr.resources[i];
    }

    // Update is called once per frame
    void Update()
    {
        if (mouse == null) mouse = Mouse.current;
        else
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, int.MaxValue) && !IsPointerOverUI(Input.mousePosition))
            {
                towerRangeIndicator.position = new Vector3(hit.point.x, 0, hit.point.z);
                placeable = TowerPlaceable(hit.point);
                if (placeable)
                    selectedTowerPreview.material = previewGood;
                else
                    selectedTowerPreview.material = previewBad;
                bridge.mouseRC = hit;
                if (selectedTower >= 0)
                {
                    if (mouse.leftButton.wasReleasedThisFrame && placeable)
                        PlaceTower(hit.point);
                }
            }
        }
        if ((int)Time.time / 5 != (int)(Time.time - Time.deltaTime) / 5)
            for (int i = 0; i < ResMgr.resources.Length; i++)
            {
                ResMgr.resources[i] += ResMgr.resourceTrickle[i];
                if (ResMgr.resourceTrickle[i] != 0)
                    UpdateDisplay(i, ResMgr.resources[i]);
            }
    }

    public void SelectTower(int index)
    {
        if (index > towerInfoDescs.Length)
        {
            return;
        }
        selectedTower = index;
        if (index < 0)
        {
            towerInfoText.text = "";
            towerRangeIndicator.gameObject.SetActive(false);
            return;
        }
        towerInfoText.text = towerInfoDescs[index];
        towerRangeIndicator.gameObject.SetActive(true);
        selectedTowerMesh.mesh = previewMeshes[index];
        towerRangeVFX.SetFloat("MinRange", TowerData.range[index].x);
        towerRangeVFX.SetFloat("MaxRange", TowerData.range[index].y);
        towerRangeVFX.Reinit();
    }

    private bool TowerPlaceable(Vector3 point)
    {
        if (selectedTower < 0 || selectedTower > ResMgr.towerCost.Length || bridge.mouseOnpath || (point - new Vector3(500, 0, 500)).sqrMagnitude > 20000) return false;
        for (int i = 0; i < ResMgr.towerCost[selectedTower].Length; i++)
            if (ResMgr.resources[3 + i] < ResMgr.towerCost[selectedTower][i]) return false;
        int2 towerMapIndex = new int2((int)point.x / 10, (int)point.z / 10);
        for (int i = 0; i < ResMgr.towerLocations[towerMapIndex.x][towerMapIndex.y].Count; i++)
            if ((ResMgr.towerLocations[towerMapIndex.x][towerMapIndex.y][i].x - point.x) * (ResMgr.towerLocations[towerMapIndex.x][towerMapIndex.y][i].x - point.x) < 3.9f
                && (ResMgr.towerLocations[towerMapIndex.x][towerMapIndex.y][i].y - point.z) * (ResMgr.towerLocations[towerMapIndex.x][towerMapIndex.y][i].y - point.z) < 3.9f)
                return false;
        float2 pointValue = new float2(point.x, point.z);
        switch (selectedTower)
        {
            case 3:
                for (int i = 0; i < ResMgr.resDepots[0].Length; i++)
                    if (math.lengthsq(pointValue - ResMgr.resDepots[0][i]) > 100 && math.lengthsq(pointValue - ResMgr.resDepots[0][i]) < 200)
                        return true;
                return false;
            case 4:
                for (int i = 0; i < ResMgr.resDepots[1].Length; i++)
                    if (math.lengthsq(pointValue - ResMgr.resDepots[1][i]) > 100 && math.lengthsq(pointValue - ResMgr.resDepots[1][i]) < 200)
                        return true;
                return false;
            default:
                return true;
        }
    }

    public void PlaceTower(Vector3 point)
    {
        int2 towerMapIndex = new int2((int)point.x / 10, (int)point.z / 10);
        float2 towerMapValue = new float2(point.x, point.z);
        for (int i = 0; i < ResMgr.towerCost[selectedTower].Length; i++)
        {
            ResMgr.resources[3 + i] -= ResMgr.towerCost[selectedTower][i];
            UpdateDisplay(3 + i, ResMgr.resources[3 + i]);
        }
        switch (selectedTower)
        {
            case 3:
                ResMgr.resourceTrickle[4]++;
                bridge.OtherBuildingsList.Add(new TowerStats(new float3(point.x, 0, point.z), selectedTower));
                break;
            case 4:
                ResMgr.resourceTrickle[5]++;
                bridge.OtherBuildingsList.Add(new TowerStats(new float3(point.x, 0, point.z), selectedTower));
                break;
            default:
                bridge.TowerList.Add(new TowerStats(new float3(point.x, 0, point.z), selectedTower));
                break;
        }
        ResMgr.towerLocations[towerMapIndex.x][towerMapIndex.y].Add(towerMapValue);
        if (towerMapValue.x % 10 < 2)
            ResMgr.towerLocations[towerMapIndex.x - 1][towerMapIndex.y].Add(towerMapValue);
        else if (towerMapValue.x % 10 > 8)
            ResMgr.towerLocations[towerMapIndex.x + 1][towerMapIndex.y].Add(towerMapValue);
        if (towerMapValue.y % 10 < 2)
            ResMgr.towerLocations[towerMapIndex.x][towerMapIndex.y - 1].Add(towerMapValue);
        else if (towerMapValue.y % 10 > 8)
            ResMgr.towerLocations[towerMapIndex.x][towerMapIndex.y + 1].Add(towerMapValue);
    }

    bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        return results.Count > 0;
    }

    public void UpdateDisplay(int index, int amount)
    {
        resourceAmt[index].text = "" + amount;
    }

    public void UpdateDisplay(int index, string display)
    {
        resourceAmt[index].text = display;
    }

    public void ToggleTutorial()
    {
        if (mainMenu.activeSelf) return;
        if (tutorial.activeSelf)
            Time.timeScale = 1;
        else Time.timeScale = 0;
        tutorial.SetActive(!tutorial.activeSelf);
    }

    public void GameOver()
    {
        gameOverScreen.SetActive(true);
        Time.timeScale = 0;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        mainMenu.SetActive(false);
    }
}
