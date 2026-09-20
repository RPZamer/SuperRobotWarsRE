using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    // this gets its own script so this dictionary can be attached to other units without overlap from the Movement script.
    Dictionary<Vector3Int, GameObject> OccupiedCells = new Dictionary<Vector3Int, GameObject>();    // stores the xyz of the cell and the unit its on

    public static GridManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public bool IsOccupied(Vector3Int Cell)
    {
        return OccupiedCells.ContainsKey(Cell);
    }

    // this method is for when a unit goes to a cell so its to update that cell in the dictionary
    public void OccupyCell(Vector3Int Cell, GameObject Unit)
    {
        OccupiedCells[Cell] = Unit;
    }

    // this is just the delete method to remove it from the dictionary.
    public void FreeCell(Vector3Int Cell)
    {
        OccupiedCells.Remove(Cell);
    }
}
