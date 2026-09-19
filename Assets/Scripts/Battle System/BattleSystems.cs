using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleSystem : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Battlefield battlefield;
    [SerializeField] private Camera battleCamera;
    [SerializeField] private DialogSystem dialogSystem;
    // WEEK 3: Assign the ActionsMenu object from the Hierarchy.
    [SerializeField] private ActionsMenu actionsMenu;

    [Header("Turns")]
    [SerializeField] private BattleTeam playerTeam = BattleTeam.Player;
    [Min(0f)][SerializeField] private float enemyMoveDelay = 0.5f;

    // WEEK 3 DMG CHECK: Toggle the numeric damage formula breakdown in the Inspector.
    [Header("Diagnostics")]
    [SerializeField] private bool debugBattleCalculations = true;

    private void Awake() => BattleDebug.Enabled = debugBattleCalculations;
    private void OnValidate() => BattleDebug.Enabled = debugBattleCalculations;

    private BattleUnit selectedUnit;
    // WEEK 3: Expose the currently selected player unit so movement,
    // combat, and UI systems can access the active unit without
    // directly modifying the BattleSystem's private selection state.
    public BattleUnit SelectedUnit => selectedUnit;
    private bool isPlayerTurn;

    // WEEK 3: Remember the current menu step and selected weapon.
    private enum SelectionStep { Movement, Actions, Weapons, Targets, Standby }
    private SelectionStep selectionStep;
    private Weapon selectedWeapon;

    // Battle messages remain readable briefly after their typewriter animation finishes.
    private const float MessageHoldSeconds = 0.75f;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Start()
    {
        if (battlefield == null)
        {
            battlefield = FindFirstObjectByType<Battlefield>();
        }

        if (battleCamera == null)
        {
            battleCamera = Camera.main;
        }

        if (dialogSystem == null)
        {
            dialogSystem = FindFirstObjectByType<DialogSystem>();
        }


        if (battlefield == null)
        {
            Debug.LogError("BattleSystem needs a Battlefield in the scene.", this);
            enabled = false;
            return;
        }

        // WEEK 3: Use only the ActionsMenu object and UI references made in the scene.
        if (actionsMenu == null || !actionsMenu.Bind(this))
        {
            Debug.LogError("BattleSystem needs a configured ActionsMenu.", this);
            enabled = false;
            return;
        }
        battlefield.RegisterSceneUnits();
        StartCoroutine(BeginPlayerTurn());
    }

    private void Update()
    {
        if (isPlayerTurn)
        {
            // WEEK 3: Right-click backs out even while the cursor is over a menu.
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) Back();
            else HandleMouse();
        }
    }

    private void HandleMouse()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || battleCamera == null)
        {
            return;
        }

        // WEEK 3: Menu buttons handle their own clicks. Movement and target clicks always reach the battlefield.
        if (selectionStep == SelectionStep.Actions || selectionStep == SelectionStep.Weapons) return;
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float distanceToGrid = Mathf.Abs(battleCamera.transform.position.z - battlefield.transform.position.z);
        Vector3 screenPosition = new(mousePosition.x, mousePosition.y, distanceToGrid);
        Vector3 worldPosition = battleCamera.ScreenToWorldPoint(screenPosition);
        ConfirmCell(battlefield.WorldToGrid(worldPosition));
    }

    // WEEK 3: Move first, then choose an action, a weapon, and a target.
    private void ConfirmCell(Vector2Int position)
    {
        if (!battlefield.IsInside(position) || selectedUnit == null) return;
        BattleUnit occupant = battlefield.GetUnit(position);

        if (selectionStep == SelectionStep.Targets)
        {
            // WEEK 3: Send enemy clicks through the target check before attacking.
            ChooseAttackTarget(occupant);
            return;
        }

        if (selectionStep == SelectionStep.Weapons) return;
        if (occupant != null && occupant.Team == playerTeam)
        {
            if (!selectedUnit.HasMoved) SelectUnit(occupant);
            else ShowActions();
            return;
        }

        // WEEK 3: Report every movement cell click before attempting the move.
        if (occupant == null)
            Debug.Log($"[Move Debug] Trying {selectedUnit.name} from {selectedUnit.GridPosition} to {position}.", this);
        if (occupant == null && battlefield.TryMove(selectedUnit, position))
        {
            battlefield.ShowMovement(selectedUnit);
            // WEEK 3: Report the move before reopening the action menu.
            Debug.Log($"[Move Debug] Move succeeded. New position: {position}.", this);
            ShowActions();
        }
        else if (occupant == null)
        {
            // WEEK 3: Report when the battlefield refuses the selected movement cell.
            Debug.Log($"[Move Debug] Move rejected. Cell {position} is not in this mech's current movement range.", this);
        }
        else if (occupant != null && selectedUnit.GetUsableWeapon(occupant) != null)
        {
            ShowActions();
        }
    }

    // WEEK 3: The action menu appears when an enemy is in reach, or after a move.
    private void ShowActions()
    {
        selectionStep = SelectionStep.Actions;
        selectedWeapon = null;
        battlefield.ShowMovement(selectedUnit);
        actionsMenu.ShowActions(HasAnyTarget());
    }

    private bool HasAnyTarget()
    {
        if (selectedUnit == null) return false;
        foreach (BattleUnit target in battlefield.Units)
            if (selectedUnit.GetUsableWeapon(target) != null) return true;
        return false;
    }

    // WEEK 3: Both the menu and target highlights check the same selected weapon.
    public bool HasTarget(Weapon weapon)
    {
        if (selectedUnit == null || weapon == null) return false;
        foreach (BattleUnit target in battlefield.Units)
            if (selectedUnit.CanUseWeapon(weapon, target)) return true;
        return false;
    }

    // WEEK 3: Close the action choices and return to the selected unit's movement range.
    public void OpenMovement()
    {
        // WEEK 3: Report whether the Move button can return to movement selection.
        if (!isPlayerTurn || selectedUnit == null || selectionStep != SelectionStep.Actions)
        {
            Debug.Log($"[Move Debug] Move button ignored. PlayerTurn: {isPlayerTurn}; selected: {selectedUnit != null}; step: {selectionStep}.", this);
            return;
        }
        Debug.Log($"[Move Debug] Move button accepted for {selectedUnit.name}.", this);
        actionsMenu.Hide();
        selectedWeapon = null;
        selectionStep = SelectionStep.Movement;
        battlefield.ShowMovement(selectedUnit);
    }
    // WEEK 3: Attack opens all weapons; start by previewing the first usable one.
    public void OpenWeapons()
    {
        // WEEK 3: Report whether Attack can open a weapon list with a valid enemy target.
        bool hasTarget = HasAnyTarget();
        Debug.Log($"[Attack Debug] Attack button. PlayerTurn: {isPlayerTurn}; step: {selectionStep}; target available: {hasTarget}.", this);
        if (!isPlayerTurn || selectionStep != SelectionStep.Actions || !hasTarget) return;
        if (selectedWeapon == null)
        {
            foreach (Weapon weapon in selectedUnit.Mech.Weapons)
                if (HasTarget(weapon)) { selectedWeapon = weapon; break; }
        }
        selectionStep = SelectionStep.Weapons;
        actionsMenu.ShowWeapons(selectedUnit, selectedWeapon);
        battlefield.ShowWeaponRange(selectedUnit, selectedWeapon);
    }

    // WEEK 3: Clicking a weapon previews its range immediately, before confirming it.
    public void PreviewWeapon(Weapon weapon)
    {
        if (!isPlayerTurn || selectionStep != SelectionStep.Weapons) return;
        selectedWeapon = weapon;
        actionsMenu.SetWeapon(weapon);
        battlefield.ShowWeaponRange(selectedUnit, weapon);
    }

    // WEEK 3: Confirm or double-click closes the list and allows this weapon's targets.
    public void ConfirmWeapon()
    {
        // WEEK 3: Report whether the selected weapon has at least one valid enemy target.
        bool hasTarget = HasTarget(selectedWeapon);
        Debug.Log($"[Attack Debug] Confirm weapon: {selectedWeapon?.WeaponName ?? "none"}; target available: {hasTarget}.", this);
        if (!isPlayerTurn || selectionStep != SelectionStep.Weapons || !hasTarget) return;
        // WEEK 3: Close the weapon menu right away so an enemy tile can be clicked immediately.
        actionsMenu.Hide();
        selectionStep = SelectionStep.Targets;
        battlefield.ShowWeaponRange(selectedUnit, selectedWeapon);
        Debug.Log($"[Attack Debug] Target selection is active for {selectedWeapon.WeaponName}.", this);
    }

    // WEEK 3: Right-click returns to the previous menu step.
    public void Back()
    {
        if (!isPlayerTurn || selectedUnit == null) return;
        if (selectionStep == SelectionStep.Targets)
        {
            actionsMenu.Hide();
            selectionStep = SelectionStep.Weapons;
            actionsMenu.ShowWeapons(selectedUnit, selectedWeapon);
            battlefield.ShowWeaponRange(selectedUnit, selectedWeapon);
        }
        else if (selectionStep == SelectionStep.Weapons)
        {
            ShowActions();
        }
        else if (selectionStep == SelectionStep.Standby)
        {
            UndoStandby();
        }
        // WEEK 3: Do not change movement while the Attack/Standby menu is open.
    }

    // WEEK 3: Standby ends this unit's action without undoing its move.
    public void Standby()
    {
        if (!isPlayerTurn || selectedUnit == null || selectionStep != SelectionStep.Actions) return;
        actionsMenu.Hide();
        selectedWeapon = null;
        battlefield.ClearHighlights();
        selectionStep = SelectionStep.Standby;
    }

    // WEEK 3: Right-click after Standby restores the unit's action at its current position.
    private void UndoStandby()
    {
        selectionStep = SelectionStep.Movement;
        battlefield.ShowMovement(selectedUnit);
        ShowActions();
    }

    private IEnumerator BeginPlayerTurn()
    {
        isPlayerTurn = false;

        // WEEK 3: Reset player movement at the start of the player turn.
        foreach (BattleUnit unit in battlefield.Units)
            if (unit.Team == playerTeam) unit.BeginTurn();

        BattleUnit player = FindFirstUnit(playerTeam);
        if (player == null)
        {
            yield break;
        }

        yield return ShowBattleMessage(player.Pilot, PilotEmotion.Motivated, $"{GetPilotName(player)}'s turn.");
        SelectUnit(player);
        isPlayerTurn = true;
    }

    private IEnumerator ResolvePlayerAttack(BattleUnit target, Weapon weapon)
    {
        // WEEK 3: Use the chosen weapon rather than automatically picking the first one.
        yield return ResolveAttack(selectedUnit, target, weapon);

        if (FindFirstUnit(OpposingTeam(playerTeam)) != null)
        {
            yield return RunEnemyTurn();
        }
    }

    private IEnumerator RunEnemyTurn()
    {
        isPlayerTurn = false;

        BattleTeam enemyTeam = OpposingTeam(playerTeam);
        // WEEK 3: Reset enemy movement at the start of the enemy turn.
        foreach (BattleUnit unit in battlefield.Units)
            if (unit.Team == enemyTeam) unit.BeginTurn();
        BattleUnit enemy = FindFirstUnit(enemyTeam);
        BattleUnit player = FindClosestUnit(enemy, playerTeam);

        if (enemy == null || player == null)
        {
            yield break;
        }

        yield return ShowBattleMessage(enemy.Pilot, PilotEmotion.Angry, $"{GetPilotName(enemy)}'s turn.");

        if (enemyMoveDelay > 0f)
        {
            yield return new WaitForSeconds(enemyMoveDelay);
        }

        // WEEK 3: Let the enemy attack from its weapon range instead of only one square away.
        if (enemy.GetUsableWeapon(player) != null)
        {
            yield return ResolveAttack(enemy, player);
        }
        else
        {
            // WEEK 3: After moving, let the enemy attack with any affordable weapon in range.
            MoveEnemyCloser(enemy, player);
            if (enemy.GetUsableWeapon(player) != null)
                yield return ResolveAttack(enemy, player);
        }

        if (FindFirstUnit(playerTeam) != null && FindFirstUnit(enemyTeam) != null)
        {
            yield return BeginPlayerTurn();
        }
    }

    // WEEK 3: Handle a defender first strike before finishing the incoming attack.
    private IEnumerator ResolveAttack(BattleUnit attacker, BattleUnit target, Weapon weapon = null)
    {
        if (attacker == null || target == null) yield break;
        // WEEK 3: Players supply their chosen weapon; enemies still choose automatically.
        if (weapon == null) weapon = attacker.GetUsableWeapon(target);
        if (!attacker.CanUseWeapon(weapon, target)) yield break;

        Weapon firstStrike = target.GetUsableWeapon(attacker, WeaponType.FirstStrike);
        // WEEK 3: If both weapons have FirstStrike, the unit that started the attack goes first.
        if ((weapon.Type & WeaponType.FirstStrike) == 0 && firstStrike != null)
        {
            yield return PerformAttack(target, attacker, firstStrike);
            // WEEK 3: A unit defeated by the first strike cannot finish its attack.
            if (attacker == null || attacker.IsDefeated) yield break;
        }
        yield return PerformAttack(attacker, target, weapon);
    }

    // WEEK 3: Pay for one weapon use and collect the units its attack will hit.
    private IEnumerator PerformAttack(BattleUnit attacker, BattleUnit target, Weapon weapon)
    {
        if (attacker == null || target == null)
        {
            yield break;
        }

        if (weapon == null || !attacker.TrySpendEnergy(weapon.EnergyCost))
        {
            yield break;
        }

        // WEEK 3: Save the target list before damage removes defeated units from the grid.
        List<BattleUnit> targets = new();
        if (weapon.Classification == WeaponClassification.SingleTarget)
        {
            targets.Add(target);
        }
        else if (weapon.Classification == WeaponClassification.Map)
        {
            foreach (BattleUnit candidate in battlefield.Units)
            {
                // WEEK 3: MAP attacks hit every unit in weapon range, including allies.
                if (candidate == null || candidate.IsDefeated || candidate.Pilot == null || candidate.Mech == null)
                    continue;
                if (weapon.IsInRange(ManhattanDistance(attacker.GridPosition, candidate.GridPosition)))
                    targets.Add(candidate);
            }
        }
        else
        {
            targets.Add(target);
            // WEEK 3: Hit at most one extra enemy. Check one square above, then left, then right.
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int offset in offsets)
            {
                BattleUnit adjacent = battlefield.GetUnit(target.GridPosition + offset);
                if (adjacent != null && adjacent.CanBeTargetedBy(attacker))
                {
                    targets.Add(adjacent);
                    break;
                }
            }
        }

        // WEEK 3: Energy was paid once. Check hit and critical chance separately for each target.
        foreach (BattleUnit hitTarget in targets)
        {
            if (hitTarget != null && !hitTarget.IsDefeated)
                yield return ResolveWeaponHit(attacker, hitTarget, weapon);
        }
    }

    // WEEK 3: Roll hit and critical chance, apply formula damage, and show the battle result.
    private IEnumerator ResolveWeaponHit(BattleUnit attacker, BattleUnit target, Weapon weapon)
    {
        PilotBase attackerPilot = attacker.Pilot;
        PilotBase targetPilot = target.Pilot;
        string targetName = GetPilotName(target);
        int distance = ManhattanDistance(attacker.GridPosition, target.GridPosition);
        // WEEK 3: Limit the hit chance to 0-100 and stop this hit if the attack misses.
        int hitRate = Mathf.Clamp(BattleFormulas.AccuracyRate(attacker, target, weapon, distance), 0, 100);
        if (Random.Range(0, 100) >= hitRate)
        {
            yield return ShowBattleMessage(targetPilot, PilotEmotion.Default, $"{GetPilotName(attacker)} missed.");
            yield break;
        }

        // WEEK 3: Roll for a critical hit and use the result when calculating damage.
        int criticalRate = Mathf.Clamp(BattleFormulas.CriticalRate(attacker, target, weapon), 0, 100);
        bool critical = Random.Range(0, 100) < criticalRate;
        int damage = target.TakeDamage(BattleFormulas.Damage(attacker, target, weapon, critical));
        bool defeated = target.IsDefeated;

        // Damage and defeat messages use text only, so their portrait flag remains false.
        yield return ShowBattleMessage(
            targetPilot,
            defeated ? PilotEmotion.Defeated : PilotEmotion.Sad,
            $"{targetName} took {damage} damage.");

        if (!defeated)
        {
            yield break;
        }

        yield return ShowBattleMessage(targetPilot, PilotEmotion.Defeated, $"{targetName} was defeated.");

        if (attackerPilot == null)
        {
            yield break;
        }

        // Success lines play in list order and are the only battle messages that show a portrait.
        foreach (string successLine in attackerPilot.OnSuccessLines)
        {
            if (!string.IsNullOrWhiteSpace(successLine))
            {
                yield return ShowBattleMessage(attackerPilot, PilotEmotion.Motivated, successLine, true);
            }
        }
    }

    private IEnumerator ShowBattleMessage(
        PilotBase pilot,
        PilotEmotion emotion,
        string message,
        bool showPortrait = false)
    {
        // Dialogue is optional; skipping it must never stop the battle coroutine.
        if (dialogSystem == null)
        {
            yield break;
        }

        dialogSystem.SetVisibleImmediate(true);
        dialogSystem.DisplayLine(pilot, emotion, message, showPortrait);

        while (dialogSystem.IsTyping)
        {
            yield return null;
        }

        yield return new WaitForSecondsRealtime(MessageHoldSeconds);

        yield return dialogSystem.FadeOut();
    }

