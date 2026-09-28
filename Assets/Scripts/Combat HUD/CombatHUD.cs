using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : MonoBehaviour
{
    [Header("Selected Unit")]
    [SerializeField] private TMP_Text selectedUnitNameText;
    [SerializeField] private TMP_Text selectedUnitHPText;
    // Preserve the team's terrain readout.
    [SerializeField] private TMP_Text tileInforText;

    // WEEK 3: Store the persistent HUD text used to display the final
    // Victory or Defeat result when the scenario ends.
    [Header("Scenario Result")]
    [SerializeField] private TMP_Text scenarioResultText;

    // WEEK 3: Store the HUD text used to display information about
    // the enemy currently being targeted by the player.
    [Header("Targeted Enemy")]
    [SerializeField] private TMP_Text targetedEnemyNameText;
    [SerializeField] private TMP_Text targetedEnemyHPText;
    [SerializeField] private Button EndPhaseButton;

    private BattleSystem battleHud;

    // WEEK 4: UI VISIBILITY - Hide unit and target readouts until they are needed.
    private void Awake()
    {
        ClearSelectedUnit();
        ClearTargetedEnemy();
    }

    private void Start()
    {
        battleHud = FindFirstObjectByType<BattleSystem>();
        if (EndPhaseButton != null && battleHud != null)
            EndPhaseButton.onClick.AddListener(battleHud.EndPlayerPhase);
    }

    // WEEK 3: Displays the currently selected player's mech information.
    public void ShowSelectedUnit(BattleUnit unit)
    {
        if (unit == null || unit.Mech == null)
        {
            ClearSelectedUnit();
            return;
        }

        // WEEK 4: UI VISIBILITY - Restore selected-unit text and clear the previous target.
        selectedUnitNameText.enabled = true;
        selectedUnitHPText.enabled = true;
        ClearTargetedEnemy();
        selectedUnitNameText.text = unit.Mech.MechName;
        selectedUnitHPText.text = $"HP: {unit.CurrentHealth} / {unit.Mech.Health}";
        ClearTargetedEnemy();
        selectedUnitNameText.text = unit.Mech.MechName;
        selectedUnitHPText.text = $"HP: {unit.CurrentHealth} / {unit.Mech.Health}";

        if (tileInforText != null)
        {
            tileInforText.enabled = true;
            tileInforText.text = $"Terrain: {unit.TileType}";
        }
    }

    // WEEK 3: Displays identifying information and current HP for
    // the enemy currently being targeted by the player.
    public void ShowTargetedEnemy(BattleUnit enemy)
    {
        if (enemy == null || enemy.Mech == null)
        {
            ClearTargetedEnemy();
            return;
        }

        // WEEK 4: UI VISIBILITY - Restore target text when an attack target is chosen.
        targetedEnemyNameText.enabled = true;
        targetedEnemyHPText.enabled = true;
        targetedEnemyNameText.text = $"Enemy: {enemy.Mech.MechName}";
        targetedEnemyHPText.text = $"HP: {enemy.CurrentHealth} / {enemy.Mech.Health}";
    }

    // WEEK 3: Clears enemy information when there is no valid enemy target
    // so the HUD does not continue displaying information from an old target.
    public void ClearTargetedEnemy()
    {
        // WEEK 4: UI VISIBILITY - Hide unused labels without changing their Inspector references or layout.
        targetedEnemyNameText.enabled = false;
        targetedEnemyHPText.enabled = false;
    }

    // WEEK 3: Store the HUD text used to show the current battle phase
    // and turn number so the player always knows whose turn it is.
    [Header("Battle Status")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text turnText;

    // WEEK 3: Updates the Combat HUD with the current battle phase
    // and turn number whenever control changes between teams.
    public void ShowBattleStatus(string phaseName, int turnNumber)
    {
        // WEEK 4: UI VISIBILITY - Clear old readouts at phase changes; keep phase and turn visible.
        ClearSelectedUnit();
        ClearTargetedEnemy();
        phaseText.text = phaseName;
        turnText.text = $"Turn: {turnNumber}";
    }

    // WEEK 3: Clears old information so the HUD never shows data
    // from a unit that is no longer selected.
    public void ClearSelectedUnit()
    {
        // WEEK 4: UI VISIBILITY - Hide the readout while preserving manual Inspector assignments.
        selectedUnitNameText.enabled = false;
        selectedUnitHPText.enabled = false;
        if (tileInforText != null) tileInforText.enabled = false;
    }

    // WEEK 3: Hide the final scenario result while normal battle
    // gameplay is still in progress.
    public void HideScenarioResult()
    {
        if (scenarioResultText != null)
        {
            scenarioResultText.gameObject.SetActive(false);
        }
    }

    // WEEK 3: Display a persistent Victory or Defeat message after
    // the battle reaches its final scenario result.
    public void ShowScenarioResult(string resultText)
    {
        // WEEK 4: UI VISIBILITY - Hide combat readouts when the final result appears.
        ClearSelectedUnit();
        ClearTargetedEnemy();
        if (scenarioResultText != null)
        {
            scenarioResultText.text = resultText;
            scenarioResultText.gameObject.SetActive(true);
        }
    }

    // Preserve the team's explicit HUD visibility controls.
    public void ShowHUD() => gameObject.SetActive(true);
    public void HideHUD() => gameObject.SetActive(false);
}