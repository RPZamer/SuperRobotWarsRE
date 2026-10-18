
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Analytics;

public class SortieDeployment : MonoBehaviour
{
    [Header("Battlefield")]
    [SerializeField] private Battlefield battlefield;

    [Header("Optional Sortie Units")]
    [SerializeField] private BattleUnit mazingerZ;
    [SerializeField] private BattleUnit greatMazinger;
    [SerializeField] private BattleUnit gundam;
    [SerializeField] private BattleUnit getter1;

    [Header("Available Sortie Units")]
    [SerializeField] private List<BattleUnit> availableUnits = new();

    [HeaderAttribute("Selected Sortie Units")]
    [SerializeField] private List<BattleUnit> selectedUnits = new();

    [Header ("Deployment Selection Buttons")]
    [SerializeField] private GameObject mazingerZButton;
    [SerializeField] private GameObject greatMazingerButton;
    [SerializeField] private GameObject gundamButton;
    [Header("Mission Deployment Restrictions")]
    [SerializeField] private bool greatMazingerIsReinforcement = false;

    private BattleUnit selectedUnit;
    public bool DeploymentConfirmed 
    { 
        get; private set; 
    }
    private bool deploymentInitialized = false;

    public void SelectUnit(BattleUnit unit)
    {
        if (DeploymentConfirmed || unit == null)
            return;

        if (greatMazingerIsReinforcement && unit == greatMazinger)
        {
            Debug.LogWarning("Great Mazinger is reserved for reinforcement.");
            return;
        }

        if (!unit.gameObject.activeInHierarchy)
        {
            Debug.LogWarning(unit.name + " is not available for deployment.");
            return;
        }

        ToggleSortieUnit(unit);
    }
    public void SelectMazingerZ()
    {
        SelectUnit(mazingerZ);
    }
    public void SelectGreatMazinger()
    {
        SelectUnit(greatMazinger);

    }
    public void SelectGundam()
    {
        SelectUnit(gundam);
    }
    public void SelectGetter1()
    {
        SelectUnit(availableUnits[3]);
    }
    public void SelectVenusA()
    {
        SelectUnit(availableUnits[4]);
    }

    public void SelectDianaA()
    {
        SelectUnit(availableUnits[5]);
    }

    public void SelectBossBorot()
    {
        SelectUnit(availableUnits[6]);
    }

    public void SelectGetterQ()
    {
        SelectUnit(availableUnits[7]);
    }
    public void ToggleSortieUnit(BattleUnit unit)
    {
        if (DeploymentConfirmed || unit == null)
            return;
        if (!availableUnits.Contains(unit))
            return;
        if (selectedUnits.Contains(unit))
        {
            selectedUnits.Remove(unit);
            Debug.Log(unit.name + " removed from sortie.");
            return;
        }
        if (selectedUnits.Count >= maxSortieUnits)
        {
            Debug.Log("Maximum sortie units selected.");
            return;
        }
        selectedUnits.Add(unit);
        Debug.Log(unit.name + " selected for sortie.");

    }

    public void PlaceSelectedUnit(Vector2Int gridPosition)
    {
        if (selectedUnit == null)
            return;
        if (!deploymentPositions.Contains(gridPosition))
            return;

        if (!battlefield.TryDeployUnit(selectedUnit, gridPosition))
            return;
    
        Debug.Log(selectedUnit.name + " moved to " + gridPosition);

        selectedUnit = null;

    }
    public void StartMission()
    {
        if (selectedUnits.Count == 0)
        {
            Debug.LogWarning("Select at least one mech before starting.");
            return;

        }
        if (selectedUnits.Count > maxSortieUnits)
        {
            Debug.LogWarning("Too many mechs selected for sortie.");
            return;
        }

        if (!deploymentInitialized)
        {
            Debug.LogWarning("Deployment is not ready.");
            return;
        }
        DeploymentConfirmed = true;

        Debug.Log("Deployment confirmed. Starting mission!");

        transform.Find("Deployment Canvas").gameObject.SetActive(false);

        foreach (Transform child in transform)
        {
            if (child.name == "Deployment Zone")
            {
                child.gameObject.SetActive(false);
            }
        }
    }
    private void Update()
    {
        if (!deploymentInitialized || DeploymentConfirmed)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;
        /*
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;
        */
        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector3 mousePosition =
            camera.ScreenToWorldPoint(Input.mousePosition);

        Vector2Int gridPosition =
            battlefield.WorldToGrid(mousePosition);

        if (!deploymentPositions.Contains(gridPosition))
            return;

        BattleUnit clickedUnit = battlefield.GetUnit(gridPosition);
        if (selectedUnit == null)
        {
            if (clickedUnit != null &&
                selectedUnits.Contains(clickedUnit))
            {
                selectedUnit = clickedUnit;
                Debug.Log("Selected: " + clickedUnit.name);
            }
        }
        else
        {
            MoveDeploymentUnit(gridPosition);
        }
    }

