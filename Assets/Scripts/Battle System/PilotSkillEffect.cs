using UnityEngine;

// WEEK 4: PILOT SKILLS - Shared SRW Y skill rules. Never change the pilot/mech/weapon asset at runtime.

public static class PilotSkillEffects
{
    private static readonly int[] AmmoPercent = { 100, 120, 150 };
    private static readonly int[] EnergyPercent = { 100, 90, 80 };
    private static readonly int[] DamageReduction = { 0, 10, 10, 20, 20, 30, 40, 50, 60, 70 };

    public static int AmmoCapacity(PilotBase pilot, Weapon weapon)
    {
        if (weapon == null || weapon.MaxAmmo <= 0) return 0;
        int percent = AmmoPercent[pilot != null ? pilot.SaveBLevel : 0];
        return (int)System.Math.Min(int.MaxValue, ((long)weapon.MaxAmmo * percent + 99) / 100);
    }

    // WEEK 4: PILOT SKILLS - Round fractional EN costs up; a positive-cost weapon never becomes free.
    public static int EnergyCost(PilotBase pilot, Weapon weapon)
    {
        if (weapon == null || weapon.EnergyCost <= 0) return 0;
        int percent = EnergyPercent[pilot != null ? pilot.SaveELevel : 0];
        return (int)(((long)weapon.EnergyCost * percent + 99) / 100);
    }

    // WEEK 4: PILOT SKILLS - Strict HP boundaries: L9 begins BELOW 90%, L1 BELOW 10%.
    public static int PotentialStage(int level, int currentHP, int maxHP)
    {
        if (level <= 0 || currentHP <= 0 || maxHP <= 0) return 0;
        int hpBand = (int)System.Math.Min(10L, (long)currentHP * 10 / maxHP);
        return Mathf.Clamp(Mathf.Clamp(level, 0, 9) - hpBand, 0, 9);
    }

    public static int HitDodgeBonus(int stage) => Mathf.Clamp(stage, 0, 9) * 5;
    public static int CriticalBonus(int stage) => Mathf.Clamp(stage, 0, 9) * 6;
    public static int DamageReductionPercent(int stage) => DamageReduction[Mathf.Clamp(stage, 0, 9)];

    // WEEK 7: Counters and temporary effects belong to a deployed unit, never the shared pilot asset.
    // Separate crew morale lets Getter Overflow check all three pilots and preserves morale on form changes.
    internal sealed class State
    {
        internal int SupportAttacksUsed, SupportDefendsUsed;
        // WEEK 7 DEBUG: Remember only reporting state; these fields never decide ability activation.
        internal int LastDiagnosticMask = -1;
        internal PilotBase LastDiagnosticPilot;
        internal readonly System.Collections.Generic.HashSet<string> WarningKeys = new();
        internal bool TacticalStandbyPending, ZealActive, HitAndAwayPending;
        internal WeaponStatus Status, PendingStatus;
        internal int StatusStrength, PendingStatusStrength;
        internal readonly System.Collections.Generic.Dictionary<PilotBase, int> Morale = new();
    }

    private static readonly int[] MoraleLimitBonus = { 0, 5, 10, 20, 30 };
    private static readonly int[] NewtypeHitDodge = { 0, 6, 8, 10, 12, 14, 16, 18, 20, 25 };
    private static readonly int[] AbilityPower = { 0, 50, 100, 150, 200, 250, 300, 350, 400, 500 };
    // WEEK 7: Akurasu T uses a 30 SKL lead at its single level. RE's extra levels are a project rule.
    // https://akurasu.net/wiki/Super_Robot_Wars/T/Pilot_Skills
    private static readonly int[] SecondAttackLead = { int.MaxValue, 30, 20, 10, 1 };

    public static int MaximumMoraleBonus(PilotBase pilot) => pilot != null ? MoraleLimitBonus[pilot.BreakMoraleLimitLevel] : 0;
    public static int StartingMorale(PilotBase pilot) => BattleUnit.StartingMorale + (pilot != null ? 5 * pilot.FightersSpiritLevel : 0);
    internal static void Initialize(BattleUnit unit)
    {
        foreach (PilotBase pilot in unit.SpiritCrew)
            unit.PilotSkillState.Morale[pilot] = Mathf.Clamp(StartingMorale(pilot), BattleUnit.MinimumMorale, unit.GetMaximumMorale(pilot));
    }

