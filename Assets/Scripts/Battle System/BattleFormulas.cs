using UnityEngine;

// WEEK 6 CHANGES PLEASE READ: Incoming attacks previously had no player-selected reaction value.
// This enum describes the three ordinary defensive commands without adding Spirit effects or saved unit settings.
// The battle controller keeps the selected value only for the current exchange.
public enum BattleReaction { Counter, Evade, Defend }

// WEEK 4: PILOT SKILLS - Existing battle formulas plus Potential. Tile bonuses and EXP remain deferred.
public static class BattleFormulas
{
    // WEEK 6 CHANGES PLEASE READ: Combat and the reaction preview need the same final hit percentage.
    // This helper preserves the existing 0-100 clamp and halves that chance only when Evade is chosen.
    // Odd percentages round down, and ordinary attacks retain their existing chance through the default Counter value.
    public static int ReactionHitRate(int accuracyRate, BattleReaction reaction = BattleReaction.Counter)
    {
        int chance = Mathf.Clamp(accuracyRate, 0, 100);
        return reaction == BattleReaction.Evade ? chance / 2 : chance;
    }

    // WEEK 6 CHANGES PLEASE READ: Defend reduces an incoming hit without changing armor or shared assets.
    // The battle controller applies this helper to calculated damage before the existing barriers and shield HP.
    // Damage is halved and rounded down only for Defend, while Counter and Evade keep normal damage on a hit.
    public static int ReactionDamage(int damage, BattleReaction reaction = BattleReaction.Counter) =>
        reaction == BattleReaction.Defend ? Mathf.Max(0, damage) / 2 : Mathf.Max(0, damage);

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
        // WEEK 5 CHANGES PLEASE READ: Accuracy previously used raw pilot/mech values and a fixed sight value of 140.
        // Hyper/Super pilot bonuses and GUND mobility/sight bonuses would have no combat effect through those reads.
        // The effect helpers supply current values while keeping the existing terrain, size and distance formula intact.
        TerrainType attackTerrain = weapon.TravelsToTargetTerrain ? defender.Terrain : attacker.Terrain;
        float accuracy = (MechSkillEffect.PilotStat(attacker, attacker.Pilot.Accuracy) / 2f + MechSkillEffect.Sight(attacker)) * UnitTerrainModifier(attacker, attackTerrain)
            + weapon.AccuracyModifier;
        float evade = (MechSkillEffect.PilotStat(defender, defender.Pilot.Evade) / 2f + MechSkillEffect.Mobility(defender)) * UnitTerrainModifier(defender);
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
        // WEEK 5 CHANGES PLEASE READ: Critical chance previously read the raw Skill values on shared pilot assets.
        // Hyper/Super adds 10 to effective pilot Skill, so both attacker and defender must use that value here.
        // Delegating the two reads applies the bonus without mutating assets or changing the existing critical calculation.
        int rate = MechSkillEffect.PilotStat(attacker, attacker.Pilot.Skill) - MechSkillEffect.PilotStat(defender, defender.Pilot.Skill) + weapon.CriticalModifier
            + PilotSkillEffects.CriticalBonus(attacker.PotentialStage);
        return rate;
    }

    // WEEK 3 DMG CHECK: Keep the numeric breakdown tied to the exact values used by the damage formula.
    public static int Damage(BattleUnit attacker, BattleUnit defender, Weapon weapon, bool critical)
    {
        // WEEK 5 CHANGES PLEASE READ: Damage previously read only raw pilot stats and weapon power, with no mech damage multiplier.
        // Hyper/Super pilot bonuses, GUND power and Mazin Power need to participate in this existing calculation.
        // Effective inputs and the final multiplier keep previews pure, while barrier EN and shield HP are handled only on actual hits.
        PilotBase attackPilot = attacker.Pilot;
        PilotBase defensePilot = defender.Pilot;
        int attackStat = MechSkillEffect.PilotStat(attacker, weapon.DamageType == WeaponDamageType.Melee ? attackPilot.Melee : attackPilot.Ranged);
        int weaponPower = attacker.GetWeaponPower(weapon);
        int defenseStat = MechSkillEffect.PilotStat(defender, defensePilot.Defense);
        int armor = MechSkillEffect.Armor(defender);
        float mechMultiplier = MechSkillEffect.DamageMultiplier(attacker);
        TerrainRating weaponRating = weapon.TerrainRatings.Get(defender.Terrain);
        TerrainRating armorRating = defender.Mech.TerrainRatings.Get(defender.Terrain);
        float weaponTerrain = TerrainRatings.Modifier(weaponRating);
        float armorTerrain = TerrainRatings.Modifier(armorRating);

        // WEEK 4 MORALE SYSTEM: Use each unit's live battle morale.
        float attackFactor = (attackStat + attacker.CurrentMorale) / 200f;
        float attackBeforeTerrain = attackFactor * weaponPower;
        float attack = attackBeforeTerrain * weaponTerrain;

        float defenseFactor = (defenseStat + defender.CurrentMorale) / 200f;
        float defenseBeforeTerrain = defenseFactor * armor;
        float defense = defenseBeforeTerrain * armorTerrain;

        float difference = attack - defense;
        float criticalMultiplier = critical ? 1.25f : 1f;
        // WEEK 4: PILOT SKILLS - Use defender HP BEFORE this hit. Apply reduction once, before final truncation.
        int reduction = PilotSkillEffects.DamageReductionPercent(defender.PotentialStage);
        float raw = difference * criticalMultiplier * mechMultiplier;
        if (reduction > 0) raw = raw * (100 - reduction) / 100f;
        int truncated = (int)raw;
        int damage = Mathf.Max(0, truncated);

        // WEEK 3 DMG CHECK: Print substituted equations and unrounded float values in one Console entry per damage calculation.
        BattleDebug.Log(
            $"[DMG CHECK] {attacker.name} -> {defender.name} | Weapon: {weapon.WeaponName}\n" +
            $"ATTACK INPUTS: {weapon.DamageType}={attackStat}, attacker Morale (Will)={attacker.CurrentMorale}, effective weapon Power={weaponPower}\n" +
            $"WEAPON TERRAIN: defender terrain={defender.Terrain}, weapon rating={weaponRating}, multiplier={weaponTerrain:R}\n" +
            $"Attack factor = ({attackStat} + {attacker.CurrentMorale}) / 200 = {attackFactor:R}\n" +
            $"Attack before terrain = {attackFactor:R} * {weaponPower} = {attackBeforeTerrain:R}\n" +
            $"ATTACK = {attackBeforeTerrain:R} * {weaponTerrain:R} = {attack:R}\n" +
            $"DEFENSE INPUTS: effective pilot Defense={defenseStat}, defender Morale (Will)={defender.CurrentMorale}, mech Armor={armor}\n" +
            $"ARMOR TERRAIN: defender terrain={defender.Terrain}, mech rating={armorRating}, multiplier={armorTerrain:R}\n" +
            $"Defense factor = ({defenseStat} + {defender.CurrentMorale}) / 200 = {defenseFactor:R}\n" +
            $"Defense before terrain = {defenseFactor:R} * {armor} = {defenseBeforeTerrain:R}\n" +
            $"DEFENSE = {defenseBeforeTerrain:R} * {armorTerrain:R} = {defense:R}\n" +
            $"Attack - Defense = {attack:R} - {defense:R} = {difference:R}\n" +
            "Defender terrain bonus: deferred (effective x1)\n" +
            $"Potential damage reduction: {reduction}%\n" +
            "Relationship/Ace final damage modifiers: deferred (effective x1)\n" +
            $"Critical={(critical ? 1 : 0)}, multiplier={criticalMultiplier:R}\n" +
            $"Mazin Power multiplier={mechMultiplier:R}\n" +
            $"Raw damage = {difference:R} * {criticalMultiplier:R} * {mechMultiplier:R} * {100 - reduction}/100 = {raw:R}\n" +
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