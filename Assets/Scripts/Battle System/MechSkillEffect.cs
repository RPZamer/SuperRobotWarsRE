using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// WEEK 5 MECH SKILL: Keep enum values compatible with the older saved mech and weapon settings.
public enum RegenerationLevel { None, S, M, L }
public enum MechBarrier { None, BeamCoat, DistortionField, AuraBarrier, Escutcheon }
public enum SpecialEvasionAbility { None, DoubleImage, OpenGet, Offshoot }
public enum MechMode { None, Hyper, Super }
[Flags] public enum WeaponAttribute { None = 0, Beam = 1, Gravity = 2 }

// WEEK 5 MECH SKILL: All ability rules live here; each BattleUnit owns a separate State.
// Shared mech, pilot and weapon assets remain configuration and are never changed by an effect.
public static class MechSkillEffect
{
    internal sealed class State
    {
        internal int Shield;
        internal float ShieldFraction = 1f;
        internal bool MazinActivated;
        internal MechMode ActiveMode;
        internal int GundSteps;
        internal BattleUnit CombinedInto;
        internal readonly List<Part> Parts = new();
        internal readonly List<PilotBase> Crew = new();
    }

    internal sealed class Part
    {
        internal BattleUnit Unit;
        internal MechBase Mech;
        internal PilotBase Pilot;
        internal bool MazinActivated;
        internal readonly List<PilotBase> Crew = new();
    }

    public static bool DebugEnabled { get; set; } = true;
    public static void Log(BattleUnit unit, string message)
    {
        if (DebugEnabled && unit != null) BattleDebug.Log($"[Mech Skill] {unit.name}: {message}", unit);
    }

    internal static void Initialize(BattleUnit unit)
    {
        State state = unit.MechSkillState;
        state.Shield = unit.Mech != null ? unit.Mech.ShieldHealth : 0;
        state.ShieldFraction = 1f;
        state.MazinActivated = false;
        state.ActiveMode = MechMode.None;
        state.GundSteps = 0;
        state.Crew.Clear();
        if (unit.Pilot != null) state.Crew.Add(unit.Pilot);
        foreach (PilotBase pilot in unit.AdditionalPilots)
            if (pilot != null && !state.Crew.Contains(pilot)) state.Crew.Add(pilot);
        RefreshMorale(unit);
    }

    // WEEK 5 MECH SKILL: Hyper/Super unlock matching moves and add 10 to the six pilot combat stats at morale 120+.
    // Mech bonuses come only from a configured form's own stats; mode activation does not invent unit bonuses.
    // GUND adds 10 mobility/sight/power per complete morale step; movement stops at +2.
    public static int ModeBonus(BattleUnit unit) => unit.MechSkillState.ActiveMode != MechMode.None ? 10 : 0;
    public static int GUNDSteps(BattleUnit unit) => unit.Mech != null && unit.Mech.GUNDFormat ?
        Mathf.Clamp((unit.CurrentMorale - 100) / 10, 0, 5) : 0;
    public static int PilotStat(BattleUnit unit, int value) => value + ModeBonus(unit);
    public static int Mobility(BattleUnit unit) => unit.Mech.Mobility + 10 * GUNDSteps(unit);
    public static int Sight(BattleUnit unit) => unit.Mech.Sight + 10 * GUNDSteps(unit);
    public static int Armor(BattleUnit unit) => unit.Mech.Armor;
    public static int Movement(BattleUnit unit) => unit.Mech.Movement + Mathf.Min(2, GUNDSteps(unit));
    public static int WeaponPower(BattleUnit unit, Weapon weapon) => weapon.Power + 10 * GUNDSteps(unit);
    public static float DamageMultiplier(BattleUnit unit) =>
        unit.Mech.MazinPower && unit.MechSkillState.MazinActivated ? 1.2f : 1f;
    public static bool IsWeaponUnlocked(BattleUnit unit, Weapon weapon) => weapon != null &&
        (weapon.RequiredMode == MechMode.None || weapon.RequiredMode == unit.MechSkillState.ActiveMode);

