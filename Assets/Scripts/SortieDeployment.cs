
using UnityEngine;
using System.Collections.Generic;

public class SortieDeployment : MonoBehaviour
{
    [Header("Battlefield")]
    [SerializeField] private Battlefield battlefield;

    [Header("Optional Sortie Units")]
    [SerializeField] private BattleUnit mazingerZ;
    [SerializeField] private BattleUnit greatMazinger;
    [SerializeField] private BattleUnit gundam;
    [Header("Sortie Settings")]
    [SerializeField, Range(1, 5)] private int maxSortieUnits = 5;

    [Header("Numbered Deployment Positions")]
    [SerializeField] private List<Vector2Int> deploymentPositions = new();

    private void Awake()
    {
        CheckSortieSelection();
        DeploySelectedUnits();
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
        return unit == mazingerZ ||
               unit == greatMazinger ||
               unit == gundam;
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
    private void DeploySelectedUnits()
    {
        if (!SortieData.HasCustomSelection)
        {
            Debug.Log("Using default mission deployment.");
            return;
        }

        if (battlefield == null ||
            deploymentPositions.Count < maxSortieUnits)
        {
            Debug.LogError("Sortie configuration is incomplete.");
            return;
        }
        int slotIndex = 0;

        if (SortieData.KojiSelected)
        {
            DeployUnit(mazingerZ, true, slotIndex);
            slotIndex++;
        }
        else
        {
            DeployUnit(mazingerZ, false, slotIndex);
        }

        if (SortieData.TetsuyaSelected)
        {
            DeployUnit(greatMazinger, true, slotIndex);
            slotIndex++;
        }
        else
        {
            DeployUnit(greatMazinger, false, slotIndex);
        }

        if (SortieData.AmuroSelected)
        {
            DeployUnit(gundam, true, slotIndex);
            slotIndex++;
        }
        else
        {
            DeployUnit(gundam, false, slotIndex);
        }
    }
}