    [Header("Sortie Settings")]
    [SerializeField, Range(1, 8)] private int maxSortieUnits = 3;

    [Header("Numbered Deployment Positions")]
    [SerializeField] private List<Vector2Int> deploymentPositions = new();

    [Header("Deployment Tile Visual")]
    [SerializeField] private GameObject deploymentTilePrefab;

    private void Awake()
    {
        CheckSortieSelection();
        LoadSortieSelection();
        UpdateSortieVisibility();
        //DeploySelectedUnits();
        UpdateDeploymentButtons();
        ShowDeploymentTiles();
    }
    
    private System.Collections.IEnumerator Start()
    {
        yield return null;
        InitializeDeployment();
    }

   private void UpdateDeploymentButtons()
    {
        //if (!SortieData.HasCustomSelection)
           // return;

        if (mazingerZButton != null)
            mazingerZButton.SetActive(SortieData.KojiSelected);

        if (greatMazingerButton != null)
        {
            greatMazingerButton.SetActive(!greatMazingerIsReinforcement &&
            (!SortieData.HasCustomSelection || SortieData.TetsuyaSelected));
        }

        if (gundamButton != null)
            gundamButton.SetActive(SortieData.AmuroSelected);
    }
    private void ShowDeploymentTiles()
    {
        foreach(Vector2Int position in deploymentPositions)
        {
            GameObject tile = Instantiate(deploymentTilePrefab,
            battlefield.GridToWorld(position),
            Quaternion.identity,
            transform);

            tile.name = "Deployment Zone";
            Debug.Log("Deployment Zone " + position + " | World Position: " + tile.transform.position);
            tile.transform.localScale = Vector3.one * battlefield.CellSize;

        }
    }

    [ContextMenu("Validate Deployment Positions")]
    public void ValidateDeploymentPositions()
    {
        if (battlefield == null)
        {
            Debug.LogWarning("Sortie Deployment needs a Battlefield.");
            return;
        }

        if (deploymentPositions.Count < maxSortieUnits)
        {
            Debug.LogWarning("Not enough deployment positions.");
            return;
        }

        HashSet<Vector2Int> uniquePositions = new();

        BattleUnit[] sceneUnits = FindObjectsByType<BattleUnit>(FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < maxSortieUnits; i++)
        {
            Vector2Int position = deploymentPositions[i];

            if (!battlefield.IsInside(position))
            {
                Debug.LogWarning("Deployment Slot " + (i + 1) +
                    " is outside the battlefield.");
            }

            if (!uniquePositions.Add(position))
            {
                Debug.LogWarning("Deployment Slot " + (i + 1) +
                    " is a duplicate position.");
            }

            foreach (BattleUnit unit in sceneUnits)
            {
                if (IsOptionalUnit(unit))
                    continue;

                if (!unit.gameObject.activeInHierarchy)
                    continue;

                Vector2Int unitPosition =
                    battlefield.WorldToGrid(unit.transform.position);

                if (unitPosition == position)
                {
                    Debug.LogWarning("Deployment Slot " + (i + 1) +
                        " overlaps with " + unit.name);
                }
            }
        }
    }

