using UnityEngine;
using System.Collections.Generic;
using System;



public class TerrainDetector : MonoBehaviour
{
    //Stores a battlefield grid cell's position and its terrain type.
    [Serializable] public class TerrainCell
    {
        public Vector2Int Position;
        public TerrainType Terrain = TerrainType.Ground;
    }

    [Header("Battlefield Terrain")]
    //Any cell not specifically listed below will be treated as ground.
    [SerializeField] private TerrainType defaultTerrain = TerrainType.Ground;
    // Find the terrain assigned to a bttlefield grid position.
    [SerializeField] private List<TerrainCell> terrainCells = new();

    public TerrainType GetTerrain(Vector2Int position)
    {
        foreach (TerrainCell cell in terrainCells)
        {
            //If this is the requested cell, return its assigned terrain type.
            if (cell.Position == position)
            {
                return cell.Terrain;
            }
        }
        // Use the default terrain when the cell has no special terrain assigned.
        return defaultTerrain;
    }


}
