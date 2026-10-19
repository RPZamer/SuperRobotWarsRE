using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// WEEK 6: Plays story cutscenes using the existing dialogue system.
public class CutsceneManager : MonoBehaviour
{
    [Header("WEEK 6: Cutscene References")]
    [SerializeField] private DialogSystem dialogSystem;
    [SerializeField] private SceneTransition sceneTransition;

    [Header("WEEK 6: Cutscene Settings")]
    [SerializeField] private CutsceneData openingCutscene;

    // WEEK 7: CUTSCENE SORTIE INTEGRATION
    // Levels 3, 4, and the final stage deploy after opening dialogue.
    [Header("WEEK 7: Sortie Deployment")]
    [SerializeField] private bool sortieAfterOpeningCutscene;

    // WEEK 6: Shared reinforcement system for allied and enemy units.
    [Header("WEEK 6: Reinforcement References")]
    [SerializeField] private EnemyReinforcementReveal reinforcementReveal;
    [SerializeField] private BattleMusic battleMusic;

    // WEEK 6: Configure reinforcement events separately for each level.
    // A value of -1 disables that event.
    [Header("WEEK 6: Reinforcement Dialogue Triggers")]
    [SerializeField] private int alliedReinforcementAfterLine = -1;
    [SerializeField] private int enemyReinforcementAfterLine = 13;

    // WEEK 7: Holding left click automatically advances dialogue.
    [Header("WEEK 7: Skip Cutscene Settings")]
    [SerializeField, Min(0.01f)]
    private float skipDialogueInterval = 0.08f;

    private bool isPlaying;
    private bool advanceRequested;
    private bool eventPlaying;

    // WEEK 7: Tracks the next time held input may advance dialogue.
    private float nextSkipTime;

    // WEEK 6: Other systems can check whether a cutscene is active.
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (dialogSystem == null)
        {
            dialogSystem = FindFirstObjectByType<DialogSystem>();
        }

        if (sceneTransition == null)
        {
            sceneTransition = FindFirstObjectByType<SceneTransition>();
        }

        if (reinforcementReveal == null)
        {
            reinforcementReveal =
                FindFirstObjectByType<EnemyReinforcementReveal>();
        }