    private bool IsOptionalUnit(BattleUnit unit)
    {
        return unit != null && availableUnits.Contains(unit);
    }
    public bool IsAvailableSortieUnit(BattleUnit unit)
    {
        return unit != null && availableUnits.Contains(unit);
    }
    private void LoadSortieSelection()
    {
        selectedUnits.Clear();

        if (!SortieData.HasCustomSelection)
        {
            Debug.LogWarning("No Intermission sortie selection found.");
            return;
        }

        if (availableUnits.Count < 8)
        {
            Debug.LogError("The sortie roster needs eight units.");
            return;
        }

        if (SortieData.KojiSelected)
            selectedUnits.Add(availableUnits[0]);

        if (SortieData.TetsuyaSelected)
            selectedUnits.Add(availableUnits[1]);

        if (SortieData.AmuroSelected)
            selectedUnits.Add(availableUnits[2]);

        if (SortieData.RyomaSelected)
            selectedUnits.Add(availableUnits[3]);

        if (SortieData.SayakaSelected)
            selectedUnits.Add(availableUnits[4]);

        if (SortieData.JunSelected)
            selectedUnits.Add(availableUnits[5]);

        if (SortieData.BossSelected)
            selectedUnits.Add(availableUnits[6]);

        if (SortieData.MichiruSelected)
            selectedUnits.Add(availableUnits[7]);

        Debug.Log("Loaded " + selectedUnits.Count +
                  " units from Intermission.");
    }
    private bool IsSelectedSortieUnit(BattleUnit unit)
    {
        return unit != null && selectedUnits.Contains(unit);

    }
    private void UpdateSortieVisibility()
    {
        foreach (BattleUnit unit in availableUnits)
        {
            if (unit == null)
                continue;
            SpriteRenderer sprite = unit.GetComponent<SpriteRenderer>();

            if (sprite != null)
            {
                sprite.enabled = IsSelectedSortieUnit(unit);
            }
        }
    }
    private void InitializeDeployment()
    {
        deploymentInitialized = false;

        if (!DeploySelectedUnits())
        {
            Debug.LogError("Initial deployment failed.");
            return;
        }

        UpdateSortieVisibility();

        deploymentInitialized = true;

        Debug.Log("Initial sortie deployment complete.");
    }

    private void CheckSortieSelection()
    {
        if (!SortieData.HasCustomSelection)
        {
            Debug.Log("No custom selection. Using default units.");
            return;
        }

        Debug.Log("Mazinger Z selected: " + SortieData.KojiSelected);
        Debug.Log("Great Mazinger selected: " + SortieData.TetsuyaSelected);
        Debug.Log("Gundam selected: " + SortieData.AmuroSelected);
    }

