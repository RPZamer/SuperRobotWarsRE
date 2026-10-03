using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [Header("Your Weapon UI")]
    [SerializeField] private CanvasGroup weaponsPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private RectTransform weaponContent;
    [SerializeField] private WeaponRowView weaponRowTemplate;

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

    // WEEK 3: Hide only your assigned panels. This script does not create or arrange UI.
    // WEEK 3: Find the Move button created in the ActionsMenu hierarchy.
    private void Awake()
    {
        if (moveButton == null)
            moveButton = transform.Find("ActionPanel/MoveButton")?.GetComponent<Button>();

        // WEEK 3: Cache the existing Action Panel RectTransform so its
        // screen position can follow the currently selected player unit.
        if (actionsPanel != null)
        {
            actionsRect = actionsPanel.GetComponent<RectTransform>();
        }

        // WEEK 3: Cache the Canvas containing the Actions Menu so the
        // selected unit's world position can be converted into UI space.
        actionsCanvas = GetComponentInParent<Canvas>();

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
        if (StatusButton != null) StatusButton.onClick.AddListener(UnitScreen);

        // WEEK 4: SPIRIT COMMANDS - Connect the Spirit menu controls.
        if (spiritButton != null)
            spiritButton.onClick.AddListener(OpenSpiritMenu);

        if (spiritBackButton != null)
            spiritBackButton.onClick.AddListener(CloseSpiritMenu);

        // WEEK 4: The template itself stays hidden.
        // Copies of it are created when the Spirit menu opens.
        if (spiritButtonTemplate != null)
            spiritButtonTemplate.gameObject.SetActive(false);

        // WEEK 4: MOTHERSHIP - Optional references keep existing scenes working until UI is assigned.
        if (boardButton != null)
            boardButton.onClick.AddListener(battle.OpenBoarding);

        if (hangarButton != null)
            hangarButton.onClick.AddListener(battle.OpenHangar);

        if (closeHangarButton != null)
            closeHangarButton.onClick.AddListener(battle.Back);

        if (passengerRowTemplate != null)
            passengerRowTemplate.gameObject.SetActive(false);

        if ((boardButton != null || hangarButton != null) && !HasHangarUI)
        {
            Debug.LogWarning(
                "WEEK 4: MOTHERSHIP - Assign the hangar panel, content, title and configured passenger row template.",
                this);
        }

        return true;
    }

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
        }
        hangarTitleText.text = $"{ship.Unit.Mech.MechName} - Docked units: {passengerRows.Count}";
        passengerContent.anchoredPosition = Vector2.zero;
        SetVisible(hangarPanel, true);
    }

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
            row.ShowAmmo(unit, weapon);
            rows.Add(row);
        }

        weaponContent.anchoredPosition = Vector2.zero;
        SetVisible(weaponsPanel, true);
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
        Set(weaponPowerText, weapon.Power.ToString());
        Set(weaponRangeText, weapon.MinRange + "-" + weapon.MaxRange);
        // WEEK 4: MOTHERSHIP - Existing weapons with zero Max Ammo still only display energy cost.
        Set(weaponEnergyCostText, unit.GetWeaponEnergyCost(weapon).ToString() + (weapon.MaxAmmo > 0 ? $" Ammo | {unit.GetAmmo(weapon)}/{unit.GetAmmoCapacity(weapon)}"  : string.Empty));
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
        // WEEK 4: MOTHERSHIP - Never carry the Dock label into a later menu or phase.
        if (standbyButton != null) SetDockConfirmation(false);
        SetVisible(actionsPanel, false);
        SetVisible(weaponsPanel, false);
        SetVisible(hangarPanel, false);
        SetVisible(UnitInfoPanel, false);

        // WEEK 4: SPIRIT COMMANDS - Hide Spirit with the other menus.
        SetVisible(spiritPanel, false);
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
        Set(meleeText, $"Melee: {pilot.Melee}");
        Set(rangedText, $"Ranged: {pilot.Ranged}");
        Set(defenseText, $"Defense: {pilot.Defense}");
        Set(evadeText, $"Evade: {pilot.Evade}");
        Set(accuracyText, $"Accuracy: {pilot.Accuracy}");
        Set(skillText, $"Skill: {pilot.Skill}");
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
        }
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

        // WEEK 4: Retrieve the pilot's assigned Spirit Commands.
        SpiritCommandBase[] spirits =
            selectedUnit.Pilot.GetSpiritCommands();

        foreach (SpiritCommandBase spirit in spirits)
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
    selectedUnit.CurrentSpiritPoints >= spirit.SpiritPointCost;

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
                UseSpiritCommand);
        }
    }


    // WEEK 4: SPIRIT COMMANDS - Activate a selected Spirit Command.
    // WEEK 4: SPIRIT COMMANDS - Activate a selected Spirit Command.
    private void UseSpiritCommand(SpiritCommandBase spirit)
    {
        if (battle == null ||
            battle.SelectedUnit == null ||
            spirit == null)
        {
            return;
        }

        BattleUnit selectedUnit = battle.SelectedUnit;

        // WEEK 4: SPIRIT COMMANDS - These commands require the player
        // to choose a specific unit on the battlefield.
        bool requiresTarget =
            spirit.Effect == SpiritCommandEffect.Trust ||
            spirit.Effect == SpiritCommandEffect.Prospect ||
            spirit.Effect == SpiritCommandEffect.Daunt ||
            spirit.Effect == SpiritCommandEffect.Confuse;

        if (requiresTarget)
        {
            // WEEK 4: Tell BattleSystem which Spirit Command is waiting
            // for a battlefield target.
            battle.BeginSpiritTargeting(spirit);

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
            selectedUnit.Battlefield);

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