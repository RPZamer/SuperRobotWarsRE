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
    public Battlefield Battlefield => battlefield;

    public PilotBase Pilot => pilot;
    // WEEK 3: Expose mech settings and keep energy and movement state on this individual unit.
    public MechBase Mech => mech;
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

    public int CurrentEnergy { get; private set; }

    // WEEK 4: Current Spirit Points belong to this individual battle unit.
    public int CurrentSpiritPoints { get; private set; }
    public bool HasMoved { get; private set; }
    
    // WEEK 3: Each grid step costs one energy. Stop movement after this unit has already moved.

    // WEEK 3: Track whether this unit has completed its full action
    // during the current phase.
    public bool HasActed { get; private set; }
    
    // WEEK 4: Accel adds 3 spaces to the unit's next movement.
    public int AvailableMovement =>
        mech != null && !HasMoved && !IsDocked && !HasActed
            ? Mathf.Min(mech.Movement + (accelActive ? 3 : 0), CurrentEnergy)
            : 0;

    // WEEK 3: Allow movement at the start of a turn, then remember when it has been used.

    // WEEK 3: Reset movement and action availability when this unit's
    // new phase begins, then restore its original visual state.
    public void BeginTurn()
    {
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
    public int CurrentHealth { get; private set; }

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
    public bool CanUseWeapon(Weapon weapon, BattleUnit target)
    {
        if (weapon == null || mech == null || pilot == null || IsDefeated || IsDocked ||
            target == null || !target.CanBeTargetedBy(this) || !CanAffordWeapon(weapon))
            return false;

        bool owned = false;
        foreach (Weapon equipped in mech.Weapons)
            if (equipped == weapon) { owned = true; break; }
        int distance = Mathf.Abs(GridPosition.x - target.GridPosition.x)
            + Mathf.Abs(GridPosition.y - target.GridPosition.y);
        return owned && weapon.IsInRange(distance);
    }

    // WEEK 3: Pick the first weapon that can reach the enemy and has enough energy.
    public Weapon GetUsableWeapon(BattleUnit target, WeaponType requiredType = WeaponType.None)
    {
        if (mech == null || pilot == null || IsDefeated || IsDocked || target == null ||
            !target.CanBeTargetedBy(this))
            return null;

        int distance = Mathf.Abs(GridPosition.x - target.GridPosition.x)
            + Mathf.Abs(GridPosition.y - target.GridPosition.y);
        // WEEK 3: All weapons can attack after moving if their range and energy cost allow it.
        foreach (Weapon weapon in mech.Weapons)
        {
            if (weapon != null &&
                weapon.IsInRange(distance) && CanAffordWeapon(weapon) &&
                (weapon.Type & requiredType) == requiredType)
                return weapon;
        }
        return null;
    }

    // WEEK 3: Check that an extra adjacent target is a living enemy with pilot and mech data.
    public bool CanBeTargetedBy(BattleUnit attacker)
    {
        return attacker != null && !attacker.IsDocked && Team != attacker.Team && !IsDefeated && !IsDocked && mech != null && pilot != null;
    }

    // WEEK 4: MOTHERSHIP - Pay EN and one round together, once per attack, even on a miss.
    // WEEK 4: PILOT SKILLS - All UI, AI and resource spending share these effective values.
    public int GetAmmoCapacity(Weapon weapon) => PilotSkillEffects.AmmoCapacity(pilot, weapon);
    public int GetWeaponEnergyCost(Weapon weapon) => PilotSkillEffects.EnergyCost(pilot, weapon);
    public int PotentialStage => PilotSkillEffects.PotentialStage(pilot != null ? pilot.PotentialLevel : 0,
        CurrentHealth, mech != null ? mech.Health : 0);

    public int GetAmmo(Weapon weapon) => weapon != null && ammunition.TryGetValue(weapon, out int count) ? count : 0;

    public bool CanAffordWeapon(Weapon weapon) => weapon != null && ammunition.ContainsKey(weapon) &&
        !IsDefeated && !IsDocked && GetWeaponEnergyCost(weapon) <= CurrentEnergy &&
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
    public bool TrySpendSpiritPoints(int amount)
    {
        if (amount < 0 || amount > CurrentSpiritPoints || IsDefeated)
        {
            return false;
        }

        CurrentSpiritPoints -= amount;
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
    }

    // WEEK 4 MORALE SYSTEM: Future abilities and equipment can raise the ceiling safely.
    public void SetMoraleMaximumBonus(int bonus)
    {
        moraleMaximumBonus = Mathf.Clamp(bonus, 0, AbsoluteMaximumMorale - BaseMaximumMorale);
        CurrentMorale = Mathf.Clamp(CurrentMorale, MinimumMorale, MaximumMorale);
    }

    public void RechargeEnergy()
    {
        int energyRecharge = Mathf.RoundToInt(Mech.Energy * 0.08f);     // this recharges the energy by 8% of the mech's maximum energy each turn
        CurrentEnergy = Mathf.Min(CurrentEnergy + energyRecharge, Mech.Energy);
    }
}