using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

// WEEK 3: ActionsMenu only fills TMP fields that you make and assign in the Inspector.
[DisallowMultipleComponent]
public class ActionsMenu : MonoBehaviour
{
    [Header("Your Action UI")]
    [SerializeField] private CanvasGroup actionsPanel;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button standbyButton;
    // WEEK 5 CHANGES PLEASE READ: Some saved menu prefabs already contain mech buttons, but this script no longer reads those references.
    // Restoring their field names lets the existing controls call the new shared ability rules without prefab edits.
    // The references remain optional because older scenes can use the same commands through the battle keyboard shortcuts.
    [SerializeField] private Button transformButton;
    [SerializeField] private Button combineButton;
    [SerializeField] private Button separateButton;
    [SerializeField] private Button getterChangeButton;
    [SerializeField] private Button repairButton;
    [SerializeField] private Button resupplyButton;
    // Preserve the team's status panel and existing scene assignments.
    [SerializeField] private Button StatusButton;
    [SerializeField] private CanvasGroup UnitInfoPanel;
    // WEEK 4: MOTHERSHIP - Optional explicit reference; existing scenes resolve the button's label automatically.
    [SerializeField] private TMP_Text standbyLabel;

    // WEEK 4: SPIRIT COMMANDS - UI used to display and activate
    // the selected pilot's assigned Spirit Commands.
    [Header("WEEK 4: SPIRIT COMMANDS")]
    [SerializeField] private Button spiritButton;
    [SerializeField] private CanvasGroup spiritPanel;
    [SerializeField] private Button spiritBackButton;
    [SerializeField] private TMP_Text spiritPointText;
    [SerializeField] private SpiritCommandButton spiritButtonTemplate;
    [SerializeField] private RectTransform spiritButtonContent;

    // WEEK 4: Track generated Spirit Command buttons so they can
    // be cleared and rebuilt whenever the menu opens.
    private readonly List<SpiritCommandButton> spiritCommandButtons = new();
    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER can present eighteen crew commands, exceeding the saved Spirit panel's fixed list.
    // A runtime scroll viewport keeps every command reachable and clips the taller pilot-labelled rows within the existing panel.
    // This optional UI state is created only when a crew menu needs it, so no prefab hierarchy or ordinary initial menu layout needs editing.
    private ScrollRect getterSpiritScroll;

    [Header("Your Weapon UI")]
    [SerializeField] private CanvasGroup weaponsPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private RectTransform weaponContent;
    [SerializeField] private WeaponRowView weaponRowTemplate;

    // WEEK 6 CHANGES PLEASE READ: The existing action panel has no controls for an incoming enemy attack.
    // These separate reaction controls show Counter, Evade, Defend and a final Begin Combat confirmation.
    // Saved Inspector references preserve every normal action button and reuse the existing weapon panel for counter selection.
    [Header("WEEK 6: Counter, Evade and Defend")]
    [SerializeField] private CanvasGroup reactionPanel;
    [SerializeField] private Button counterButton;
    [SerializeField] private Button evadeButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button beginCombatButton;
    [SerializeField] private Button counterWeaponBackButton;
    [SerializeField] private TMP_Text reactionPreviewText;
    public bool HasReactionUI => reactionPanel != null && counterButton != null && evadeButton != null &&
        defendButton != null && beginCombatButton != null && counterWeaponBackButton != null && reactionPreviewText != null;

    // WEEK 4: MOTHERSHIP - Assign these optional controls to enable boarding and the hangar list.
    [Header("WEEK 4: MOTHERSHIP")]
    [SerializeField] private Button boardButton;
    [SerializeField] private Button hangarButton;
    [SerializeField] private CanvasGroup hangarPanel;
    [SerializeField] private RectTransform passengerContent;
    [SerializeField] private PassengerRowView passengerRowTemplate;
    [SerializeField] private TMP_Text hangarTitleText;
    [SerializeField] private Button closeHangarButton;
    private readonly List<PassengerRowView> passengerRows = new();
    public bool HasHangarUI => hangarPanel != null && passengerContent != null &&
        passengerRowTemplate != null && passengerRowTemplate.IsConfigured && hangarTitleText != null;

