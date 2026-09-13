using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialogSystem : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private CanvasGroup dialogueCanvas;
    [SerializeField] private Text speakerNameText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Image profileBox;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float secondsPerCharacter = 0.03f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
    [SerializeField] private bool hideOnAwake = true;

    private Coroutine typingCoroutine;
    private string completeLine = string.Empty;

    public bool IsTyping { get; private set; }
    public bool IsVisible => dialogueCanvas != null && dialogueCanvas.alpha > 0f;

    private void Awake()
    {
        if (hideOnAwake)
        {
            SetVisibleImmediate(false);
        }
    }

    public void DisplayLine(PilotBase pilot, PilotEmotion emotion, string line, bool showPortrait = false)
    {
        StopTyping();

        completeLine = line ?? string.Empty;
        speakerNameText.text = pilot != null ? pilot.PilotName : string.Empty;
        dialogueText.text = string.Empty;

        if (profileBox != null)
        {
            profileBox.sprite = showPortrait && pilot != null ? pilot.GetPortrait(emotion) : null;
            profileBox.enabled = profileBox.sprite != null;
        }

        typingCoroutine = StartCoroutine(TypeLine());
    }

    public void CompleteTyping()
    {
        if (!IsTyping)
        {
            return;
        }

        StopTyping();
        dialogueText.text = completeLine;
    }

    public IEnumerator FadeIn()
    {
        yield return FadeTo(1f);
    }

    public IEnumerator FadeOut()
    {
        yield return FadeTo(0f);
    }

    public void SetVisibleImmediate(bool visible)
    {
        if (dialogueCanvas == null)
        {
            return;
        }

        dialogueCanvas.alpha = visible ? 1f : 0f;
        dialogueCanvas.interactable = visible;
        dialogueCanvas.blocksRaycasts = visible;
    }

    private IEnumerator TypeLine()
    {
        IsTyping = true;

        if (secondsPerCharacter <= 0f)
        {
            dialogueText.text = completeLine;
        }
        else
        {
            for (int characterCount = 1; characterCount <= completeLine.Length; characterCount++)
            {
                dialogueText.text = completeLine.Substring(0, characterCount);
                yield return new WaitForSecondsRealtime(secondsPerCharacter);
            }
        }

        IsTyping = false;
        typingCoroutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (dialogueCanvas == null)
        {
            yield break;
        }

        float startingAlpha = dialogueCanvas.alpha;

        if (fadeDuration <= 0f)
        {
            dialogueCanvas.alpha = targetAlpha;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                dialogueCanvas.alpha = Mathf.Lerp(startingAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }
        }

        bool visible = targetAlpha > 0f;
        dialogueCanvas.alpha = targetAlpha;
        dialogueCanvas.interactable = visible;
        dialogueCanvas.blocksRaycasts = visible;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        IsTyping = false;
    }

    private void OnDisable()
    {
        StopTyping();
    }
}