    public static int SpiritCost(PilotBase pilot, SpiritCommandBase spirit) => spirit == null ? 0 :
        pilot != null && pilot.SpDown ? (int)(((long)spirit.SpiritPointCost * 80 + 99) / 100) : spirit.SpiritPointCost;
    public static bool ClearMindActive(BattleUnit unit) => unit.Pilot != null && unit.Pilot.ClearMind && unit.CurrentMorale >= 140;
    public static bool HasFirstStrike(BattleUnit unit, Weapon weapon) => weapon != null &&
        ((weapon.Type & WeaponType.FirstStrike) != 0 || ClearMindActive(unit));
    public static int StatBonus(BattleUnit unit) => (ClearMindActive(unit) ? 20 : 0) -
        (HasStatus(unit, WeaponStatus.StatsDown) ? Mathf.Max(unit.PilotSkillState.StatusStrength, unit.PilotSkillState.PendingStatusStrength) : 0);
    public static int FinalHitBonus(BattleUnit unit) => NewtypeHitDodge[unit.Pilot.NewtypeLevel] +
        (unit.CurrentMorale >= 130 ? (unit.Pilot.Instinct ? 10 : 0) + (unit.Pilot.Supreme ? 30 : 0) : 0);
    public static int FinalDodgeBonus(BattleUnit unit) => FinalHitBonus(unit) +
        (unit.Pilot.AuraPowerLevel > 0 ? 2 + 2 * unit.Pilot.AuraPowerLevel : 0);
    public static int FinalCriticalBonus(BattleUnit unit) => unit.Pilot.Supreme && unit.CurrentMorale >= 130 ? 30 : 0;

    // WEEK 7: Power/range bonuses are effective values; each form still owns its original weapon data.
    // Newtype level two follows the second row labelled Lv1 in the supplied list.
    public static int WeaponPowerBonus(BattleUnit unit, Weapon weapon) =>
        50 * (weapon.DamageType == WeaponDamageType.Melee ? unit.Pilot.InfightLevel : unit.Pilot.GunfightLevel) +
        (weapon.RequiredNewtypeLevel > 0 ? AbilityPower[unit.Pilot.NewtypeLevel] : 0) +
        (weapon.RequiredAuraPowerLevel > 0 ? AbilityPower[unit.Pilot.AuraPowerLevel] : 0);
    public static int WeaponRangeBonus(BattleUnit unit, Weapon weapon) => weapon.RequiredNewtypeLevel == 0 ? 0 :
        unit.Pilot.NewtypeLevel == 9 ? 2 : unit.Pilot.NewtypeLevel >= 5 ? 1 : 0;
    public static bool IsWeaponUnlocked(BattleUnit unit, Weapon weapon) => unit.Pilot != null &&
        unit.Pilot.NewtypeLevel >= weapon.RequiredNewtypeLevel && unit.Pilot.AuraPowerLevel >= weapon.RequiredAuraPowerLevel;
    public static int AuraBarrierBonus(BattleUnit unit) => 100 * unit.Pilot.AuraPowerLevel;

    // WEEK 7: Use the supplied RE thresholds and multipliers rather than replacing existing mech formulas.
    public static bool GetterOverflowActive(BattleUnit unit)
    {
        if (!GetterUnit.IsGetterForm(unit.Mech) || unit.SpiritCrew.Count != 3) return false;
        bool hasAbility = false;
        foreach (PilotBase pilot in unit.SpiritCrew)
        {
            hasAbility |= pilot.GetterOverflow;
            if (unit.GetMorale(pilot) < 140) return false;
        }
        return hasAbility;
    }
    public static float DamageDealtMultiplier(BattleUnit unit)
    {
        float multiplier = 1f;
        // WEEK 7: The author confirmed the document's reduction was a typo; use Akurasu's +20% damage.
        if (unit.Pilot.Attacker && unit.CurrentMorale >= 130) multiplier *= 1.2f;
        if (unit.Pilot.Battler && unit.CurrentMorale >= 150) multiplier *= 1.1f;
        if (unit.CurrentMorale >= 160 && GetterOverflowActive(unit)) multiplier *= 1.2f;
        return multiplier;
    }
    public static float DamageTakenMultiplier(BattleUnit unit) => unit.Pilot.Guard && unit.CurrentMorale >= 130 ? 0.9f : 1f;

