using System.Collections.Generic;
using UnityEngine;

public enum BattleTeam
{
    Player,
    Enemy
}

[RequireComponent(typeof(SpriteRenderer))]
public class BattleUnit : MonoBehaviour
{
    [SerializeField] private PilotBase pilot;
    // WEEK 5 CHANGES PLEASE READ: Getter forms need their saved crew and each unit needs independent shield/mode state.
    // These small access points let MechSkillEffect manage form changes without editing shared mech or pilot assets.
    // Public callers still read the ordinary unit properties, while only code in this assembly can update transition resources.
    [SerializeField] private List<PilotBase> additionalPilots = new();
    internal MechSkillEffect.State MechSkillState { get; } = new();
    internal IReadOnlyList<PilotBase> AdditionalPilots => additionalPilots;
    internal Dictionary<Weapon, int> AbilityAmmo => ammunition;
    internal SpriteRenderer AbilitySprite => spriteRenderer;
    public int CurrentShieldHealth => MechSkillState.Shield;
    public bool IsCombinedComponent => MechSkillState.CombinedInto != null;
    public int GetWeaponPower(Weapon weapon) => MechSkillEffect.WeaponPower(this, weapon);
    [ContextMenu("WEEK 5: Log Mech Skills")]
    public void LogMechSkills() => MechSkillEffect.Log(this, MechSkillEffect.Describe(this));
    internal void CurrentMoraleForAbility(int value)
    {
        CurrentMorale = Mathf.Clamp(value, MinimumMorale, MaximumMorale);
        MechSkillEffect.RefreshMorale(this);
    }
    // WEEK 3: Assign the mech asset and the terrain this unit is currently using.
    [SerializeField] private MechBase mech;
    [SerializeField] private TerrainType terrain = TerrainType.Ground;
    [SerializeField] private BattleTeam team;
    [SerializeField] private TileTypes tileType = TileTypes.Ground;
    [SerializeField] private Vector2Int startingPosition;

    // WEEK 4 MORALE SYSTEM: Every unit starts at 100 morale. Abilities and equipment
    // can raise the normal 150 maximum by up to 50, for an absolute max of 200.
    [Header("Morale")]
    [Range(0, 50)][SerializeField] private int moraleMaximumBonus;

    private Battlefield battlefield;
    private SpriteRenderer spriteRenderer;
    private Color normalColor = Color.white;

    // WEEK 3: Track the unit's temporary visual state so selection,
    // enemy AI actions, and completed actions can use different colors.
    private bool isSelected;
    private bool isActing;

    // WEEK 4: Runtime Spirit Command state.
    private bool valorActive;
    private bool soulActive;
    private bool smashActive;
    private bool accelActive;

    // WEEK 4: Allow the battle system to check active Spirit effects.
    public bool ValorActive => valorActive;
    public bool SoulActive => soulActive;
    public bool SmashActive => smashActive;
    public bool AccelActive => accelActive;

    // WEEK 4: MOTHERSHIP - Keep runtime state on this unit, never on shared mech assets.
    private readonly Dictionary<Weapon, int> ammunition = new();
    public Mothership DockedAt { get; private set; }
    public bool IsDocked => DockedAt != null;
    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: A Hangar selection needs to display its passenger before that passenger leaves the ship.
    // These fields remember only the sprite's temporary preview sorting state so it can appear above the carrier.
    // Movement, actions and dock ownership remain unchanged until the existing deployment routine places the unit.
    private bool showingDockedPreview;
    private int dockedPreviewSortingOrder;
    public Battlefield Battlefield => battlefield;

    public PilotBase Pilot { get => pilot; internal set => pilot = value; }
    // WEEK 3: Expose mech settings and keep energy and movement state on this individual unit.
    public MechBase Mech { get => mech; internal set => mech = value; }
    public TerrainType Terrain => terrain;

    // Allow the terrain detector to update this unit's terrain type when it moves to a new grid cell.
    public void SetTerrain(TerrainType newTerrain)
    {
        terrain = newTerrain;
    }

