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
    [SerializeField] private Vector2Int startingPosition;

    private Battlefield battlefield;
    private SpriteRenderer spriteRenderer;
    private Color normalColor = Color.white;

    // WEEK 3: Track the unit's temporary visual state so selection,
    // enemy AI actions, and completed actions can use different colors.
    private bool isSelected;
    private bool isActing;

    public PilotBase Pilot => pilot;
    // WEEK 3: Expose mech settings and keep energy and movement state on this individual unit.
    public MechBase Mech => mech;
    public TerrainType Terrain => terrain;
    public int CurrentEnergy { get; private set; }
    public bool HasMoved { get; private set; }
    // WEEK 3: Each grid step costs one energy. Stop movement after this unit has already moved.

    // WEEK 3: Track whether this unit has completed its full action
    // during the current phase.
    public bool HasActed { get; private set; }
    public int AvailableMovement => mech != null && !HasMoved ? Mathf.Min(mech.Movement, CurrentEnergy) : 0;

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
    }
    public void MarkMoved() => HasMoved = true;
    public BattleTeam Team => team;
    public Vector2Int GridPosition { get; private set; }
    public int CurrentHealth { get; private set; }

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
        if (amount < 0 || amount > CurrentEnergy || IsDefeated) return false;
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
        if (weapon == null || mech == null || pilot == null || IsDefeated ||
            target == null || !target.CanBeTargetedBy(this) || weapon.EnergyCost > CurrentEnergy)
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
        if (mech == null || pilot == null || IsDefeated || target == null ||
            target.IsDefeated || target.Mech == null || target.Pilot == null || target.Team == Team)
            return null;

        int distance = Mathf.Abs(GridPosition.x - target.GridPosition.x)
            + Mathf.Abs(GridPosition.y - target.GridPosition.y);
        // WEEK 3: All weapons can attack after moving if their range and energy cost allow it.
        foreach (Weapon weapon in mech.Weapons)
        {
            if (weapon != null &&
                weapon.IsInRange(distance) && weapon.EnergyCost <= CurrentEnergy &&
                (weapon.Type & requiredType) == requiredType)
                return weapon;
        }
        return null;
    }

    // WEEK 3: Check that an extra adjacent target is a living enemy with pilot and mech data.
    public bool CanBeTargetedBy(BattleUnit attacker)
    {
        return attacker != null && Team != attacker.Team && !IsDefeated && mech != null && pilot != null;
    }

    public int TakeDamage(int damage)
    {
        int appliedDamage = Mathf.Min(CurrentHealth, Mathf.Max(0, damage));
        CurrentHealth -= appliedDamage;

        if (IsDefeated)
        {
            // WEEK 3: Remove a defeated unit from the grid only if it has a battlefield.
            if (battlefield != null) battlefield.Remove(this);
            Destroy(gameObject);
        }

        return appliedDamage;
    }
}