    // WEEK 7: Ignore Size needs the missing larger-target damage penalty to have an effect.
    // Akurasu T's Info Dump uses 10% per size rank; only the requested penalty is added here, with no larger-attacker bonus.
    // https://akurasu.net/wiki/Super_Robot_Wars/T/Info_Dump
    public static float SizeDamageMultiplier(BattleUnit attacker, BattleUnit defender)
    {
        int ranks = Mathf.Max(0, (int)defender.Mech.Size - (int)attacker.Mech.Size - attacker.Pilot.IgnoreSizeLevel);
        return 1f - ranks * 0.1f;
    }
    public static int TerrainBonus(BattleUnit unit, bool defense)
    {
        if (unit.Terrain != TerrainType.Ground || unit.Battlefield == null) return 0;
        CustomTiles tile = unit.Battlefield.GetBonusTile(unit.GridPosition);
        if (tile == null) return 0;
        int bonus = defense ? tile.DefenseBonusPercent : tile.EvasionBonusPercent;
        return Mathf.Clamp(bonus * (unit.Pilot.ApplyTerrain ? 2 : 1), 0, 100);
    }

    // WEEK 7: Phase hooks reset only the matching support budget. Player-phase morale abilities
    // run for both teams, while Tactical Standby is queued only for player-controlled units that can still act.
    internal static void BeginPlayerPhase(BattleUnit unit, bool playerControlled)
    {
        if (unit == null || unit.IsDefeated || unit.IsCombinedComponent) return;
        int previousUses = unit.PilotSkillState.SupportAttacksUsed;
        unit.PilotSkillState.SupportAttacksUsed = 0;
        if (unit.Pilot.SupportAttackLevel > 0) DebugLog(unit, $"Player phase: Support Attack budget reset {previousUses}->0/{unit.Pilot.SupportAttackLevel}.");
        // WEEK 7: A sub-pilot with Battler gains their own morale even when another Getter pilot leads.
        foreach (PilotBase pilot in unit.SpiritCrew)
            if (pilot.Battler)
            {
                int before = unit.GetMorale(pilot);
                unit.ChangePilotMorale(pilot, 3);
                DebugLog(unit, $"Battler: {pilot.PilotName} morale {before}->{unit.GetMorale(pilot)} (+3 requested, clamped to cap).");
            }
        if (GetterOverflowActive(unit))
        {
            foreach (PilotBase pilot in unit.SpiritCrew)
            {
                int before = unit.GetMorale(pilot);
                unit.ChangePilotMorale(pilot, 5);
                DebugLog(unit, $"Getter Overflow: {pilot.PilotName} morale {before}->{unit.GetMorale(pilot)} (+5 requested).");
            }
        }
        if (playerControlled && unit.PilotSkillState.TacticalStandbyPending)
        {
            unit.PilotSkillState.TacticalStandbyPending = false;
            unit.ActivateAccel();
            unit.PilotSkillState.ZealActive = true;
            DebugLog(unit, "Tactical Standby activated: Accel and one Zeal extra action granted.");
        }
    }
    internal static void EndPlayerPhase(BattleUnit unit)
    {
        unit.PilotSkillState.TacticalStandbyPending = unit.Pilot != null && unit.Pilot.TacticalStandby &&
            !unit.HasActed && !unit.PilotSkillState.HitAndAwayPending && !unit.IsDefeated && !unit.IsDocked && !unit.IsCombinedComponent &&
            !HasStatus(unit, WeaponStatus.Inactionable);
        if (unit.Pilot != null && unit.Pilot.TacticalStandby)
            DebugLog(unit, $"Tactical Standby end-phase check: queued={unit.PilotSkillState.TacticalStandbyPending}; acted={unit.HasActed}, movement-only={unit.HitAndAwayPending}, docked={unit.IsDocked}, Inactionable={HasStatus(unit, WeaponStatus.Inactionable)}.");
    }
    internal static void BeginOwnTurn(BattleUnit unit)
    {
        State state = unit.PilotSkillState;
        WeaponStatus previous = state.Status;
        state.HitAndAwayPending = false;
        // Weapon statuses affect the current exchange and the recipient's next own phase, then expire.
        state.Status = state.PendingStatus; state.StatusStrength = state.PendingStatusStrength;
        state.PendingStatus = WeaponStatus.None; state.PendingStatusStrength = 0;
        if (HasStatus(unit, WeaponStatus.Inactionable)) unit.MarkActed();
        if (previous != WeaponStatus.None || state.Status != WeaponStatus.None)
            DebugLog(unit, $"Own-phase status update: {previous}->{state.Status}; acted={unit.HasActed} (Inactionable spends this turn).");
    }
    // WEEK 7: Zeal grants another action after attacks, Standby or a support-device action.
    // Inactionable always spends the action; Hit and Away applies only when an attack left movement unused.
    internal static void CompleteAttackAction(BattleUnit unit) => CompleteAction(unit, true);
    internal static void CompleteAction(BattleUnit unit, bool attack = false)
    {
        if (HasStatus(unit, WeaponStatus.Inactionable))
        {
            unit.MarkActed();
            DebugLog(unit, "Action finished under Inactionable: no Zeal/Hit and Away action is granted.");
            return;
        }
        if (unit.PilotSkillState.ZealActive)
        {
            unit.PilotSkillState.ZealActive = false;
            unit.HasMoved = false; unit.HasActed = false;
            DebugLog(unit, "Zeal consumed: movement and one full action remain available.");
        }
        else if (attack && unit.Pilot.HitAndAway && !unit.HasMoved)
        {
            unit.PilotSkillState.HitAndAwayPending = true;
            DebugLog(unit, "Hit and Away activated: unused movement remains, another attack is blocked.");
        }
        else
        {
            unit.MarkActed();
            if (attack && unit.Pilot.HitAndAway) DebugLog(unit, "Hit and Away not granted: movement was already used.");
        }
    }