    [Header("Pilot and Mech TMP Fields")]
    [SerializeField] private TMP_Text pilotNameText;
    [SerializeField] private TMP_Text mechNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text moraleText;
    [FormerlySerializedAs("meleePower")][SerializeField] private TMP_Text meleeText;
    [FormerlySerializedAs("rangedPower")][SerializeField] private TMP_Text rangedText;
    [FormerlySerializedAs("defenseStat")][SerializeField] private TMP_Text defenseText;
    [FormerlySerializedAs("evadeStat")][SerializeField] private TMP_Text evadeText;
    [FormerlySerializedAs("accuracyStat")][SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text skillText;

    [Header("Pilot Terrain TMP Fields")]
    [SerializeField] private TMP_Text pilotAirText;
    [SerializeField] private TMP_Text pilotGroundText;
    [SerializeField] private TMP_Text pilotWaterText;
    [SerializeField] private TMP_Text pilotSpaceText;

    [Header("Mech Terrain TMP Fields")]
    [SerializeField] private TMP_Text mechAirText;
    [SerializeField] private TMP_Text mechGroundText;
    [SerializeField] private TMP_Text mechWaterText;
    [SerializeField] private TMP_Text mechSpaceText;

    [Header("Selected Weapon TMP Fields")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text weaponTypeText;
    [SerializeField] private TMP_Text weaponClassificationText;
    [FormerlySerializedAs("weaponDamageText")][SerializeField] private TMP_Text weaponPowerText;
    [SerializeField] private TMP_Text weaponRangeText;
    [SerializeField] private TMP_Text weaponEnergyCostText;
    [SerializeField] private TMP_Text weaponAccuracyText;
    [SerializeField] private TMP_Text weaponCriticalText;
    [SerializeField] private TMP_Text weaponAirText;
    [SerializeField] private TMP_Text weaponGroundText;
    [SerializeField] private TMP_Text weaponWaterText;
    [SerializeField] private TMP_Text weaponSpaceText;

    [Header("Optional Presentation")]
    // WEEK 4: UI FADE - Fade existing panels over 0.25 seconds; adjustable in the Inspector.
    [Min(0f)][SerializeField] private float panelFadeSeconds = 0.25f;
    [Min(0.1f)][SerializeField] private float blinkSeconds = 0.8f;
    [SerializeField] private Color normalWeaponColor = Color.white;
    [SerializeField] private Color selectedWeaponColor = new(0.3f, 0.75f, 1f);

    private readonly List<WeaponRowView> rows = new();
    private BattleSystem battle;
    private BattleUnit unit;
    private Weapon selected;
    private Weapon lastClicked;
    private float lastClickTime;

    // WEEK 3: Position the Action Panel beside the currently selected
    // player unit instead of keeping the action controls fixed on screen.
    private RectTransform actionsRect;
    private Canvas actionsCanvas;

    [SerializeField] private Vector2 actionMenuOffset = new Vector2(40f, 30f);
    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: A menu following its unit can cover another unit the player wants to select.
    // These fields remember a manually dragged position for the current unit and keep a dedicated drag handle separate from action buttons.
    // Selecting a different unit resumes automatic placement, so existing unit-following behavior remains available.
    private ActionsMenuDragHandle actionDragHandle;
    private BattleUnit actionPositionUnit;
    private bool actionPositionDragged;

    // WEEK 3: Hide only your assigned panels. This script does not create or arrange UI.
    // WEEK 3: Find the Move button created in the ActionsMenu hierarchy.
    private void Awake()
    {
        if (moveButton == null)
            moveButton = transform.Find("ActionPanel/MoveButton")?.GetComponent<Button>();

        // WEEK 6 CHANGES PLEASE READ: The transparent ActionPanel Image in UI, W4 UI and W5 UI could intercept End Turn clicks.
        // Its Raycast Target is now disabled in those saved prefabs so the decorative background no longer receives pointer hits.
        // The existing CanvasGroup and child button raycasts remain enabled, keeping action controls usable while allowing HUD clicks through.
        // WEEK 3: Cache the existing Action Panel RectTransform so its
        // screen position can follow the currently selected player unit.
        if (actionsPanel != null)
        {
            actionsRect = actionsPanel.GetComponent<RectTransform>();
        }

        // WEEK 3: Cache the Canvas containing the Actions Menu so the
        // selected unit's world position can be converted into UI space.
        actionsCanvas = GetComponentInParent<Canvas>();
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: The saved action menus previously had no drag surface.
        // This adds a small runtime handle above their existing controls without changing prefab hierarchies or button callbacks.
        // The decorative full-screen background remains non-interactive, preserving the End Turn click fix.
        PrepareActionMenuDragging();

        // WEEK 4: UI FADE - Start hidden so panels do not flash during scene loading.
        HidePanelsImmediately();
    }

    // WEEK 4: UI FADE - Reset visibility when this menu component is disabled.
    private void OnDisable()
    {
        HidePanelsImmediately();
    }

    // WEEK 4: UI FADE - Skip animation for startup and disable cleanup.
    private void HidePanelsImmediately()
    {
        // WEEK 6 CHANGES PLEASE READ: The reaction panel must start hidden and disappear when this menu is disabled.
        // It follows the same immediate visibility reset as the existing panels.
        // The counter-only Back button is also hidden so it cannot appear in a normal weapon list.
        SetVisible(reactionPanel, false, true);
        if (counterWeaponBackButton != null) counterWeaponBackButton.gameObject.SetActive(false);
        SetVisible(actionsPanel, false, true);
        SetVisible(weaponsPanel, false, true);
        SetVisible(hangarPanel, false, true);
        SetVisible(UnitInfoPanel, false, true);

        // WEEK 4: SPIRIT COMMANDS - Start the Spirit panel hidden.
        SetVisible(spiritPanel, false, true);
    }

    // WEEK 3: Require the controls and TMP fields you create before battle starts.
    public bool Bind(BattleSystem system)
    {
        battle = system;

        // WEEK 6 CHANGES PLEASE READ: Different scene and prefab assignments can make the same battle scripts behave differently.
        // These startup diagnostics identify missing controls and older UI settings before the existing binding checks run.
        // They only write Console warnings and leave Inspector assignments, button callbacks and battle rules unchanged.
        WarnSetup();

        if (!IsConfigured())
        {
            Debug.LogError("ActionsMenu is missing assigned TMP fields or controls.", this);
            return false;
        }

        // Existing battle controls.
        moveButton.onClick.AddListener(battle.OpenMovement);
        attackButton.onClick.AddListener(battle.OpenWeapons);
        standbyButton.onClick.AddListener(battle.Standby);
        confirmButton.onClick.AddListener(battle.ConfirmWeapon);
        // WEEK 6 CHANGES PLEASE READ: New reaction buttons need the same guarded runtime binding as existing controls.
        // These listeners select a response, open counter weapons or confirm the waiting exchange without persistent prefab callbacks.
        // Back reuses the controller's reaction branch, while missing references produce a clear setup error.
        if (HasReactionUI)
        {
            counterButton.onClick.AddListener(battle.ChooseCounter);
            evadeButton.onClick.AddListener(battle.ChooseEvade);
            defendButton.onClick.AddListener(battle.ChooseDefend);
            beginCombatButton.onClick.AddListener(battle.BeginReactionCombat);
            counterWeaponBackButton.onClick.AddListener(battle.Back);
        }
        else Debug.LogError("WEEK 6: ActionsMenu needs the assigned reaction panel, buttons, Back button and preview text.", this);
        if (StatusButton != null) StatusButton.onClick.AddListener(UnitScreen);
        // WEEK 5 CHANGES PLEASE READ: The restored button references need runtime listeners just like Move and Attack.
        // These listeners call the guarded BattleSystem callbacks so UI clicks follow phase, targeting and undo rules.
        // Optional null checks preserve menus that have no mech buttons assigned.
        if (transformButton != null) transformButton.onClick.AddListener(battle.TransformSelected);
        if (combineButton != null) combineButton.onClick.AddListener(battle.CombineSelected);
        if (separateButton != null) separateButton.onClick.AddListener(battle.SeparateSelected);
        if (getterChangeButton != null) getterChangeButton.onClick.AddListener(battle.GetterChangeSelected);
        if (repairButton != null) repairButton.onClick.AddListener(battle.OpenRepair);
        if (resupplyButton != null) resupplyButton.onClick.AddListener(battle.OpenResupply);

        // WEEK 4: SPIRIT COMMANDS - Connect the Spirit menu controls.
        if (spiritButton != null)
            spiritButton.onClick.AddListener(OpenSpiritMenu);

        if (spiritBackButton != null)
            spiritBackButton.onClick.AddListener(CloseSpiritMenu);

        // WEEK 4: The template itself stays hidden.
        // Copies of it are created when the Spirit menu opens.
        if (spiritButtonTemplate != null)
            spiritButtonTemplate.gameObject.SetActive(false);

        // WEEK 6 CHANGES PLEASE READ: UI, W4 UI and W5 UI contained an always-visible DockButton with no assigned click handler.
        // That unconnected duplicate is now inactive in those prefabs, leaving the assigned W4 Dock Button as the movement shortcut.
        // Standby still becomes the sole Dock confirmation after choosing a reachable ship, and the existing boarding callbacks remain unchanged.
        // WEEK 4: MOTHERSHIP - Optional references keep existing scenes working until UI is assigned.
        if (boardButton != null)
            boardButton.onClick.AddListener(battle.OpenBoarding);

        if (hangarButton != null)
            hangarButton.onClick.AddListener(battle.OpenHangar);

        if (closeHangarButton != null)
            closeHangarButton.onClick.AddListener(battle.Back);

        if (passengerRowTemplate != null)
            passengerRowTemplate.gameObject.SetActive(false);

        return true;
    }

    // WEEK 6 CHANGES PLEASE READ: The old missing-UI messages did not identify the individual Inspector fields needing attention.
    // This startup check names required action, reaction and Hangar references and reports unavailable UI input components.
    // It also detects the transparent background and duplicate Dock settings from older prefabs without repairing or disabling anything.
    private void WarnSetup()
    {
        WarnMissingSetupFields("Action/weapon menu",
            (actionsPanel, nameof(actionsPanel)), (weaponsPanel, nameof(weaponsPanel)),
            (moveButton, nameof(moveButton)), (attackButton, nameof(attackButton)),
            (standbyButton, nameof(standbyButton)), (confirmButton, nameof(confirmButton)),
            (weaponContent, nameof(weaponContent)), (weaponRowTemplate, nameof(weaponRowTemplate)),
            (pilotNameText, nameof(pilotNameText)), (mechNameText, nameof(mechNameText)),
            (healthText, nameof(healthText)), (energyText, nameof(energyText)), (moraleText, nameof(moraleText)),
            (weaponNameText, nameof(weaponNameText)), (weaponTypeText, nameof(weaponTypeText)),
            (weaponRangeText, nameof(weaponRangeText)), (weaponEnergyCostText, nameof(weaponEnergyCostText)),
            (weaponAccuracyText, nameof(weaponAccuracyText)), (weaponCriticalText, nameof(weaponCriticalText)),
            (weaponAirText, nameof(weaponAirText)), (weaponGroundText, nameof(weaponGroundText)),
            (weaponWaterText, nameof(weaponWaterText)), (weaponSpaceText, nameof(weaponSpaceText)));
        WarnMissingSetupFields("Counter/Evade/Defend menu",
            (reactionPanel, nameof(reactionPanel)), (counterButton, nameof(counterButton)),
            (evadeButton, nameof(evadeButton)), (defendButton, nameof(defendButton)),
            (beginCombatButton, nameof(beginCombatButton)), (counterWeaponBackButton, nameof(counterWeaponBackButton)),
            (reactionPreviewText, nameof(reactionPreviewText)));
        if (boardButton != null || hangarButton != null || FindObjectsByType<Mothership>().Length > 0)
        {
            WarnMissingSetupFields("Hangar menu",
                (hangarButton, nameof(hangarButton)), (hangarPanel, nameof(hangarPanel)),
                (passengerContent, nameof(passengerContent)), (passengerRowTemplate, nameof(passengerRowTemplate)),
                (hangarTitleText, nameof(hangarTitleText)), (closeHangarButton, nameof(closeHangarButton)));
            if (passengerRowTemplate != null && !passengerRowTemplate.IsConfigured)
                SetupWarning("passengerRowTemplate is incomplete. Assign its PassengerRowView Button and Label fields so docked units can be listed and deployed.");
        }
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isActiveAndEnabled)
            SetupWarning("The menu needs an active parent Canvas. Check the Canvas assignment and active state in this scene.");
        else if (!canvas.TryGetComponent(out GraphicRaycaster raycaster) || !raycaster.isActiveAndEnabled)
            SetupWarning("The parent Canvas needs an enabled GraphicRaycaster for menu button clicks.");
        EventSystem events = EventSystem.current;
        if (events == null)
            SetupWarning("No active EventSystem was found. Add or enable the scene's EventSystem to receive UI clicks.");
        else
        {
            bool hasInputModule = false;
            foreach (BaseInputModule module in events.GetComponents<BaseInputModule>())
                hasInputModule |= module.isActiveAndEnabled;
            if (!hasInputModule)
                SetupWarning("The active EventSystem has no enabled UI input module. Check its InputSystemUIInputModule and assigned UI actions.");
        }
        if (actionsPanel != null && actionsPanel.TryGetComponent(out Image background) && background.raycastTarget && background.color.a == 0f)
            SetupWarning("ActionPanel's transparent Image still has Raycast Target enabled. This older prefab setting can block End Turn; untick Raycast Target on that Image while keeping the buttons enabled.");
        foreach (Button button in GetComponentsInChildren<Button>(true))
            if (button.name == "DockButton" && button != boardButton && button != standbyButton && button.gameObject.activeSelf)
                SetupWarning("An extra DockButton is active but is not the assigned boardButton or standbyButton. Check for the unused duplicate in an older UI prefab; the assigned shortcut and Standby-to-Dock confirmation already handle boarding.");
    }

    // WEEK 6 CHANGES PLEASE READ: A single generic setup message made it difficult to compare two copies of the same scene.
    // This helper collects missing references into one warning per UI feature using the exact serialized field names.
    // The warnings run during binding rather than every frame and point to the menu that needs Inspector assignments.
    private void WarnMissingSetupFields(string feature, params (Object reference, string field)[] fields)
    {
        List<string> missing = new();
        foreach (var field in fields)
            if (field.reference == null) missing.Add(field.field);
        if (missing.Count > 0)
            SetupWarning($"{feature} is missing Inspector assignments: {string.Join(", ", missing)}. Assign these fields on this ActionsMenu and compare the scene/prefab with your working copy.");
    }

    // WEEK 6 CHANGES PLEASE READ: Matching object names alone cannot identify which scene contains a setup problem.
    // This helper includes the scene path and menu name in every new setup warning and supplies the object as Console context.
    // Clicking a warning therefore selects the affected menu without changing any runtime setup.
    private void SetupWarning(string message) =>
        Debug.LogWarning($"[WEEK 6 SETUP WARNING] scene='{gameObject.scene.path}', menu='{name}': {message}", this);

    // WEEK 4: MOTHERSHIP - Refresh after input callbacks, so Dock cannot be left showing Standby.
    private void LateUpdate()
    {
        // WEEK 4: UI FADE - A closing panel remains visible briefly but is no longer an active menu.
        if (battle != null && actionsPanel != null && actionsPanel.interactable)
            SetDockConfirmation(battle.IsDockConfirmation);
    }

    // WEEK 3: Blink only the selected weapon row using colors you choose.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && UnitInfoPanel != null && UnitInfoPanel.interactable)
        {
            SetVisible(UnitInfoPanel, false);
            SetVisible(actionsPanel, true);
            return;
        }
        // WEEK 4: UI FADE - Update all three existing panels before the weapon-only update.
        FadePanel(actionsPanel);
        FadePanel(weaponsPanel);
        FadePanel(hangarPanel);
        FadePanel(UnitInfoPanel);

        // WEEK 4: SPIRIT COMMANDS - Update Spirit panel visibility too.
        FadePanel(spiritPanel);

        // WEEK 6 CHANGES PLEASE READ: The added reaction panel shares the existing visibility transition.
        // Updating it here keeps its CanvasGroup consistent with the other menus.
        // No existing panel timing or weapon-row animation is changed.
        FadePanel(reactionPanel);

        if (weaponsPanel == null || weaponsPanel.alpha <= 0f) return;
        foreach (WeaponRowView row in rows)
            row.SetSelected(row.Weapon == selected, normalWeaponColor, selectedWeaponColor, blinkSeconds);
    }

