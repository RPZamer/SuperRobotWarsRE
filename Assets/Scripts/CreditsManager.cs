using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsManager : MonoBehaviour
{
    [Header("WEEK 7: Credits References")]
    [SerializeField] private RectTransform scrollingCredits;
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private SceneTransition sceneTransition;

    [Header("WEEK 7: Credits Settings")]
    [SerializeField] private float scrollSpeed = 65f;
    [SerializeField] private float endingDelay = 3f;

    [Header("WEEK 7: Scene Settings")]
    [SerializeField] private string mainMenuScene = "Main Menu";

    // WEEK 7: Tracks whether the credits have finished scrolling.
    private bool creditsFinished;

    // WEEK 7: Credits supplied by the Super Robot Wars RE team.
    private const string CreditsContent =
        "<size=65><color=#FFD700>SUPER ROBOT WARS RE</color></size>\n" +
        "<size=40>DEMO CREDITS</size>\n\n\n" +

        "<size=48><color=#FFD700>PRESTO GAMING</color></size>\n\n" +

        "<size=36>GAMEPLAY AND UNIT DESIGN</size>\n" +
        "<size=42>Emerson Moya</size>\n\n" +

        "<size=36>CUTSCENES</size>\n" +
        "<size=42>Xavier Ware</size>\n\n" +

        "<size=36>TERRAIN IMPLEMENTATION</size>\n" +
        "<size=42>William Tyler</size>\n\n" +

        "<size=36>SORTIE DEPLOYMENT</size>\n" +
        "<size=42>William Tyler</size>\n\n" +

        "<size=36>MUSIC</size>\n" +
        "<size=42>Xavier Ware</size>\n\n" +

        "<size=36>UI AND UX</size>\n" +
        "<size=42>Alaze Rivera</size>\n\n" +

        "<size=36>ANIMATIONS AND SFX</size>\n" +
        "<size=42>Alaze Rivera</size>\n\n" +

        "<size=36>STORYBOARD</size>\n" +
        "<size=42>Emerson Moya</size>\n\n" +

        "<size=36>SCENE DYNAMICS</size>\n" +
        "<size=42>Xavier Ware</size>\n\n" +

        "<size=36>SPIRIT COMMANDS IMPLEMENTATION</size>\n" +
        "<size=42>Xavier Ware</size>\n\n\n" +

        "<size=48><color=#FFD700>ORIGINAL COPYRIGHT HOLDERS</color></size>\n\n" +

        "<size=36>MAZINGER Z / GREAT MAZINGER</size>\n" +
        "<size=32>Go Nagai, Toei Animation\nVis Media, Dynamic Planning</size>\n\n" +

        "<size=36>GETTER ROBO</size>\n" +
        "<size=32>Ken Ishikawa, Go Nagai\nToei Animation, Vis Media\nDynamic Planning, Bandai Visuals</size>\n\n" +

        "<size=36>GUNDAM</size>\n" +
        "<size=32>Sunrise, Sotsu, Bandai Filmworks</size>\n\n" +

        "<size=36>SUPER ROBOT WARS</size>\n" +
        "<size=32>Bandai Namco, Banpresto</size>\n\n\n" +

        "<size=60><color=#FFD700>THANK YOU FOR PLAYING!</color></size>";

    private void Start()
    {
        // WEEK 7: Ensure the credits can scroll even if the
        // previous battle scene was paused.
        Time.timeScale = 1f;

        if (scrollingCredits == null || creditsText == null)
        {
            Debug.LogError(
                "WEEK 7: Credits UI references are missing.",
                this);

            enabled = false;
            return;
        }

        // WEEK 7: Display the team's credits.
        creditsText.richText = true;
        creditsText.text = CreditsContent;

        // WEEK 7: Calculate the full height of the credits.
        creditsText.ForceMeshUpdate();

        float creditsHeight = creditsText.preferredHeight;

        scrollingCredits.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            creditsHeight);

        // WEEK 7: Position the credits below the screen.
        Canvas.ForceUpdateCanvases();

        RectTransform canvasRect =
            scrollingCredits.GetComponentInParent<Canvas>()
                .GetComponent<RectTransform>();

        float screenHeight = canvasRect.rect.height;

        scrollingCredits.anchoredPosition =
            new Vector2(
                scrollingCredits.anchoredPosition.x,
                -screenHeight / 2f - creditsHeight / 2f);
    }

    private void Update()
    {
        if (creditsFinished)
        {
            return;
        }

        // WEEK 7: Scroll the credits upward.
        scrollingCredits.anchoredPosition +=
            Vector2.up * scrollSpeed * Time.unscaledDeltaTime;

        // WEEK 7: Check when the entire credits text
        // has moved above the screen.
        RectTransform canvasRect =
            scrollingCredits.GetComponentInParent<Canvas>()
                .GetComponent<RectTransform>();

        float screenHeight = canvasRect.rect.height;
        float creditsHeight = scrollingCredits.rect.height;

        float endPosition =
            screenHeight / 2f + creditsHeight / 2f;

        if (scrollingCredits.anchoredPosition.y >= endPosition)
        {
            creditsFinished = true;
            StartCoroutine(FinishCredits());
        }
    }

    // WEEK 7: Wait briefly after the credits finish.
    private IEnumerator FinishCredits()
    {
        yield return new WaitForSecondsRealtime(endingDelay);

        // WEEK 7: Return to the main menu using
        // the existing scene transition system.
        if (sceneTransition != null)
        {
            sceneTransition.FadeToScene(mainMenuScene);
        }
        else
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}