    private static int Distance(BattleUnit a, BattleUnit b) => Mathf.Abs(a.GridPosition.x - b.GridPosition.x) + Mathf.Abs(a.GridPosition.y - b.GridPosition.y);
    // WEEK 7 FIX: Support belongs to a different allied unit, never the acting unit itself.
    // Reject missing/unavailable allies and missing battlefields before checking team and adjacency.
    // Both the preview and combat execution use this guard; Second Attack is a separate ability.
    private static bool CanSupport(BattleUnit unit, BattleUnit ally) => unit != null && ally != null && unit != ally &&
        !unit.IsDefeated && !unit.IsDocked && !unit.IsCombinedComponent && unit.Pilot != null && unit.Mech != null &&
        !ally.IsDefeated && !ally.IsDocked && !ally.IsCombinedComponent && ally.Pilot != null && ally.Mech != null &&
        unit.Battlefield != null &&
        unit.Team == ally.Team && unit.Battlefield == ally.Battlefield && Distance(unit, ally) == 1 &&
        !HasStatus(unit, WeaponStatus.Inactionable);
    public static bool CanSecondAttack(BattleUnit attacker, BattleUnit target) => attacker != null && target != null &&
        !attacker.IsDefeated && !target.IsDefeated && attacker.Pilot.SecondAttackLevel > 0 &&
        MechSkillEffect.PilotStat(attacker, attacker.Pilot.Skill) - MechSkillEffect.PilotStat(target, target.Pilot.Skill) >= SecondAttackLead[attacker.Pilot.SecondAttackLevel];
    // WEEK 7: Menus and execution share exact candidate checks, so selecting support never reserves or spends a use.
    public static bool CanSupportAttack(BattleUnit unit, BattleUnit attacker, BattleUnit target, Weapon weapon) =>
        CanSupport(unit, attacker) && !unit.HasActed && !unit.HitAndAwayPending &&
        unit.PilotSkillState.SupportAttacksUsed < unit.Pilot.SupportAttackLevel && unit.CanUseWeapon(weapon, target, true);
    public static bool CanSupportDefend(BattleUnit unit, BattleUnit target) => CanSupport(unit, target) &&
        unit.PilotSkillState.SupportDefendsUsed < unit.Pilot.SupportDefendLevel;
    public static bool TryCounterCut(BattleUnit target, Weapon weapon, int roll = -1)
    {
        if (!weapon.CanBeParried || !target.Mech.HasSword || target.Pilot.CounterCutLevel == 0)
        {
            if (target.Pilot.CounterCutLevel > 0)
            {
                DebugLog(target, $"Counter Cut not applicable: weapon parryable={weapon.CanBeParried}, mech has sword={target.Mech.HasSword}.");
                if (!target.Mech.HasSword) ValidateSetup(target);
            }
            return false;
        }
        if (roll < 0) roll = Random.Range(0, 100);
        bool parried = roll >= 0 && roll < target.Pilot.CounterCutLevel * 5;
        DebugLog(target, $"Counter Cut L{target.Pilot.CounterCutLevel}: roll={roll}, chance={target.Pilot.CounterCutLevel * 5}%, parried={parried}.");
        return parried;
    }

