using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

    // SCENE DEBUGS: Track active fades only to warn about interruptions or overlapping transitions.
    private int activeFades;

    private void Start()
    {
        if (fadeImage != null)
        {
            StartCoroutine(FadeIn());
        }
        else
        {
            Debug.LogWarning("SCENE DEBUGS: Startup fade skipped because the fade image is not assigned.", this);
        }
    }

    private IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);

        Color fadeColor = fadeImage.color;
        fadeColor.a = 1f; // Start with opaque
        fadeImage.color = fadeColor;

        float elapsedTime = 0f;
        activeFades++;
        LogSceneDebug("FadeIn started", elapsedTime);
        float nextDebugTime = Time.realtimeSinceStartup + 1f;
        bool stallWarningLogged = false;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeColor.a = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = fadeColor;
            LogFadeProgress("FadeIn", elapsedTime, ref nextDebugTime, ref stallWarningLogged);
            yield return null;
        }
        fadeColor.a = 0f; // Ensure it's fully transparent
        fadeImage.color = fadeColor;
        activeFades--;
        fadeImage.gameObject.SetActive(false);
        LogSceneDebug("FadeIn completed", elapsedTime);

    }

    // Start a fade to blackbefore loading the next scene
    public void FadeToScene(string sceneName)
    {
        if (activeFades > 0)
        {
            Debug.LogWarning("SCENE DEBUGS: Another fade is still running. Overlapping fades can write different alpha values to the same image; check repeated or early FadeToScene calls.", this);
        }
        LogSceneDebug($"FadeToScene requested: {sceneName}", 0f);
        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        if (fadeImage == null)
        {
            Debug.LogError("SCENE DEBUGS: Fade image is not assigned in the inspector.", this);
            yield break;

        }
        fadeImage.gameObject.SetActive(true);

        Color fadeColor = fadeImage.color;

        fadeColor.a = 0f; // Start with transparent
        fadeImage.color = fadeColor;

        float elapsedTime = 0f;
        activeFades++;
        LogSceneDebug($"FadeOut started: {sceneName}", elapsedTime);
        float nextDebugTime = Time.realtimeSinceStartup + 1f;
        bool stallWarningLogged = false;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;


            fadeColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = fadeColor;
            LogFadeProgress("FadeOut", elapsedTime, ref nextDebugTime, ref stallWarningLogged);
            yield return null;
        }

        fadeColor.a = 1f; // Ensure it's fully opaque
        fadeImage.color = fadeColor;

        activeFades--;
        LogSceneDebug($"FadeOut completed; loading: {sceneName}", elapsedTime);
        SceneManager.LoadScene(sceneName);
    }

    // SCENE DEBUGS: Real-time progress logs continue when scaled game time is paused.
    // Limit them to once per second so a stuck fade is visible without logging every frame.
    private void LogFadeProgress(string fade, float elapsedTime, ref float nextDebugTime, ref bool stallWarningLogged)
    {
        if (Time.realtimeSinceStartup < nextDebugTime) return;

        nextDebugTime = Time.realtimeSinceStartup + 1f;
        LogSceneDebug($"{fade} progress", elapsedTime);

        // SCENE DEBUGS: Warn once per fade if scaled time cannot advance the fade, while keeping normal progress logs.
        if (!stallWarningLogged && Time.deltaTime <= 0f)
        {
            stallWarningLogged = true;
            Debug.LogWarning($"SCENE DEBUGS: {fade} is stalled at alpha={fadeImage.color.a:F3}: deltaTime={Time.deltaTime:F6}, timeScale={Time.timeScale:F3}. The fade uses scaled time; check whether PauseMenu or another system has paused game time.", this);
        }
    }

    // SCENE DEBUGS: Record timing and overlay state to identify why a fade stops.
    // These logs observe the existing fade; they do not change its timing or visibility.
    private void LogSceneDebug(string stage, float elapsedTime)
    {
        string imageState = fadeImage == null ? "missing" :
            $"{fadeImage.name}, alpha={fadeImage.color.a:F3}, active={fadeImage.gameObject.activeInHierarchy}, enabled={fadeImage.enabled}";
        Debug.Log($"SCENE DEBUGS: {stage} | scene={gameObject.scene.name}, transition={name} | elapsed={elapsedTime:F3}/{fadeDuration:F3}, timeScale={Time.timeScale:F3}, deltaTime={Time.deltaTime:F6}, unscaledDeltaTime={Time.unscaledDeltaTime:F6} | image={imageState}", this);
    }

    private void OnDisable()
    {
        // SCENE DEBUGS: Deactivating the object can stop its fade coroutine; scene unloading also logs here.
        if (activeFades > 0)
        {
            Debug.LogWarning($"SCENE DEBUGS: SceneTransition disabled during {activeFades} unfinished fade(s) in {gameObject.scene.name}. Deactivating its GameObject stops its coroutines and can leave the overlay visible; check object activation or unexpected scene unloading.", this);
        }
        else
        {
            Debug.Log($"SCENE DEBUGS: SceneTransition disabled in {gameObject.scene.name}; no fade was running.", this);
        }
    }
}