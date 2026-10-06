using System.Collections.Generic;
using UnityEngine;

// WEEK 5 GETTER: One BattleUnit owns this runtime crew and form inventory; forms are existing MechBase assets.
// HP, EN, morale, position, actions and temporary effects remain on that same BattleUnit.
// Each pilot keeps their own SP, and each form keeps its own weapon ammo without changing shared assets.
public sealed class GetterUnit
{
    private BattleUnit unit;
    private readonly Dictionary<PilotBase, int> spiritPoints = new();
    private readonly Dictionary<MechBase, Dictionary<Weapon, int>> formAmmo = new();

    internal void Initialize(BattleUnit owner)
    {
        unit = owner;
        spiritPoints.Clear(); formAmmo.Clear();
        foreach (PilotBase pilot in unit.SpiritCrew) SetSpiritPoints(pilot, pilot.MaxSpiritPoints);
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER setup failures previously used a generic message hidden by debug switches.
        // Ordinary Console logs now identify the missing assignment when the crew initializes, so another project can find its setup problem.
        // These messages only inspect existing data and leave the working form and crew validation unchanged.
        if (!IsGetterForm(unit.Mech))
        {
            if (unit.Mech != null && (unit.Mech.GetterForms.Count > 0 || unit.Mech.MainGetterPilot != null))
                Debug.LogWarning("[WEEK 5 GETTER] " + SetupProblem(), unit);
            return;
        }
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER's separate machines have a compatibility ID but intentionally have no three-form crew yet.
        // Recognize that saved combination setup in diagnostic output so valid Eagle, Jaguar and Bear components are not reported as missing forms.
        // They still return before main-pilot selection as before, and Combine remains responsible for assembling the actual Getter crew.
        if (unit.Mech.GetterForms.Count == 0 && unit.Mech.MainGetterPilot == null && IsGetterForm(unit.Mech.CombineInto))
        {
            Debug.Log("[WEEK 5 GETTER] " + SetupProblem(), unit);
            return;
        }
        if (Forms() == null)
        {
            Debug.LogWarning("[WEEK 5 GETTER] Setup incomplete: " + SetupProblem(), unit);
            return;
        }
        unit.Pilot = unit.Mech.MainGetterPilot;
        Debug.Log("[WEEK 5 GETTER] Initialized one deployed unit: " + Describe(), unit);
    }

    public int GetSpiritPoints(PilotBase pilot) => pilot != null && spiritPoints.TryGetValue(pilot, out int sp) ? sp : 0;
    internal void SetSpiritPoints(PilotBase pilot, int value)
    {
        if (pilot != null) spiritPoints[pilot] = Mathf.Clamp(value, 0, pilot.MaxSpiritPoints);
    }

    internal static bool IsGetterForm(MechBase form) => form != null && !string.IsNullOrWhiteSpace(form.GetterCompatibilityId);
    private bool Compatible(MechBase form)
    {
        if (!IsGetterForm(form) || form.GetterCompatibilityId != unit.Mech.GetterCompatibilityId ||
            form.RequiredGetterPilots != 3 || form.RequiredGetterCrew.Count != 3 || form.MainGetterPilot == null) return false;
        HashSet<PilotBase> recipe = new(form.RequiredGetterCrew);
        return recipe.Count == 3 && !recipe.Contains(null) && recipe.Contains(form.MainGetterPilot) && recipe.SetEquals(unit.SpiritCrew);
    }

    // WEEK 5 GETTER: Reuse the saved compatibility ID, crew recipe and form links instead of adding another asset format.
    private List<MechBase> Forms()
    {
        if (unit == null || !Compatible(unit.Mech)) return null;
        List<MechBase> forms = new() { unit.Mech };
        foreach (MechBase form in unit.Mech.GetterForms)
        {
            if (!Compatible(form) || forms.Contains(form)) return null;
            forms.Add(form);
        }
        if (forms.Count != 3) return null;
        HashSet<PilotBase> mains = new();
        foreach (MechBase form in forms)
        {
            if (!mains.Add(form.MainGetterPilot) || form.GetterForms.Count != 2) return null;
            HashSet<MechBase> links = new(form.GetterForms);
            if (links.Count != 2 || links.Contains(form)) return null;
            foreach (MechBase linked in links) if (!forms.Contains(linked)) return null;
        }
        return forms;
    }

