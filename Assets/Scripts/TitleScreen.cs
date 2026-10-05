using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI PressAnyKeyText;
    [SerializeField] GameObject TitleButtons;

    [SerializeField] float FadeSpeed = 2f;

    void Start()
    {
        TitleButtons.SetActive(false);
        PressAnyKeyText.gameObject.SetActive(true);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        float alpha = Mathf.PingPong(Time.time * FadeSpeed, 1f);

        Color color = PressAnyKeyText.color;    // orignal color
        color.a = alpha;
        PressAnyKeyText.color = color;

        if (Input.anyKeyDown)       // press any key
        {
            PressAnyKeyText.gameObject.SetActive(false);
            TitleButtons.SetActive(true);
        }
    }
 
    public void LoadGame()
    {
        SceneManager.LoadScene("First Level");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
