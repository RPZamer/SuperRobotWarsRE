using UnityEngine;

// WEEK 3: Calculate the basic battle results. Skills, tile bonuses and EXP are not included yet.
public static class BattleFormulas
{
    // WEEK 3: Look up how the defending mech size changes the chance to hit it.
    public static float SizeModifier(MechSize size) => size switch
    {
        MechSize.LL => 1.4f,
        MechSize.L => 1.2f,
        MechSize.S => 0.8f,
        MechSize.SS => 0.1f,
        _ => 1f
    };

    // WEEK 3: Combine the pilot and mech ratings for the terrain the unit is on.
    public static float UnitTerrainModifier(BattleUnit unit)
    {
        return TerrainRatings.CombinedModifier(
            unit.Pilot.TerrainRatings.Get(unit.Terrain),
            unit.Mech.TerrainRatings.Get(unit.Terrain));
    }

    // WEEK 3: Compare accuracy with dodge, then apply defender size and distance.
    public static int AccuracyRate(BattleUnit attacker, BattleUnit defender, Weapon weapon, int distance)
    {
        float accuracy = (attacker.Pilot.Accuracy / 2f + 140f) * UnitTerrainModifier(attacker)
            + weapon.AccuracyModifier;
        float evade = (defender.Pilot.Evade / 2f + defender.Mech.Mobility) * UnitTerrainModifier(defender);
        // WEEK 3: Keep decimal values until this final result. The hit roll later limits it to 0-100.
        return (int)((accuracy - evade) * SizeModifier(defender.Mech.Size) + (5 - distance) * 3f);
    }

    // WEEK 3: Compare pilot Skill stats and add the weapon critical bonus.
    public static int CriticalRate(BattleUnit attacker, BattleUnit defender, Weapon weapon)
    {
        return attacker.Pilot.Skill - defender.Pilot.Skill + weapon.CriticalModifier;
    }

    // WEEK 3 DMG CHECK: Keep the numeric breakdown tied to the exact values used by the damage formula.
    public static int Damage(BattleUnit attacker, BattleUnit defender, Weapon weapon, bool critical)
    {
        PilotBase attackPilot = attacker.Pilot;
        PilotBase defensePilot = defender.Pilot;
        int attackStat = weapon.DamageType == WeaponDamageType.Melee ? attackPilot.Melee : attackPilot.Ranged;
        TerrainRating weaponRating = weapon.TerrainRatings.Get(defender.Terrain);
        TerrainRating armorRating = defender.Mech.TerrainRatings.Get(defender.Terrain);
        float weaponTerrain = TerrainRatings.Modifier(weaponRating);
        float armorTerrain = TerrainRatings.Modifier(armorRating);

        float attackFactor = (attackStat + attackPilot.Morale) / 200f;
        float attackBeforeTerrain = attackFactor * weapon.Power;
        float attack = attackBeforeTerrain * weaponTerrain;

        float defenseFactor = (defensePilot.Defense + defensePilot.Morale) / 200f;
        float defenseBeforeTerrain = defenseFactor * defender.Mech.Armor;
        float defense = defenseBeforeTerrain * armorTerrain;

        float difference = attack - defense;
        float criticalMultiplier = critical ? 1.25f : 1f;
        float raw = difference * criticalMultiplier;
        int truncated = (int)raw;
        int damage = Mathf.Max(0, truncated);

        // WEEK 3 DMG CHECK: Print substituted equations and unrounded float values in one Console entry per damage calculation.
        BattleDebug.Log(
            $"[DMG CHECK] {attacker.name} -> {defender.name} | Weapon: {weapon.WeaponName}\n" +
            $"ATTACK INPUTS: {weapon.DamageType}={attackStat}, attacker Morale (Will)={attackPilot.Morale}, weapon Power={weapon.Power}\n" +
            $"WEAPON TERRAIN: defender terrain={defender.Terrain}, weapon rating={weaponRating}, multiplier={weaponTerrain:R}\n" +
            $"Attack factor = ({attackStat} + {attackPilot.Morale}) / 200 = {attackFactor:R}\n" +
            $"Attack before terrain = {attackFactor:R} * {weapon.Power} = {attackBeforeTerrain:R}\n" +
            $"ATTACK = {attackBeforeTerrain:R} * {weaponTerrain:R} = {attack:R}\n" +
            $"DEFENSE INPUTS: pilot Defense={defensePilot.Defense}, defender Morale (Will)={defensePilot.Morale}, mech Armor={defender.Mech.Armor}\n" +
            $"ARMOR TERRAIN: defender terrain={defender.Terrain}, mech rating={armorRating}, multiplier={armorTerrain:R}\n" +
            $"Defense factor = ({defensePilot.Defense} + {defensePilot.Morale}) / 200 = {defenseFactor:R}\n" +
            $"Defense before terrain = {defenseFactor:R} * {defender.Mech.Armor} = {defenseBeforeTerrain:R}\n" +
            $"DEFENSE = {defenseBeforeTerrain:R} * {armorTerrain:R} = {defense:R}\n" +
            $"Attack - Defense = {attack:R} - {defense:R} = {difference:R}\n" +
            "Defender terrain bonus: deferred (effective x1)\n" +
            "Skill/relationship/Ace final damage modifiers: deferred (effective x1)\n" +
            $"Critical={(critical ? 1 : 0)}, multiplier={criticalMultiplier:R}\n" +
            $"Raw damage = {difference:R} * {criticalMultiplier:R} = {raw:R}\n" +
            $"Truncate toward zero = {truncated}\n" +
            $"FINAL FORMULA DAMAGE = Max(0, {truncated}) = {damage}",
            attacker);
        return damage;
    }
}

// WEEK 3 DMG CHECK: Log only in the Editor or development builds when enabled.
public static class BattleDebug
{
    public static bool Enabled { get; set; } = true;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context = null)
    {
        if (Enabled) Debug.Log(message, context);
    }
}