    public TileTypes TileType => tileType;
    public void SetTileType(TileTypes newTileType)
    {
        tileType = newTileType;
    }

    public int CurrentEnergy { get; internal set; }

    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER previously shared one SP number between whichever pilot led the form.
    // GetterUnit now stores each crew member's own SP, while CurrentSpiritPoints continues to mean the current main pilot's SP.
    // The existing battle unit still owns shared resources and actions, so changing the main pilot cannot refill or transfer another pilot's SP.
    internal GetterUnit GetterState { get; } = new();
    public IReadOnlyList<PilotBase> SpiritCrew => MechSkillState.Crew;
    public int CurrentSpiritPoints { get => GetterState.GetSpiritPoints(pilot); internal set => GetterState.SetSpiritPoints(pilot, value); }
    public int GetSpiritPoints(PilotBase crewPilot) => GetterState.GetSpiritPoints(crewPilot);
    public bool HasSpiritPilot(PilotBase crewPilot) => crewPilot != null && MechSkillState.Crew.Contains(crewPilot);
    [ContextMenu("WEEK 5 GETTER: Log Crew")]
    public void LogGetterCrew() => GetterState.Log(GetterState.Describe());
    public bool HasMoved { get; internal set; }

    // WEEK 3: Each grid step costs one energy. Stop movement after this unit has already moved.

    // WEEK 3: Track whether this unit has completed its full action
    // during the current phase.
    public bool HasActed { get; internal set; }

    // WEEK 4: Accel adds 3 spaces to the unit's next movement.
    // WEEK 5 CHANGES PLEASE READ: Movement previously read only the raw mech asset and Accel bonus.
    // GUND needs its current morale bonus included when reachability is calculated.
    // Delegating the base movement to MechSkillEffect keeps the existing EN cap and once-per-phase movement rules.
    public int AvailableMovement =>
        mech != null && !HasMoved && !IsDocked && !HasActed && !IsCombinedComponent
            ? Mathf.Min(MechSkillEffect.Movement(this) + (accelActive ? 3 : 0), CurrentEnergy)
            : 0;

    // WEEK 3: Allow movement at the start of a turn, then remember when it has been used.

    // WEEK 3: Reset movement and action availability when this unit's
    // new phase begins, then restore its original visual state.
    public void BeginTurn()
    {
        // WEEK 5 CHANGES PLEASE READ: BeginTurn previously reset actions without applying any mech phase effects.
        // Hidden combined components must not receive separate turns, and HP/EN regeneration must run for the unit's own team phase.
        // Calling the shared effect here also covers docked passengers through the existing ship BeginPassengerTurn callback.
        if (IsCombinedComponent) return;
        MechSkillEffect.BeginPhase(this);
        HasMoved = false;
        HasActed = false;
        isSelected = false;
        isActing = false;
        RefreshVisualState();
        // WEEK 4: MOTHERSHIP - Passengers are off-grid, so their ship updates them each phase.
        if (TryGetComponent(out Mothership ship)) ship.BeginPassengerTurn();
    }
    public void MarkMoved() => HasMoved = true;
    public BattleTeam Team => team;
    // WEEK 4 UNIT IDENTITY: This serialized cell is the authoritative spawn
    // location. Runtime movement changes GridPosition, never this value.
    public Vector2Int StartingPosition => startingPosition;
    public Vector2Int GridPosition { get; private set; }
    public int CurrentHealth { get; internal set; }

    // WEEK 4 MORALE SYSTEM: Keep mutable morale on the battle unit, not the shared PilotBase asset.
    public const int MinimumMorale = 50;
    public const int StartingMorale = 100;
    public const int BaseMaximumMorale = 150;
    public const int AbsoluteMaximumMorale = 200;
    public int CurrentMorale { get; private set; } = StartingMorale;
    public int MaximumMorale => Mathf.Clamp(BaseMaximumMorale + moraleMaximumBonus,
        BaseMaximumMorale, AbsoluteMaximumMorale);

