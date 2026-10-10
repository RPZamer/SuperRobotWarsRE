using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleSystem : MonoBehaviour
{
    [Header("Scene")]
    //Assign the SceneTransition object from the Hierarchy.
    [SerializeField] private SceneTransition sceneTransition;
    [SerializeField] private Battlefield battlefield;
    [SerializeField] private Camera battleCamera;
    [SerializeField] private DialogSystem dialogSystem;
    // WEEK 3: Assign the ActionsMenu object from the Hierarchy.
    [SerializeField] private ActionsMenu actionsMenu;
    // WEEK 3: Connect the Combat HUD so the battle system can send selected unit and battle state information to the player.
    [SerializeField] private CombatHUD combatHUD;

    // WEEK 5: MUSIC SYSTEM - Controls overworld and mech battle themes.
    [SerializeField] private BattleMusic battleMusic;

    // WEEK 4: SPIRIT COMMANDS - Stores a targeted Spirit Command
    // while the player chooses an ally or enemy on the battlefield.
    private SpiritCommandBase pendingSpiritCommand;
    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER targeted Spirits may belong to a sub-pilot rather than the main pilot.
    // This small companion field preserves that pilot while the player chooses a battlefield target.
    // The successful execution and cancellation paths clear both values together without changing the selected combat pilot.
    private PilotBase pendingSpiritPilot;

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

    // WEEK 4: MOTHERSHIP - Reuse UI raycast results when checking a left-click.
    private readonly List<RaycastResult> pointerHits = new();
    private BattleUnit selectedUnit;
    // WEEK 3: Expose the currently selected player unit so movement,
    // combat, and UI systems can access the active unit without
    // directly modifying the BattleSystem's private selection state.
    public BattleUnit SelectedUnit => selectedUnit;

    // WEEK 4: SPIRIT COMMANDS - Allow the Spirit Command UI
    // to access the current battlefield without modifying it.
    public Battlefield Battlefield => battlefield;
    private bool isPlayerTurn;
    // WEEK 4: MOTHERSHIP - The phase button is available only while player commands are allowed.
    public bool CanEndPlayerPhase => isActiveAndEnabled && isPlayerTurn && !ScenarioEnded;
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
    // WEEK 4: MOTHERSHIP - Boarding, hangar list and deployment reuse the selection flow.
    // WEEK 5 CHANGES PLEASE READ: Selection previously knew only movement, combat and transport targeting.
    // Repair/Resupply need an explicit friendly-target step so their clicks cannot switch units or move behind a menu.
    // This extra state reuses the existing grid click, highlight and cancel flow instead of adding another controller.
    private enum SelectionStep { Movement, Actions, Weapons, Targets, Standby, Boarding, Hangar, Deployment, MechSupport }
    private bool resupplyTargeting;
    private readonly Dictionary<BattleUnit, (BattleUnit player, BattleUnit enemy)> combinationObjectives = new();
    private BattleUnit passengerToDeploy;
    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Selecting a Hangar passenger now opens a cancellable action preview above its ship.
    // This tracks the source carrier, original action flags and launch movement costs until the passenger confirms an action.
    // Keeping this state separate from normal docking lets cancellation refund launch EN and Accel without changing carrier occupancy.
    private Mothership deploymentShip;
    private bool deploymentHadMoved, deploymentHadActed;
    private int deploymentEnergy;
    private bool deploymentHadAccel, deploymentUsedMovement;
    // WEEK 4: MOTHERSHIP - Preview docking without overwriting the ship's grid occupancy.
    private Mothership pendingDock;
    // WEEK 4: MOTHERSHIP - The menu reads the same docking state used by its confirmation callback.
    public bool IsDockConfirmation => pendingDock != null && selectedUnit != null &&
        selectionStep == SelectionStep.Actions;
    // WEEK 4: MOTHERSHIP - Keep unconfirmed movement rollback per unit when switching selection.
    private readonly Dictionary<BattleUnit, (Vector2Int position, int energy)> unconfirmedMoves = new();
    public bool CanUndoSelectedMove => selectedUnit != null && !selectedUnit.HasActed &&
        unconfirmedMoves.TryGetValue(selectedUnit, out var move) &&
        (battlefield.GetUnit(move.position) == null || battlefield.GetUnit(move.position) == selectedUnit);
    private SelectionStep selectionStep;
    private Weapon selectedWeapon;

    // WEEK 6 CHANGES PLEASE READ: Enemy attacks previously resolved without waiting for a defensive choice.
    // These fields hold the attacker, defender, weapons and chosen response for one exchange only.
    // Separate pending and weapon-selection flags allow reaction controls during Enemy Phase without enabling normal player commands.
    private BattleUnit reactionAttacker, reactionDefender;
    private Weapon reactionIncomingWeapon, reactionCounterWeapon;
    private BattleReaction selectedReaction;
    private bool reactionPending, choosingCounterWeapon, reactionConfirmed;
    public bool IsChoosingCounterWeapon => reactionPending && choosingCounterWeapon;

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
        // WEEK 6 CHANGES PLEASE READ: Enemy Phase input was previously ignored while its coroutine ran.
        // A pending reaction now accepts C, E, D, Enter and Back through the same guarded callbacks as the buttons.
        // Returning here keeps movement, unit selection and ordinary action shortcuts locked while the enemy waits.
        if (reactionPending)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame) Back();
                else if (Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    if (choosingCounterWeapon) ConfirmWeapon(); else BeginReactionCombat();
                }
                else if (!choosingCounterWeapon)
                {
                    if (Keyboard.current.cKey.wasPressedThisFrame) ChooseCounter();
                    else if (Keyboard.current.eKey.wasPressedThisFrame) ChooseEvade();
                    else if (Keyboard.current.dKey.wasPressedThisFrame) ChooseDefend();
                }
            }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) Back();
            return;
        }
        if (isPlayerTurn)
        {
            // WEEK 5 CHANGES PLEASE READ: The current battle menus have no working bindings for the restored mech commands.
            // Keyboard callbacks make those commands available even in scenes whose older UI prefab has no skill buttons.
            // They share the same guarded command methods as optional saved buttons, so they cannot bypass targeting or phase restrictions.
            if (Keyboard.current != null && CanIssueMechCommand)
            {
                if (Keyboard.current.fKey.wasPressedThisFrame) TransformSelected();
                else if (Keyboard.current.gKey.wasPressedThisFrame) CombineSelected();
                else if (Keyboard.current.hKey.wasPressedThisFrame) SeparateSelected();
                else if (Keyboard.current.tKey.wasPressedThisFrame) GetterChangeSelected();
                else if (Keyboard.current.rKey.wasPressedThisFrame) OpenRepair();
                else if (Keyboard.current.uKey.wasPressedThisFrame) OpenResupply();
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Back();
                return;
            }
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

        // WEEK 4: MOTHERSHIP - Only controls consume map clicks; background Images and TMP text do not.
        // Raycast at this click's position instead of using the EventSystem's previous pointer state.
        if (IsPointerOverControl(Mouse.current.position.ReadValue())) return;

        // WEEK 4: MOTHERSHIP - Map clicks may switch friendly units even while a menu is open.
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float distanceToGrid = Mathf.Abs(battleCamera.transform.position.z - battlefield.transform.position.z);
        Vector3 screenPosition = new(mousePosition.x, mousePosition.y, distanceToGrid);
        Vector3 worldPosition = battleCamera.ScreenToWorldPoint(screenPosition);
        ConfirmCell(battlefield.WorldToGrid(worldPosition));
    }

    // WEEK 4: MOTHERSHIP - Keep End Phase/button clicks off the grid without blocking the map background.
    private bool IsPointerOverControl(Vector2 position)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;
        pointerHits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = position }, pointerHits);
        foreach (RaycastResult hit in pointerHits)
        {
            if (!(hit.module is GraphicRaycaster) || hit.gameObject == null) continue;
            Selectable control = hit.gameObject.GetComponentInParent<Selectable>();
            // Disabled buttons still occupy UI space and should not send clicks through to a unit.
            if (control != null && control.isActiveAndEnabled) return true;
        }
        return false;
    }

    // WEEK 4: SPIRIT COMMANDS - Begins battlefield targeting for
    // Spirit Commands that require a specific ally or enemy.
    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER command buttons now identify which crew member owns the chosen Spirit.
    // Capturing that pilot here makes later target clicks charge the correct independent SP pool.
    // The optional argument retains existing main-pilot calls and rejects pilots outside the selected unit's crew.
    public void BeginSpiritTargeting(SpiritCommandBase spirit, PilotBase spiritPilot = null)
    {
        if (selectedUnit == null || spirit == null)
        {
            return;
        }

        spiritPilot ??= selectedUnit.Pilot;
        if (!selectedUnit.HasSpiritPilot(spiritPilot)) return;
        pendingSpiritCommand = spirit;
        pendingSpiritPilot = spiritPilot;

        Debug.Log(
            $"{spiritPilot.PilotName} is choosing a target for {spirit.CommandName}.");
    }

    // WEEK 3: Move first, then choose an action, a weapon, and a target.
    private void ConfirmCell(Vector2Int position)
    {
        if (!CanEndPlayerPhase || !battlefield.IsInside(position)) return;

        BattleUnit occupant = battlefield.GetUnit(position);

        // WEEK 5 CHANGES PLEASE READ: Friendly map clicks normally select another unit before any command can use it.
        // Repair/Resupply must receive that click first so the chosen adjacent ally becomes the support target.
        // A successful command commits movement and spends the user's action, while an invalid target leaves the selection pending.
        if (selectionStep == SelectionStep.MechSupport)
        {
            bool used = resupplyTargeting ? MechSkillEffect.TryResupply(selectedUnit, occupant) : MechSkillEffect.TryRepair(selectedUnit, occupant);
            if (used) FinishMechCommand();
            return;
        }

        // WEEK 4: SPIRIT COMMANDS - If a targeted Spirit Command is
        // waiting, this click belongs to Spirit targeting instead of
        // movement, unit selection, or attacking.
        if (pendingSpiritCommand != null)
        {
            if (occupant == null)
            {
                Debug.LogWarning(
                    $"{pendingSpiritCommand.CommandName} requires a unit target.");

                return;
            }

            SpiritCommandBase spiritToUse = pendingSpiritCommand;

            // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER target selection previously executed every Spirit as the current main pilot.
            // The saved command owner is now forwarded to SpiritSystem so only that pilot pays the cost.
            // A successful target selection clears both pending values, while invalid targets preserve the original owner for another click.
            bool used = SpiritSystem.UseSpirit(
                selectedUnit,
                spiritToUse,
                battlefield,
                occupant,
                pendingSpiritPilot);

            if (used)
            {
                pendingSpiritCommand = null;
                pendingSpiritPilot = null;

                // WEEK 4: Return to the normal action menu after the
                // targeted Spirit Command successfully activates.
                ShowActions();
            }

            return;
        }

        // WEEK 4: MOTHERSHIP - A second click on the previewed ship must not select it and cancel Dock.

        // WEEK 4: MOTHERSHIP - A second click on the previewed ship must not select it and cancel Dock.
        if (IsDockConfirmation && occupant == pendingDock.Unit)
        {
            ShowActions();
            return;
        }

        // WEEK 4: MOTHERSHIP - In Move mode, clicking a reachable ship previews that destination.
        if (selectionStep == SelectionStep.Movement && selectedUnit != null && occupant != null &&
            occupant.TryGetComponent(out Mothership carrier) && carrier.CanDock(selectedUnit))
        {
            pendingDock = carrier;
            selectedUnit.transform.position = battlefield.GridToWorld(carrier.Unit.GridPosition);
            ShowActions();
            return;
        }

        // WEEK 4: MOTHERSHIP - Left-click any friendly map unit to take control or inspect it.
        // Switching cancels pending targeting/deployment, but never resets movement or actions.
        if (occupant != null && occupant.Team == playerTeam && !occupant.IsDefeated && !occupant.IsDocked)
        {
            SelectUnit(occupant);
            return;
        }
        if (selectedUnit == null) return;
        // Empty/enemy clicks must not execute movement behind an open menu.
        if (selectionStep == SelectionStep.Actions || selectionStep == SelectionStep.Weapons ||
            selectionStep == SelectionStep.Hangar || selectionStep == SelectionStep.Boarding) return;
        if (selectionStep == SelectionStep.Deployment)
        {
            // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Exit selection previously kept the carrier selected and immediately reopened its Hangar.
            // The selected passenger now uses its movement from the carrier and receives its own action menu at the destination.
            // Launch EN and Accel are saved for cancellation, while an already-spent passenger only receives its existing adjacent exit.
            Mothership ship = deploymentShip;
            int launchEnergy = selectedUnit.CurrentEnergy;
            bool launchAccel = selectedUnit.AccelActive;
            bool usesMovement = !selectedUnit.HasMoved && !selectedUnit.HasActed;
            if (ship != null && ship.TryDeploy(passengerToDeploy, position))
            {
                deploymentEnergy = launchEnergy;
                deploymentHadAccel = launchAccel;
                deploymentUsedMovement = usesMovement;
                selectedUnit.ShowDockedPreview(false);
                if (selectedUnit.HasActed) CommitDeployment();
                if (combatHUD != null) combatHUD.ShowSelectedUnit(selectedUnit);
                ShowActions();
            }
            return;
        }

        if (selectionStep == SelectionStep.Targets)
        {
            // WEEK 3: Send enemy clicks through the target check before attacking.
            ChooseAttackTarget(occupant);
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
        Vector2Int moveOrigin = selectedUnit.GridPosition;
        int energyBeforeMove = selectedUnit.CurrentEnergy;
        if (occupant == null && battlefield.TryMove(selectedUnit, position))
        {
            unconfirmedMoves[selectedUnit] = (moveOrigin, energyBeforeMove);
            battlefield.ShowMovement(selectedUnit);
            // Preserve the team's terrain HUD refresh after movement.
            if (combatHUD != null) combatHUD.ShowSelectedUnit(selectedUnit);
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
        // WEEK 4: MOTHERSHIP - Show transport choices alongside the existing actions.
        actionsMenu.ShowMothershipActions(selectedUnit, CanBoardSelected());
        // WEEK 5 CHANGES PLEASE READ: Saved mech command buttons currently have no availability refresh in the action menu.
        // They must follow the selected unit's actual recipe, resources and action state rather than staying visible for every mech.
        // The menu now reads the shared skill rules here, keeping ordinary action and transport handling in place.
        actionsMenu.ShowMechSkillActions(selectedUnit);
        // WEEK 4: MOTHERSHIP - One menu refresh always restores the correct Dock/Standby state.
        actionsMenu.SetDockConfirmation(pendingDock != null);
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: A passenger hovering above the occupied carrier tile cannot finish an action on that tile.
        // Its preview menu therefore offers Move to choose an adjacent exit while Attack and Standby wait until it is placed outside.
        // This reuses the normal menu and keeps existing docked-unit combat restrictions intact.
        if (deploymentShip != null && selectedUnit == passengerToDeploy && selectedUnit.IsDocked)
            actionsMenu.ShowDeploymentPreviewActions();
        if (pendingDock != null) battlefield.ClearHighlights();

        // WEEK 3: Position the Action HUD beside the currently selected unit
        // before displaying the player's available actions.
        actionsMenu.PositionActionsBesideUnit(selectedUnit, battleCamera);
    }

    // WEEK 4: MOTHERSHIP - Choose a neighboring friendly ship, then open its stored-unit list.
    private bool CanBoardSelected()
    {
        if (selectedUnit == null || selectedUnit.HasActed || selectedUnit.IsDocked) return false;
        foreach (BattleUnit unit in battlefield.Units)
            if (unit.TryGetComponent(out Mothership ship) && ship.CanDock(selectedUnit)) return true;
        return false;
    }

    // WEEK 4: MOTHERSHIP - Legacy Board callbacks now enter movement instead of docking from adjacency.
    public void OpenBoarding() => OpenMovement();

    private void CancelDockPreview()
    {
        if (ReferenceEquals(pendingDock, null)) return;
        if (selectedUnit != null) selectedUnit.transform.position = battlefield.GridToWorld(selectedUnit.GridPosition);
        pendingDock = null;
        actionsMenu.SetDockConfirmation(false);
    }

    // WEEK 5 FIXES: Originally, both gameplay scenes overrode the passenger-row template with null, disabling this hangar flow.
    // Those overrides were removed so the scenes inherit the configured template already present in the W4 UI prefab.
    // This allows the existing passenger list and deployment callbacks to work without creating another UI system.
    public void OpenHangar()
    {
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 DEBUG: Hangar requests previously returned silently when their existing guards rejected them.
        // This diagnostic records callback arrival, player-phase state, selected ship, pending docking and required UI availability.
        // It distinguishes a button that never sends a click from a request blocked by battle state without changing those guards.
        Debug.Log($"[WEEK 6 DOCK DEBUG] OpenHangar requested: selected={selectedUnit?.name ?? "none"}, playerPhase={isPlayerTurn}, step={selectionStep}, pendingDock={pendingDock != null}, defeated={selectedUnit?.IsDefeated}, isShip={selectedUnit != null && selectedUnit.GetComponent<Mothership>() != null}, hangarUI={actionsMenu != null && actionsMenu.HasHangarUI}, frame={Time.frameCount}.", this);
        if (pendingDock != null) return;
        if (!isPlayerTurn || selectedUnit == null || selectedUnit.IsDefeated ||
            !selectedUnit.TryGetComponent(out Mothership ship) || !actionsMenu.HasHangarUI) return;
        passengerToDeploy = null;
        selectionStep = SelectionStep.Hangar;
        battlefield.ClearHighlights();
        actionsMenu.ShowPassengers(ship);
    }

    public void ChoosePassenger(BattleUnit passenger)
    {
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 DEBUG: Passenger-row clicks previously gave no indication that the deployment callback was reached.
        // This diagnostic records the clicked passenger, current menu step and player-phase state before the existing checks run.
        // It helps locate a stalled transition between the Hangar list and exit-tile selection without altering selection behavior.
        Debug.Log($"[WEEK 6 DOCK DEBUG] Passenger clicked: selected={selectedUnit?.name ?? "none"}, passenger={passenger?.name ?? "none"}, playerPhase={isPlayerTurn}, step={selectionStep}, dockedAt={(passenger != null && passenger.DockedAt != null ? passenger.DockedAt.name : "none")}, moved={passenger?.HasMoved}, acted={passenger?.HasActed}.", this);
        if (!isPlayerTurn || selectionStep != SelectionStep.Hangar || selectedUnit == null ||
            !selectedUnit.TryGetComponent(out Mothership ship) || !ship.CanDeploy(passenger)) return;
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Clicking Deploy previously skipped the passenger's menu and went directly to exit selection.
        // It now selects the stored passenger, displays its sprite above the carrier and opens its actions without removing it from the Hangar.
        // The original action flags are retained so cancellation or same-phase deployment cannot grant another turn.
        selectedUnit.SetSelected(false);
        deploymentShip = ship;
        passengerToDeploy = passenger;
        deploymentHadMoved = passenger.HasMoved;
        deploymentHadActed = passenger.HasActed;
        deploymentUsedMovement = false;
        selectedUnit = passenger;
        passenger.SetGridPosition(ship.Unit.GridPosition);
        passenger.SetSelected(true);
        passenger.ShowDockedPreview(true);
        if (combatHUD != null) combatHUD.ShowSelectedUnit(passenger);
        ShowActions();
    }

    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: A pending launch must not strand an off-grid preview or leave a cancelled passenger outside.
    // Cancellation returns temporary placement aboard, refunds launch movement EN and Accel, and restores the original action flags.
    // The preview sprite and selection bookkeeping are then cleared while the carrier keeps its original grid occupancy.
    private void CancelDeployment()
    {
        if (deploymentShip == null || passengerToDeploy == null) return;
        BattleUnit passenger = passengerToDeploy;
        if (!passenger.IsDocked && !deploymentShip.RestoreDeploymentPassenger(passenger))
        {
            Debug.LogWarning("[WEEK 6 SETUP WARNING] Could not return the pending passenger to its ship; keeping its existing map placement.", passenger);
            CommitDeployment();
            return;
        }
        if (deploymentUsedMovement)
        {
            passenger.RestoreMove(deploymentEnergy);
            if (deploymentHadAccel) passenger.ActivateAccel();
        }
        else if (unconfirmedMoves.TryGetValue(passenger, out var move)) passenger.RestoreMove(move.energy);
        unconfirmedMoves.Remove(passenger);
        passenger.HasMoved = deploymentHadMoved;
        passenger.HasActed = deploymentHadActed;
        passenger.SetSelected(false);
        passenger.ShowDockedPreview(false);
        deploymentShip = null;
        passengerToDeploy = null;
    }

    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Confirming a launched passenger's action makes its transport placement permanent.
    // This releases the preview state and commits the carrier's previous transport move only after the passenger has left the ship.
    // It does not reset the passenger's action flags or change normal combat and Standby costs.
    private void CommitDeployment(bool completedBoarding = false)
    {
        if (deploymentShip == null || passengerToDeploy == null || (passengerToDeploy.IsDocked && !completedBoarding)) return;
        unconfirmedMoves.Remove(deploymentShip.Unit);
        passengerToDeploy.ShowDockedPreview(false);
        deploymentShip = null;
        passengerToDeploy = null;
    }

    private bool HasAnyTarget()
    {
        if (selectedUnit == null) return false;
        foreach (BattleUnit target in battlefield.Units)
            if (selectedUnit.GetUsableWeapon(target) != null) return true;
        return false;
    }

    // WEEK 5 CHANGES PLEASE READ: BattleSystem currently has no command entry points for mech transformations or support devices.
    // These callbacks delegate ability rules to MechSkillEffect and retain this controller's selection, undo and objective bookkeeping.
    // This makes saved buttons and keyboard commands work together without duplicating the ability implementations.
    private bool CanIssueMechCommand => CanEndPlayerPhase && selectedUnit != null && !selectedUnit.IsDefeated &&
        selectedUnit.Team == playerTeam && pendingDock == null && pendingSpiritCommand == null &&
        (selectionStep == SelectionStep.Actions || selectionStep == SelectionStep.Movement || selectionStep == SelectionStep.Standby);

    private void FinishMechCommand()
    {
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Successful mech commands already commit a selected unit's movement.
        // They must also finish any pending launch before normal command bookkeeping continues.
        // This prevents a completed command from later being undone by returning its passenger to the Hangar.
        CommitDeployment();
        unconfirmedMoves.Remove(selectedUnit);
        foreach (MechSkillEffect.Part part in selectedUnit.MechSkillState.Parts) unconfirmedMoves.Remove(part.Unit);
        if (combatHUD != null) combatHUD.ShowSelectedUnit(selectedUnit);
        if (selectedUnit.HasActed) ReturnToPlayerSelection(selectedUnit);
        else ShowActions();
    }
    public void TransformSelected()
    {
        if (CanIssueMechCommand && MechSkillEffect.TryTransform(selectedUnit)) FinishMechCommand();
    }
    public void GetterChangeSelected()
    {
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER's button could stop silently at the controller's existing command guard.
        // An attempted button command now logs whether selection, phase or another pending command is blocking it.
        // The same guard still decides permission, so this adds an explanation without changing when a unit can transform.
        if (!CanIssueMechCommand)
        {
            string reason = !isActiveAndEnabled ? "BattleSystem is disabled or inactive." :
                ScenarioEnded ? "The scenario has ended." :
                !isPlayerTurn ? "It is not the player phase." :
                selectedUnit == null ? "No unit is selected." :
                selectedUnit.IsDefeated ? "The selected unit is defeated." :
                selectedUnit.Team != playerTeam ? "The selected unit is not on the player team." :
                pendingDock != null ? "Finish or cancel the docking preview first." :
                pendingSpiritCommand != null ? "Finish or cancel Spirit targeting first." :
                "Return to unit actions before changing form (current menu: " + selectionStep + ").";
            Debug.LogWarning("[WEEK 5 GETTER] Getter Change blocked: " + reason, this);
            return;
        }
        if (MechSkillEffect.TryGetterChange(selectedUnit)) FinishMechCommand();
    }
    public void CombineSelected()
    {
        if (!CanIssueMechCommand) return;
        var objectives = (playerObjectiveUnit, enemyObjectiveUnit);
        if (!MechSkillEffect.TryCombine(selectedUnit)) return;
        combinationObjectives[selectedUnit] = objectives;
        foreach (MechSkillEffect.Part part in selectedUnit.MechSkillState.Parts)
        {
            if (playerObjectiveUnit == part.Unit) playerObjectiveUnit = selectedUnit;
            if (enemyObjectiveUnit == part.Unit) enemyObjectiveUnit = selectedUnit;
        }
        FinishMechCommand();
    }
    public void SeparateSelected()
    {
        if (!CanIssueMechCommand || !MechSkillEffect.CanSeparate(selectedUnit)) return;
        List<BattleUnit> parts = new();
        foreach (MechSkillEffect.Part part in selectedUnit.MechSkillState.Parts) parts.Add(part.Unit);
        if (!MechSkillEffect.TrySeparate(selectedUnit)) return;
        if (combinationObjectives.TryGetValue(selectedUnit, out var objectives))
        {
            if (playerObjectiveUnit == selectedUnit) playerObjectiveUnit = objectives.player;
            if (enemyObjectiveUnit == selectedUnit) enemyObjectiveUnit = objectives.enemy;
            combinationObjectives.Remove(selectedUnit);
        }
        foreach (BattleUnit part in parts) unconfirmedMoves.Remove(part);
        FinishMechCommand();
    }
    public void OpenRepair() => OpenMechSupport(false);
    public void OpenResupply() => OpenMechSupport(true);
    private void OpenMechSupport(bool resupply)
    {
        if (!CanIssueMechCommand || selectedUnit.HasActed) return;
        bool available = false;
        foreach (BattleUnit target in battlefield.Units)
            if (resupply ? MechSkillEffect.CanResupply(selectedUnit, target) : MechSkillEffect.CanRepair(selectedUnit, target))
                available = true;
        if (!available) { MechSkillEffect.Log(selectedUnit, $"{(resupply ? "Resupply" : "Repair")} has no eligible adjacent target."); return; }
        resupplyTargeting = resupply;
        selectionStep = SelectionStep.MechSupport;
        actionsMenu.Hide(); battlefield.ClearHighlights();
        foreach (BattleUnit target in battlefield.Units)
            if (resupply ? MechSkillEffect.CanResupply(selectedUnit, target) : MechSkillEffect.CanRepair(selectedUnit, target))
                battlefield.ShowTransportCell(target.GridPosition);
    }

    // WEEK 3: Both the menu and target highlights check the same selected weapon.
    public bool HasTarget(Weapon weapon)
    {
        // WEEK 6 CHANGES PLEASE READ: Weapon rows previously searched every enemy using player-phase selection.
        // During counter selection the incoming attacker is the only valid target and defensive movement rules apply.
        // Ordinary weapon menus continue through the original checks below.
        if (IsChoosingCounterWeapon) return CanCounterWith(weapon);
        if (selectedUnit == null || weapon == null) return false;
        foreach (BattleUnit target in battlefield.Units)
            if (selectedUnit.CanUseWeapon(weapon, target)) return true;
        return false;
    }

    // WEEK 3: Close the action choices and return to the selected unit's movement range.
    public void OpenMovement()
    {
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Move on the hovering preview now shows the passenger's movement range from the carrier.
        // The highlighted cells use the same paths and EN limit as normal movement, while spent passengers retain adjacent retrieval.
        // The passenger stays aboard until a valid destination is clicked, and a fresh launch consumes its one movement for this turn.
        if (isPlayerTurn && deploymentShip != null && selectedUnit == passengerToDeploy && selectedUnit.IsDocked && selectionStep == SelectionStep.Actions)
        {
            actionsMenu.Hide();
            selectionStep = SelectionStep.Deployment;
            battlefield.ClearHighlights();
            foreach (Vector2Int cell in deploymentShip.GetDeploymentMovementCells(selectedUnit).Keys) battlefield.ShowTransportCell(cell);
            return;
        }
        // WEEK 3: Report whether the Move button can return to movement selection.
        if (!isPlayerTurn || selectedUnit == null || selectionStep != SelectionStep.Actions || selectedUnit.HasActed)
        {
            Debug.Log($"[Move Debug] Move button ignored. PlayerTurn: {isPlayerTurn}; selected: {selectedUnit != null}; step: {selectionStep}.", this);
            return;
        }
        CancelDockPreview();
        if (selectedUnit.HasMoved && !TryUndoSelectedMove()) return;
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
        if (!isPlayerTurn || pendingDock != null || selectionStep != SelectionStep.Actions || selectedUnit == null || selectedUnit.HasActed || !hasTarget) return;
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
        // WEEK 6 CHANGES PLEASE READ: Counter weapon previews must work while normal player commands are locked.
        // A usable row now updates only the pending exchange's counter weapon and existing weapon details.
        // It never switches the initiating weapon, changes battlefield targets or spends resources.
        if (IsChoosingCounterWeapon)
        {
            if (CanCounterWith(weapon)) { reactionCounterWeapon = weapon; actionsMenu.SetWeapon(weapon); }
            return;
        }
        if (!isPlayerTurn || selectionStep != SelectionStep.Weapons) return;
        selectedWeapon = weapon;
        actionsMenu.SetWeapon(weapon);
        battlefield.ShowWeaponRange(selectedUnit, weapon);
    }

    // WEEK 3: Confirm or double-click closes the list and allows this weapon's targets.
    public void ConfirmWeapon()
    {
        // WEEK 6 CHANGES PLEASE READ: Confirming a counter weapon should return to the combat preview.
        // The defensive branch validates that weapon against the incoming attacker and closes only the weapon picker.
        // Begin Combat remains a separate confirmation, and ordinary attack confirmation keeps its original flow.
        if (IsChoosingCounterWeapon)
        {
            if (!CanCounterWith(reactionCounterWeapon)) return;
            choosingCounterWeapon = false;
            ShowReactionMenu();
            return;
        }
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

    // WEEK 4: MOTHERSHIP - Undo only an unconfirmed move; never overwrite another unit's tile.
    private bool TryUndoSelectedMove()
    {
        if (!CanUndoSelectedMove) return false;
        var move = unconfirmedMoves[selectedUnit];
        if (!battlefield.TryUndoMove(selectedUnit, move.position, move.energy)) return false;
        unconfirmedMoves.Remove(selectedUnit);
        return true;
    }

    // WEEK 3: Right-click returns to the previous menu step.
    public void Back()
    {
        // WEEK 6 CHANGES PLEASE READ: Back previously required Player Phase and could not leave a counter weapon list.
        // A pending reaction now returns to its preview without cancelling the enemy's committed attack.
        // This branch runs before the ordinary phase guard and does not undo movement or alter the selected player unit.
        if (reactionPending)
        {
            choosingCounterWeapon = false;
            ShowReactionMenu();
            return;
        }
        if (!isPlayerTurn || selectedUnit == null) return;
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER targeted Spirits retain both a command and its owning pilot until selection completes.
        // Back must clear that pair without applying an effect or spending either pilot's SP.
        // Returning to the existing action menu prevents a cancelled sub-pilot command from executing on a later map click.
        if (pendingSpiritCommand != null)
        {
            pendingSpiritCommand = null; pendingSpiritPilot = null;
            ShowActions(); return;
        }
        // WEEK 5 CHANGES PLEASE READ: The existing Back handler cannot recognize a pending friendly support command.
        // Repair/Resupply targeting needs to cancel without healing, resupplying or spending an action.
        // Returning to ShowActions reuses the existing menu reset and clears its support highlights.
        if (selectionStep == SelectionStep.MechSupport) { ShowActions(); return; }
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Back from a pending launch must leave the passenger inside its original carrier.
        // Cancelling exit targeting first returns to the hovering menu, while cancelling that menu or subsequent movement returns to the Hangar.
        // Weapon and Spirit submenus retain their existing Back behavior so no cancelled attack or command is executed.
        if (deploymentShip != null && selectionStep == SelectionStep.Deployment) { ShowActions(); return; }
        if (deploymentShip != null && (selectionStep == SelectionStep.Actions || selectionStep == SelectionStep.Movement))
        {
            Mothership carrier = deploymentShip;
            CancelDeployment();
            SelectUnit(carrier.Unit);
            OpenHangar();
            return;
        }
        if (pendingDock != null)
        {
            CancelDockPreview();
            actionsMenu.Hide();
            selectionStep = SelectionStep.Movement;
            battlefield.ShowMovement(selectedUnit);
            return;
        }
        // WEEK 4: MOTHERSHIP - Cancel transport without moving a unit or consuming an action.
        if (selectionStep == SelectionStep.Deployment)
        {
            OpenHangar();
            return;
        }
        if (selectionStep == SelectionStep.Boarding || selectionStep == SelectionStep.Hangar)
        {
            ShowActions();
            return;
        }
        // WEEK 4: MOTHERSHIP - Leave an acted ship's menu so another unit can be selected.
        if (selectionStep == SelectionStep.Actions && selectedUnit.HasActed)
        {
            actionsMenu.Hide();
            battlefield.ClearHighlights();
            selectionStep = SelectionStep.Standby;
            return;
        }
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
        else if (selectionStep == SelectionStep.Actions || selectionStep == SelectionStep.Movement)
        {
            // WEEK 4: MOTHERSHIP - Back out of an unconfirmed move and restore its EN.
            if (selectedUnit.HasMoved && !TryUndoSelectedMove())
            {
                Debug.Log("Cannot undo this move: its starting tile is occupied or the move is committed.", this);
                return;
            }
            actionsMenu.Hide();
            selectedWeapon = null;
            selectionStep = SelectionStep.Movement;
            battlefield.ShowMovement(selectedUnit);
        }
    }

    // WEEK 3: Standby ends this unit's action without undoing its move.
    // WEEK 3: Standby permanently completes this unit's action for the
    // current Player Phase and visually marks the unit as finished.
    public void Standby()
    {
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: A hovering passenger cannot use Standby to occupy the carrier's tile.
        // Standby remains available after choosing an exit, where it confirms the pending launch through the existing action-ending flow.
        // This guard prevents keyboard or direct callbacks from bypassing the preview menu's disabled Standby button.
        if (deploymentShip != null && selectedUnit != null && selectedUnit.IsDocked) return;
        if (!isPlayerTurn || selectedUnit == null ||
            selectionStep != SelectionStep.Actions || selectedUnit.HasActed)
        {
            return;
        }

        // WEEK 4: MOTHERSHIP - The renamed Standby button confirms the pending docking move.
        if (pendingDock != null)
        {
            Mothership carrier = pendingDock;
            if (!carrier.TryDock(selectedUnit))
            {
                CancelDockPreview();
                ShowActions();
                return;
            }
            // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: An exited passenger can choose normal boarding before it finishes its pending launch.
            // Successful boarding confirms that launch before selection changes, including when it boards its original carrier again.
            // The action spent by TryDock is therefore preserved instead of being restored by preview cancellation.
            CommitDeployment(true);
            pendingDock = null;
            unconfirmedMoves.Remove(selectedUnit);
            SelectUnit(carrier.Unit);
            OpenHangar();
            return;
        }

        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Standby confirms an exited passenger's pending launch before ending its action.
        // The source ship can no longer undo its transport move once that action is committed.
        // Ordinary Standby still follows the same existing movement and action bookkeeping below.
        CommitDeployment();
        actionsMenu.Hide();
        selectedWeapon = null;
        battlefield.ClearHighlights();

        // WEEK 3: Mark this individual unit as finished without ending
        // the entire Player Phase.
        BattleUnit completedUnit = selectedUnit;
        completedUnit.SetSelected(false);
        completedUnit.MarkActed();
        unconfirmedMoves.Remove(completedUnit);
        ReturnToPlayerSelection(completedUnit);
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

    // WEEK 4 TURN FLOW: A completed unit must not remain as the active selection.
    // Clear its UI/state and hand control to the next unit that can still act.
    private void ReturnToPlayerSelection(BattleUnit completedUnit)
    {
        if (completedUnit != null) completedUnit.SetSelected(false);

        selectedUnit = null;
        selectedWeapon = null;
        pendingDock = null;
        passengerToDeploy = null;
        selectionStep = SelectionStep.Movement;
        actionsMenu.Hide();
        battlefield.ClearHighlights();

        if (combatHUD != null)
        {
            combatHUD.ClearSelectedUnit();
            combatHUD.ClearTargetedEnemy();
        }

        isPlayerTurn = !ScenarioEnded && FindFirstUnit(playerTeam) != null &&
            FindFirstUnit(OpposingTeam(playerTeam)) != null;
        if (!isPlayerTurn) return;

        foreach (BattleUnit unit in battlefield.Units)
        {
            if (unit == null || unit.Team != playerTeam || unit.IsDefeated ||
                unit.IsDocked || unit.HasActed) continue;
            SelectUnit(unit);
            break;
        }
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

        // WEEK 5: MUSIC SYSTEM - Restore the stage's overworld music
        // when a new Player Phase begins.
        if (battleMusic != null)
        {
            battleMusic.PlayOverworldMusic();
        }

        // WEEK 3: Update the Combat HUD when the player's phase begins
        // while keeping the current turn number visible.
        if (combatHUD != null)
        {
            combatHUD.ShowBattleStatus("Player Phase", currentTurn);
        }

        // WEEK 3: Reset player movement at the start of the player turn.
        foreach (BattleUnit unit in battlefield.Units)
        {
            if (unit.Team == playerTeam)
            {
                unit.BeginTurn();

                // WEEK 5 CHANGES PLEASE READ: The player phase already adds a baseline 8% EN recovery to every unit.
                // Adding S/M/L regeneration on top would turn the requested 10/20/30% into 18/28/38%.
                // Units with EN Regen now use its phase hook alone, while ordinary units keep their existing baseline recovery.
                if (unit.Mech.ENRegeneration == RegenerationLevel.None) unit.RechargeEnergy();
            }
        }

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

        // WEEK 4 TURN FLOW: Release the completed attacker and continue with
        // another living player unit that has not acted this phase.
        ReturnToPlayerSelection(attackingUnit);
    }

    // WEEK 3: Allow the player to manually end the Player Phase.
    // Individual units keep their movement/action state until the next
    // Player Phase begins, preventing any unit from acting twice.
    public void EndPlayerPhase()
    {
        if (!CanEndPlayerPhase)
        {
            return;
        }
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Ending the phase cancels any passenger launch that has not confirmed an action.
        // The unit returns aboard before enemy targeting or the next passenger turn reset can run.
        // Completed deployments and the normal end-phase sequence remain unchanged.
        CancelDeployment();
        // WEEK 4: MOTHERSHIP - Unconfirmed docking is canceled before enemies can act.
        CancelDockPreview();
        passengerToDeploy = null;
        unconfirmedMoves.Clear();
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER targeted Spirits retain a pilot owner while waiting for a map click.
        // Ending the phase must discard that unfinished command before the next phase selects another unit.
        // Clearing both pending fields here prevents a previous crew member's command from charging a newly selected unit's SP.
        pendingSpiritCommand = null; pendingSpiritPilot = null;

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

        // WEEK 5: MUSIC SYSTEM - Return to the stage's overworld music
        // when the Player Phase ends.
        if (battleMusic != null)
        {
            battleMusic.PlayOverworldMusic();
        }

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

        // WEEK 6 CHANGES PLEASE READ: Incoming non-MAP attacks need a player response before either unit fires.
        // The dedicated exchange waits for confirmation and then reuses PerformAttack for costs, hits and results.
        // Player-initiated attacks and MAP attacks continue through their existing resolution below.
        if (attacker.Team != playerTeam && target.Team == playerTeam &&
            weapon.Classification != WeaponClassification.Map && actionsMenu != null)
        {
            yield return ResolveReactionAttack(attacker, target, weapon);
            yield break;
        }

        // WEEK 5 FIXES: Originally, a MAP attack could trigger the same defensive first-strike lookup as an ordinary attack.
        // MAP attacks now skip that lookup because units in their area do not counter them.
        // Ordinary attacks still request a defensive weapon explicitly so a moved defender can use FirstStrike without PostMovement.
        Weapon firstStrike = weapon.Classification == WeaponClassification.Map ? null :
            target.GetUsableWeapon(attacker, WeaponType.FirstStrike, true);
        // WEEK 3: If both weapons have FirstStrike, the unit that started the attack goes first.
        if ((weapon.Type & WeaponType.FirstStrike) == 0 && firstStrike != null)
        {
            yield return PerformAttack(target, attacker, firstStrike);
            // WEEK 3: A unit defeated by the first strike cannot finish its attack.
            if (attacker == null || attacker.IsDefeated) yield break;
        }
        yield return PerformAttack(attacker, target, weapon);
    }

    // WEEK 6 CHANGES PLEASE READ: The existing first-strike-only exchange could not perform a selected ordinary counter.
    // This coroutine pauses before resource spending, performs at most one chosen counter and rechecks survival and affordability.
    // FirstStrike keeps its existing weapon-tag priority, and finally clears every reaction field even when combat ends early.
    private IEnumerator ResolveReactionAttack(BattleUnit attacker, BattleUnit defender, Weapon incoming)
    {
        if (!actionsMenu.HasReactionUI)
        {
            Debug.LogError("WEEK 6: Assign the reaction controls on ActionsMenu before resolving an enemy attack.", actionsMenu);
            yield break;
        }
        reactionAttacker = attacker;
        reactionDefender = defender;
        reactionIncomingWeapon = incoming;
        reactionCounterWeapon = defender.GetUsableWeapon(attacker, WeaponType.None, true);
        selectedReaction = reactionCounterWeapon != null ? BattleReaction.Counter : BattleReaction.Defend;
        reactionConfirmed = false;
        choosingCounterWeapon = false;
        reactionPending = true;
        try
        {
            ShowReactionMenu();
            while (!reactionConfirmed && !ScenarioEnded && attacker != null && !attacker.IsDefeated &&
                defender != null && !defender.IsDefeated) yield return null;
            reactionPending = false;
            actionsMenu.Hide();
            if (!reactionConfirmed || ScenarioEnded || !attacker.CanUseWeapon(incoming, defender)) yield break;

            bool counter = selectedReaction == BattleReaction.Counter && CanCounterWith(reactionCounterWeapon);
            bool first = counter && (reactionCounterWeapon.Type & WeaponType.FirstStrike) != 0 &&
                (incoming.Type & WeaponType.FirstStrike) == 0;
            if (first) yield return PerformAttack(defender, attacker, reactionCounterWeapon);
            if (ScenarioEnded || attacker == null || attacker.IsDefeated || defender == null || defender.IsDefeated) yield break;
            yield return PerformAttack(attacker, defender, incoming);
            if (!first && counter && !ScenarioEnded && CanCounterWith(reactionCounterWeapon))
                yield return PerformAttack(defender, attacker, reactionCounterWeapon);
        }
        finally
        {
            reactionPending = choosingCounterWeapon = reactionConfirmed = false;
            reactionAttacker = reactionDefender = null;
            reactionIncomingWeapon = reactionCounterWeapon = null;
            selectedReaction = BattleReaction.Counter;
            if (actionsMenu != null) actionsMenu.Hide();
        }
    }

    // WEEK 6 CHANGES PLEASE READ: Counter availability must use the incoming enemy rather than any target on the map.
    // Existing weapon validation already checks ownership, range, resources and single-target defensive eligibility.
    // Sharing it between UI confirmation and retaliation prevents moved or acted defenders from being incorrectly blocked.
    private bool CanCounterWith(Weapon weapon) => reactionDefender != null && reactionAttacker != null &&
        reactionDefender.CanUseWeapon(weapon, reactionAttacker, true);

    // WEEK 6 CHANGES PLEASE READ: The reaction preview needs its own participants while player selection remains untouched.
    // This method supplies live hit chances and the selected counter weapon to the new panel.
    // Previewing only reads battle state, so EN, ammo, morale and barriers are not consumed.
    private void ShowReactionMenu()
    {
        if (!reactionPending || reactionAttacker == null || reactionDefender == null) return;
        actionsMenu.ShowReaction(reactionAttacker, reactionDefender, reactionIncomingWeapon,
            selectedReaction, reactionCounterWeapon, CanCounterWith(reactionCounterWeapon));
    }

    // WEEK 6 CHANGES PLEASE READ: Counter now opens the existing weapon picker instead of firing immediately.
    // Its rows validate single-target weapons against this attacker using counterattack rules.
    // Confirm or Back returns to the reaction preview, leaving Begin Combat as the final commitment.
    public void ChooseCounter()
    {
        if (!reactionPending || choosingCounterWeapon || !CanCounterWith(reactionCounterWeapon)) return;
        selectedReaction = BattleReaction.Counter;
        choosingCounterWeapon = true;
        actionsMenu.ShowWeapons(reactionDefender, reactionCounterWeapon);
    }

    // WEEK 6 CHANGES PLEASE READ: Evade and Defend select a response without starting the enemy attack.
    // Their shared callback refreshes the preview and leaves the enemy coroutine waiting for Begin Combat.
    // Neither choice spends SP, changes stats or consumes the defender's normal action.
    public void ChooseEvade() => ChooseReaction(BattleReaction.Evade);
    public void ChooseDefend() => ChooseReaction(BattleReaction.Defend);
    private void ChooseReaction(BattleReaction reaction)
    {
        if (!reactionPending || choosingCounterWeapon) return;
        selectedReaction = reaction;
        ShowReactionMenu();
    }

    // WEEK 6 CHANGES PLEASE READ: Begin Combat commits the preview once and resumes the waiting enemy coroutine.
    // Counter is checked again here so an unavailable weapon cannot be confirmed through a stale button or shortcut.
    // Closing pending input immediately prevents a second click from changing the exchange after commitment.
    public void BeginReactionCombat()
    {
        if (!reactionPending || choosingCounterWeapon || reactionConfirmed || ScenarioEnded ||
            (selectedReaction == BattleReaction.Counter && !CanCounterWith(reactionCounterWeapon))) return;
        reactionConfirmed = true;
        reactionPending = false;
        actionsMenu.Hide();
    }

    // WEEK 6 CHANGES PLEASE READ: A defensive modifier must belong to one incoming weapon and defender only.
    // Matching all three references excludes retaliation, secondary adjacent targets and unrelated attacks.
    // Those other hits use Counter's neutral modifiers, preserving their existing calculations.
    private BattleReaction ReactionForHit(BattleUnit attacker, BattleUnit defender, Weapon weapon) =>
        attacker == reactionAttacker && defender == reactionDefender && weapon == reactionIncomingWeapon
            ? selectedReaction : BattleReaction.Counter;

    // WEEK 3: Pay for one weapon use and collect the units its attack will hit.
    private IEnumerator PerformAttack(BattleUnit attacker, BattleUnit target, Weapon weapon)
    {
        if (attacker == null || target == null)
        {
            yield break;
        }

        // WEEK 4: MOTHERSHIP - Ammo and EN are spent once, including first strikes and MAP attacks.
        if (weapon == null || !attacker.TrySpendWeapon(weapon))
        {
            yield break;
        }

        // WEEK 5: MUSIC SYSTEM - Play the attacking mech's assigned theme
        // after the weapon has been successfully activated.
        if (battleMusic != null && attacker.Mech != null)
        {
            battleMusic.PlayMechTheme(attacker.Mech.BattleTheme);
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
                // WEEK 5 FIXES: Originally, every MAP attack collected units by distance and always included allies in that radius.
                // We now use the selected weapon's shape and Hits Allies setting so each MAP weapon can define its own affected units.
                // The attacker and docked units are excluded, while collecting targets before damage preserves one EN/ammo charge for the whole attack.
                if (candidate == null || candidate == attacker || candidate.IsDefeated || candidate.IsDocked ||
                    candidate.Pilot == null || candidate.Mech == null ||
                    (!weapon.MapHitsAllies && candidate.Team == attacker.Team))
                    continue;
                if (weapon.IsInMapArea(attacker.GridPosition, target.GridPosition, candidate.GridPosition))
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

        // WEEK 6: Resolve every target before consuming Strike.
        foreach (BattleUnit hitTarget in targets)
        {
            if (hitTarget != null && !hitTarget.IsDefeated)
            {
                yield return ResolveWeaponHit(attacker, hitTarget, weapon);
            }
        }

        // WEEK 6: Strike expires after the entire attack,
        // including MAP and multi-target attacks.
        if (attacker != null && attacker.StrikeActive)
        {
            attacker.ConsumeStrike();
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

        // WEEK 6: Alert guarantees evasion against the next incoming attack.
        // Consume Alert before the normal accuracy and special-evasion checks.
        if (target.AlertActive)
        {
            target.ConsumeAlert();
            target.ChangeMorale(1);

            yield return ShowBattleMessage(
                targetPilot,
                PilotEmotion.Default,
                $"{targetName} avoided the attack with Alert.");

            yield break;
        }

        // WEEK 6 CHANGES PLEASE READ: The incoming hit roll previously had no Evade command modifier.
        // Resolve the response for this exact hit and use the same helper as the reaction preview.
        // Special evasion still runs afterward only when this normal roll would hit.
        // WEEK 6: Keep the existing reaction and accuracy calculations.
        BattleReaction reaction = ReactionForHit(attacker, target, weapon);

        int hitRate = BattleFormulas.ReactionHitRate(
            BattleFormulas.AccuracyRate(attacker, target, weapon, distance),
            reaction);

        // WEEK 6: Strike guarantees accuracy against every target
        // in the current attack. PerformAttack consumes it afterward.
        if (attacker.StrikeActive)
        {
            hitRate = 100;
        }

        if (Random.Range(0, 100) >= hitRate)
        {
            // WEEK 4 MORALE SYSTEM: Successfully evading an attack grants 1 morale.
            target.ChangeMorale(1);
            yield return ShowBattleMessage(targetPilot, PilotEmotion.Default, $"{GetPilotName(attacker)} missed.");
            yield break;
        }

        // WEEK 5 CHANGES PLEASE READ: Normal accuracy previously led directly to damage with no special-evasion check.
        // Double Image, Open Get and Offshoot must roll only after that attack would otherwise hit.
        // This hook cancels the hit before barrier costs or shield damage and logs the ability's roll for verification.
        if (MechSkillEffect.TrySpecialEvade(target))
        {
            target.ChangeMorale(1);
            yield return ShowBattleMessage(targetPilot, PilotEmotion.Default, $"{targetName} used special evasion.");
            yield break;
        }

        // WEEK 4 MORALE SYSTEM: Landing an attack and being hit each grant 1 morale.
        attacker.ChangeMorale(1);
        target.ChangeMorale(1);

        // WEEK 3: Roll for a critical hit and use the result when calculating damage.
        int criticalRate = Mathf.Clamp(BattleFormulas.CriticalRate(attacker, target, weapon), 0, 100);
        bool critical = Random.Range(0, 100) < criticalRate;
        // WEEK 5 CHANGES PLEASE READ: Weapon damage previously went straight to mech HP through TakeDamage.
        // Direct hits must now apply the equipped barrier and separate shield HP before the remaining damage reaches the mech.
        // The shared direct-hit entry point connects those skills while leaving TakeDamage available for poison and other bypass effects.

        // WEEK 6: Keep the existing damage formula and defensive reaction.
        int incomingDamage = BattleFormulas.ReactionDamage(
            BattleFormulas.Damage(attacker, target, weapon, critical),
            reaction);

        // WEEK 6: Persist reduces damage from the next successful attack.
        // Apply this before barriers and shields so the existing equipment
        // systems still process the remaining damage normally.
        if (target.PersistActive && incomingDamage > 0)
        {
            incomingDamage = Mathf.Max(1, incomingDamage / 8);
            target.ConsumePersist();
        }

        // WEEK 6: Preserve the existing barrier and shield damage handling.
        int damage = MechSkillEffect.TakeWeaponDamage(
            target, incomingDamage, weapon);
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

        // WEEK 4 MORALE SYSTEM: An enemy shot down grants 2 morale, while every
        // surviving ally of the defeated unit gains 1 morale for the ally loss.
        if (attacker.Team != target.Team)
        {
            attacker.ChangeMorale(2);
        }
        foreach (BattleUnit ally in battlefield.Units)
        {
            if (ally != null && !ally.IsDefeated && ally.Team == target.Team)
            {
                ally.ChangeMorale(1);
            }
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

        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Switching map selection must not abandon an unconfirmed passenger preview.
        // Clicking that same temporary passenger keeps its menu open, while selecting another unit cancels the launch and returns it aboard.
        // This leaves normal unit selection and Spirit ownership cleanup on their existing paths below.
        if (deploymentShip != null && unit == passengerToDeploy) { ShowActions(); return; }
        CancelDeployment();
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER Spirit ownership belongs to the unit that opened the command menu.
        // Selecting another unit through controller callbacks must cancel any unfinished command and its saved pilot.
        // This keeps a stale sub-pilot selection from executing against the next unit while leaving every crew member's SP unchanged.
        pendingSpiritCommand = null; pendingSpiritPilot = null;

        // WEEK 3: Deselect the previous unit first. Its visual state will
        // automatically return to grey if it already acted, or its original
        // color if it is still available this phase.
        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
        }

        CancelDockPreview();
        selectedUnit = unit;
        // WEEK 4: MOTHERSHIP - A new selection cancels an unfinished deployment choice.
        passengerToDeploy = null;

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
            // WEEK 4: MOTHERSHIP - An acted ship may still inspect/deploy passengers, but cannot act twice.
            // WEEK 5 CHANGES PLEASE READ: Selecting an acted unit normally closes its action menu completely.
            // Getter Change is explicitly allowed after acting, so a valid Getter must retain access to that command.
            // Ordinary movement/attack buttons remain disabled by HasActed, preventing this exception from granting another turn.
            if ((selectedUnit.GetComponent<Mothership>() != null && actionsMenu.HasHangarUI) || MechSkillEffect.NextGetterForm(selectedUnit) != null)
            {
                ShowActions();
                return;
            }
            selectionStep = SelectionStep.Standby;
            return;
        }

        // WEEK 3: Fresh units begin in Movement mode and may continue through
        // the normal Move, Attack, or Standby action flow.
        selectionStep = SelectionStep.Movement;
        battlefield.ShowMovement(selectedUnit);

        // WEEK 4: MOTHERSHIP - Transport actions must also be accessible when no enemy is nearby.
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER previously left fresh units in the movement view when no enemy or ship was nearby.
        // A Getter with an available form change must also open its action menu so the new W5 button is reachable immediately on selection.
        // Adding this eligibility case preserves ordinary unit selection and does not reset movement or grant another action.
        if (selectedUnit.HasMoved || HasAnyTarget() || CanBoardSelected() || selectedUnit.GetComponent<Mothership>() != null ||
            MechSkillEffect.NextGetterForm(selectedUnit) != null)
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
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Selecting a valid attack target confirms the exited passenger's pending launch.
        // The preview is released before the existing combat coroutine starts spending weapon resources.
        // Cancelling weapon or target selection earlier still returns through the normal Back flow without committing deployment.
        CommitDeployment();
        // WEEK 4: MOTHERSHIP - Confirming an attack commits its movement, even if the attack misses.
        if (selectedUnit != null) unconfirmedMoves.Remove(selectedUnit);
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

        // WEEK 3: Great Mazinger Z is the designated enemy objective. >>>> Changed to all enemy units defeated.
        // Defeating it completes the scenario with Victory.
        //if (enemyObjectiveUnit != null && enemyObjectiveUnit.IsDefeated)
        BattleTeam enemyTeam = OpposingTeam(playerTeam);

        if (FindFirstUnit(enemyTeam) == null)
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

        CancelDockPreview();
        passengerToDeploy = null;
        unconfirmedMoves.Clear();
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

        if (scenarioResult == ScenarioResult.Victory)
        {
            StartCoroutine(TransitionToIntermission());
        }
    }
    private IEnumerator TransitionToIntermission()
    {
        yield return new WaitForSeconds(2f);
        if (sceneTransition != null)
        {
            sceneTransition.FadeToScene("Intermission");
        }
        else
        {
            Debug.LogWarning("SceneTransition component is not assigned. Cannot transition to Intermission scene.", this);
        }
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