    // WEEK 7: Filter status flags before applying any effect. Endurance leaves Inactionable intact,
    // and statuses are applied through this one entry point only after a weapon actually connects.
    public static bool HasStatus(BattleUnit unit, WeaponStatus status) =>
        ((unit.PilotSkillState.Status | unit.PilotSkillState.PendingStatus) & status) != 0;
    public static void ApplyWeaponStatus(BattleUnit target, Weapon weapon)
    {
        if (target == null || target.IsDefeated) return;
        WeaponStatus status = target.Pilot.SpiritEndurance ? weapon.StatusEffects & WeaponStatus.Inactionable : weapon.StatusEffects;
        if (weapon.StatusEffects != WeaponStatus.None)
            DebugLog(target, $"Weapon statuses from {weapon.WeaponName}: incoming={weapon.StatusEffects}, Endurance={target.Pilot.SpiritEndurance}, blocked={weapon.StatusEffects & ~status}, applied={status}, strength={weapon.StatusStrength}.");
        if ((status & WeaponStatus.MoraleDown) != 0) target.ReduceMorale(weapon.StatusStrength);
        if ((status & WeaponStatus.SPDown) != 0) target.ReduceSpiritPoints(weapon.StatusStrength);
        target.PilotSkillState.PendingStatus |= status & (WeaponStatus.StatsDown | WeaponStatus.Immobile | WeaponStatus.Inactionable);
        // WEEK 7: Ordinary hits must not strengthen an existing Stats Down debuff through a weapon's unused default strength.
        if ((status & WeaponStatus.StatsDown) != 0)
            target.PilotSkillState.PendingStatusStrength = Mathf.Max(target.PilotSkillState.PendingStatusStrength, weapon.StatusStrength);
        if ((status & WeaponStatus.Inactionable) != 0) target.MarkActed();
    }