        if (battleMusic == null)
        {
            battleMusic = FindFirstObjectByType<BattleMusic>();
        }
    }
    // WEEK 7: CUTSCENE SORTIE INTEGRATION
    // Control whether the opening cutscene happens before or after deployment.
    private IEnumerator Start()
    {
        SortieDeployment sortieDeployment =
            FindFirstObjectByType<SortieDeployment>();

        if (sortieAfterOpeningCutscene && sortieDeployment != null)
        {
            // WEEK 7: Hide deployment before opening dialogue begins.
            sortieDeployment.SetDeploymentAvailable(false);

            // WEEK 7: Play the opening cutscene first.
            if (openingCutscene != null)
            {
                PlayCutscene(openingCutscene);

                // WEEK 7: Wait until the entire cutscene finishes.
                yield return new WaitUntil(() => !isPlaying);
            }

            // WEEK 7: Reveal deployment after the opening dialogue.
            sortieDeployment.SetDeploymentAvailable(true);

            // WEEK 7: Wait for the player to press START MISSION.
            yield return new WaitUntil(
                () => sortieDeployment.DeploymentConfirmed);
        }
        else
        {
            // WEEK 6: Preserve the original deployment-first behavior.
            if (sortieDeployment != null)
            {
                yield return new WaitUntil(
                    () => sortieDeployment.DeploymentConfirmed);
            }

            if (openingCutscene != null)
            {
                PlayCutscene(openingCutscene);
            }
        }
    }

    private void Update()
    {
        // WEEK 6: Prevent input during reinforcement animations.
        if (!isPlaying || eventPlaying)
        {
            return;
        }

        // WEEK 7: Detect whether the player is holding left click.
        bool holdingSkip =
            Mouse.current != null &&
            Mouse.current.leftButton.isPressed;

        // WEEK 7: Keep the original keyboard and mouse controls.
        bool advancePressed =
            (Keyboard.current != null &&
             (Keyboard.current.spaceKey.wasPressedThisFrame ||
              Keyboard.current.enterKey.wasPressedThisFrame)) ||
            (Mouse.current != null &&
             Mouse.current.leftButton.wasPressedThisFrame);

        // WEEK 7: Holding left click automatically advances dialogue.
        if (holdingSkip)
        {
            if (Time.unscaledTime < nextSkipTime)
            {
                return;
            }

            nextSkipTime =
                Time.unscaledTime + skipDialogueInterval;

            if (dialogSystem != null && dialogSystem.IsTyping)
            {
                dialogSystem.CompleteTyping();
            }
            else
            {
                advanceRequested = true;
            }

            return;
        }

        // WEEK 7: Reset the skip timer when left click is released.
        nextSkipTime = 0f;

        if (!advancePressed)
        {
            return;
        }

        // WEEK 6: First press completes the typewriter animation.
        if (dialogSystem != null && dialogSystem.IsTyping)
        {
            dialogSystem.CompleteTyping();
            return;
        }

        // WEEK 6: Second press advances to the next dialogue line.
        advanceRequested = true;
    }

    // WEEK 6: Used for both opening and victory cutscenes.
    public void PlayCutscene(CutsceneData cutscene)
    {
        if (cutscene == null || isPlaying)
        {
            return;
        }

        StartCoroutine(RunCutscene(cutscene));
    }

    // WEEK 7: FINAL STAGE - Play a cutscene and wait
    // until all its dialogue has finished.
    public IEnumerator PlayCutsceneAndWait(CutsceneData cutscene)
    {
        if (cutscene == null)
        {
            yield break;
        }

        // WEEK 7: Wait for any current cutscene to finish.
        yield return new WaitUntil(() => !isPlaying);

        // WEEK 7: Run the requested cutscene to completion.
        yield return RunCutscene(cutscene);
    }
    private IEnumerator RunCutscene(CutsceneData cutscene)
    {
        isPlaying = true;

        // WEEK 7: Reset held-input timing for each new cutscene.
        nextSkipTime = 0f;
        advanceRequested = false;

        if (dialogSystem == null)
        {
            Debug.LogError(
                "WEEK 6: CutsceneManager needs a DialogSystem.",
                this);

            isPlaying = false;
            yield break;
        }

        // WEEK 6: Show the existing dialogue interface.
        yield return dialogSystem.FadeIn();

        if (cutscene.lines != null)
        {
            for (int lineIndex = 0;
                 lineIndex < cutscene.lines.Length;
                 lineIndex++)
            {
                CutsceneLine line = cutscene.lines[lineIndex];

                if (line == null)
                {
                    continue;
                }

                advanceRequested = false;

                dialogSystem.DisplayLine(
                    line.pilot,
                    line.emotion,
                    line.dialogue,
                    line.showPortrait);

                // WEEK 6: Wait for the player to advance the dialogue.
                yield return new WaitUntil(
                    () => advanceRequested && !dialogSystem.IsTyping);

                // WEEK 6: Only opening cutscenes trigger reinforcement events.
                if (cutscene != openingCutscene)
                {
                    continue;
                }

                // WEEK 6: Reveal allied reinforcements at the configured line.
                if (lineIndex == alliedReinforcementAfterLine)
                {
                    eventPlaying = true;
                    advanceRequested = false;

                    // WEEK 6: Allied reinforcements use their assigned music.
                    if (battleMusic != null)
                    {
                        battleMusic.PlayAllyReinforcementMusic();
                    }

                    if (reinforcementReveal != null)
                    {
                        yield return reinforcementReveal.RevealAllies();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "WEEK 6: Reinforcement Reveal is missing.",
                            this);
                    }

                    eventPlaying = false;
                }

                // WEEK 6: Reveal enemy reinforcements at the configured line.
                if (lineIndex == enemyReinforcementAfterLine)
                {
                    eventPlaying = true;
                    advanceRequested = false;

                    if (battleMusic != null)
                    {
                        battleMusic.PlayEnemyReinforcementMusic();
                    }

                    if (reinforcementReveal != null)
                    {
                        yield return reinforcementReveal.RevealEnemies();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "WEEK 6: Reinforcement Reveal is missing.",
                            this);
                    }

                    eventPlaying = false;
                }
            }
        }

        // WEEK 6: Hide dialogue after the final line.
        yield return dialogSystem.FadeOut();

        // WEEK 6: Restore overworld music when opening dialogue ends.
        if (cutscene == openingCutscene && battleMusic != null)
        {
            battleMusic.FadeToOverworldMusic();
        }

        // WEEK 7: Clear skip input when the cutscene finishes.
        advanceRequested = false;
        nextSkipTime = 0f;

        isPlaying = false;

        // WEEK 6: Preserve optional scene transitions.
        if (cutscene.transitionToScene)
        {
            if (sceneTransition != null &&
                !string.IsNullOrWhiteSpace(cutscene.destinationScene))
            {
                sceneTransition.FadeToScene(cutscene.destinationScene);
            }
            else
            {
                Debug.LogWarning(
                    "WEEK 6: Cutscene transition is missing a " +
                    "SceneTransition or destination scene.",
                    this);
            }
        }
    }
}