    internal static void RefreshMorale(BattleUnit unit)
    {
        if (unit.Mech == null || unit.IsDefeated) return;
        State state = unit.MechSkillState;
        if (unit.Mech.MazinPower && unit.CurrentMorale >= 130 && !state.MazinActivated)
        {
            state.MazinActivated = true;
            Log(unit, $"Mazin Power activated at morale {unit.CurrentMorale}: damage x1.2 for this stage.");
        }
        MechMode mode = unit.CurrentMorale >= 120 ? unit.Mech.Mode : MechMode.None;
        if (state.ActiveMode != mode)
        {
            state.ActiveMode = mode;
            Log(unit, $"Mode={mode}, morale={unit.CurrentMorale}, combat stat bonus={ModeBonus(unit)}; matching weapons updated.");
        }
        int steps = GUNDSteps(unit);
        if (state.GundSteps != steps)
        {
            state.GundSteps = steps;
            Log(unit, $"GUND: morale={unit.CurrentMorale}, steps={steps}/5, mobility/sight/power +{10 * steps}, move +{Mathf.Min(2, steps)}.");
        }
    }

    // WEEK 5 MECH SKILL: Team phase start restores S/M/L = 10/20/30%, rounded up and capped.
    public static void BeginPhase(BattleUnit unit)
    {
        if (unit.Mech == null || unit.IsDefeated || unit.IsCombinedComponent) return;
        int hp = unit.CurrentHealth, en = unit.CurrentEnergy;
        int hpPercent = Mathf.Clamp((int)unit.Mech.HPRegeneration, 0, 3) * 10;
        int enPercent = Mathf.Clamp((int)unit.Mech.ENRegeneration, 0, 3) * 10;
        unit.RestoreHealthAndEnergy(Mathf.CeilToInt(unit.Mech.Health * hpPercent / 100f),
            Mathf.CeilToInt(unit.Mech.Energy * enPercent / 100f));
        if (hpPercent > 0 || enPercent > 0)
            Log(unit, $"Regen HP {hpPercent}%: {hp}->{unit.CurrentHealth}; EN {enPercent}%: {en}->{unit.CurrentEnergy}.");
        RefreshMorale(unit);
    }

    // WEEK 5 MECH SKILL: Roll only after normal accuracy hits; an explicit roll supports deterministic checks.
    public static bool TrySpecialEvade(BattleUnit unit, int roll = -1)
    {
        MechBase mech = unit.Mech;
        if (mech == null || unit.IsDefeated || unit.IsDocked || unit.IsCombinedComponent) return false;
        int chance = mech.SpecialEvasionAbility == SpecialEvasionAbility.OpenGet ? 25 :
            mech.SpecialEvasionAbility != SpecialEvasionAbility.None ? 30 : Mathf.Clamp(mech.LegacyEvasionChance, 0, 100);
        int threshold = mech.SpecialEvasionAbility == SpecialEvasionAbility.None ? mech.LegacyEvasionMorale : 130;
        if (chance == 0 || unit.CurrentMorale < threshold) return false;
        if (roll < 0) roll = UnityEngine.Random.Range(0, 100);
        bool evaded = roll >= 0 && roll < chance;
        Log(unit, $"{mech.SpecialEvasionAbility}: morale={unit.CurrentMorale}, roll={roll}, chance={chance}%, evaded={evaded}.");
        return evaded;
    }

    // WEEK 5 MECH SKILL: Ammo-style recovery also restores shields, including docking and resupply Spirits.
    public static void RestoreShield(BattleUnit unit)
    {
        if (unit.Mech == null || unit.IsDefeated) return;
        int before = unit.MechSkillState.Shield;
        unit.MechSkillState.Shield = unit.Mech.ShieldHealth;
        unit.MechSkillState.ShieldFraction = 1f;
        if (unit.Mech.ShieldHealth > 0) Log(unit, $"Shield resupplied: {before}->{unit.CurrentShieldHealth}/{unit.Mech.ShieldHealth}.");
    }

