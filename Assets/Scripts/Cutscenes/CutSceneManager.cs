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

    // WEEK 6: Shared reinforcement system for allied and enemy units.
    [Header("WEEK 6: Reinforcement References")]
    [SerializeField] private EnemyReinforcementReveal reinforcementReveal;
    [SerializeField] private BattleMusic battleMusic;

    // WEEK 6: Configure reinforcement events separately for each level.
    // A value of -1 disables that event.
    [Header("WEEK 6: Reinforcement Dialogue Triggers")]
    [SerializeField] private int alliedReinforcementAfterLine = -1;
    [SerializeField] private int enemyReinforcementAfterLine = 13;

    private bool isPlaying;
    private bool advanceRequested;
    private bool eventPlaying;

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

    private void Start()
    {
        // WEEK 6: Automatically begin the opening cutscene.
        if (openingCutscene != null)
        {
            PlayCutscene(openingCutscene);
        }
    }

    private void Update()
    {
        // WEEK 6: Prevent input during reinforcement animations.
        if (!isPlaying || eventPlaying)
        {
            return;
        }

        bool advancePressed =
            (Keyboard.current != null &&
             (Keyboard.current.spaceKey.wasPressedThisFrame ||
              Keyboard.current.enterKey.wasPressedThisFrame)) ||
            (Mouse.current != null &&
             Mouse.current.leftButton.wasPressedThisFrame);

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

    private IEnumerator RunCutscene(CutsceneData cutscene)
    {
        isPlaying = true;

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