    public bool IsDefeated => CurrentHealth <= 0;
    private void Awake()
    {
        // WEEK 3: Cache the SpriteRenderer and remember its normal color.
        // The Unit Selection System uses these values to visually distinguish
        // the currently selected player unit without permanently changing its appearance.
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalColor = spriteRenderer.color;

        // WEEK 3: Initialize the mech's current battle health and energy
        // using the newer MechBase data used by the battle system.
        CurrentHealth = mech != null ? mech.Health : 1;
        CurrentEnergy = mech != null ? mech.Energy : 0;

        // WEEK 4: Start battle with the pilot's maximum Spirit Points.
        CurrentSpiritPoints = pilot != null ? pilot.MaxSpiritPoints : 0;
        // WEEK 4 MORALE SYSTEM: Reset morale to its required battle starting value.
        CurrentMorale = Mathf.Clamp(StartingMorale, MinimumMorale, MaximumMorale);
        // WEEK 5 CHANGES PLEASE READ: Awake previously initialized only ordinary HP/EN/ammo resources.
        // Shields, morale modes and the saved Getter crew require their own state before battle starts.
        // Initializing the effect state here gives every deployed unit independent values without changing its shared assets.
        MechSkillEffect.Initialize(this);
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER needs all assigned pilots initialized before the first form's ammo is restored.
        // The separate GetterUnit state reads the existing crew and validates the saved three-form configuration here.
        // This also selects the configured main pilot when starting directly in Getter 2 or Getter 3 without spawning additional units.
        GetterState.Initialize(this);
        RestoreAmmo();

        if (mech != null)
        {
            // WEEK 3: Use the mech's battle sprite while keeping the cached
            // SpriteRenderer available for selected/unselected visual feedback.
            spriteRenderer.sprite = mech.BattleSprite;
        }
        else
        {
            Debug.LogError("BattleUnit needs a MechBase asset.", this);
        }
    }