    // WEEK 5 MECH SKILL: Direct hits pass through barrier, shield HP, then mech HP.
    // TakeDamage remains the HP-only route for poison/indirect damage; previews do not spend barrier EN.
    public static int TakeWeaponDamage(BattleUnit unit, int damage, Weapon weapon)
    {
        if (unit.Mech == null || weapon == null || unit.IsDefeated || unit.IsDocked || unit.IsCombinedComponent) return 0;
        int remaining = Mathf.Max(0, damage), reduction = 0, cost = 0;
        bool beam = (weapon.Attributes & WeaponAttribute.Beam) != 0;
        bool gravity = (weapon.Attributes & WeaponAttribute.Gravity) != 0;
        switch (unit.Mech.Barrier)
        {
            case MechBarrier.BeamCoat:
                if (beam) { reduction = 1000; cost = 5; }
                break;
            case MechBarrier.DistortionField:
                if (gravity || beam) { reduction = gravity ? 5000 : 3000; cost = 5; }
                break;
            case MechBarrier.AuraBarrier:
                if (weapon.DamageType == WeaponDamageType.Ranged) reduction = 1000; break;
            case MechBarrier.Escutcheon:
                if (beam && remaining <= 3000) reduction = remaining; break;
        }
        if (remaining > 0 && reduction > 0)
        {
            int enBefore = unit.CurrentEnergy;
            bool applied = cost == 0 || unit.TrySpendEnergy(cost);
            if (applied) remaining = Mathf.Max(0, remaining - reduction);
            Log(unit, $"{unit.Mech.Barrier}: incoming={damage}, reduction={(applied ? reduction : 0)}, EN {enBefore}->{unit.CurrentEnergy}, applied={applied}.");
        }
        int shieldBefore = unit.CurrentShieldHealth;
        int absorbed = Mathf.Min(shieldBefore, remaining);
        unit.MechSkillState.Shield -= absorbed;
        if (unit.Mech.ShieldHealth > 0)
            unit.MechSkillState.ShieldFraction = unit.CurrentShieldHealth / (float)unit.Mech.ShieldHealth;
        int hpBefore = unit.CurrentHealth;
        int hpDamage = unit.TakeDamage(remaining - absorbed);
        Log(unit, $"Direct hit={damage}: shield {shieldBefore}->{unit.CurrentShieldHealth} (absorbed {absorbed}), HP {hpBefore}->{unit.CurrentHealth} (damage {hpDamage}).");
        return absorbed + hpDamage;
    }

    private static bool OnGrid(BattleUnit unit) => unit != null && !unit.IsDefeated && !unit.IsDocked &&
        !unit.IsCombinedComponent && unit.Mech != null && unit.Pilot != null && unit.Battlefield != null &&
        unit.Battlefield.GetUnit(unit.GridPosition) == unit;
    private static int Distance(BattleUnit a, BattleUnit b) =>
        Mathf.Abs(a.GridPosition.x - b.GridPosition.x) + Mathf.Abs(a.GridPosition.y - b.GridPosition.y);
    private static bool CanSupport(BattleUnit unit, BattleUnit target) => OnGrid(unit) && OnGrid(target) &&
        unit != target && !unit.HasActed && unit.Team == target.Team && unit.Battlefield == target.Battlefield && Distance(unit, target) == 1;
    public static int RepairHealing(int level) => 500 + 60 * Mathf.Clamp(level, 1, 9999);
    public static bool CanRepair(BattleUnit unit, BattleUnit target) => CanSupport(unit, target) &&
        unit.Mech.RepairDevice && target.CurrentHealth < target.Mech.Health;
    public static bool TryRepair(BattleUnit unit, BattleUnit target)
    {
        if (!CanRepair(unit, target)) { Log(unit, "Repair rejected: device, action, adjacency/team or damaged-target requirement failed."); return false; }
        int before = target.CurrentHealth, healing = RepairHealing(unit.Pilot.Level);
        target.RestoreHealth(healing);
        unit.MarkActed();
        Log(unit, $"Repair L{unit.Pilot.Level}: 500 + 60*level = {healing}; {target.name} HP {before}->{target.CurrentHealth}; action spent.");
        return true;
    }
    public static bool NeedsResupply(BattleUnit unit)
    {
        if (unit.CurrentEnergy < unit.Mech.Energy || unit.CurrentShieldHealth < unit.Mech.ShieldHealth) return true;
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER can need ammo in a form that is currently inactive.
        // Checking only the visible form would incorrectly disable the resupply device for that shared unit.
        // GetterUnit checks its saved form inventories here while ordinary mechs retain the existing equipped-weapon check.
        if (unit.GetterState.NeedsAmmo()) return true;
        foreach (Weapon weapon in unit.Mech.Weapons)
            if (weapon != null && weapon.MaxAmmo > 0 && unit.GetAmmo(weapon) < unit.GetAmmoCapacity(weapon)) return true;
        return false;
    }
    public static bool CanResupply(BattleUnit unit, BattleUnit target) => CanSupport(unit, target) &&
        unit.Mech.ResupplyDevice && !unit.HasMoved && NeedsResupply(target);
    public static void RestoreSupplies(BattleUnit unit)
    {
        int before = unit.CurrentEnergy;
        unit.RestoreHealthAndEnergy(0, unit.Mech.Energy);
        unit.RestoreAmmo();
        Log(unit, $"Resupply: EN {before}->{unit.CurrentEnergy}, all equipped ammo and shield restored.");
    }
    public static bool TryResupply(BattleUnit unit, BattleUnit target)
    {
        if (!CanResupply(unit, target)) { Log(unit, "Resupply rejected: device, movement/action, adjacency/team or missing supplies requirement failed."); return false; }
        int morale = target.CurrentMorale;
        RestoreSupplies(target);
        target.ChangeMorale(-10);
        unit.MarkActed();
        Log(unit, $"Resupply device -> {target.name}: morale {morale}->{target.CurrentMorale}; action spent.");
        return true;
    }

