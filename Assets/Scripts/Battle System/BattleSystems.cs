using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BattleSystem : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Battlefield battlefield;
    [SerializeField] private Camera battleCamera;
    [SerializeField] private DialogSystem dialogSystem;
    // WEEK 3: Assign the ActionsMenu object from the Hierarchy.
    [SerializeField] private ActionsMenu actionsMenu;
    // WEEK 3: Connect the Combat HUD so the battle system can send selected unit and battle state information to the player.
    [SerializeField] private CombatHUD combatHUD;

    // WEEK 3: Assign the specific objective units for this scenario.
    // Mazinger Z being defeated causes Defeat.
    // Great Mazinger Z being defeated causes Victory.
    [Header("Scenario Objectives")]
    [SerializeField] private BattleUnit playerObjectiveUnit;
    [SerializeField] private BattleUnit enemyObjectiveUnit;

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
    // WEEK 3: Track the current battle turn so the Combat HUD
    // can show the player which turn is currently being played.
    private int currentTurn = 1;

    // WEEK 3: Store the possible final states for the current battle scenario.
    // Only one final result can become active during normal gameplay.
    private enum ScenarioResult
    {
        InProgress,
        Victory,
        Defeat
    }

    // WEEK 3: Track the final scenario result so player and enemy actions
    // can stop permanently after Victory or Defeat has been reached.
    private ScenarioResult scenarioResult = ScenarioResult.InProgress;

    private bool ScenarioEnded =>
        scenarioResult != ScenarioResult.InProgress;

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

        // WEEK 3: Require both designated objective units before the scenario
        // begins so a missing objective cannot create an invalid battle state.
        if (playerObjectiveUnit == null || enemyObjectiveUnit == null)
        {
            Debug.LogError(
                "BattleSystem requires both a Player Objective Unit and Enemy Objective Unit.",
                this);

            enabled = false;
            return;
        }

        // WEEK 3: Begin every scenario with no Victory or Defeat message
        // visible while normal battle gameplay is still in progress.
        if (combatHUD != null)
        {
            combatHUD.HideScenarioResult();
        }

        StartCoroutine(BeginPlayerTurn());
    }

    private void Update()
    {
        // Right clicking and pressing escape exits a menu
        if (isPlayerTurn)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Back();
                return;
            }

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                Back();
                return;
            }

            HandleMouse();
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

        // WEEK 3: Allow any player unit to be selected for inspection.
        // Units that already acted can still highlight yellow, but they
        // cannot receive another action during the same Player Phase.
        if (occupant != null && occupant.Team == playerTeam)
        {
            SelectUnit(occupant);

            if (occupant.HasActed)
            {
                actionsMenu.Hide();
                battlefield.ClearHighlights();
                selectionStep = SelectionStep.Standby;
            }

            return;
        }

        // WEEK 3: A unit that already completed its action may still be
        // selected for inspection, but it cannot move or act again.
        if (selectedUnit.HasActed)
        {
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

        // WEEK 3: Position the Action HUD beside the currently selected unit
        // before displaying the player's available actions.
        actionsMenu.PositionActionsBesideUnit(selectedUnit, battleCamera);
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
    // WEEK 3: Standby permanently completes this unit's action for the
    // current Player Phase and visually marks the unit as finished.
    public void Standby()
    {
        if (!isPlayerTurn || selectedUnit == null ||
            selectionStep != SelectionStep.Actions || selectedUnit.HasActed)
        {
            return;
        }

        actionsMenu.Hide();
        selectedWeapon = null;
        battlefield.ClearHighlights();

        // WEEK 3: Mark this individual unit as finished without ending
        // the entire Player Phase.
        selectedUnit.SetSelected(false);
        selectedUnit.MarkActed();

        selectionStep = SelectionStep.Standby;
    }

    // WEEK 3: Standby now permanently completes the selected unit's
    // action for this phase, so it cannot be restored by right-clicking.
    private void UndoStandby()
    {
        if (selectedUnit == null || selectedUnit.HasActed)
        {
            return;
        }

        selectionStep = SelectionStep.Movement;
        battlefield.ShowMovement(selectedUnit);
        ShowActions();
    }

    private IEnumerator BeginPlayerTurn()
    {
        // WEEK 3: Never begin another Player Phase after the scenario
        // has already reached Victory or Defeat.
        if (ScenarioEnded)
        {
            yield break;
        }

        isPlayerTurn = false;

        // WEEK 3: Update the Combat HUD when the player's phase begins
        // while keeping the current turn number visible.
        if (combatHUD != null)
        {
            combatHUD.ShowBattleStatus("Player Phase", currentTurn);
        }

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
        // WEEK 3: Remember which player unit is performing the attack so
        // its action can be completed after combat finishes.
        BattleUnit attackingUnit = selectedUnit;

        // WEEK 3: Use the chosen weapon rather than automatically picking the first one.
        yield return ResolveAttack(attackingUnit, target, weapon);

        // WEEK 3: If this attack completed the scenario, stop the normal
        // Player Phase flow so control cannot resume after Victory or Defeat.
        if (ScenarioEnded)
        {
            yield break;
        }
        // WEEK 3: Completing an attack finishes only this individual unit's
        // action. The Player Phase remains active until the player ends it.
        if (attackingUnit != null && !attackingUnit.IsDefeated)
        {
            attackingUnit.SetSelected(false);
            attackingUnit.MarkActed();
        }

        selectedWeapon = null;
        actionsMenu.Hide();
        battlefield.ClearHighlights();
        selectionStep = SelectionStep.Standby;

        // WEEK 3: Return control to the Player Phase so another available
        // player unit can be selected before the phase is manually ended.
        if (FindFirstUnit(playerTeam) != null &&
            FindFirstUnit(OpposingTeam(playerTeam)) != null)
        {
            isPlayerTurn = true;
        }
    }

    // WEEK 3: Allow the player to manually end the Player Phase.
    // Individual units keep their movement/action state until the next
    // Player Phase begins, preventing any unit from acting twice.
    public void EndPlayerPhase()
    {
        if (!isPlayerTurn)
        {
            return;
        }

        isPlayerTurn = false;

        // WEEK 3: Remove the current selection and close player controls
        // before handing control to the enemy AI.
        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
        }

        selectedUnit = null;
        selectedWeapon = null;

        actionsMenu.Hide();
        battlefield.ClearHighlights();

        StartCoroutine(RunEnemyTurn());
    }
    private IEnumerator RunEnemyTurn()
    {
        // WEEK 3: Never begin another Enemy Phase after the scenario
        // has already reached Victory or Defeat.
        if (ScenarioEnded)
        {
            yield break;
        }

        isPlayerTurn = false;

        // WEEK 3: Update the Combat HUD when the Enemy Phase begins
        // while keeping the current turn number visible.
        if (combatHUD != null)
        {
            combatHUD.ShowBattleStatus("Enemy Phase", currentTurn);
        }

        BattleTeam enemyTeam = OpposingTeam(playerTeam);

        // WEEK 3: Create a snapshot of the enemy units before processing
        // actions so battlefield changes during combat do not modify the
        // collection currently being processed.
        List<BattleUnit> enemies = new();

        foreach (BattleUnit unit in battlefield.Units)
        {
            if (unit != null && unit.Team == enemyTeam && !unit.IsDefeated)
            {
                unit.BeginTurn();
                enemies.Add(unit);
            }
        }


        // WEEK 3: Process every eligible enemy exactly once during the
        // Enemy Phase instead of allowing only the first enemy to act.
        foreach (BattleUnit enemy in enemies)
        {
            // WEEK 3: Stop remaining enemy units from acting immediately
            // after the scenario reaches Victory or Defeat.
            if (ScenarioEnded)
            {
                yield break;
            }
            if (enemy == null || enemy.IsDefeated || enemy.HasActed)
            {
                continue;
            }

            // WEEK 3: Stop processing enemy actions if all player units
            // have been defeated.
            BattleUnit player = FindClosestUnit(enemy, playerTeam);

            if (player == null)
            {
                break;
            }

            // WEEK 3: Highlight only the enemy currently performing its
            // AI action in blue.
            enemy.SetActing(true);

            yield return ShowBattleMessage(
                enemy.Pilot,
                PilotEmotion.Angry,
                $"{GetPilotName(enemy)}'s turn.");

            if (enemyMoveDelay > 0f)
            {
                yield return new WaitForSeconds(enemyMoveDelay);
            }

            // WEEK 3: Attack immediately when a valid player target is
            // already within weapon range.
            if (enemy.GetUsableWeapon(player) != null)
            {
                yield return ResolveAttack(enemy, player);
            }
            else
            {
                // WEEK 3: If the enemy cannot currently attack, move toward
                // the selected player target using valid reachable grid cells.
                MoveEnemyCloser(enemy, player);

                // WEEK 3: Recheck attack availability after movement.
                if (!enemy.IsDefeated &&
                    player != null &&
                    !player.IsDefeated &&
                    enemy.GetUsableWeapon(player) != null)
                {
                    yield return ResolveAttack(enemy, player);
                }
            }

            // WEEK 3: This enemy has completed its one action for the phase.
            // Removing the blue acting state allows MarkActed to display grey.
            if (enemy != null && !enemy.IsDefeated)
            {
                enemy.SetActing(false);
                enemy.MarkActed();
            }

            if (enemyMoveDelay > 0f)
            {
                yield return new WaitForSeconds(enemyMoveDelay);
            }

            // WEEK 3: Stop remaining enemy units from acting immediately
            // after the scenario reaches Victory or Defeat.
            if (ScenarioEnded)
            {
                yield break;
            }
        }

        // WEEK 3: The Enemy Phase ends only after every required enemy
        // action has been processed.
        if (!ScenarioEnded &&
    FindFirstUnit(playerTeam) != null &&
    FindFirstUnit(enemyTeam) != null)
        {
            currentTurn++;
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

        // WEEK 3: Check the designated Victory and Defeat objectives
        // immediately after combat damage has been applied.
        EvaluateScenarioResult();

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

        // WEEK 3: Deselect the previous unit first. Its visual state will
        // automatically return to grey if it already acted, or its original
        // color if it is still available this phase.
        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
        }

        selectedUnit = unit;

        // WEEK 3: The currently selected player unit always highlights yellow,
        // including units that have already completed their action.
        selectedUnit.SetSelected(true);

        // WEEK 3: Keep selected unit information available for inspection
        // whether or not the unit has already acted this phase.
        if (combatHUD != null)
        {
            combatHUD.ShowSelectedUnit(selectedUnit);
        }

        selectedWeapon = null;
        actionsMenu.Hide();
        battlefield.ClearHighlights();

        // WEEK 3: An acted unit may still be selected and highlighted yellow
        // for inspection, but it cannot receive another action this phase.
        if (selectedUnit.HasActed)
        {
            selectionStep = SelectionStep.Standby;
            return;
        }

        // WEEK 3: Fresh units begin in Movement mode and may continue through
        // the normal Move, Attack, or Standby action flow.
        selectionStep = SelectionStep.Movement;
        battlefield.ShowMovement(selectedUnit);

        if (HasAnyTarget())
        {
            ShowActions();
        }
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
        // WEEK 3: Show the valid enemy target's mech name and current HP
        // on the Combat HUD before the attack is resolved.
        if (combatHUD != null)
        {
            combatHUD.ShowTargetedEnemy(target);
        }
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

    // WEEK 3: Evaluate the designated scenario objectives after relevant
    // combat events. Once a final result is reached, it cannot be changed.
    private void EvaluateScenarioResult()
    {
        if (ScenarioEnded)
        {
            return;
        }

        // WEEK 3: Great Mazinger Z is the designated enemy objective.
        // Defeating it completes the scenario with Victory.
        if (enemyObjectiveUnit != null && enemyObjectiveUnit.IsDefeated)
        {
            EndScenario(ScenarioResult.Victory);
            return;
        }

        // WEEK 3: Mazinger Z is the designated player objective.
        // Defeating it completes the scenario with Defeat.
        if (playerObjectiveUnit != null && playerObjectiveUnit.IsDefeated)
        {
            EndScenario(ScenarioResult.Defeat);
        }
    }

    // WEEK 3: Store one permanent final result and immediately stop normal
    // player controls and battlefield interaction.
    private void EndScenario(ScenarioResult result)
    {
        if (ScenarioEnded)
        {
            return;
        }

        scenarioResult = result;
        isPlayerTurn = false;

        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
        }

        selectedUnit = null;
        selectedWeapon = null;

        actionsMenu.Hide();
        battlefield.ClearHighlights();

        // WEEK 3: Keep the final scenario result visible on the Combat HUD
        // after normal battle controls have been stopped.
        if (combatHUD != null)
        {
            combatHUD.ShowScenarioResult(
                scenarioResult == ScenarioResult.Victory ? "VICTORY" : "DEFEAT");
        }

        Debug.Log($"[SCENARIO END] {scenarioResult}", this);
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