    // WEEK 3: Selection always has visual priority so even a unit that
    // already acted can still turn yellow when the player clicks it.
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshVisualState();
    }

    // WEEK 3: Highlight an enemy blue while its AI action is actively
    // being processed. When finished, it can return to grey.
    public void SetActing(bool acting)
    {
        isActing = acting;
        RefreshVisualState();
    }

    // WEEK 3: Mark this unit as finished for the current phase.
    // Finished units appear grey unless currently selected or acting.
    public void MarkActed()
    {
        HasActed = true;
        RefreshVisualState();
    }

    // WEEK 3: Restore the unit's normal appearance without changing
    // its logical action state. Phase management will use this when needed.
    public void RestoreNormalColor()
    {
        isSelected = false;
        isActing = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }

    // WEEK 3: Visual priority is Selected > Acting > Acted > Normal.
    private void RefreshVisualState()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (isSelected)
        {
            spriteRenderer.color = Color.yellow;
        }
        else if (isActing)
        {
            spriteRenderer.color = Color.blue;
        }
        else if (HasActed)
        {
            spriteRenderer.color = Color.grey;
        }
        else
        {
            spriteRenderer.color = normalColor;
        }
    }
    public bool PlaceOn(Battlefield targetBattlefield)
    {
        battlefield = targetBattlefield;
        return battlefield != null && battlefield.TryPlace(this, startingPosition);
    }

    public void SetGridPosition(Vector2Int position)
    {
        GridPosition = position;
        transform.position = battlefield.GridToWorld(position);
    }

    // WEEK 3: Spend energy only when the unit is alive and can afford the full cost.
    public bool TrySpendEnergy(int amount)
    {
        if (amount < 0 || amount > CurrentEnergy || IsDefeated || IsDocked) return false;
        CurrentEnergy -= amount;
        return true;
    }

    // WEEK 3: Undo an unconfirmed move by restoring its energy and movement allowance.
    public void RestoreMove(int energyBeforeMove)
    {
        CurrentEnergy = Mathf.Clamp(energyBeforeMove, 0, mech.Energy);
        HasMoved = false;
    }

    // WEEK 3: Use the exact selected weapon, checking ownership, energy and target range.
    public bool CanUseWeapon(Weapon weapon, BattleUnit target, bool counterattack = false)
    {
        if (weapon == null || mech == null || pilot == null || IsDefeated || IsDocked ||
            target == null || !target.CanBeTargetedBy(this) || !CanAffordWeapon(weapon))
            return false;

        // WEEK 5 FIXES: Originally, moving did not prevent this unit from initiating an attack with an untagged weapon.
        // We now require PostMovement after movement, while defensive first strikes ignore the defender's movement state.
        // Limiting defensive weapons to single targets preserves that exception without allowing area attacks as counters.
        if (counterattack ? weapon.Classification != WeaponClassification.SingleTarget :
            HasMoved && !weapon.CanUseAfterMoving) return false;

        bool owned = false;
        foreach (Weapon equipped in mech.Weapons)
            if (equipped == weapon) { owned = true; break; }
        return owned && weapon.IsInTargetRange(GridPosition, target.GridPosition);
    }

    // WEEK 3: Pick the first weapon that can reach the enemy and has enough energy.
    public Weapon GetUsableWeapon(BattleUnit target, WeaponType requiredType = WeaponType.None, bool counterattack = false)
    {
        if (mech == null || pilot == null || IsDefeated || IsDocked || target == null ||
            !target.CanBeTargetedBy(this))
            return null;

        // WEEK 5 FIXES: Originally, automatic weapon selection had its own distance/resource checks and allowed every weapon after moving.
        // It now calls CanUseWeapon so the menu, enemy AI and confirmed attacks apply the same movement and MAP shape rules.
        // This keeps weapon availability consistent while retaining the existing required-tag filter and selection order.
        foreach (Weapon weapon in mech.Weapons)
        {
            if (weapon != null &&
                CanUseWeapon(weapon, target, counterattack) &&
                (weapon.Type & requiredType) == requiredType)
                return weapon;
        }
        return null;
    }

    // WEEK 3: Check that an extra adjacent target is a living enemy with pilot and mech data.
    // WEEK 5 CHANGES PLEASE READ: Combined components remain actual unit objects so Separate can restore them.
    // Their hidden objects must not attack or become independent combat targets while part of another mech.
    // This guard keeps the ordinary team/health checks and rejects both a hidden target and a hidden attacker.
    public bool CanBeTargetedBy(BattleUnit attacker)
    {
        return attacker != null && !attacker.IsDocked && !attacker.IsCombinedComponent && !IsCombinedComponent && Team != attacker.Team && !IsDefeated && !IsDocked && mech != null && pilot != null;
    }

    // WEEK 4: MOTHERSHIP - Pay EN and one round together, once per attack, even on a miss.
    // WEEK 4: PILOT SKILLS - All UI, AI and resource spending share these effective values.
    public int GetAmmoCapacity(Weapon weapon) => PilotSkillEffects.AmmoCapacity(pilot, weapon);
    public int GetWeaponEnergyCost(Weapon weapon) => PilotSkillEffects.EnergyCost(pilot, weapon);
    public int PotentialStage => PilotSkillEffects.PotentialStage(pilot != null ? pilot.PotentialLevel : 0,
        CurrentHealth, mech != null ? mech.Health : 0);

    public int GetAmmo(Weapon weapon) => weapon != null && ammunition.TryGetValue(weapon, out int count) ? count : 0;

    // WEEK 5 CHANGES PLEASE READ: Resource availability previously allowed every equipped move regardless of its mode requirement.
    // Hyper/Super-only moves must stay locked until their mode is active, including defensive first strikes.
    // This shared guard also prevents hidden combination parts firing, while retaining the existing ammo and EN checks.
    // WEEK 5 CHANGES PLEASE READ: Weapon availability previously checked mode, EN and ammo without an individual morale threshold.
    // The shared check must also require the weapon's minimum morale so menus, enemy selection and defensive attacks follow the same rule.
    // Resource spending uses this guard too, preventing locked attacks from consuming EN or ammo while leaving skill activation rules intact.
    public bool CanAffordWeapon(Weapon weapon) => weapon != null && ammunition.ContainsKey(weapon) &&
        !IsDefeated && !IsDocked && !IsCombinedComponent && CurrentMorale >= weapon.RequiredMorale &&
        MechSkillEffect.IsWeaponUnlocked(this, weapon) && GetWeaponEnergyCost(weapon) <= CurrentEnergy &&
        (weapon.MaxAmmo == 0 || GetAmmo(weapon) > 0);

    public bool TrySpendWeapon(Weapon weapon)
    {
        if (!CanAffordWeapon(weapon) || !TrySpendEnergy(GetWeaponEnergyCost(weapon))) return false;
        if (weapon.MaxAmmo > 0) ammunition[weapon]--;
        if (pilot != null && pilot.SaveELevel > 0 && weapon.EnergyCost > 0)
            PilotSkillEffects.Log(this, $"Save E L{pilot.SaveELevel}: {weapon.WeaponName} EN {weapon.EnergyCost} -> {GetWeaponEnergyCost(weapon)}");
        return true;
    }

    public void RestoreAmmo()
    {
        if (mech == null) return;
        // WEEK 5 CHANGES PLEASE READ: Docking already restores ammo, but shields were not included in that recovery path.
        // Shield Equipment is resupplied like ammo, so the same callback now restores its independent HP pool.
        // This connects docking, device resupply and Spirit resupply without duplicating shield recovery in each system.
        MechSkillEffect.RestoreShield(this);
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER keeps independent ammo inventories for its three forms.
        // Restoring only the currently equipped weapons would leave the other forms empty even after docking or resupply.
        // This callback refills the Getter inventories together while retaining the existing single-form recovery for ordinary mechs.
        if (GetterState.RestoreAmmo()) return;
        foreach (Weapon weapon in mech.Weapons)
        {
            if (weapon == null) continue;
            ammunition[weapon] = GetAmmoCapacity(weapon);
            if (pilot != null && pilot.SaveBLevel > 0 && weapon.MaxAmmo > 0)
                PilotSkillEffects.Log(this, $"Save B L{pilot.SaveBLevel}: {weapon.WeaponName} ammo {weapon.MaxAmmo} -> {GetAmmoCapacity(weapon)} (refilled)");
        }
    }

    // WEEK 4: MOTHERSHIP - Clamp dock recovery to this mech's maximum stats.
    public void RestoreHealthAndEnergy(int health, int energy)
    {
        if (mech == null || IsDefeated) return;
        int previousPotential = PotentialStage;
        CurrentHealth = Mathf.Clamp(CurrentHealth + Mathf.Max(0, health), 0, mech.Health);
        LogPotentialChange(previousPotential);
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + Mathf.Max(0, energy), 0, mech.Energy);
    }

    internal void EnterDock(Mothership ship)
    {
        battlefield.Remove(this);
        DockedAt = ship;
        SetSelected(false);
        MarkActed();
        RestoreAmmo();
        spriteRenderer.enabled = false;
    }

    internal void LeaveDock()
    {
        DockedAt = null;
        spriteRenderer.enabled = true;
        RefreshVisualState();
    }

    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Hangar deployment previously hid the passenger until an exit cell was clicked.
    // This visual preview shows the stored unit slightly above its carrier and restores its original sprite order when the preview ends.
    // It never occupies the carrier's grid cell or resets the passenger's spent movement and action flags.
    internal void ShowDockedPreview(bool visible)
    {
        if (spriteRenderer == null) return;
        if (visible && IsDocked)
        {
            if (!showingDockedPreview) dockedPreviewSortingOrder = spriteRenderer.sortingOrder;
            showingDockedPreview = true;
            spriteRenderer.enabled = true;
            SpriteRenderer carrierSprite = DockedAt.Unit.AbilitySprite;
            spriteRenderer.sortingOrder = carrierSprite != null ? Mathf.Max(dockedPreviewSortingOrder, carrierSprite.sortingOrder + 1) : dockedPreviewSortingOrder;
            transform.position = DockedAt.Unit.transform.position + Vector3.up * 0.25f;
        }
        else
        {
            if (showingDockedPreview) spriteRenderer.sortingOrder = dockedPreviewSortingOrder;
            showingDockedPreview = false;
            spriteRenderer.enabled = !IsDocked;
            if (battlefield != null) transform.position = battlefield.GridToWorld(GridPosition);
        }
    }

    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Cancelling an unconfirmed launch must return the unit aboard without treating it as new boarding.
    // This restores dock ownership and hides the sprite after removing only this passenger's temporary map occupancy.
    // It deliberately avoids MarkActed and ammo restoration so cancellation does not spend an action or refill ammunition.
    internal void ReturnDeploymentToDock(Mothership ship)
    {
        battlefield.Remove(this);
        DockedAt = ship;
        SetGridPosition(ship.Unit.GridPosition);
        SetSelected(false);
        ShowDockedPreview(false);
    }

    // WEEK 4: MOTHERSHIP - Use the strongest friendly aura without modifying shared pilot data.
    public int CommanderBonus
    {
        get
        {
            int bonus = 0;
            if (battlefield == null || IsDocked || IsDefeated) return bonus;
            foreach (BattleUnit unit in battlefield.Units)
                if (unit != null && unit.TryGetComponent(out Mothership ship))
                    bonus = Mathf.Max(bonus, ship.GetCommanderBonus(this));
            return bonus;
        }
    }

    // WEEK 4: PILOT SKILLS - Log only when damage/healing changes the bonus, never each frame or preview.
    private void LogPotentialChange(int previousStage)
    {
        int stage = PotentialStage;
        if (stage == previousStage || IsDefeated) return;
        PilotSkillEffects.Log(this, stage == 0 ? "Potential inactive after healing." :
            $"Potential L{pilot.PotentialLevel}: hit/dodge +{PilotSkillEffects.HitDodgeBonus(stage)}, " +
            $"crit +{PilotSkillEffects.CriticalBonus(stage)}, damage taken -{PilotSkillEffects.DamageReductionPercent(stage)}% (HP {CurrentHealth}/{mech.Health})");
    }

    // WEEK 4: Spend Spirit Points only when the pilot can afford the command.
    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER sub-pilots must spend their own SP without becoming the combat pilot.
    // The optional pilot argument preserves existing callers and allows SpiritSystem to charge the selected crew member.
    // Crew membership and affordability are checked before any points are spent, so invalid pilot selections cannot consume another pilot's pool.
    public bool TrySpendSpiritPoints(int amount, PilotBase crewPilot = null)
    {
        crewPilot ??= pilot;
        if (!HasSpiritPilot(crewPilot) || amount < 0 || amount > GetSpiritPoints(crewPilot) || IsDefeated)
        {
            return false;
        }

        int before = GetSpiritPoints(crewPilot);
        GetterState.SetSpiritPoints(crewPilot, before - amount);
        if (SpiritCrew.Count > 1) GetterState.Log($"{crewPilot.PilotName} spent {amount} SP: {before}->{GetSpiritPoints(crewPilot)}; main pilot remains {pilot.PilotName}.");
        return true;
    }

    // WEEK 4: Restore Spirit Points without exceeding the pilot's maximum.
    public void RestoreSpiritPoints(int amount)
    {
        if (amount <= 0 || pilot == null || IsDefeated)
        {
            return;
        }

        CurrentSpiritPoints =
            Mathf.Clamp(CurrentSpiritPoints + amount, 0, pilot.MaxSpiritPoints);
    }

    // WEEK 4: Restore HP without exceeding the mech's maximum Health.
    public void RestoreHealth(int amount)
    {
        if (amount <= 0 || mech == null || IsDefeated)
        {
            return;
        }

        int previousPotential = PotentialStage;

        CurrentHealth =
            Mathf.Clamp(CurrentHealth + amount, 0, mech.Health);

        // WEEK 4: Keep the teammate's Potential system synchronized after healing.
        LogPotentialChange(previousPotential);
    }

    // WEEK 4: Compatibility helpers for Spirit Commands.
    // Morale remains controlled by the teammate's centralized ChangeMorale method.
    public void AddMorale(int amount)
    {
        if (amount > 0 && !IsDefeated)
        {
            ChangeMorale(amount);
        }
    }

    public void ReduceMorale(int amount)
    {
        if (amount > 0 && !IsDefeated)
        {
            ChangeMorale(-amount);
        }
    }

    // WEEK 4: Reduce SP without allowing it to fall below zero.
    public void ReduceSpiritPoints(int amount)
    {
        if (amount <= 0 || IsDefeated)
        {
            return;
        }

        CurrentSpiritPoints =
            Mathf.Max(0, CurrentSpiritPoints - amount);
    }

    // WEEK 4: Valor makes the next attack deal 2X damage.
    public void ActivateValor()
    {
        valorActive = true;
    }

    public void ConsumeValor()
    {
        valorActive = false;
    }

    // WEEK 4: Soul makes the next attack deal 2.5X damage.
    public void ActivateSoul()
    {
        soulActive = true;
    }

    public void ConsumeSoul()
    {
        soulActive = false;
    }

    // WEEK 4: Smash guarantees a critical hit on the next attack.
    public void ActivateSmash()
    {
        smashActive = true;
    }

    public void ConsumeSmash()
    {
        smashActive = false;
    }

    // WEEK 4: Accel adds 3 spaces to the unit's next movement.
    public void ActivateAccel()
    {
        accelActive = true;
    }

    public void ConsumeAccel()
    {
        accelActive = false;
    }
    public int TakeDamage(int damage)
    {
        int previousPotential = PotentialStage;
        int appliedDamage = Mathf.Min(CurrentHealth, Mathf.Max(0, damage));
        CurrentHealth -= appliedDamage;
        LogPotentialChange(previousPotential);

        if (IsDefeated)
        {
            // WEEK 5 CHANGES PLEASE READ: Defeating a combined mech previously had no hidden component objects to clean up.
            // MechSkillEffect records the actual parts for Separate, so those parts must be removed when their combined unit dies.
            // This callback prevents surviving inactive parts without changing the existing ship or battlefield cleanup.
            MechSkillEffect.OnDefeated(this);
            // WEEK 4: MOTHERSHIP - Clean up passengers with their carrier; no new loss condition.
            if (TryGetComponent(out Mothership ship)) ship.DefeatPassengers();
            // WEEK 3: Remove a defeated unit from the grid only if it has a battlefield.
            if (battlefield != null) battlefield.Remove(this);
            Destroy(gameObject);
        }

        return appliedDamage;
    }

    // WEEK 4 MORALE SYSTEM: All morale changes use one clamped entry point.
    public void ChangeMorale(int amount)
    {
        CurrentMorale = Mathf.Clamp(CurrentMorale + amount, MinimumMorale, MaximumMorale);
        // WEEK 5 CHANGES PLEASE READ: Morale previously changed without notifying any mech ability.
        // Mazin Power needs to latch at 130, while modes and GUND must update when their morale thresholds change.
        // Refreshing here makes weapon unlocks and effective stats current for both combat and Spirit morale changes.
        MechSkillEffect.RefreshMorale(this);
    }

    // WEEK 4 MORALE SYSTEM: Future abilities and equipment can raise the ceiling safely.
    public void SetMoraleMaximumBonus(int bonus)
    {
        moraleMaximumBonus = Mathf.Clamp(bonus, 0, AbsoluteMaximumMorale - BaseMaximumMorale);
        CurrentMorale = Mathf.Clamp(CurrentMorale, MinimumMorale, MaximumMorale);
        // WEEK 5 CHANGES PLEASE READ: Lowering the morale cap can also lower the unit's current morale.
        // That change must refresh mode unlocks and GUND bonuses just like ChangeMorale does.
        // This keeps effective stats consistent while preserving already-activated Mazin Power for the stage.
        MechSkillEffect.RefreshMorale(this);
    }

    public void RechargeEnergy()
    {
        int energyRecharge = Mathf.RoundToInt(Mech.Energy * 0.08f);     // this recharges the energy by 8% of the mech's maximum energy each turn
        CurrentEnergy = Mathf.Min(CurrentEnergy + energyRecharge, Mech.Energy);
    }
}