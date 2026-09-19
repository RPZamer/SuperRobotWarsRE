using System;
using UnityEngine;

// WEEK 3: List the terrain types used by pilots, mechs and weapons.
public enum TerrainType { Air, Ground, Water, Space }
// WEEK 3: Give each letter its table value so pilot and mech ratings can be added.
public enum TerrainRating { D = 0, C = 1, B = 2, A = 3, S = 4 }

// WEEK 3: Store a separate rating for each terrain. New ratings start at A.
[Serializable]
public class TerrainRatings
{
    [SerializeField] private TerrainRating air = TerrainRating.A;
    [SerializeField] private TerrainRating ground = TerrainRating.A;
    [SerializeField] private TerrainRating water = TerrainRating.A;
    [SerializeField] private TerrainRating space = TerrainRating.A;

    // WEEK 3: Read the rating for the terrain being used.
    public TerrainRating Get(TerrainType terrain) => terrain switch
    {
        TerrainType.Air => air,
        TerrainType.Water => water,
        TerrainType.Space => space,
        _ => ground
    };

    // WEEK 3: Turn one terrain letter into its damage multiplier from the supplied table.
    public static float Modifier(TerrainRating rating) => rating switch
    {
        TerrainRating.S => 1.1f,
        TerrainRating.A => 1f,
        TerrainRating.B => 0.9f,
        TerrainRating.C => 0.8f,
        _ => 0.4f
    };

    // WEEK 3: Add pilot and mech ratings and look up their shared hit and dodge multiplier.
    public static float CombinedModifier(TerrainRating pilot, TerrainRating mech)
    {
        int total = (int)pilot + (int)mech;
        if (total >= 7) return 1.1f;
        if (total == 6) return 1f;
        if (total >= 4) return 0.9f;
        if (total >= 2) return 0.8f;
        return 0.4f;
    }
}