    public bool CanChangeForm(MechBase form)
    {
        List<MechBase> forms = Forms();
        return forms != null && form != unit.Mech && forms.Contains(form) && !unit.IsDefeated &&
            !unit.IsDocked && !unit.IsCombinedComponent && unit.Battlefield != null &&
            unit.Battlefield.GetUnit(unit.GridPosition) == unit;
    }

    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER's availability checks run repeatedly and must remain free of Console spam.
    // This separate explanation names missing pilots, form links and battle placement only at setup or an attempted command.
    // It reads the same requirements without changing eligibility, assigning assets or repairing anything automatically.
    private string SetupProblem()
    {
        if (unit == null) return "Getter crew has not initialized; start the battle first.";
        if (unit.Mech == null) return "BattleUnit is missing its Mech Base assignment.";
        if (!IsGetterForm(unit.Mech)) return unit.Mech.MechName + " is missing its Getter Compatibility Id.";
        if (unit.Mech.GetterForms.Count == 0 && unit.Mech.MainGetterPilot == null && IsGetterForm(unit.Mech.CombineInto))
            return unit.Mech.MechName + " is a Getter component; use Combine to enter " + unit.Mech.CombineInto.MechName + " before changing forms.";
        if (unit.Mech.GetterForms.Count != 2) return unit.Mech.MechName + " needs the other two forms in Getter Forms.";
        List<MechBase> forms = new() { unit.Mech };
        foreach (MechBase form in unit.Mech.GetterForms)
        {
            if (form == null) return unit.Mech.MechName + " has an unassigned entry in Getter Forms.";
            if (forms.Contains(form)) return unit.Mech.MechName + " has a duplicate or self-reference in Getter Forms.";
            forms.Add(form);
        }
        HashSet<PilotBase> mains = new();
        HashSet<PilotBase> crew = new(unit.SpiritCrew);
        foreach (MechBase form in forms)
        {
            string name = form.MechName;
            if (!IsGetterForm(form)) return name + " is missing its Getter Compatibility Id.";
            if (form.GetterCompatibilityId != unit.Mech.GetterCompatibilityId) return name + " has a different Getter Compatibility Id.";
            if (form.RequiredGetterPilots != 3) return name + " must have Required Getter Pilots set to 3.";
            if (form.MainGetterPilot == null) return name + " is missing its Main Getter Pilot.";
            if (form.RequiredGetterCrew.Count != 3) return name + " needs three entries in Required Getter Crew.";
            HashSet<PilotBase> recipe = new(form.RequiredGetterCrew);
            if (recipe.Contains(null)) return name + " has an unassigned entry in Required Getter Crew.";
            if (recipe.Count != 3) return name + " has duplicate pilots in Required Getter Crew.";
            if (!recipe.Contains(form.MainGetterPilot)) return name + "'s Main Getter Pilot is not in its Required Getter Crew.";
            foreach (PilotBase pilot in recipe)
                if (!crew.Contains(pilot))
                    return name + " requires " + pilot.PilotName + "; that exact pilot asset was not found in BattleUnit's Pilot / Additional Pilots.";
            if (!recipe.SetEquals(unit.SpiritCrew)) return name + "'s Required Getter Crew does not match the deployed unit's three pilot assets.";
            if (!mains.Add(form.MainGetterPilot)) return name + " shares its Main Getter Pilot with another form; each form needs a different main pilot.";
            if (form.GetterForms.Count != 2) return name + " needs the other two forms in Getter Forms.";
            HashSet<MechBase> links = new(form.GetterForms);
            if (links.Contains(null)) return name + " has an unassigned entry in Getter Forms.";
            if (links.Count != 2 || links.Contains(form)) return name + " has a duplicate or self-reference in Getter Forms.";
            foreach (MechBase linked in links)
                if (!forms.Contains(linked)) return name + " links to a Getter Form outside this three-form set.";
        }
        return null;
    }

    internal void ReportChangeFailure(MechBase requestedForm = null)
    {
        string problem = SetupProblem();
        if (problem == null)
        {
            if (unit.IsDefeated) problem = "The Getter is defeated.";
            else if (unit.IsDocked) problem = "The Getter is docked; deploy it from the ship first.";
            else if (unit.IsCombinedComponent) problem = "This unit is a hidden combination component.";
            else if (unit.Pilot == null) problem = "The Getter is missing its current Pilot assignment.";
            else if (unit.Battlefield == null) problem = "The Getter has no Battlefield; place it under the active Battle Field.";
            else if (unit.Battlefield.GetUnit(unit.GridPosition) != unit) problem = "The Getter cannot be found at its grid position in the Battlefield.";
            else if (requestedForm == unit.Mech) problem = "The requested form is already the current form.";
            else problem = "The requested form is not one of this Getter's linked forms, or the unit is unavailable.";
        }
        Debug.LogWarning("[WEEK 5 GETTER] Cannot change form: " + problem, unit);
    }

    // WEEK 5 GETTER: Changing forms replaces the main pilot and form data, never the battlefield object or spent actions.
    public bool TryChangeForm(MechBase form)
    {
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER form-change attempts previously returned only a gated generic failure log.
        // A rejected attempt now writes its specific reason, and a successful change writes the form, main pilot and preserved battle state directly to the Console.
        // This changes diagnostic output only; resource handling, pilot switching and spent actions still use the existing code below.
        if (!CanChangeForm(form)) { ReportChangeFailure(form); return false; }
        SaveAmmo();
        MechBase previous = unit.Mech;
        unit.Mech = form; unit.Pilot = form.MainGetterPilot;
        unit.CurrentHealth = Mathf.Min(unit.CurrentHealth, form.Health);
        unit.CurrentEnergy = Mathf.Min(unit.CurrentEnergy, form.Energy);
        unit.MechSkillState.Shield = Mathf.FloorToInt(form.ShieldHealth * unit.MechSkillState.ShieldFraction);
        LoadAmmo(form);
        unit.AbilitySprite.sprite = form.BattleSprite;
        MechSkillEffect.RefreshMorale(unit);
        Debug.Log($"[WEEK 5 GETTER] {previous.MechName}->{form.MechName}; {Describe()}; HP={unit.CurrentHealth}, EN={unit.CurrentEnergy}, morale={unit.CurrentMorale}, cell={unit.GridPosition}, moved={unit.HasMoved}, acted={unit.HasActed}.", unit);
        return true;
    }