    // WEEK 5 MECH SKILL: Form changes preserve resources and spent actions; returning to a form cannot refill ammo/shields.
    private static float AmmoFraction(BattleUnit unit)
    {
        float fraction = 1f;
        foreach (Weapon weapon in unit.Mech.Weapons)
            if (weapon != null && unit.GetAmmoCapacity(weapon) > 0)
                fraction = Mathf.Min(fraction, unit.GetAmmo(weapon) / (float)unit.GetAmmoCapacity(weapon));
        return fraction;
    }
    private static void ChangeForm(BattleUnit unit, MechBase form, PilotBase pilot = null)
    {
        float ammo = AmmoFraction(unit), shield = unit.MechSkillState.ShieldFraction;
        MechBase before = unit.Mech;
        unit.Mech = form;
        if (pilot != null) unit.Pilot = pilot;
        unit.CurrentHealth = Mathf.Min(unit.CurrentHealth, form.Health);
        unit.CurrentEnergy = Mathf.Min(unit.CurrentEnergy, form.Energy);
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER selects a main pilot whose SP is already stored independently.
        // Applying the old shared-pool clamp when entering a combined Getter would overwrite that pilot's preserved SP.
        // Ordinary transformations keep their existing clamp, while Getter combination uses the recorded crew pools.
        if (!GetterUnit.IsGetterForm(form)) unit.CurrentSpiritPoints = Mathf.Min(unit.CurrentSpiritPoints, unit.Pilot.MaxSpiritPoints);
        unit.MechSkillState.Shield = Mathf.FloorToInt(form.ShieldHealth * shield);
        foreach (Weapon weapon in form.Weapons)
        {
            if (weapon == null) continue;
            int limit = Mathf.FloorToInt(unit.GetAmmoCapacity(weapon) * ammo);
            unit.AbilityAmmo[weapon] = unit.AbilityAmmo.TryGetValue(weapon, out int saved) ? Mathf.Min(saved, limit) : limit;
        }
        unit.AbilitySprite.sprite = form.BattleSprite;
        RefreshMorale(unit);
        Log(unit, $"Form {before.MechName}->{form.MechName}: HP={unit.CurrentHealth}, EN={unit.CurrentEnergy}, shield={unit.CurrentShieldHealth}, moved={unit.HasMoved}, acted={unit.HasActed}.");
    }
    public static bool CanTransform(BattleUnit unit) => OnGrid(unit) && !unit.HasActed &&
        unit.Mech.TransformInto != null && unit.Mech.TransformInto != unit.Mech &&
        (unit.Mech.TransformInto.Mode == MechMode.None || unit.CurrentMorale >= 120) &&
        string.IsNullOrWhiteSpace(unit.Mech.TransformInto.GetterCompatibilityId);
    public static bool TryTransform(BattleUnit unit)
    {
        if (!CanTransform(unit)) { Log(unit, "Transform rejected: linked form, mode morale threshold or unit availability requirement failed."); return false; }
        ChangeForm(unit, unit.Mech.TransformInto); return true;
    }
    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER needs its own form transition rather than the ordinary transform's shared ammo/SP rules.
    // GetterUnit validates the three forms and crew, preserves per-form ammo, and switches the main pilot within the same BattleUnit.
    // These existing command entry points now delegate to it, retaining keyboard/menu bindings and the after-action exception.
    public static MechBase NextGetterForm(BattleUnit unit)
    {
        if (!OnGrid(unit)) return null;
        foreach (MechBase form in unit.Mech.GetterForms)
            if (unit.GetterState.CanChangeForm(form)) return form;
        return null;
    }
    public static bool TryGetterChange(BattleUnit unit)
    {
        MechBase form = NextGetterForm(unit);
        if (form == null) { Log(unit, "Getter Change rejected: linked form, compatibility ID or exact three-pilot crew missing."); return false; }
        return unit.GetterState.TryChangeForm(form);
    }