    public void ShowActions(bool canAttack)
    {
        Hide();
        moveButton.interactable = true;
        attackButton.interactable = canAttack;
        SetVisible(actionsPanel, true);
        SetDockConfirmation(false);
    }

    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Deploy now opens the stored passenger's action menu before any exit is chosen.
    // Move selects within the passenger's movement range while Attack and Standby remain unavailable on the carrier's occupied tile.
    // It requires a reachable destination and preserves spent flags, so adjacent retrieval still cannot grant another action.
    public void ShowDeploymentPreviewActions()
    {
        BattleUnit selectedUnit = battle != null ? battle.SelectedUnit : null;
        moveButton.interactable = selectedUnit != null && selectedUnit.DockedAt != null &&
            selectedUnit.DockedAt.GetDeploymentMovementCells(selectedUnit).Count > 0;
        attackButton.interactable = false;
        standbyButton.interactable = false;
        if (boardButton != null) boardButton.gameObject.SetActive(false);
        if (hangarButton != null) hangarButton.gameObject.SetActive(false);
    }

    // WEEK 4: MOTHERSHIP - Reuse the existing Standby button and its callback to confirm Dock.
    public void SetDockConfirmation(bool docking)
    {
        if (standbyButton == null) return;
        if (standbyLabel == null) standbyLabel = standbyButton.GetComponentInChildren<TMP_Text>(true);
        string caption = docking ? "Dock" : "Standby";
        if (standbyLabel != null)
        {
            if (standbyLabel.text != caption) standbyLabel.text = caption;
        }
        else
        {
            // WEEK 4: MOTHERSHIP - Support existing scenes that use a legacy UI Text label.
            Text legacyLabel = standbyButton.GetComponentInChildren<Text>(true);
            if (legacyLabel != null && legacyLabel.text != caption) legacyLabel.text = caption;
        }
        if (!docking) return;
        standbyButton.interactable = true;
        moveButton.interactable = false;
        attackButton.interactable = false;
        if (boardButton != null) boardButton.gameObject.SetActive(false);
        if (hangarButton != null) hangarButton.gameObject.SetActive(false);
    }

