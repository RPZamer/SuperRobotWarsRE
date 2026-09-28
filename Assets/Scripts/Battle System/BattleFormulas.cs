using UnityEngine;

// WEEK 4: PILOT SKILLS - Existing battle formulas plus Potential. Tile bonuses and EXP remain deferred.
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
    public static float UnitTerrainModifier(BattleUnit unit) => UnitTerrainModifier(unit, unit.Terrain);

    // WEEK 3: Attacks that travel use the attacker's pilot and mech ratings for the target terrain.
    public static float UnitTerrainModifier(BattleUnit unit, TerrainType terrain)
    {
        TerrainRating pilot = unit.Pilot.TerrainRatings.Get(terrain);
        TerrainRating mech = unit.Mech.TerrainRatings.Get(terrain);
        float modifier = TerrainRatings.CombinedModifier(pilot, mech);
        return modifier;
    }

    // WEEK 3: Compare accuracy with dodge, then apply defender size and distance.
    public static int AccuracyRate(BattleUnit attacker, BattleUnit defender, Weapon weapon, int distance)
    {
        TerrainType attackTerrain = weapon.TravelsToTargetTerrain ? defender.Terrain : attacker.Terrain;
        float accuracy = (attacker.Pilot.Accuracy / 2f + 140f) * UnitTerrainModifier(attacker, attackTerrain)
            + weapon.AccuracyModifier;
        float evade = (defender.Pilot.Evade / 2f + defender.Mech.Mobility) * UnitTerrainModifier(defender);
        // WEEK 3: Keep decimal values until this final result. The hit roll later limits it to 0-100.
        float size = SizeModifier(defender.Mech.Size);
        float distanceBonus = (5 - distance) * 3f;
        // WEEK 4: MOTHERSHIP - Commander gives hit/evasion percentage points while in range.
        // WEEK 4: PILOT SKILLS - Add final percentage points after terrain/size; the caller clamps once to 0-100.
        float raw = (accuracy - evade) * size + distanceBonus + attacker.CommanderBonus - defender.CommanderBonus
            + PilotSkillEffects.HitDodgeBonus(attacker.PotentialStage)
            - PilotSkillEffects.HitDodgeBonus(defender.PotentialStage);
        return (int)raw;
    }

    // WEEK 3: Compare pilot Skill stats and add the weapon critical bonus.
    public static int CriticalRate(BattleUnit attacker, BattleUnit defender, Weapon weapon)
    {
        int rate = attacker.Pilot.Skill - defender.Pilot.Skill + weapon.CriticalModifier
            + PilotSkillEffects.CriticalBonus(attacker.PotentialStage);
        return rate;
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

        // WEEK 4 MORALE SYSTEM: Use each unit's live battle morale.
        float attackFactor = (attackStat + attacker.CurrentMorale) / 200f;
        float attackBeforeTerrain = attackFactor * weapon.Power;
        float attack = attackBeforeTerrain * weaponTerrain;

        float defenseFactor = (defensePilot.Defense + defender.CurrentMorale) / 200f;
        float defenseBeforeTerrain = defenseFactor * defender.Mech.Armor;
        float defense = defenseBeforeTerrain * armorTerrain;

        float difference = attack - defense;
        float criticalMultiplier = critical ? 1.25f : 1f;
        // WEEK 4: PILOT SKILLS - Use defender HP BEFORE this hit. Apply reduction once, before final truncation.
        int reduction = PilotSkillEffects.DamageReductionPercent(defender.PotentialStage);
        float raw = difference * criticalMultiplier;
        if (reduction > 0) raw = raw * (100 - reduction) / 100f;
        int truncated = (int)raw;
        int damage = Mathf.Max(0, truncated);

        // WEEK 3 DMG CHECK: Print substituted equations and unrounded float values in one Console entry per damage calculation.
        BattleDebug.Log(
            $"[DMG CHECK] {attacker.name} -> {defender.name} | Weapon: {weapon.WeaponName}\n" +
            $"ATTACK INPUTS: {weapon.DamageType}={attackStat}, attacker Morale (Will)={attacker.CurrentMorale}, weapon Power={weapon.Power}\n" +
            $"WEAPON TERRAIN: defender terrain={defender.Terrain}, weapon rating={weaponRating}, multiplier={weaponTerrain:R}\n" +
            $"Attack factor = ({attackStat} + {attacker.CurrentMorale}) / 200 = {attackFactor:R}\n" +
            $"Attack before terrain = {attackFactor:R} * {weapon.Power} = {attackBeforeTerrain:R}\n" +
            $"ATTACK = {attackBeforeTerrain:R} * {weaponTerrain:R} = {attack:R}\n" +
            $"DEFENSE INPUTS: pilot Defense={defensePilot.Defense}, defender Morale (Will)={defender.CurrentMorale}, mech Armor={defender.Mech.Armor}\n" +
            $"ARMOR TERRAIN: defender terrain={defender.Terrain}, mech rating={armorRating}, multiplier={armorTerrain:R}\n" +
            $"Defense factor = ({defensePilot.Defense} + {defender.CurrentMorale}) / 200 = {defenseFactor:R}\n" +
            $"Defense before terrain = {defenseFactor:R} * {defender.Mech.Armor} = {defenseBeforeTerrain:R}\n" +
            $"DEFENSE = {defenseBeforeTerrain:R} * {armorTerrain:R} = {defense:R}\n" +
            $"Attack - Defense = {attack:R} - {defense:R} = {difference:R}\n" +
            "Defender terrain bonus: deferred (effective x1)\n" +
            $"Potential damage reduction: {reduction}%\n" +
            "Relationship/Ace final damage modifiers: deferred (effective x1)\n" +
            $"Critical={(critical ? 1 : 0)}, multiplier={criticalMultiplier:R}\n" +
            $"Raw damage = {difference:R} * {criticalMultiplier:R} * {100 - reduction}/100 = {raw:R}\n" +
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