    // WEEK 5 MECH SKILL: Combine validates an entire recipe before hiding any actual component objects.
    // Generic recipes use Required Components; saved Getter recipes use their exact three pilot assets.
    private static List<BattleUnit> Combination(BattleUnit unit)
    {
        if (!OnGrid(unit) || unit.HasActed || unit.MechSkillState.Parts.Count > 0 || unit.GetComponent<Mothership>() != null) return null;
        MechBase form = unit.Mech.CombineInto;
        if (form == null || form == unit.Mech) return null;
        bool getter = form.RequiredComponents.Count == 0 && form.RequiredGetterCrew.Count == 3;
        if (!getter && (form.RequiredComponents.Count < 2 || form.RequiredComponents.Count > 5)) return null;
        List<BattleUnit> parts = new() { unit };
        List<MechBase> remaining = new(form.RequiredComponents);
        HashSet<PilotBase> pilots = new();
        if (getter)
        {
            if (unit.MechSkillState.Crew.Count != 1 || !Contains(form.RequiredGetterCrew, unit.Pilot)) return null;
            pilots.Add(unit.Pilot);
        }
        else if (!remaining.Remove(unit.Mech)) return null;
        foreach (BattleUnit candidate in unit.Battlefield.Units)
        {
            if (candidate == unit || !OnGrid(candidate) || candidate.Team != unit.Team || candidate.HasActed ||
                candidate.Mech.CombineInto != form || candidate.MechSkillState.Parts.Count > 0 ||
                candidate.GetComponent<Mothership>() != null || Distance(unit, candidate) != 1) continue;
            if (getter ? candidate.MechSkillState.Crew.Count == 1 && Contains(form.RequiredGetterCrew, candidate.Pilot) && pilots.Add(candidate.Pilot) : remaining.Remove(candidate.Mech))
                parts.Add(candidate);
        }
        if (getter)
        {
            HashSet<PilotBase> recipe = new(form.RequiredGetterCrew);
            if (recipe.Count != 3 || recipe.Contains(null) || form.MainGetterPilot == null || !recipe.Contains(form.MainGetterPilot) || !pilots.SetEquals(recipe)) return null;
        }
        else if (remaining.Count > 0) return null;
        return parts;
    }
    private static bool Contains<T>(IReadOnlyList<T> list, T value)
    {
        foreach (T item in list) if (EqualityComparer<T>.Default.Equals(item, value)) return true;
        return false;
    }
    public static bool CanCombine(BattleUnit unit) => Combination(unit) != null;
    public static bool TryCombine(BattleUnit unit)
    {
        List<BattleUnit> parts = Combination(unit);
        if (parts == null) { Log(unit, "Combine rejected: complete compatible, living, adjacent, unacted component recipe unavailable."); return false; }
        State state = unit.MechSkillState;
        MechBase form = unit.Mech.CombineInto;
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER components may have spent different amounts of SP before combining.
        // Capturing each actual pilot's pool before the crew list changes prevents the previous lowest-SP rule from merging them.
        // Getter combinations transfer those pools and initialize all form inventories, while generic combinations keep their existing resource rules.
        Dictionary<PilotBase, int> getterSP = GetterUnit.IsGetterForm(form) ? GetterUnit.ComponentSpiritPoints(parts) : null;
        float hp = 1f, en = 1f, ammo = 1f, shield = 1f;
        int morale = unit.CurrentMorale, sp = unit.CurrentSpiritPoints;
        bool moved = false;
        state.Crew.Clear();
        foreach (BattleUnit part in parts)
        {
            Part saved = new() { Unit = part, Mech = part.Mech, Pilot = part.Pilot, MazinActivated = part.MechSkillState.MazinActivated };
            saved.Crew.Add(part.Pilot);
            foreach (PilotBase crewPilot in part.AdditionalPilots)
                if (crewPilot != null && !saved.Crew.Contains(crewPilot)) saved.Crew.Add(crewPilot);
            state.Parts.Add(saved);
            hp = Mathf.Min(hp, part.CurrentHealth / (float)Mathf.Max(1, part.Mech.Health));
            en = Mathf.Min(en, part.CurrentEnergy / (float)Mathf.Max(1, part.Mech.Energy));
            ammo = Mathf.Min(ammo, AmmoFraction(part)); shield = Mathf.Min(shield, part.MechSkillState.ShieldFraction);
            morale = Mathf.Min(morale, part.CurrentMorale); sp = Mathf.Min(sp, part.CurrentSpiritPoints); moved |= part.HasMoved;
            if (!state.Crew.Contains(part.Pilot)) state.Crew.Add(part.Pilot);
            foreach (PilotBase crewPilot in part.AdditionalPilots)
                if (crewPilot != null && !state.Crew.Contains(crewPilot)) state.Crew.Add(crewPilot);
            if (part != unit)
            {
                part.MechSkillState.CombinedInto = unit;
                unit.Battlefield.Remove(part);
                part.gameObject.SetActive(false);
            }
        }
        unit.CurrentMoraleForAbility(morale);
        if (getterSP != null) unit.GetterState.ApplySpiritPoints(getterSP);
        ChangeForm(unit, form, form.MainGetterPilot);
        unit.CurrentHealth = Mathf.Max(1, Mathf.FloorToInt(form.Health * hp));
        unit.CurrentEnergy = Mathf.FloorToInt(form.Energy * en);
        if (getterSP == null) unit.CurrentSpiritPoints = Mathf.Min(sp, unit.Pilot.MaxSpiritPoints);
        unit.HasMoved = moved;
        state.ShieldFraction = shield; state.Shield = Mathf.FloorToInt(form.ShieldHealth * shield);
        foreach (Weapon weapon in form.Weapons)
            if (weapon != null) unit.AbilityAmmo[weapon] = Mathf.Min(unit.GetAmmo(weapon), Mathf.FloorToInt(unit.GetAmmoCapacity(weapon) * ammo));
        if (getterSP != null) unit.GetterState.EnterCombinedForm(ammo);
        Log(unit, $"Combine succeeded: {parts.Count} actual parts -> {form.MechName}; weakest HP/EN/ammo/shield ratios preserved.");
        return true;
    }