    // WEEK 4: MOTHERSHIP - Ships use the same action menu, with a button for their hangar.
    public void ShowMothershipActions(BattleUnit selectedUnit, bool canBoard)
    {
        bool canAct = selectedUnit != null && !selectedUnit.HasActed;
        moveButton.interactable = canAct &&
            (!selectedUnit.HasMoved || (battle != null && battle.CanUndoSelectedMove));
        attackButton.interactable &= canAct;
        standbyButton.interactable = canAct;
        if (boardButton != null)
        {
            // WEEK 4: MOTHERSHIP - Show Dock when this unit can begin movement toward a friendly carrier.
            boardButton.gameObject.SetActive(canBoard);
            boardButton.interactable = canAct && canBoard;
        }
        if (hangarButton != null)
        {
            // WEEK 4: MOTHERSHIP - Show the fourth action only when this ship has living docked units.
            Mothership ship = selectedUnit != null ? selectedUnit.GetComponent<Mothership>() : null;
            bool hasPassengers = false;
            if (ship != null)
                foreach (BattleUnit passenger in ship.Passengers)
                    if (passenger != null && !passenger.IsDefeated && passenger.DockedAt == ship)
                    {
                        hasPassengers = true;
                        break;
                    }
            hangarButton.gameObject.SetActive(hasPassengers);
            hangarButton.interactable = HasHangarUI;
        }
        // WEEK 6 CHANGES PLEASE READ - WEEK 6 DEBUG: The Hangar button previously exposed no explanation for missing or disabled UI.
        // This diagnostic runs when a ship's actions are displayed and reports its passengers, button state and individual required references.
        // It identifies incomplete menu setup without enabling controls or changing the saved UI configuration.
        if (selectedUnit != null && selectedUnit.TryGetComponent(out Mothership debugShip))
            Debug.Log($"[WEEK 6 DOCK DEBUG] Hangar button state: ship={selectedUnit.name}, shipActed={selectedUnit.HasActed}, passengers={debugShip.Passengers.Count}, buttonAssigned={hangarButton != null}, buttonActive={hangarButton != null && hangarButton.gameObject.activeInHierarchy}, buttonInteractable={hangarButton != null && hangarButton.IsInteractable()}, hangarUI={HasHangarUI}, panelAssigned={hangarPanel != null}, contentAssigned={passengerContent != null}, rowAssigned={passengerRowTemplate != null}, rowConfigured={passengerRowTemplate != null && passengerRowTemplate.IsConfigured}, titleAssigned={hangarTitleText != null}.", this);
    }

    // WEEK 4: MOTHERSHIP - Rebuild the list from the ship's stored units, including unavailable passengers.
    public void ShowPassengers(Mothership ship)
    {
        if (!HasHangarUI || ship == null) return;
        Hide();
        foreach (PassengerRowView row in passengerRows)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        passengerRows.Clear();
        foreach (BattleUnit passenger in ship.Passengers)
        {
            if (passenger == null || passenger.IsDefeated) continue;
            PassengerRowView row = Instantiate(passengerRowTemplate, passengerContent);
            row.gameObject.SetActive(true);
            row.SetPassenger(passenger, ship.CanDeploy(passenger), () => battle.ChoosePassenger(passenger));
            passengerRows.Add(row);
            // WEEK 6 CHANGES PLEASE READ - WEEK 6 DEBUG: A disabled passenger row previously showed no detailed availability information in the Console.
            // This diagnostic records each displayed passenger's action state, ship ownership and number of usable adjacent exits.
            // It explains why retrieval may be unavailable while keeping the row's existing deployment rules and callbacks intact.
            Debug.Log($"[WEEK 6 DOCK DEBUG] Hangar passenger row: ship={ship.Unit.name}, passenger={passenger.name}, dockedHere={passenger.DockedAt == ship}, moved={passenger.HasMoved}, acted={passenger.HasActed}, canDeploy={ship.CanDeploy(passenger)}, freeExits={ship.GetDeploymentCells().Count}, rowActive={row.gameObject.activeInHierarchy}, panelInteractable={hangarPanel.interactable} (panel opens after rows are built).", row);
        }
        hangarTitleText.text = $"{ship.Unit.Mech.MechName} - Docked units: {passengerRows.Count}";
        passengerContent.anchoredPosition = Vector2.zero;
        SetVisible(hangarPanel, true);
    }

