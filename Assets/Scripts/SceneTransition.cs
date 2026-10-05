using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;

    private void Start()
    {
        if (fadeImage != null)
        {
            StartCoroutine(FadeIn());
        }    
    }

    private IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);

        Color fadeColor = fadeImage.color;
        fadeColor.a = 1f; // Start with opaque
        fadeImage.color = fadeColor;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeColor.a = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = fadeColor;
            yield return null;
        }
        fadeColor.a = 0f; // Ensure it's fully transparent
        fadeImage.color = fadeColor;
        fadeImage.gameObject.SetActive(false);

    }   

    // Start a fade to blackbefore loading the next scene
    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
       if (fadeImage == null)
        {
          Debug.LogError("Fade image is not assigned in the inspector.");
            yield break;

        }
        fadeImage.gameObject.SetActive(true);

        Color fadeColor = fadeImage.color;

        fadeColor.a = 0f; // Start with transparent
        fadeImage.color = fadeColor;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
           
            
            fadeColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = fadeColor;
            yield return null;
        }

        fadeColor.a = 1f; // Ensure it's fully opaque
        fadeImage.color = fadeColor;

        SceneManager.LoadScene(sceneName);
    }
}