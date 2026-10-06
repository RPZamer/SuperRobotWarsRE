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

    // WEEK 4: MOTHERSHIP - Deploy to an empty adjacent cell without resetting action state.
    public bool TryDeploy(BattleUnit passenger, Vector2Int position)
    {
        if (!CanDeploy(passenger) || !GetDeploymentCells().Contains(position)) return false;
        if (!Unit.Battlefield.TryPlace(passenger, position)) return false;
        passengers.Remove(passenger);
        passenger.LeaveDock();
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
        if (Unit.IsDefeated) return;
        passengers.RemoveAll(passenger => passenger == null || passenger.IsDefeated);
        foreach (BattleUnit passenger in passengers)
        {
            passenger.BeginTurn();
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