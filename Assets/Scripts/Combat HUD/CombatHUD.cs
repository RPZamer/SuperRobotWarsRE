using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public class CombatHUD : MonoBehaviour
{
    [Header("Selected Unit")]
    [SerializeField] private TMP_Text selectedUnitNameText;
    [SerializeField] private TMP_Text selectedUnitHPText;
    // Preserve the team's terrain readout.
    [SerializeField] private TMP_Text tileInforText;

    // WEEK 3: Store the persistent HUD text used to display the final
    // Victory or Defeat result when the scenario ends.
    [Header("Scenario Result Screens")]
    [SerializeField] private GameObject victoryCanvas;
    [SerializeField] private GameObject defeatCanvas;

    [Header("Defeat Screen Fade")]
    [SerializeField] private CanvasGroup defeatBackgroundGroup;
    [SerializeField] private CanvasGroup defeatTextGroup;

    [SerializeField] private float backgroundFadeDuration = 2f;
    [SerializeField] private float textFadeDuration = 1f;

    [SerializeField] private GameObject resultButtons;


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
        HideScenarioResult();

        battleHud = FindFirstObjectByType<BattleSystem>();
        // WEEK 6 CHANGES PLEASE READ: End Turn previously remained silent when this HUD could not connect its button to a battle controller.
        // Startup warnings now identify missing references and multiple active controllers that could select the wrong battle on another setup.
        // The existing listener is still attached exactly as before, and these diagnostics do not assign references or change turn availability.
        string setupContext = $"[WEEK 6 SETUP WARNING] scene='{gameObject.scene.path}', HUD='{name}': ";
        if (EndPhaseButton == null)
            Debug.LogWarning(setupContext + "CombatHUD's EndPhaseButton field is unassigned. Assign the End Turn Button so this HUD can connect its click to EndPlayerPhase.", this);
        if (battleHud == null)
            Debug.LogWarning(setupContext + "No active BattleSystem was found. This HUD cannot connect End Turn; check that the intended battle controller exists and is active in this scene.", this);
        int activeBattles = 0;
        foreach (BattleSystem system in FindObjectsByType<BattleSystem>())
            if (system.isActiveAndEnabled) activeBattles++;
        if (activeBattles > 1)
            Debug.LogWarning(setupContext + $"Found {activeBattles} active BattleSystems. The existing automatic lookup selected '{battleHud?.name}'; check for duplicate controllers or scenes because End Turn may target a different battle.", this);
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
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
        }
        if (defeatCanvas != null)
        {
            defeatCanvas.SetActive(false);

        }
    }

    // WEEK 3: Display a persistent Victory or Defeat message after
    // the battle reaches its final scenario result.
    public void ShowScenarioResult()
    {
        // WEEK 4: UI VISIBILITY - Hide combat readouts when the final result appears.
        ClearSelectedUnit();
        ClearTargetedEnemy();
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(true);
        }
    }

    // Preserve the team's explicit HUD visibility controls.
    public void ShowHUD() => gameObject.SetActive(true);
    public void HideHUD() => gameObject.SetActive(false);


    public void ShowVictoryScreen()
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
        }

        if (defeatCanvas != null)
        {
            defeatCanvas.SetActive(false);
        }

        if (resultButtons != null)
        {
            resultButtons.SetActive(false);
        }
    }

    public void ShowDefeatScreen()
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
        }

        if (defeatCanvas != null)
        {
            defeatCanvas.SetActive(true);
        }

        // make the fade in background appear
        defeatCanvas.SetActive(true);

        defeatBackgroundGroup.alpha = 0f;
        defeatTextGroup.alpha = 0f;

        StartCoroutine(DefeatFadeSequence());
    }


    // button functions for the defeat screen
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToIntermission()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Intermission");
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title Screen");
    }

    private IEnumerator DefeatFadeSequence()
    {
        // Fade in the black background first.
        float elapsed = 0f;

        while (elapsed < backgroundFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            defeatBackgroundGroup.alpha = Mathf.Clamp01(elapsed / backgroundFadeDuration);

            yield return null;
        }

        defeatBackgroundGroup.alpha = 1f;

        // after the background fades in, fade in the text
        elapsed = 0f;

        while (elapsed < textFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            defeatTextGroup.alpha = Mathf.Clamp01(elapsed / textFadeDuration);

            yield return null;
        }

        defeatTextGroup.alpha = 1f;

        // show the buttons after the background and text have faded in
        if (resultButtons != null)
        { 
            resultButtons.SetActive(true);
        }
    }
}