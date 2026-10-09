using System.Collections.Generic;
using UnityEngine;

// WEEK 4: MOTHERSHIP - Add beside BattleUnit; normal movement and combat stay on BattleUnit.
[DisallowMultipleComponent]
[RequireComponent(typeof(BattleUnit))]
public class Mothership : MonoBehaviour
{
    [Header("WEEK 4: MOTHERSHIP - Recovery per friendly phase")]
    [Range(0, 100)][SerializeField] private int healthRecoveryPercent = 30;
    [Range(0, 100)][SerializeField] private int energyRecoveryPercent = 30;
    [Header("WEEK 4: MOTHERSHIP - Commander")]
    [Min(0)][SerializeField] private int commanderRange = 3;
    [Range(0, 100)][SerializeField] private int commanderBonus = 10;

    private readonly List<BattleUnit> passengers = new();
    private BattleUnit unit;
    public BattleUnit Unit => unit != null ? unit : unit = GetComponent<BattleUnit>();
    public IReadOnlyList<BattleUnit> Passengers => passengers;

    public bool CanDock(BattleUnit passenger)
    {
        Battlefield grid = Unit.Battlefield;
        return isActiveAndEnabled && grid != null && !Unit.IsDefeated && !Unit.IsDocked &&
            passenger != null && passenger != Unit && passenger.Team == Unit.Team &&
            passenger.Mech != null && passenger.Pilot != null && !passenger.IsDefeated &&
            !passenger.IsDocked && !passenger.HasActed && passenger.Battlefield == grid &&
            passenger.GetComponent<Mothership>() == null &&
            grid.GetUnit(Unit.GridPosition) == Unit && grid.GetUnit(passenger.GridPosition) == passenger &&
            grid.TryGetDockSteps(passenger, Unit, out _);
    }

    // WEEK 4: MOTHERSHIP - Boarding frees the tile, hides the passenger and fills all ammo.
    public bool TryDock(BattleUnit passenger)
    {
        if (!CanDock(passenger) || !Unit.Battlefield.TryGetDockSteps(passenger, Unit, out int steps) ||
            !passenger.TrySpendEnergy(steps)) return false;
        // WEEK 4: MOTHERSHIP - Commit movement onto the ship only when Dock is confirmed.
        passenger.EnterDock(this);
        passenger.SetGridPosition(Unit.GridPosition);
        passenger.MarkMoved();
        passengers.Add(passenger);
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 DEBUG: Docking previously gave no record of the stored passenger's action state.
        // This diagnostic runs only after docking succeeds and identifies the ship, passenger and stored-unit count.
        // It confirms that boarding spent the current action without changing any docking or turn rules.
        Debug.Log($"[WEEK 6 DOCK DEBUG] Docked: ship={Unit.name}, passenger={passenger.name}, dockedHere={passenger.DockedAt == this}, moved={passenger.HasMoved}, acted={passenger.HasActed}, passengers={passengers.Count}.", this);
        return true;
    }

    // WEEK 5 FIXES: Originally, HasActed blocked deployment even though boarding itself spends the passenger's action.
    // We removed that deployment restriction so a living passenger can leave through an empty adjacent cell during the same phase.
    // Leaving still preserves its spent action, allowing transport without granting another move or attack.
    public bool CanDeploy(BattleUnit passenger)
    {
        return isActiveAndEnabled && !Unit.IsDefeated && !Unit.IsDocked &&
            passenger != null && passenger.DockedAt == this && passengers.Contains(passenger) &&
            !passenger.IsDefeated && GetDeploymentCells().Count > 0;
    }

    // WEEK 6 CHANGES PLEASE READ: A fresh passenger previously exited to an adjacent cell before using its movement separately.
    // Deployment now uses its movement range from the carrier and pays EN for the actual path, consuming movement and Accel on success.
    // Already-spent passengers retain adjacent retrieval without receiving another action, and failed placement restores the stored state.
    public bool TryDeploy(BattleUnit passenger, Vector2Int position)
    {
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 DEBUG: A rejected deployment previously returned without showing which exit was requested.
        // This diagnostic records the passenger's ownership, action state and eligibility alongside the requested exit cell.
        // It now reports movement reachability so a valid destination farther from the carrier is not mistaken for a blocked exit.
        var destinations = GetDeploymentMovementCells(passenger);
        Debug.Log($"[WEEK 6 DOCK DEBUG] Deploy attempt: ship={Unit.name}, passenger={passenger?.name ?? "none"}, dockedHere={passenger != null && passenger.DockedAt == this}, stored={passenger != null && passengers.Contains(passenger)}, acted={passenger?.HasActed}, canDeploy={CanDeploy(passenger)}, cell={position}, validExit={destinations.ContainsKey(position)}.", this);
        if (!destinations.TryGetValue(position, out int steps)) return false;
        int energy = passenger.CurrentEnergy;
        bool hadMoved = passenger.HasMoved;
        passenger.LeaveDock();
        if (!passenger.TrySpendEnergy(steps) || !Unit.Battlefield.TryPlace(passenger, position))
        {
            passenger.RestoreMove(energy);
            passenger.HasMoved = hadMoved;
            passenger.ReturnDeploymentToDock(this);
            return false;
        }
        passengers.Remove(passenger);
        if (steps > 0)
        {
            passenger.MarkMoved();
            if (passenger.AccelActive) passenger.ConsumeAccel();
        }
        return true;
    }

