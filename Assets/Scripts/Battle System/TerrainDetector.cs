using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Tilemaps;




public class TerrainDetector : MonoBehaviour
{
    //Stores a battlefield grid cell's position and its terrain type.
    [Serializable] public class TerrainCell
    {
        public Vector2Int Position;
        public TerrainType Terrain = TerrainType.Ground;
    }
    

    //Tilemap containing the panted terrain data for this battlefield.
    [SerializeField] private Tilemap terrainTilemap;
    //Battlefield that this terrain detector is assigned to. Used to get the battlefield's grid size and other data.
    [SerializeField] private Battlefield battlefield;

    [Header("Battlefield Terrain")]
    //Any cell not specifically listed below will be treated as ground.
    [SerializeField] private TerrainType defaultTerrain = TerrainType.Ground;
    // Find the terrain assigned to a bttlefield grid position.
    [SerializeField] private List<TerrainCell> terrainCells = new();

    public TerrainType GetTerrain(Vector2Int position)
    {
        if (terrainTilemap != null && battlefield != null)
        {
            Vector3 worldPosition = battlefield.GridToWorld(position);

            Vector3Int tilePosition = terrainTilemap.WorldToCell(worldPosition);

            

            CustomTiles tile = terrainTilemap.GetTile<CustomTiles>(tilePosition);
            Debug.Log("[TERRAIN TEST] Battle Cell: " + position
   + " | World: " + worldPosition
   + " | Tilemap Cell: " + tilePosition
   + " | Tile Found: " + (tile != null ? tile.Tile.ToString() : "NONE"));
            if (tile != null)
            {
                return ConvertTileType(tile.Tile);
            }
           

        }

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

    public TileTypes GetTileType(Vector2Int position)
    {
        if (terrainTilemap != null && battlefield != null)
        {
            Vector3 worldPosition = battlefield.GridToWorld(position);
            Vector3Int tilePosition = terrainTilemap.WorldToCell(worldPosition);
            
            CustomTiles tile = terrainTilemap.GetTile<CustomTiles>(tilePosition);
            if (tile != null)
            {
                return tile.Tile;
            }
        }
        return TileTypes.Ground;
    }

    private TerrainType ConvertTileType(TileTypes tileType)
    {
        return tileType switch
        {

            //BONUSES HAVE NOT BEEN VERIFIED FOR ALL TERRAIN TYPES. Some terrain types may not have bonuses implemented yet, so they will default to ground.
            //Asteroid is not currently implemented, so it will default to ground.
            TileTypes.Ground => TerrainType.Ground,
            TileTypes.Water => TerrainType.Water,
            TileTypes.Forest => TerrainType.Ground,
            TileTypes.Mountain => TerrainType.Ground,
            TileTypes.Space => TerrainType.Space,
            //TileTypes.Asteroid => TerrainType.Asteroid,
            TileTypes.Colony => TerrainType.Ground,
            //TileTypes.Canyon => TerrainType.Canyon,
            _ => TerrainType.Ground
        };
    }

}