    // WEEK 5 CHANGES PLEASE READ: Mech buttons must not remain active for units without their matching ability.
    // The menu now reads shared eligibility checks and disables commands whose action, recipe or adjacent target is unavailable.
    // This updates existing controls in place while keeping Getter Change's explicit after-action exception.
    public void ShowMechSkillActions(BattleUnit selectedUnit)
    {
        if (selectedUnit == null || selectedUnit.Mech == null) return;
        MechBase mech = selectedUnit.Mech;
        bool repair = false, resupply = false;
        if (selectedUnit.Battlefield != null)
            foreach (BattleUnit target in selectedUnit.Battlefield.Units)
            {
                repair |= MechSkillEffect.CanRepair(selectedUnit, target);
                resupply |= MechSkillEffect.CanResupply(selectedUnit, target);
            }
        SetSkillButton(transformButton, mech.TransformInto != null, MechSkillEffect.CanTransform(selectedUnit));
        SetSkillButton(combineButton, mech.CombineInto != null, MechSkillEffect.CanCombine(selectedUnit));
        SetSkillButton(separateButton, selectedUnit.MechSkillState.Parts.Count > 0, MechSkillEffect.CanSeparate(selectedUnit));
        SetSkillButton(getterChangeButton, mech.GetterForms.Count > 0, MechSkillEffect.NextGetterForm(selectedUnit) != null);
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER's optional button previously disappeared or disabled itself without explaining missing setup.
        // Opening actions for a Getter now writes a direct warning if the button is unassigned or no form can be selected.
        // Ordinary units and existing button eligibility remain unchanged, and the warning runs only when this action menu is shown.
        if (mech.GetterForms.Count > 0 || GetterUnit.IsGetterForm(mech))
        {
            if (getterChangeButton == null)
                Debug.LogWarning("[WEEK 5 GETTER] ActionsMenu is missing its Getter Change Button assignment; use W5 UI or assign that button in the Inspector.", this);
            if (MechSkillEffect.NextGetterForm(selectedUnit) == null) selectedUnit.GetterState.ReportChangeFailure();
        }
        SetSkillButton(repairButton, mech.RepairDevice, repair);
        SetSkillButton(resupplyButton, mech.ResupplyDevice, resupply);
    }
    private static void SetSkillButton(Button button, bool visible, bool available)
    {
        if (button == null) return;
        button.gameObject.SetActive(visible);
        button.interactable = available;
    }

    // WEEK 6 CHANGES PLEASE READ: The same weapon list now serves ordinary attacks and the pending counter choice.
    // Counter rows use the incoming attacker and defensive movement rules, with unusable rows disabled.
    // The counter-only Back button returns to the preview, and normal weapon lists retain their existing behavior.
    // WEEK 3: Create a row from your template for every mech weapon.
    public void ShowWeapons(BattleUnit selectedUnit, Weapon highlighted)
    {
        Hide();
        unit = selectedUnit;
        selected = highlighted;
        lastClicked = null;

        foreach (WeaponRowView row in rows)
            Destroy(row.gameObject);
        rows.Clear();

        SetPilotAndMechFields(unit);

        foreach (Weapon weapon in unit.Mech.Weapons)
        {
            if (weapon == null) continue;
            WeaponRowView row = Instantiate(weaponRowTemplate, weaponContent);
            row.name = weapon.WeaponName;
            row.gameObject.SetActive(true);
            row.SetWeapon(
                weapon,
                unit.CanAffordWeapon(weapon),
                battle.HasTarget(weapon),
                () => ClickWeapon(weapon));
            // WEEK 4: MOTHERSHIP - Display this unit's ammo without requiring additional TMP fields.
            row.ShowAmmo(unit, weapon, battle.IsChoosingCounterWeapon);
            if (battle.IsChoosingCounterWeapon) row.SetCounterAvailability(battle.HasTarget(weapon));
            rows.Add(row);
        }

        weaponContent.anchoredPosition = Vector2.zero;
        SetVisible(weaponsPanel, true);
        if (counterWeaponBackButton != null) counterWeaponBackButton.gameObject.SetActive(battle.IsChoosingCounterWeapon);
        SetWeapon(selected);
    }

    // WEEK 3: One click previews; a quick second click confirms the same weapon.
    private void ClickWeapon(Weapon weapon)
    {
        bool doubleClick = lastClicked == weapon && Time.unscaledTime - lastClickTime <= 0.35f;
        lastClicked = weapon;
        lastClickTime = Time.unscaledTime;
        battle.PreviewWeapon(weapon);
        if (doubleClick) battle.ConfirmWeapon();
    }

    // WEEK 3: Fill each selected-weapon TMP field independently.
    public void SetWeapon(Weapon weapon)
    {
        selected = weapon;
        confirmButton.interactable = weapon != null && battle != null && battle.HasTarget(weapon);
        // WEEK 4: UI VISIBILITY - Hide empty or stale details until a weapon is selected.
        SetWeaponDetailsVisible(weapon != null);
        if (weapon == null) return;

        Set(weaponNameText, weapon.WeaponName);
        Set(weaponTypeText, weapon.DamageType.ToString());
        Set(weaponClassificationText, weapon.Classification.ToString());
        // WEEK 5 CHANGES PLEASE READ: Weapon details previously displayed base power even when GUND modifies actual damage.
        // Reading effective power here makes the selected weapon's displayed value match the battle formula.
        // This reuses the existing text field and requires no UI layout changes.
        Set(weaponPowerText, unit.GetWeaponPower(weapon).ToString());
        Set(weaponRangeText, weapon.MinRange + "-" + weapon.MaxRange);
        // WEEK 4: MOTHERSHIP - Existing weapons with zero Max Ammo still only display energy cost.
        Set(weaponEnergyCostText, unit.GetWeaponEnergyCost(weapon).ToString() + (weapon.MaxAmmo > 0 ? $" Ammo | {unit.GetAmmo(weapon)}/{unit.GetAmmoCapacity(weapon)}" : string.Empty));
        Set(weaponAccuracyText, weapon.AccuracyModifier.ToString("+0;-0;0"));
        Set(weaponCriticalText, weapon.CriticalModifier.ToString("+0;-0;0"));
        SetTerrainFields(weapon.TerrainRatings, weaponAirText, weaponGroundText, weaponWaterText, weaponSpaceText);
    }

    // WEEK 4: UI VISIBILITY - Keep the manually assigned fields; show their values only for a selected weapon.
    private void SetWeaponDetailsVisible(bool visible)
    {
        weaponNameText.enabled = visible;
        weaponTypeText.enabled = visible;
        weaponClassificationText.enabled = visible;
        weaponPowerText.enabled = visible;
        weaponRangeText.enabled = visible;
        weaponEnergyCostText.enabled = visible;
        weaponAccuracyText.enabled = visible;
        weaponCriticalText.enabled = visible;
        weaponAirText.enabled = visible;
        weaponGroundText.enabled = visible;
        weaponWaterText.enabled = visible;
        weaponSpaceText.enabled = visible;
    }