    // WEEK 5 MECH SKILL: Separate requires recorded components and validates every exit before placing any part.
    private static List<Vector2Int> SeparationCells(BattleUnit unit)
    {
        if (!OnGrid(unit) || unit.HasActed || unit.MechSkillState.Parts.Count < 2) return null;
        List<Vector2Int> cells = new() { unit.GridPosition };
        foreach (Part part in unit.MechSkillState.Parts) if (part.Unit == null) return null;
        foreach (Vector2Int direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int cell = unit.GridPosition + direction;
            if (unit.Battlefield.IsInside(cell) && unit.Battlefield.GetUnit(cell) == null) cells.Add(cell);
        }
        return cells.Count >= unit.MechSkillState.Parts.Count ? cells : null;
    }
    public static bool CanSeparate(BattleUnit unit) => SeparationCells(unit) != null;
    public static bool TrySeparate(BattleUnit unit)
    {
        List<Vector2Int> cells = SeparationCells(unit);
        if (cells == null) { Log(unit, "Separate rejected: no recorded parts, action spent or not enough empty adjacent exits."); return false; }
        List<Part> parts = new(unit.MechSkillState.Parts);
        float hp = unit.CurrentHealth / (float)Mathf.Max(1, unit.Mech.Health);
        float en = unit.CurrentEnergy / (float)Mathf.Max(1, unit.Mech.Energy);
        float ammo = AmmoFraction(unit), shield = unit.MechSkillState.ShieldFraction;
        int morale = unit.CurrentMorale, sp = unit.CurrentSpiritPoints;
        bool moved = unit.HasMoved;
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER pilots retain their own SP while serving in any of the three forms.
        // Separation must return those live pools to the original components rather than copy the current main pilot's SP to everyone.
        // Taking a snapshot before changing any component pilot preserves earned or spent SP without affecting generic separation.
        Dictionary<PilotBase, int> getterSP = GetterUnit.IsGetterForm(unit.Mech) ? unit.GetterState.CopySpiritPoints() : null;
        for (int i = 0; i < parts.Count; i++)
        {
            Part part = parts[i]; BattleUnit component = part.Unit;
            component.MechSkillState.CombinedInto = null;
            ChangeForm(component, part.Mech, part.Pilot);
            component.MechSkillState.Parts.Clear();
            component.MechSkillState.Crew.Clear(); component.MechSkillState.Crew.AddRange(part.Crew);
            component.MechSkillState.MazinActivated = part.MazinActivated;
            component.CurrentHealth = Mathf.Max(1, Mathf.FloorToInt(part.Mech.Health * hp));
            component.CurrentEnergy = Mathf.FloorToInt(part.Mech.Energy * en);
            if (getterSP != null) component.GetterState.ApplySpiritPoints(getterSP);
            else component.CurrentSpiritPoints = Mathf.Min(component.CurrentSpiritPoints, Mathf.Min(sp, part.Pilot.MaxSpiritPoints));
            component.CurrentMoraleForAbility(morale);
            component.HasMoved = moved;
            component.MechSkillState.ShieldFraction = shield;
            component.MechSkillState.Shield = Mathf.FloorToInt(part.Mech.ShieldHealth * shield);
            foreach (Weapon weapon in part.Mech.Weapons)
                if (weapon != null) component.AbilityAmmo[weapon] = Mathf.Min(component.GetAmmo(weapon), Mathf.FloorToInt(component.GetAmmoCapacity(weapon) * ammo));
            component.gameObject.SetActive(true);
            if (component != unit) unit.Battlefield.TryPlace(component, cells[i]);
            component.SetSelected(false);
        }
        unit.MechSkillState.Parts.Clear();
        Log(unit, $"Separate succeeded: {parts.Count} original components placed; resource ratios and movement preserved.");
        return true;
    }

    internal static void OnDefeated(BattleUnit unit)
    {
        foreach (Part part in unit.MechSkillState.Parts)
            if (part.Unit != null && part.Unit != unit) UnityEngine.Object.Destroy(part.Unit.gameObject);
        unit.MechSkillState.Parts.Clear();
    }

    public static string Describe(BattleUnit unit) => unit.Mech == null ? "No mech assigned" :
        $"Shield {unit.CurrentShieldHealth}/{unit.Mech.ShieldHealth}; Barrier={unit.Mech.Barrier}; Evasion={unit.Mech.SpecialEvasionAbility}; " +
        $"HP/EN regen={unit.Mech.HPRegeneration}/{unit.Mech.ENRegeneration}; Mode={unit.MechSkillState.ActiveMode}; " +
        $"Mazin x{DamageMultiplier(unit)}; GUND steps={GUNDSteps(unit)}; " +
        $"Mobility={Mobility(unit)}, Sight={Sight(unit)}, Armor={Armor(unit)}, Move={Movement(unit)}; " +
        $"Repair={unit.Mech.RepairDevice}, Resupply={unit.Mech.ResupplyDevice}, parts={unit.MechSkillState.Parts.Count}, crew={unit.MechSkillState.Crew.Count}.";
}