private void MoveEnemyCloser(BattleUnit enemy, BattleUnit target)
    {
        List<Vector2Int> openMoves = new();
        List<Vector2Int> closerMoves = new();
        int currentDistance = ManhattanDistance(enemy.GridPosition, target.GridPosition);

        // WEEK 3: Choose enemy moves from reachable squares rather than only neighboring squares.
        foreach (Vector2Int position in battlefield.GetReachableCells(enemy).Keys)
        {

            if (position == enemy.GridPosition)
            {
                continue;
            }

            openMoves.Add(position);

            if (ManhattanDistance(position, target.GridPosition) < currentDistance)
            {
                closerMoves.Add(position);
            }
        }

        List<Vector2Int> choices = closerMoves.Count > 0 ? closerMoves : openMoves;

        if (choices.Count > 0)
        {
            battlefield.TryMove(enemy, choices[Random.Range(0, choices.Count)]);
        }
    }

    private BattleUnit FindFirstUnit(BattleTeam team)
    {
        foreach (BattleUnit unit in battlefield.Units)
        {
            if (unit != null && !unit.IsDefeated && unit.Team == team)
            {
                return unit;
            }
        }

        return null;
    }

    private BattleUnit FindClosestUnit(BattleUnit source, BattleTeam team)
    {
        if (source == null)
        {
            return null;
        }

        BattleUnit closest = null;
        int closestDistance = int.MaxValue;

        foreach (BattleUnit unit in battlefield.Units)
        {
            if (unit == null || unit.IsDefeated || unit.Team != team)
            {
                continue;
            }

            int distance = ManhattanDistance(source.GridPosition, unit.GridPosition);
            if (distance < closestDistance)
            {
                closest = unit;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void SelectUnit(BattleUnit unit)
    {

        if (unit == null || unit.Team != playerTeam)
        {
            return;
        }

        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
        }

        selectedUnit = unit;
        // WEEK 3: Visually mark the newly selected player unit so the player
        // can immediately distinguish the active unit from other units.
        selectedUnit.SetSelected(true);

        // WEEK 3: Reset the action state for the newly selected unit.
        // This preserves integration with the movement, weapon, and combat UI systems.
        selectedWeapon = null;
        selectionStep = SelectionStep.Movement;
        actionsMenu.Hide();
        battlefield.ShowMovement(selectedUnit);
        if (HasAnyTarget()) ShowActions();
    }

    // WEEK 3: Check the clicked enemy and start the chosen weapon attack with one click.
    private void ChooseAttackTarget(BattleUnit target)
    {
        // WEEK 3: Report why an enemy click cannot become the chosen weapon attack.
        if (!selectedUnit.CanUseWeapon(selectedWeapon, target))
        {
            Debug.Log($"[Attack Debug] Target rejected. Weapon: {selectedWeapon?.WeaponName ?? "none"}; target: {target?.name ?? "empty"}.", this);
            return;
        }
        Debug.Log($"[Attack Debug] Target accepted: {target.name} with {selectedWeapon.WeaponName}.", this);
        // WEEK 3: Click a valid enemy once to attack with the weapon already chosen.
        Weapon weapon = selectedWeapon;
        PrepareForTurnChange();
        StartCoroutine(ResolvePlayerAttack(target, weapon));
    }

    private void PrepareForTurnChange()
    {
        isPlayerTurn = false;
        // WEEK 3: Close the action UI when a confirmed attack commits the turn.
        actionsMenu.Hide();
        battlefield.ClearHighlights();
    }

    private static BattleTeam OpposingTeam(BattleTeam team)
    {
        return team == BattleTeam.Player ? BattleTeam.Enemy : BattleTeam.Player;
    }

    private static string GetPilotName(BattleUnit unit)
    {
        return unit != null && unit.Pilot != null ? unit.Pilot.PilotName : "Pilot";
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}