    public void Hide()
    {
        // WEEK 6 CHANGES PLEASE READ: Changing menus or finishing combat must close the reaction controls as well.
        // Hide the new panel and counter-only Back button through the same cleanup used by existing menus.
        // Pending combat data remains owned by BattleSystem so weapon selection can hide and reopen this panel safely.
        SetVisible(reactionPanel, false);
        if (counterWeaponBackButton != null) counterWeaponBackButton.gameObject.SetActive(false);
        // WEEK 4: MOTHERSHIP - Never carry the Dock label into a later menu or phase.
        if (standbyButton != null) SetDockConfirmation(false);
        SetVisible(actionsPanel, false);
        SetVisible(weaponsPanel, false);
        SetVisible(hangarPanel, false);
        SetVisible(UnitInfoPanel, false);

        // WEEK 4: SPIRIT COMMANDS - Hide Spirit with the other menus.
        SetVisible(spiritPanel, false);
    }

    // WEEK 6 CHANGES PLEASE READ: Incoming attacks now show both participants and a selectable defensive response.
    // The preview calculates the same incoming hit chance used by combat and shows a legal counter weapon's hit chance separately.
    // Only the chosen response is highlighted, and Counter is disabled when the defender cannot retaliate.
    public void ShowReaction(BattleUnit attacker, BattleUnit defender, Weapon incoming,
        BattleReaction reaction, Weapon counter, bool canCounter)
    {
        Hide();
        int distance = Mathf.Abs(attacker.GridPosition.x - defender.GridPosition.x) +
            Mathf.Abs(attacker.GridPosition.y - defender.GridPosition.y);
        int incomingHit = BattleFormulas.ReactionHitRate(BattleFormulas.AccuracyRate(attacker, defender, incoming, distance), reaction);
        string counterInfo = canCounter && reaction == BattleReaction.Counter
            ? $"{counter.WeaponName} | HIT {BattleFormulas.ReactionHitRate(BattleFormulas.AccuracyRate(defender, attacker, counter, distance))}%"
            : canCounter ? "No counterattack" : "Counter Unavailable";
        reactionPreviewText.text = $"{attacker.Mech.MechName}\n{incoming.WeaponName} | HIT {incomingHit}%\n\n" +
            $"{defender.Mech.MechName} | HP {defender.CurrentHealth}/{defender.Mech.Health}\n" +
            $"EN {defender.CurrentEnergy}/{defender.Mech.Energy} | {reaction}\n{counterInfo}";
        counterButton.interactable = canCounter;
        SetReactionHighlight(counterButton, reaction == BattleReaction.Counter);
        SetReactionHighlight(evadeButton, reaction == BattleReaction.Evade);
        SetReactionHighlight(defendButton, reaction == BattleReaction.Defend);
        beginCombatButton.interactable = reaction != BattleReaction.Counter || canCounter;
        reactionPanel.transform.SetAsLastSibling();
        SetVisible(reactionPanel, true);
    }

    // WEEK 6 CHANGES PLEASE READ: A visible selection is needed before the player commits the reaction.
    // Each reaction button keeps its existing tint behavior while its background distinguishes the current choice.
    // These colors affect only the new controls and never the team's ordinary action buttons.
    private static void SetReactionHighlight(Button button, bool selected)
    {
        if (button.targetGraphic != null)
            button.targetGraphic.color = selected ? new Color(0.25f, 0.5f, 0.9f) : new Color(0.12f, 0.22f, 0.25f);
    }

    private void SetPilotAndMechFields(BattleUnit selectedUnit)
    {
        PilotBase pilot = selectedUnit.Pilot;
        MechBase mech = selectedUnit.Mech;
        Set(pilotNameText, $"Pilot: {pilot.PilotName}");
        Set(mechNameText, $"Mech: {mech.MechName}");
        Set(healthText, $"HP: {selectedUnit.CurrentHealth}/{mech.Health}");
        Set(energyText, $"EN: {selectedUnit.CurrentEnergy}/{mech.Energy}");
        // WEEK 4 MORALE SYSTEM: Display the selected unit's changing battle morale.
        Set(moraleText, $"Morale: {selectedUnit.CurrentMorale}/{selectedUnit.MaximumMorale}");
        // WEEK 5 CHANGES PLEASE READ: Status previously displayed raw pilot values even when a morale mode boosts combat stats.
        // These six fields now read the same effective pilot values as the formulas so Hyper/Super can be checked on the sheet.
        // The shared pilot asset remains unchanged, and the existing fields keep their current positions and labels.
        Set(meleeText, $"Melee: {MechSkillEffect.PilotStat(selectedUnit, pilot.Melee)}");
        Set(rangedText, $"Ranged: {MechSkillEffect.PilotStat(selectedUnit, pilot.Ranged)}");
        Set(defenseText, $"Defense: {MechSkillEffect.PilotStat(selectedUnit, pilot.Defense)}");
        Set(evadeText, $"Evade: {MechSkillEffect.PilotStat(selectedUnit, pilot.Evade)}");
        Set(accuracyText, $"Accuracy: {MechSkillEffect.PilotStat(selectedUnit, pilot.Accuracy)}");
        Set(skillText, $"Skill: {MechSkillEffect.PilotStat(selectedUnit, pilot.Skill)}");
        SetTerrainFields(pilot.TerrainRatings, pilotAirText, pilotGroundText, pilotWaterText, pilotSpaceText);
        SetTerrainFields(mech.TerrainRatings, mechAirText, mechGroundText, mechWaterText, mechSpaceText);
    }

    private static void SetTerrainFields(TerrainRatings ratings, TMP_Text air, TMP_Text ground, TMP_Text water, TMP_Text space)
    {
        Set(air, $"Air: {ratings.Get(TerrainType.Air)}");
        Set(ground, $"Ground: {ratings.Get(TerrainType.Ground)}");
        Set(water, $"Water: {ratings.Get(TerrainType.Water)}");
        Set(space, $"Space: {ratings.Get(TerrainType.Space)}");
    }

    // WEEK 4: Only require the core controls needed for the battle menu.
    // Extra stat, terrain, weapon-detail, mothership, and Spirit UI fields
    // are allowed to remain optional so incomplete UI does not disable battle controls.
    private bool IsConfigured() =>
        actionsPanel != null &&
        weaponsPanel != null &&
        moveButton != null &&
        attackButton != null &&
        standbyButton != null &&
        confirmButton != null &&
        weaponContent != null &&
        weaponRowTemplate != null &&
        pilotNameText != null &&
        mechNameText != null &&
        healthText != null &&
        energyText != null &&
        moraleText != null &&
        weaponNameText != null &&
        weaponTypeText != null &&
        weaponRangeText != null &&
        weaponEnergyCostText != null &&
        weaponAccuracyText != null &&
        weaponCriticalText != null &&
        weaponAirText != null &&
        weaponGroundText != null &&
        weaponWaterText != null &&
        weaponSpaceText != null;

