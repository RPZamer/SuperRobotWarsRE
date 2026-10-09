using System;
using UnityEngine;

// WEEK 3: Choose whether this weapon uses the pilot Melee or Ranged stat.
public enum WeaponDamageType { Melee, Ranged }

// WEEK 5 FIXES: Originally, every weapon could initiate an attack after moving regardless of its tags.
// We needed PostMovement to control that permission so ordinary weapons must fire before movement.
// Keeping the existing flag values allows this fix to use the tags already saved on mech assets.
[Flags]
public enum WeaponType { None = 0, FirstStrike = 1, PostMovement = 2 }

// WEEK 3: Choose a single target, one extra adjacent enemy, or every unit in MAP range.
public enum WeaponClassification { SingleTarget, AdjacentTargets, Map }

// WEEK 5 FIXES: Originally, every MAP weapon used the same burst radius around its attacker.
// We needed separate shape settings to support rectangular columns and widening cones.
// Burst remains the default so existing MAP weapons retain their original area until a new shape is selected.
public enum MapWeaponShape { Burst, Column, Cone }

// WEEK 3: Store each weapon inside its mech asset and show its settings in the Inspector.
[Serializable]
public class Weapon
{
    // WEEK 3: Set the weapon name, damage type, attack timing and target area.
    [SerializeField] private string weaponName;
    [SerializeField] private WeaponDamageType damageType;
    // WEEK 3: Enable for attacks that travel to the enemy's terrain. Changes accuracy terrain only, not grid position.
    [SerializeField] private bool travelsToTargetTerrain;
    // WEEK 5 FIXES: Originally, this saved PostMovement flag did not affect attack availability.
    // The battle checks now read it so each weapon can explicitly allow firing after movement.
    // It can still be combined with FirstStrike without changing the existing asset format.
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private WeaponClassification classification;
    // WEEK 5 CHANGES PLEASE READ: Barriers cannot recognize beam/gravity attacks from the existing Melee/Ranged setting.
    // Independent attribute flags identify those attacks, and Required Mode identifies Hyper/Super-only moves.
    // Their defaults preserve existing weapons, while the effect script handles reductions and morale-based unlocking.
    [SerializeField] private WeaponAttribute attributes;
    [SerializeField] private MechMode requiredMode;
    public WeaponAttribute Attributes => attributes;
    public MechMode RequiredMode => requiredMode;
    // WEEK 5 CHANGES PLEASE READ: Weapons previously had mode restrictions but no individual minimum morale requirement.
    // Some attacks need their own threshold, which is checked against the unit's existing morale without spending it.
    // Zero leaves a weapon unrestricted by morale, while Required Mode remains a separate condition when configured.
    [Tooltip("Minimum unit morale needed to fire; 0 means no morale requirement. Morale is not spent.")]
    [Min(0)][SerializeField] private int requiredMorale;
    public int RequiredMorale => requiredMorale;
    // WEEK 5 FIXES: Originally, MAP weapons had no individual shape or ally-damage settings.
    // These fields let each weapon define a column's length/width or a cone's length/final width.
    // Hits Allies stays enabled by default to preserve existing friendly fire, and can be disabled per weapon.
    [Header("WEEK 5 FIXES: MAP weapons")]
    [SerializeField] private MapWeaponShape mapShape;
    [Min(1)][SerializeField] private int mapLength = 3;
    [Min(1)][SerializeField] private int mapWidth = 3;
    [SerializeField] private bool mapHitsAllies = true;
    // WEEK 3: Set the nearest and farthest grid distances this weapon can reach.
    [Min(1)][SerializeField] private int minRange = 1;
    [Min(1)][SerializeField] private int maxRange = 1;
    // WEEK 3: Set damage power, hit and critical bonuses, energy cost and terrain ratings.
    [Min(0)][SerializeField] private int power = 2;
    [SerializeField] private int accuracyModifier;
    [SerializeField] private int criticalModifier;
    [Min(0)][SerializeField] private int energyCost;
    // WEEK 4: MOTHERSHIP - Zero keeps existing weapons ammo-free; positive values limit shots.
    [Min(0)][SerializeField] private int maxAmmo;
    [SerializeField] private TerrainRatings terrainRatings = new();

    // WEEK 3: Let battle code read the weapon settings.
    public string WeaponName => weaponName;
    public WeaponDamageType DamageType => damageType;
    public bool TravelsToTargetTerrain => travelsToTargetTerrain;
    public WeaponType Type => weaponType;
    public WeaponClassification Classification => classification;
    public MapWeaponShape MapShape => mapShape;
    public bool MapHitsAllies => mapHitsAllies;
    public bool CanUseAfterMoving => (weaponType & WeaponType.PostMovement) != 0;
    public int MinRange => minRange;
    public int MaxRange => maxRange;
    public int Power => power;
    public int AccuracyModifier => accuracyModifier;
    public int CriticalModifier => criticalModifier;
    public int EnergyCost => energyCost;
    public int MaxAmmo => maxAmmo;
    public TerrainRatings TerrainRatings => terrainRatings;

    // WEEK 3: Check that a target is between the minimum and maximum range, including both ends.
    public bool IsInRange(int distance) => distance >= minRange && distance <= maxRange;

    // WEEK 5 FIXES: Originally, targeting checked only grid distance, which could not describe directional MAP shapes.
    // MAP targeting now uses the same area check as damage so the selected enemy must lie inside the chosen shape.
    // The clicked enemy chooses up/down/left/right by its larger coordinate difference, with vertical winning ties.
    public bool IsInTargetRange(Vector2Int origin, Vector2Int target) =>
        classification == WeaponClassification.Map
            ? IsInMapArea(origin, target, target)
            : IsInRange(Mathf.Abs(target.x - origin.x) + Mathf.Abs(target.y - origin.y));

    // WEEK 5 FIXES: Originally, MAP damage included every unit between the minimum and maximum grid distances.
    // This shared check preserves that burst area while adding columns and cones that start at MinRange.
    // Directional shapes extend for mapLength rows, allowing their dimensions to be configured without duplicating geometry in battle code.
    public bool IsInMapArea(Vector2Int origin, Vector2Int aim, Vector2Int cell)
    {
        Vector2Int offset = cell - origin;
        if (offset == Vector2Int.zero) return false;
        if (mapShape == MapWeaponShape.Burst)
            return IsInRange(Mathf.Abs(offset.x) + Mathf.Abs(offset.y));

        Vector2Int aiming = aim - origin;
        if (aiming == Vector2Int.zero) return false;
        Vector2Int forward = Mathf.Abs(aiming.x) > Mathf.Abs(aiming.y)
            ? new Vector2Int(aiming.x > 0 ? 1 : -1, 0)
            : new Vector2Int(0, aiming.y > 0 ? 1 : -1);
        int depth = offset.x * forward.x + offset.y * forward.y - Mathf.Max(1, minRange);
        int length = Mathf.Max(1, mapLength);
        if (depth < 0 || depth >= length) return false;
        int width = Mathf.Max(1, mapWidth);
        if (mapShape == MapWeaponShape.Cone)
            width = length == 1 ? width : 1 + (width - 1) * depth / (length - 1);
        int side = offset.x * forward.y - offset.y * forward.x;
        return side >= -(width - 1) / 2 && side <= width / 2;
    }
}