    private int Capacity(MechBase form, Weapon weapon) => PilotSkillEffects.AmmoCapacity(form.MainGetterPilot, weapon);
    private Dictionary<Weapon, int> Ammo(MechBase form)
    {
        if (!formAmmo.TryGetValue(form, out Dictionary<Weapon, int> ammo))
        {
            ammo = new(); formAmmo[form] = ammo;
            foreach (Weapon weapon in form.Weapons) if (weapon != null) ammo[weapon] = Capacity(form, weapon);
        }
        return ammo;
    }
    private void SaveAmmo()
    {
        Dictionary<Weapon, int> ammo = Ammo(unit.Mech);
        foreach (Weapon weapon in unit.Mech.Weapons) if (weapon != null) ammo[weapon] = unit.GetAmmo(weapon);
    }
    private void LoadAmmo(MechBase form)
    {
        Dictionary<Weapon, int> ammo = Ammo(form);
        foreach (Weapon weapon in form.Weapons)
            if (weapon != null) unit.AbilityAmmo[weapon] = Mathf.Clamp(ammo[weapon], 0, Capacity(form, weapon));
    }

    // WEEK 5 GETTER: Resupply and docking refill all three form inventories, including forms that have not been selected yet.
    internal bool RestoreAmmo()
    {
        List<MechBase> forms = Forms();
        if (forms == null) return false;
        foreach (MechBase form in forms)
        {
            Dictionary<Weapon, int> ammo = Ammo(form);
            foreach (Weapon weapon in form.Weapons) if (weapon != null) ammo[weapon] = Capacity(form, weapon);
        }
        LoadAmmo(unit.Mech);
        Log("Ammo resupplied for all three forms.");
        return true;
    }
    internal bool NeedsAmmo()
    {
        List<MechBase> forms = Forms();
        if (forms == null) return false;
        SaveAmmo();
        foreach (MechBase form in forms)
            foreach (Weapon weapon in form.Weapons)
                if (weapon != null && weapon.MaxAmmo > 0 && Ammo(form)[weapon] < Capacity(form, weapon)) return true;
        return false;
    }

    // WEEK 5 GETTER: Optional in-battle Combine/Separate transfers actual pilot SP rather than merging it into one pool.
    internal Dictionary<PilotBase, int> CopySpiritPoints() => new(spiritPoints);
    internal static Dictionary<PilotBase, int> ComponentSpiritPoints(List<BattleUnit> parts)
    {
        Dictionary<PilotBase, int> points = new();
        foreach (BattleUnit part in parts)
            foreach (PilotBase pilot in part.SpiritCrew)
            {
                int sp = part.GetterState.GetSpiritPoints(pilot);
                points[pilot] = points.TryGetValue(pilot, out int saved) ? Mathf.Min(saved, sp) : sp;
            }
        return points;
    }
    internal void ApplySpiritPoints(Dictionary<PilotBase, int> points)
    {
        foreach (PilotBase pilot in unit.SpiritCrew)
            if (points.TryGetValue(pilot, out int sp)) SetSpiritPoints(pilot, sp);
    }
    internal void EnterCombinedForm(float fraction)
    {
        List<MechBase> forms = Forms();
        if (forms == null) return;
        SaveAmmo();
        foreach (MechBase form in forms)
        {
            Dictionary<Weapon, int> ammo = Ammo(form);
            foreach (Weapon weapon in form.Weapons)
                if (weapon != null) ammo[weapon] = Mathf.Min(ammo[weapon], Mathf.FloorToInt(Capacity(form, weapon) * fraction));
        }
        LoadAmmo(unit.Mech);
        Log("Combined crew retains individual SP; form ammo uses the component supply limit.");
    }

    public string Describe()
    {
        if (unit == null) return "Crew initializes when battle starts.";
        List<string> crew = new();
        foreach (PilotBase pilot in unit.SpiritCrew)
            crew.Add($"{pilot.PilotName}{(pilot == unit.Pilot ? " (main)" : " (sub)")} SP={GetSpiritPoints(pilot)}/{pilot.MaxSpiritPoints}");
        return string.Join("; ", crew);
    }
    internal void Log(string message)
    {
        if (unit != null) MechSkillEffect.Log(unit, "[WEEK 5 GETTER] " + message);
    }
}