    private bool PositionSortieUnit(BattleUnit unit, int slotIndex)
    {
        if (unit == null || battlefield == null)
            return false;

        if (slotIndex < 0 || slotIndex >= deploymentPositions.Count)
        {
            Debug.LogWarning("Invalid sortie deployment slot.");
            return false;
        }

        Vector2Int gridPosition = deploymentPositions[slotIndex];

        if (!battlefield.IsInside(gridPosition))
        {
            Debug.LogWarning("Sortie position is outside battlefield.");
            return false;
        }

        for (int i = 0; i < slotIndex; i++)
        {
            if (deploymentPositions[i] == gridPosition)
            {
                Debug.LogWarning("Deployment Slot " + (slotIndex + 1) + " duplicates Slot " + (i + 1));
                return false;
            }
        }

        BattleUnit[] sceneUnits = FindObjectsByType<BattleUnit>(FindObjectsInactive.Include,FindObjectsSortMode.None);

        foreach (BattleUnit otherUnit in sceneUnits)
        {
            if (IsOptionalUnit(otherUnit))
                continue;

            if (!otherUnit.gameObject.activeInHierarchy)
                continue;

            Vector2Int otherPosition =
                battlefield.WorldToGrid(otherUnit.transform.position);

            if (otherPosition == gridPosition)
            {
                Debug.LogWarning("Cannot deploy " + unit.name +
                    ". Position occupied by " + otherUnit.name);
                return false;
            }
        }

        unit.transform.position = battlefield.GridToWorld(gridPosition);

        Debug.Log(unit.name + " assigned to sortie slot " +
            (slotIndex + 1));

        return true;
    }
    private void DeployUnit(BattleUnit unit, bool selected, int slotIndex)
    {
        if (unit == null)
        {
            Debug.LogWarning("Missing sortie unit reference.");
            return;
        }

        if (!selected)
        {
            unit.gameObject.SetActive(false);
            Debug.Log(unit.name + " was not selected for sortie.");
            return;
        }

        if (!PositionSortieUnit(unit, slotIndex))
        {
            Debug.LogError("Failed to deploy " + unit.name +
                ". Keeping its original mission position.");
        }
    }
    private bool DeploySelectedUnits()
    {
        if(selectedUnits.Count == 0 || selectedUnits.Count > maxSortieUnits)
        {
            Debug.LogError("Invalid number of sortie units.");
            return false;
        }
       if (battlefield == null|| deploymentPositions.Count < maxSortieUnits)
        {
            Debug.LogError("Sortie configureation is incomplete.");
            return false;
        }
        HashSet<Vector2Int> usedPositions = new();
        HashSet<BattleUnit> usedUnits = new();

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            BattleUnit unit = selectedUnits[i];
            Vector2Int position = deploymentPositions[i];
           

            if (unit == null || !availableUnits.Contains(unit))
            {
                Debug.LogError("Invalid sortie unit.");
                return false;
            }
            if (!usedUnits.Add(unit))
            {
                Debug.LogError("Duplicate sortie unit selected.");
                return false;
            }

            if (!battlefield.IsInside(position))
            {
                Debug.LogError("Deployment position is outside battlefield.");
                return false;
            }

            if (!usedPositions.Add(position))
            {
                Debug.LogError("Duplicate deployment position.");
                return false;
            }

            if (battlefield.GetUnit(position) != null)
            {
                Debug.LogError("Deployment position is occupied.");
                return false;
            }
        }
        int slotIndex = 0;

        foreach (BattleUnit unit in selectedUnits)
        {
            Vector2Int position = deploymentPositions[slotIndex];

            unit.PlaceOn(battlefield);
            if (!battlefield.TryDeployUnit(unit, position))
            {
                Debug.LogError("Failed to deploy " + unit.name);
                return false;
            }

            slotIndex++;
        }
        return true;
    }
    private void MoveDeploymentUnit(Vector2Int targetPosition)
    {
        if (selectedUnit == null ||
            !deploymentPositions.Contains(targetPosition))
            return;

        BattleUnit targetUnit = battlefield.GetUnit(targetPosition);
        if (targetUnit != null &&
            !selectedUnits.Contains(targetUnit))
            return;

        Vector2Int originalPosition = selectedUnit.GridPosition;

        if (targetPosition == originalPosition)
        {
            selectedUnit = null;
            return;
        }

        if (targetUnit == null)
        {
            if (battlefield.TryDeployUnit(selectedUnit, targetPosition))
            {
                Debug.Log(selectedUnit.name + " repositioned.");
            }
        }
        else
        {
           
            battlefield.Remove(selectedUnit);
            battlefield.Remove(targetUnit);

            bool firstPlaced =
                battlefield.TryDeployUnit(selectedUnit, targetPosition);

            bool secondPlaced = firstPlaced &&
                battlefield.TryDeployUnit(targetUnit, originalPosition);

            if (!firstPlaced || !secondPlaced)
            {
                battlefield.Remove(selectedUnit);
                battlefield.Remove(targetUnit);

                battlefield.TryDeployUnit(selectedUnit, originalPosition);
                battlefield.TryDeployUnit(targetUnit, targetPosition);

                Debug.LogError("Could not swap deployment positions.");
            }
        }

        selectedUnit = null;
    }
}