    // WEEK 6 CHANGES PLEASE READ: Move from the hovering preview should use the passenger's range rather than stop at adjacent exits.
    // Fresh passengers search from the carrier using mech movement, Accel and available EN, with occupied cells blocking the route.
    // Spent passengers only receive the existing adjacent retrieval cells at zero extra cost, and the carrier itself is never a destination.
    public Dictionary<Vector2Int, int> GetDeploymentMovementCells(BattleUnit passenger)
    {
        Dictionary<Vector2Int, int> cells = new();
        if (!CanDeploy(passenger)) return cells;
        if (passenger.HasMoved || passenger.HasActed)
        {
            foreach (Vector2Int cell in GetDeploymentCells()) cells[cell] = 0;
            return cells;
        }
        if (passenger.Mech == null || passenger.IsCombinedComponent) return cells;
        int movement = Mathf.Min(MechSkillEffect.Movement(passenger) + (passenger.AccelActive ? 3 : 0), passenger.CurrentEnergy);
        cells = Unit.Battlefield.GetReachableCells(Unit.GridPosition, movement);
        cells.Remove(Unit.GridPosition);
        return cells;
    }

    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: A previewed launch may need to be cancelled after the passenger chose an exit.
    // This returns that living passenger to the same ship only when its temporary map occupancy is still valid.
    // It restores the Hangar entry without running normal boarding costs, ammo recovery or action spending again.
    internal bool RestoreDeploymentPassenger(BattleUnit passenger)
    {
        if (!isActiveAndEnabled || Unit.IsDefeated || Unit.IsDocked || Unit.Battlefield == null || passenger == null || passenger.IsDefeated ||
            passenger.IsDocked || passenger.Battlefield != Unit.Battlefield || passenger.Team != Unit.Team ||
            Unit.Battlefield.GetUnit(Unit.GridPosition) != Unit || Unit.Battlefield.GetUnit(passenger.GridPosition) != passenger) return false;
        passenger.ReturnDeploymentToDock(this);
        if (!passengers.Contains(passenger)) passengers.Add(passenger);
        return true;
    }

    public List<Vector2Int> GetDeploymentCells()
    {
        List<Vector2Int> cells = new();
        Battlefield grid = Unit.Battlefield;
        if (grid == null || Unit.IsDefeated || Unit.IsDocked || grid.GetUnit(Unit.GridPosition) != Unit) return cells;
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int direction in directions)
        {
            Vector2Int position = Unit.GridPosition + direction;
            if (grid.IsInside(position) && grid.GetUnit(position) == null) cells.Add(position);
        }
        return cells;
    }

    // WEEK 4: MOTHERSHIP - Called once from the carrier's normal BeginTurn, including off-grid passengers.
    public void BeginPassengerTurn()
    {
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 DEBUG: Docked units receive their new turn through this carrier callback rather than the map list.
        // This diagnostic records when the callback is reached and how many passengers are awaiting their reset.
        // A missing entry after the next Player Phase helps locate a failure before passenger action restoration begins.
        Debug.Log($"[WEEK 6 DOCK DEBUG] Passenger phase reset reached: ship={Unit.name}, team={Unit.Team}, defeated={Unit.IsDefeated}, passengers={passengers.Count}, frame={Time.frameCount}.", this);
        if (Unit.IsDefeated) return;
        passengers.RemoveAll(passenger => passenger == null || passenger.IsDefeated);
        foreach (BattleUnit passenger in passengers)
        {
            // WEEK 6 CHANGES PLEASE READ - WEEK 6 DEBUG: The passenger's before-and-after action flags were previously unavailable in the Console.
            // These snapshots allow the diagnostic to report whether BeginTurn actually restored movement and action availability.
            // The snapshots and log are observational and leave passenger recovery and turn processing unchanged.
            bool movedBeforeReset = passenger.HasMoved;
            bool actedBeforeReset = passenger.HasActed;
            passenger.BeginTurn();
            Debug.Log($"[WEEK 6 DOCK DEBUG] Passenger reset result: ship={Unit.name}, passenger={passenger.name}, moved={movedBeforeReset}->{passenger.HasMoved}, acted={actedBeforeReset}->{passenger.HasActed}, dockedHere={passenger.DockedAt == this}, combinedComponent={passenger.IsCombinedComponent}.", passenger);
            passenger.RestoreHealthAndEnergy(
                Mathf.CeilToInt(passenger.Mech.Health * healthRecoveryPercent / 100f),
                Mathf.CeilToInt(passenger.Mech.Energy * energyRecoveryPercent / 100f));
        }
    }

    public int GetCommanderBonus(BattleUnit ally)
    {
        if (!isActiveAndEnabled || Unit.IsDefeated || Unit.IsDocked || ally == null || ally == Unit ||
            ally.IsDefeated || ally.IsDocked || ally.Team != Unit.Team || ally.Battlefield != Unit.Battlefield)
            return 0;
        return Distance(Unit.GridPosition, ally.GridPosition) <= commanderRange ? commanderBonus : 0;
    }

    // WEEK 4: MOTHERSHIP - Passengers are lost with a destroyed ship; scenario rules remain unchanged.
    public void DefeatPassengers()
    {
        foreach (BattleUnit passenger in passengers)
            if (passenger != null) passenger.TakeDamage(passenger.CurrentHealth);
        passengers.Clear();
    }

    private static int Distance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}