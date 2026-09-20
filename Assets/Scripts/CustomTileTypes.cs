using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class CustomTiles : Tile
{
    public TileTypes Tile;

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

    Impassible // for walls or canyons
}
