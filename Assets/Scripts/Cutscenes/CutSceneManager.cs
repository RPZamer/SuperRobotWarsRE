using System.Collections;
using System.Linq;
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

    // WEEK 6: References for the cinematic enemy reinforcement reveal.
    [Header("WEEK 6: Enemy Reinforcement Event")]
    [SerializeField] private EnemyReinforcementReveal reinforcementReveal;
    [SerializeField] private BattleMusic battleMusic;

    // WEEK 6: Element 13 is Amuro's warning.
    [SerializeField] private int reinforcementAfterLine = 13;

    private bool isPlaying;
    private bool advanceRequested;

    // WEEK 6: Prevent dialogue advancement during cinematic events.
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

        // WEEK 6: Find the reinforcement components if not assigned.
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
        // WEEK 6: Block input while reinforcement animation is playing.
        if (!isPlaying || eventPlaying)
        {
            return;
        }

        // WEEK 6: Advance with Space, Enter, or left mouse click.
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

        // WEEK 6: First press completes the current typewriter line.
        if (dialogSystem != null && dialogSystem.IsTyping)
        {
            dialogSystem.CompleteTyping();
            return;
        }

        // WEEK 6: Next press advances to the following dialogue line.
        advanceRequested = true;
    }

    // WEEK 6: Can also be called by the battle system after victory.
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

        // WEEK 6: Display the existing dialogue interface.
        yield return dialogSystem.FadeIn();

        if (cutscene.lines != null)
        {
            // WEEK 6: Track the dialogue index to trigger story events.
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

                // WEEK 6: Wait for the player to finish and advance.
                yield return new WaitUntil(
                    () => advanceRequested && !dialogSystem.IsTyping);

                // WEEK 6: Trigger reinforcements after Amuro's warning.
                // Only the opening cutscene uses this event.
                if (cutscene == openingCutscene &&
                    lineIndex == reinforcementAfterLine)
                {
                    eventPlaying = true;
                    advanceRequested = false;

                    // WEEK 6: Play enemy reinforcement music when Amuro
                    // notices the approaching Zakus.
                    if (battleMusic != null)
                    {
                        battleMusic.PlayEnemyReinforcementMusic();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "WEEK 6: Battle Music reference is missing.",
                            this);
                    }

                    // WEEK 6: Reveal the existing enemy units one by one.
                    if (reinforcementReveal != null)
                    {
                        yield return reinforcementReveal.RevealEnemies();
                    }
                    else
                    {
                        Debug.LogWarning(
                            "WEEK 6: Enemy Reinforcement Reveal is missing.",
                            this);
                    }

                    eventPlaying = false;
                }
            }
        }

        // WEEK 6: Hide the dialogue interface after the final line.
        yield return dialogSystem.FadeOut();

        // WEEK 6: Restore overworld music after the opening cutscene.
        // Victory and other cutscenes will not trigger this music change.
        if (cutscene == openingCutscene && battleMusic != null)
        {
            battleMusic.FadeToOverworldMusic();
        }

        isPlaying = false;

        // WEEK 6: Transition only when this cutscene requires it.
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