    // WEEK 3: Move the Action Panel beside the selected player's mech
    // by converting the unit's world position into Canvas UI coordinates.
    public void PositionActionsBesideUnit(BattleUnit selectedUnit, Camera battleCamera)
    {
        if (selectedUnit == null ||
            battleCamera == null ||
            actionsRect == null ||
            actionsCanvas == null)
        {
            return;
        }

        // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Reopening the same unit's menu previously moved it back beside that unit every time.
        // A dragged position now persists for that unit, while changing units restores automatic placement.
        // The visible controls are kept inside the Canvas so the drag handle and action buttons remain reachable.
        if (actionPositionUnit != selectedUnit)
        {
            actionPositionUnit = selectedUnit;
            actionPositionDragged = false;
        }
        if (actionPositionDragged)
        {
            if (actionDragHandle != null) actionDragHandle.ClampToCanvas();
            return;
        }
        Vector2 screenPosition =
            battleCamera.WorldToScreenPoint(selectedUnit.transform.position);

        RectTransform canvasRect =
            actionsCanvas.transform as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        // WEEK 3: Screen Space Overlay canvases do not require a camera
        // when converting the unit's screen position into local UI space.
        Camera uiCamera =
            actionsCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : actionsCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            uiCamera,
            out Vector2 localPosition))
        {
            actionsRect.anchoredPosition =
                localPosition + actionMenuOffset;
            if (actionDragHandle != null) actionDragHandle.ClampToCanvas();
        }
    }

    // WEEK 6 CHANGES PLEASE READ - WEEK 6 QUALITY OF LIFE: Dragging an action button itself would interfere with its normal command click.
    // A labelled handle is created above the existing menu artwork and controls, with its own pointer drag component.
    // Its Button component only consumes UI pointer hits so a drag cannot also select or move a battlefield unit.
    private void PrepareActionMenuDragging()
    {
        if (actionsRect == null || actionsCanvas == null || actionDragHandle != null) return;
        Canvas.ForceUpdateCanvases();
        Bounds bounds = ActionsMenuDragHandle.VisualBounds(actionsRect, actionsRect);
        GameObject handle = new("WEEK 6 Drag Actions Menu", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ActionsMenuDragHandle));
        handle.layer = actionsRect.gameObject.layer;
        RectTransform rect = (RectTransform)handle.transform;
        rect.SetParent(actionsRect, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(180, 24);
        rect.anchoredPosition = new Vector2(bounds.center.x, bounds.max.y + 14);
        handle.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.23f, 0.95f);
        handle.GetComponent<Button>().targetGraphic = handle.GetComponent<Image>();
        handle.GetComponent<Button>().navigation = new Navigation { mode = Navigation.Mode.None };
        TMP_Text source = standbyButton != null ? standbyButton.GetComponentInChildren<TMP_Text>(true) : null;
        if (source != null)
        {
            TMP_Text label = Instantiate(source, rect);
            label.name = "WEEK 6 Drag Label";
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.text = "Drag menu";
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
        actionDragHandle = handle.GetComponent<ActionsMenuDragHandle>();
        actionDragHandle.Initialize(actionsRect, actionsCanvas, () => actionPositionDragged = true);
    }

    // WEEK 4: UI FADE - Set the visibility target; normal transitions animate in Update.
    private void SetVisible(CanvasGroup panel, bool visible, bool immediately = false)
    {
        if (panel == null)
        {
            return;
        }

        // WEEK 4: Set panel visibility immediately.
        // This prevents Spirit Commands from remaining invisible
        // when the UI fade duration is greater than zero.
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }

    // WEEK 4: UI FADE - Use unscaled time so fading also works when game time is paused.
    private void FadePanel(CanvasGroup panel)
    {
        if (panel == null) return;
        float targetAlpha = panel.interactable ? 1f : 0f;
        // WEEK 4: UI FADE - Continue from the current alpha when menus change quickly; no queued fades.
        panel.alpha = panelFadeSeconds <= 0f ? targetAlpha :
            Mathf.MoveTowards(panel.alpha, targetAlpha, Time.unscaledDeltaTime / panelFadeSeconds);
    }

    private void UnitScreen()
    {
        if (battle == null || battle.SelectedUnit == null || UnitInfoPanel == null) return;
        SetPilotAndMechFields(battle.SelectedUnit);
        SetVisible(actionsPanel, false);
        SetVisible(UnitInfoPanel, true);
    }

    private static void Set(TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }

    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER's additional pilots require more command rows than the original fixed Spirit list can display.
    // This creates a masked vertical scroll viewport sized to the panel, with the existing SP display and Back button placed outside it.
    // The same assigned template and controls are reused, and the viewport persists for later menu openings without adding prefab edits.
    private void PrepareGetterSpiritList(int crewCount)
    {
        if (getterSpiritScroll != null || crewCount < 2 || spiritPanel == null) return;
        RectTransform content = spiritButtonContent as RectTransform;
        if (content == null) return;
        GameObject viewportObject = new("WEEK 5 GETTER Spirit Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.SetParent(spiritPanel.transform, false);
        viewportObject.layer = spiritPanel.gameObject.layer;
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 0.5f);
        viewport.anchoredPosition = content.anchoredPosition;
        Canvas.ForceUpdateCanvases();
        float panelHeight = ((RectTransform)spiritPanel.transform).rect.height;
        viewport.sizeDelta = new Vector2(Mathf.Max(300f, content.rect.width) + 24f, Mathf.Clamp(panelHeight * 0.65f, 100f, 520f));
        viewportObject.GetComponent<Image>().color = Color.clear;
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f); content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        ContentSizeFitter fit = content.GetComponent<ContentSizeFitter>();
        if (fit == null) fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        getterSpiritScroll = viewportObject.GetComponent<ScrollRect>();
        getterSpiritScroll.viewport = viewport; getterSpiritScroll.content = content;
        getterSpiritScroll.horizontal = false; getterSpiritScroll.vertical = true;
        getterSpiritScroll.movementType = ScrollRect.MovementType.Clamped;
        getterSpiritScroll.scrollSensitivity = 35f;
        if (spiritBackButton != null)
        {
            RectTransform back = (RectTransform)spiritBackButton.transform;
            back.anchorMin = back.anchorMax = viewport.anchorMin;
            back.anchoredPosition = viewport.anchoredPosition + new Vector2(0f, -viewport.sizeDelta.y / 2f - back.rect.height / 2f - 12f);
        }
        if (spiritPointText != null)
        {
            RectTransform sp = spiritPointText.rectTransform;
            sp.anchorMin = sp.anchorMax = viewport.anchorMin;
            sp.anchoredPosition = viewport.anchoredPosition + new Vector2(0f, viewport.sizeDelta.y / 2f + sp.rect.height / 2f + 12f);
        }
    }

    // WEEK 4: SPIRIT COMMANDS - Build the Spirit menu from the
    // commands assigned to the currently selected pilot.
    private void BuildSpiritButtons(BattleUnit selectedUnit)
    {
        // WEEK 4: Remove every previously generated Spirit Command button
        // directly from the content container before rebuilding the menu.
        if (spiritButtonContent != null)
        {
            for (int i = spiritButtonContent.childCount - 1; i >= 0; i--)
            {
                Transform child = spiritButtonContent.GetChild(i);

                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
        }

        // WEEK 4: Clear the saved button references after removing the UI objects.
        spiritCommandButtons.Clear();

        if (selectedUnit == null ||
            selectedUnit.Pilot == null ||
            spiritButtonTemplate == null ||
            spiritButtonContent == null)
        {
            return;
        }

        PrepareGetterSpiritList(selectedUnit.SpiritCrew.Count);
        // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER sub-pilots stay aboard and must retain access to their assigned Spirits.
        // This list now builds commands for each crew member, with that pilot's own affordability and owner label.
        // Each callback captures the command's pilot so using a sub-pilot Spirit never changes the main combat pilot.
        foreach (PilotBase spiritPilot in selectedUnit.SpiritCrew)
            foreach (SpiritCommandBase spirit in spiritPilot.GetSpiritCommands())
            {
                if (spirit == null)
                {
                    continue;
                }

                // WEEK 4: Clone the recovered Spirit button template.
                SpiritCommandButton newButton =
                    Instantiate(spiritButtonTemplate, spiritButtonContent);

                newButton.gameObject.SetActive(true);

                bool canAfford =
                    selectedUnit.GetSpiritPoints(spiritPilot) >= spirit.SpiritPointCost;

                // WEEK 4: Do not allow the player to spend SP repeatedly
                // on a self buff that is already waiting to be consumed.
                bool alreadyActive =
                    (spirit.Effect == SpiritCommandEffect.Valor && selectedUnit.ValorActive) ||
                    (spirit.Effect == SpiritCommandEffect.Soul && selectedUnit.SoulActive) ||
                    (spirit.Effect == SpiritCommandEffect.Smash && selectedUnit.SmashActive) ||
                    (spirit.Effect == SpiritCommandEffect.Accel && selectedUnit.AccelActive);

                bool canUse =
                    canAfford && !alreadyActive;

                newButton.Setup(
                    spirit,
                    canUse,
                    command => UseSpiritCommand(command, spiritPilot),
                    selectedUnit.SpiritCrew.Count > 1 ? $"{spiritPilot.PilotName} SP {selectedUnit.GetSpiritPoints(spiritPilot)}/{spiritPilot.MaxSpiritPoints}" : null);
            }
        if (getterSpiritScroll != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(getterSpiritScroll.content);
            getterSpiritScroll.verticalNormalizedPosition = 1f;
        }
    }


    // WEEK 4: SPIRIT COMMANDS - Activate a selected Spirit Command.
    // WEEK 4: SPIRIT COMMANDS - Activate a selected Spirit Command.
    // WEEK 5 CHANGES PLEASE READ: WEEK 5 GETTER needs the Spirit's owner preserved through both immediate and targeted execution.
    // The optional pilot is passed to the existing SpiritSystem and targeting controller rather than temporarily replacing the combat pilot.
    // Existing single-pilot commands still default to the main pilot and keep their current effects and menu flow.
    private void UseSpiritCommand(SpiritCommandBase spirit, PilotBase spiritPilot = null)
    {
        if (battle == null ||
            battle.SelectedUnit == null ||
            spirit == null)
        {
            return;
        }

        BattleUnit selectedUnit = battle.SelectedUnit;

        // WEEK 4: Determine which Spirit Commands require a selected target.
        // WEEK 6: Added Faith so it uses the existing allied targeting system.
        bool requiresTarget =
            spirit.Effect == SpiritCommandEffect.Trust ||
            spirit.Effect == SpiritCommandEffect.Prospect ||
            spirit.Effect == SpiritCommandEffect.Daunt ||
            spirit.Effect == SpiritCommandEffect.Confuse ||
            spirit.Effect == SpiritCommandEffect.Resupply ||
            spirit.Effect == SpiritCommandEffect.Faith ||
            spirit.Effect == SpiritCommandEffect.Attune ||
            spirit.Effect == SpiritCommandEffect.Hope;

        if (requiresTarget)
        {
            // WEEK 4: Tell BattleSystem which Spirit Command is waiting
            // for a battlefield target.
            battle.BeginSpiritTargeting(spirit, spiritPilot);

            // WEEK 4: Close the Spirit menu so the player can click
            // the ally or enemy they want to target.
            CloseSpiritMenu();

            Debug.Log(
                $"Choose a target for {spirit.CommandName}.");

            return;
        }

        // WEEK 4: Pass the Battlefield so group Spirit Commands
        // such as Bonds, Rally, and Dread can affect multiple units.
        bool used = SpiritSystem.UseSpirit(
            selectedUnit,
            spirit,
            selectedUnit.Battlefield,
            spiritPilot: spiritPilot);

        if (!used)
        {
            return;
        }

        // WEEK 4: Refresh the pilot's remaining SP.
        if (spiritPointText != null && selectedUnit.Pilot != null)
        {
            spiritPointText.text =
                $"SP: {selectedUnit.CurrentSpiritPoints} / " +
                $"{selectedUnit.Pilot.MaxSpiritPoints}";
        }

        // WEEK 4: Rebuild so commands the pilot can no longer
        // afford become disabled.
        BuildSpiritButtons(selectedUnit);

        // WEEK 4: After successfully using a Spirit Command,
        // return the player to the normal Action menu.
        CloseSpiritMenu();
    }


    // WEEK 4: SPIRIT COMMANDS - Open the selected pilot's Spirit menu.
    private void OpenSpiritMenu()
    {
        if (battle == null || battle.SelectedUnit == null)
        {
            Debug.LogWarning(
                "Cannot open Spirit Commands because no unit is selected.");
            return;
        }

        BattleUnit selectedUnit = battle.SelectedUnit;

        if (selectedUnit.Pilot == null)
        {
            Debug.LogWarning(
                "Selected unit does not have a pilot.");
            return;
        }

        // WEEK 4: Build buttons from this pilot's assigned commands.
        BuildSpiritButtons(selectedUnit);

        // WEEK 4: Display current and maximum Spirit Points.
        if (spiritPointText != null)
        {
            spiritPointText.text =
                $"SP: {selectedUnit.CurrentSpiritPoints} / " +
                $"{selectedUnit.Pilot.MaxSpiritPoints}";
        }

        SetVisible(actionsPanel, false);
        SetVisible(spiritPanel, true);

        Debug.Log(
            $"Opening Spirit Commands for {selectedUnit.Pilot.PilotName}. " +
            $"Current SP: {selectedUnit.CurrentSpiritPoints}");
    }


    // WEEK 4: SPIRIT COMMANDS - Return to the normal Action menu.
    private void CloseSpiritMenu()
    {
        SetVisible(spiritPanel, false);
        SetVisible(actionsPanel, true);
    }
}
