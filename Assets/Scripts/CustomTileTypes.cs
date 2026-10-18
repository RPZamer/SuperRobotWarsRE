using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class CustomTiles : Tile
{
    public TileTypes Tile;
    // WEEK 7: Apply Terrain needs real tile defense/evasion benefits to double.
    // Unconfigured tiles retain zero benefits, preserving existing maps until their tile assets are set up.
    [Range(0, 100)] public int DefenseBonusPercent;
    [Range(0, 100)] public int EvasionBonusPercent;

    // Theres both ground and flying units, flyers can pass over land so only one bool needed.
    [SerializeField] public bool GroundUnitPassable = true;
    [SerializeField] public bool NotPassable = false;      // for out of bounds

}

public enum TileTypes
{
    // Land Tiles
    Ground,
    Water,
    Forest,
    Mountain,
    // Air/Space Tiles
    Space,
    Asteroid,
    Colony,
    Canyon,

    Impassable // for walls or canyons
}

