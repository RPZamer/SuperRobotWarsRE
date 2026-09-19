using System;
using UnityEngine;

// WEEK 3: Choose whether this weapon uses the pilot Melee or Ranged stat.
public enum WeaponDamageType { Melee, Ranged }

// WEEK 3: FirstStrike attacks first when defending. All weapons allow attacks after moving.
[Flags]
public enum WeaponType { None = 0, FirstStrike = 1, PostMovement = 2 }

// WEEK 3: Choose a single target, one extra adjacent enemy, or every unit in MAP range.
public enum WeaponClassification { SingleTarget, AdjacentTargets, Map }

// WEEK 3: Store each weapon inside its mech asset and show its settings in the Inspector.
[Serializable]
public class Weapon
{
    // WEEK 3: Set the weapon name, damage type, attack timing and target area.
    [SerializeField] private string weaponName;
    [SerializeField] private WeaponDamageType damageType;
    // WEEK 3: PostMovement is kept for existing assets, but is no longer required to attack after moving.
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private WeaponClassification classification;
    // WEEK 3: Set the nearest and farthest grid distances this weapon can reach.
    [Min(1)][SerializeField] private int minRange = 1;
    [Min(1)][SerializeField] private int maxRange = 1;
    // WEEK 3: Set damage power, hit and critical bonuses, energy cost and terrain ratings.
    [Min(0)][SerializeField] private int power = 2;
    [SerializeField] private int accuracyModifier;
    [SerializeField] private int criticalModifier;
    [Min(0)][SerializeField] private int energyCost;
    [SerializeField] private TerrainRatings terrainRatings = new();

    // WEEK 3: Let battle code read the weapon settings.
    public string WeaponName => weaponName;
    public WeaponDamageType DamageType => damageType;
    public WeaponType Type => weaponType;
    public WeaponClassification Classification => classification;
    public int MinRange => minRange;
    public int MaxRange => maxRange;
    public int Power => power;
    public int AccuracyModifier => accuracyModifier;
    public int CriticalModifier => criticalModifier;
    public int EnergyCost => energyCost;
    public TerrainRatings TerrainRatings => terrainRatings;

    // WEEK 3: Check that a target is between the minimum and maximum range, including both ends.
    public bool IsInRange(int distance) => distance >= minRange && distance <= maxRange;
}