    // WEEK 7 DEBUG: Keep tracing separate from the existing Week 4 logs and formula queries.
    // Event hooks call these helpers, so previews and per-frame availability checks remain silent and pure.
    public static void DebugLog(BattleUnit unit, string message)
    {
        if (unit != null && unit.PilotSkillDebugEnabled)
            Debug.Log($"[WEEK 7 Pilot Skill] {unit.name}: {message}", unit);
    }
    public static void WarnOnce(BattleUnit unit, string key, string message)
    {
        // Warnings deliberately ignore the trace switch: a configuration/execution failure should remain visible.
        if (unit == null || unit.PilotSkillState.WarningKeys.Add(key))
            Debug.LogWarning($"[WEEK 7 Pilot Skill] {(unit != null ? unit.name : "Missing unit")}: {message}", unit);
    }
    public static void ValidateSetup(BattleUnit unit)
    {
        if (unit == null) return;
        if (unit.Pilot == null || unit.Mech == null)
        {
            WarnOnce(unit, "missing-data", "Pilot ability checks need both a Pilot and a Mech assigned on this BattleUnit.");
            return;
        }
        PilotBase pilot = unit.Pilot;
        if (pilot.EnhancedAttack && pilot.SupportAttackLevel == 0)
            WarnOnce(unit, "enhanced-without-support:" + pilot.GetEntityId(), "Enhanced Attack is enabled, but Support Attack is level 0. It cannot guarantee support criticals until this pilot has Support Attack.");
        if (pilot.CounterCutLevel > 0 && !unit.Mech.HasSword)
            WarnOnce(unit, "sword:" + unit.Mech.GetEntityId(), $"Counter Cut L{pilot.CounterCutLevel} cannot activate in {unit.Mech.MechName}: enable Has Sword on this mech if it has a sword.");
        bool overflow = false;
        foreach (PilotBase crewPilot in unit.SpiritCrew) overflow |= crewPilot.GetterOverflow;
        // A separate Getter component may legitimately be waiting to combine; do not report it as a broken three-pilot unit.
        if (overflow && !GetterUnit.IsGetterForm(unit.Mech.CombineInto))
        {
            if (!GetterUnit.IsGetterForm(unit.Mech) || unit.SpiritCrew.Count != 3)
                WarnOnce(unit, "overflow-crew:" + unit.Mech.GetEntityId(), $"Getter Overflow cannot activate: mech needs a Getter compatibility ID and exactly three crew pilots (current crew: {unit.SpiritCrew.Count}).");
            else
            {
                bool canReachDamageThreshold = false;
                foreach (PilotBase crewPilot in unit.SpiritCrew) canReachDamageThreshold |= unit.GetMaximumMorale(crewPilot) >= 160;
                if (!canReachDamageThreshold)
                    WarnOnce(unit, "overflow-cap:" + unit.Mech.GetEntityId(), "Getter Overflow's 160-morale damage bonus is unreachable with this crew's current morale caps. Its +5 phase morale can still work; raise a main pilot's morale cap for the damage bonus.");
            }
        }
    }
    public static void RefreshDiagnostics(BattleUnit unit)
    {
        if (unit == null || unit.Pilot == null || unit.Mech == null || unit.IsDefeated || !unit.PilotSkillDebugEnabled) return;
        PilotBase pilot = unit.Pilot;
        bool high = unit.CurrentMorale >= 130;
        int mask = (pilot.Instinct && high ? 1 : 0) | (pilot.Guard && high ? 2 : 0) |
            (pilot.Attacker && high ? 4 : 0) | (pilot.Supreme && high ? 8 : 0) |
            (ClearMindActive(unit) ? 16 : 0) | (pilot.Battler && unit.CurrentMorale >= 150 ? 32 : 0) |
            (GetterOverflowActive(unit) ? 64 : 0) | (GetterOverflowActive(unit) && unit.CurrentMorale >= 160 ? 128 : 0);
        State state = unit.PilotSkillState;
        if (state.LastDiagnosticMask == mask && state.LastDiagnosticPilot == pilot) return;
        state.LastDiagnosticMask = mask; state.LastDiagnosticPilot = pilot;
        string[] names = { "Instinct", "Guard", "Attacker", "Supreme", "Clear Mind", "Battler damage", "Getter Overflow phase morale", "Getter Overflow damage" };
        System.Collections.Generic.List<string> active = new();
        for (int i = 0; i < names.Length; i++) if ((mask & (1 << i)) != 0) active.Add(names[i]);
        DebugLog(unit, $"Morale abilities updated: pilot={pilot.PilotName}, morale={unit.CurrentMorale}/{unit.MaximumMorale}; active={(active.Count > 0 ? string.Join(", ", active) : "none")}. Falling below a threshold normally deactivates its effect.");
    }
    public static string Describe(BattleUnit unit)
    {
        if (unit == null || unit.Pilot == null || unit.Mech == null) return "Missing pilot or mech assignment.";
        PilotBase p = unit.Pilot;
        (string name, int level)[] settings = {
            ("Support Attack", p.SupportAttackLevel), ("Support Defend", p.SupportDefendLevel), ("Second Attack", p.SecondAttackLevel),
            ("Fighter's Spirit", p.FightersSpiritLevel), ("Break Morale Limit", p.BreakMoraleLimitLevel), ("Ignore Size", p.IgnoreSizeLevel),
            ("Infight", p.InfightLevel), ("Gunfight", p.GunfightLevel), ("Counter Cut", p.CounterCutLevel),
            ("Newtype", p.NewtypeLevel), ("Aura Power", p.AuraPowerLevel), ("Enhanced Attack", p.EnhancedAttack ? 1 : 0),
            ("Morale+ATK", p.MoralePlusATK ? 1 : 0), ("Morale+DEF", p.MoralePlusDEF ? 1 : 0), ("Instinct", p.Instinct ? 1 : 0),
            ("Guard", p.Guard ? 1 : 0), ("Attacker", p.Attacker ? 1 : 0), ("Spirit Endurance", p.SpiritEndurance ? 1 : 0),
            ("Hit and Away", p.HitAndAway ? 1 : 0), ("Apply Terrain", p.ApplyTerrain ? 1 : 0), ("Tactical Standby", p.TacticalStandby ? 1 : 0),
            ("SP Down", p.SpDown ? 1 : 0), ("Supreme", p.Supreme ? 1 : 0), ("Clear Mind", p.ClearMind ? 1 : 0),
            ("Battler", p.Battler ? 1 : 0), ("Getter Overflow", p.GetterOverflow ? 1 : 0)
        };
        System.Text.StringBuilder text = new();
        text.Append($"pilot={p.PilotName}; mech={unit.Mech.MechName}; morale={unit.CurrentMorale}/{unit.MaximumMorale}; configured skills:");
        foreach (var setting in settings) if (setting.level > 0) text.Append($" {setting.name} L{setting.level};");
        text.Append($"\nEffective hit/dodge/crit bonuses=+{FinalHitBonus(unit)}/+{FinalDodgeBonus(unit)}/+{FinalCriticalBonus(unit)}; stat bonus={StatBonus(unit)}; damage dealt x{DamageDealtMultiplier(unit):0.###}, taken x{DamageTakenMultiplier(unit):0.###}.");
        text.Append($"\nAura Barrier bonus=+{AuraBarrierBonus(unit)} (equipped barrier={unit.Mech.Barrier}); terrain evasion/defense={TerrainBonus(unit, false)}%/{TerrainBonus(unit, true)}%, grounded={unit.Terrain == TerrainType.Ground}.");
        text.Append($"\nSupport attacks used={unit.PilotSkillState.SupportAttacksUsed}/{p.SupportAttackLevel}, defenses used={unit.PilotSkillState.SupportDefendsUsed}/{p.SupportDefendLevel}; moved={unit.HasMoved}, acted={unit.HasActed}; Zeal={unit.PilotSkillState.ZealActive}, Hit and Away={unit.HitAndAwayPending}, Tactical Standby queued={unit.PilotSkillState.TacticalStandbyPending}; statuses={unit.PilotSkillState.Status | unit.PilotSkillState.PendingStatus}.");
        foreach (PilotBase crewPilot in unit.SpiritCrew)
        {
            text.Append($"\nCrew {crewPilot.PilotName}: morale={unit.GetMorale(crewPilot)}/{unit.GetMaximumMorale(crewPilot)}, Fighter's Spirit starting morale={StartingMorale(crewPilot)}.");
            if (crewPilot.SpDown)
                foreach (SpiritCommandBase spirit in crewPilot.GetSpiritCommands())
                    if (spirit != null) text.Append($" SP Down {spirit.CommandName}: {spirit.SpiritPointCost}->{SpiritCost(crewPilot, spirit)} SP;");
        }
        foreach (Weapon weapon in unit.Mech.Weapons)
            if (weapon != null) text.Append($"\nWeapon {weapon.WeaponName}: pilot power +{WeaponPowerBonus(unit, weapon)}, effective power={unit.GetWeaponPower(weapon)}, range={weapon.MinRange}-{unit.GetWeaponMaxRange(weapon)}, Newtype/Aura requirement={weapon.RequiredNewtypeLevel}/{weapon.RequiredAuraPowerLevel}, pilot requirement met={IsWeaponUnlocked(unit, weapon)}.");
        return text.ToString();
    }
    public static string DescribeSecondAttack(BattleUnit unit, BattleUnit target)
    {
        if (unit == null || unit.Pilot == null || target == null || target.Pilot == null) return "Second Attack: missing participant data.";
        int level = unit.Pilot.SecondAttackLevel;
        if (level == 0) return "Second Attack disabled (level 0).";
        int lead = MechSkillEffect.PilotStat(unit, unit.Pilot.Skill) - MechSkillEffect.PilotStat(target, target.Pilot.Skill);
        return $"Second Attack L{level}: effective SKL lead={lead}, required={SecondAttackLead[level]}, Skill check={(lead >= SecondAttackLead[level] ? "passed" : "not met")}.";
    }
    public static void ReportWeaponUse(BattleUnit unit, BattleUnit target, Weapon weapon, bool support, float multiplier)
    {
        ValidateSetup(unit);
        if (!unit.PilotSkillDebugEnabled) return;
        RefreshDiagnostics(unit);
        DebugLog(unit, $"Weapon use: {weapon.WeaponName}; pilot power +{WeaponPowerBonus(unit, weapon)}, effective power={unit.GetWeaponPower(weapon)}, range={weapon.MinRange}-{unit.GetWeaponMaxRange(weapon)}; damage multiplier x{DamageDealtMultiplier(unit):0.###}, follow-up x{multiplier:0.###}; allied support={support}, Enhanced Attack guaranteed critical={support && unit.Pilot.EnhancedAttack}; EN cost={unit.GetWeaponEnergyCost(weapon)}, EN remaining={unit.CurrentEnergy}, ammo remaining={unit.GetAmmo(weapon)}. " + DescribeSecondAttack(unit, target));
    }

    public static void Log(BattleUnit unit, string message)
    {
        if (unit != null)
            Debug.Log($"[Pilot Skill] {unit.name}: {message}", unit);
    }
}