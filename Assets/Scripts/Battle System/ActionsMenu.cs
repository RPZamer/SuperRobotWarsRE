using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [SerializeField] private Button StatusButton;


    [Header("Unit Info UI")]
    [SerializeField] private CanvasGroup UnitInfoPanel;


    [Header("Your Weapon UI")]
    [SerializeField] private CanvasGroup weaponsPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private RectTransform weaponContent;
    [SerializeField] private WeaponRowView weaponRowTemplate;

    [Header("Pilot and Mech TMP Fields")]
    [SerializeField] private TMP_Text pilotNameText;
    [SerializeField] private TMP_Text mechNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text moraleText;
    [SerializeField] private TMP_Text meleePower;
    [SerializeField] private TMP_Text rangedPower;
    [SerializeField] private TMP_Text defenseStat;
    [SerializeField] private TMP_Text evadeStat;
    [SerializeField] private TMP_Text accuracyStat;
    // [SerializeField] private TMP_Text skillText;  // What is this for?

    /*
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
    [SerializeField] private TMP_Text weaponPowerText;
    [SerializeField] private TMP_Text weaponRangeText;
    [SerializeField] private TMP_Text weaponEnergyCostText;
    [SerializeField] private TMP_Text weaponAccuracyText;
    [SerializeField] private TMP_Text weaponCriticalText;
    [SerializeField] private TMP_Text weaponAirText;
    [SerializeField] private TMP_Text weaponGroundText;
    [SerializeField] private TMP_Text weaponWaterText;
    [SerializeField] private TMP_Text weaponSpaceText;
    */

    // theres alot of text fields being used that feel unnecessary, but will be kept for now.

    [Header("Selected Weapon TMP Fields")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text weaponDamageText;
    [SerializeField] private TMP_Text weaponRangeText;
    [SerializeField] private TMP_Text weaponEnergyCostText;


    [Header("Optional Presentation")]
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

        Hide();

        SetVisible(UnitInfoPanel, false);
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

        moveButton.onClick.AddListener(battle.OpenMovement);
        attackButton.onClick.AddListener(battle.OpenWeapons);
        standbyButton.onClick.AddListener(battle.Standby);
        confirmButton.onClick.AddListener(battle.ConfirmWeapon);
        StatusButton.onClick.AddListener(UnitScreen);
        return true;
    }

    // WEEK 3: Blink only the selected weapon row using colors you choose.
    private void Update()
    {
        // pressing escape closes a menu.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCurrentMenu();
            return;
        }

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
                weapon.EnergyCost <= unit.CurrentEnergy,
                battle.HasTarget(weapon),
                () => ClickWeapon(weapon));
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
    /*
    public void SetWeapon(Weapon weapon)
    {
        selected = weapon;
        confirmButton.interactable = weapon != null && battle != null && battle.HasTarget(weapon);
        if (weapon == null) return;

        Set(weaponNameText, $"Weapon: {weapon.WeaponName}");
        Set(weaponTypeText, $"Type: {weapon.DamageType}");
        Set(weaponClassificationText, $"Class: {weapon.Classification}");
        Set(weaponPowerText, $"Power: {weapon.Power}");
        Set(weaponRangeText, $"Range: {weapon.MinRange}-{weapon.MaxRange}");
        Set(weaponEnergyCostText, $"EN Cost: {weapon.EnergyCost}");
        Set(weaponAccuracyText, $"Accuracy: {weapon.AccuracyModifier:+0;-0;0}");
        Set(weaponCriticalText, $"Critical: {weapon.CriticalModifier:+0;-0;0}");
        SetTerrainFields(weapon.TerrainRatings, weaponAirText, weaponGroundText, weaponWaterText, weaponSpaceText);
    }
    */
    // The code wont be deleted but same thing as above, dont know why we need all these text values.

    public void SetWeapon(Weapon weapon)
    {
        selected = weapon;

        confirmButton.interactable = weapon != null && battle != null && battle.HasTarget(weapon);

        if (weapon == null)
        { 
            return; 
        }

        Set(weaponNameText, "Name: " + weapon.WeaponName);
        Set(weaponDamageText, "Damage: " + weapon.Power);
        Set(weaponRangeText, "Range: " + weapon.MinRange + "-" + weapon.MaxRange);
        Set(weaponEnergyCostText, "EN Cost: " + weapon.EnergyCost);
    }


    public void Hide()
    {
        SetVisible(actionsPanel, false);
        SetVisible(weaponsPanel, false);
    }

    private void SetPilotAndMechFields(BattleUnit selectedUnit)
    {
        PilotBase pilot = selectedUnit.Pilot;
        MechBase mech = selectedUnit.Mech;
        Set(pilotNameText, $"Pilot: {pilot.PilotName}");
        Set(mechNameText, $"Mech: {mech.MechName}");
        Set(healthText, $"HP: {selectedUnit.CurrentHealth}/{mech.Health}");
        Set(energyText, $"EN: {selectedUnit.CurrentEnergy}/{mech.Energy}");
        Set(moraleText, $"Morale: {pilot.Morale}");
        Set(meleePower, $"Melee: {pilot.Melee}");
        Set(rangedPower, $"Ranged: {pilot.Ranged}");
        Set(defenseStat, $"Defense: {pilot.Defense}");
        Set(evadeStat, $"Evade: {pilot.Evade}");
        Set(accuracyStat, $"Accuracy: {pilot.Accuracy}");

        /*
        Set(skillText, $"Skill: {pilot.Skill}");
        

        SetTerrainFields(pilot.TerrainRatings, pilotAirText, pilotGroundText, pilotWaterText, pilotSpaceText);
        SetTerrainFields(mech.TerrainRatings, mechAirText, mechGroundText, mechWaterText, mechSpaceText);
        */
    }

    private static void SetTerrainFields(TerrainRatings ratings, TMP_Text air, TMP_Text ground, TMP_Text water, TMP_Text space)
    {
        Set(air, $"Air: {ratings.Get(TerrainType.Air)}");
        Set(ground, $"Ground: {ratings.Get(TerrainType.Ground)}");
        Set(water, $"Water: {ratings.Get(TerrainType.Water)}");
        Set(space, $"Space: {ratings.Get(TerrainType.Space)}");
    }

    /*
    private bool IsConfigured() =>
        actionsPanel != null && weaponsPanel != null &&
        moveButton != null && attackButton != null && standbyButton != null && confirmButton != null &&
        weaponContent != null && weaponRowTemplate != null &&
        pilotNameText != null && mechNameText != null &&
        healthText != null && energyText != null && moraleText != null &&
        meleeText != null && rangedText != null && defenseText != null && evadeText != null &&
        accuracyText != null && skillText != null &&
        pilotAirText != null && pilotGroundText != null && pilotWaterText != null && pilotSpaceText != null &&
        mechAirText != null && mechGroundText != null && mechWaterText != null && mechSpaceText != null &&
        weaponNameText != null && weaponTypeText != null && weaponClassificationText != null &&
        weaponPowerText != null && weaponRangeText != null && weaponEnergyCostText != null &&
        weaponAccuracyText != null && weaponCriticalText != null &&
        weaponAirText != null && weaponGroundText != null && weaponWaterText != null && weaponSpaceText != null;
    */

    private bool IsConfigured() => weaponNameText != null && weaponDamageText != null && weaponRangeText != null && weaponEnergyCostText != null;


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

    private static void SetVisible(CanvasGroup panel, bool visible)
    {
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }

    private static void Set(TMP_Text text, string value) => text.text = value;

    private void UnitScreen()
    {
        if (battle == null || battle.SelectedUnit == null)
        {
            return;
        }

        SetPilotAndMechFields(battle.SelectedUnit);

        SetVisible(actionsPanel, false);
        SetVisible(UnitInfoPanel, true);
    }

    private void CloseCurrentMenu()
    {
        if (UnitInfoPanel != null && UnitInfoPanel.alpha > 0f)
        {
            SetVisible(UnitInfoPanel, false);
            SetVisible(actionsPanel, true);
            return;
        }

        if (weaponsPanel != null && weaponsPanel.alpha > 0f)
        {
            SetVisible(weaponsPanel, false);
            SetVisible(actionsPanel, true);
            return;
        }
    }
}
