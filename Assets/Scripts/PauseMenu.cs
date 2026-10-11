using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;



public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject returnButton;
    private bool isPaused = false;

    
    void Start()
    {
        Time.timeScale = 1f; // Ensure the game starts unpaused
        pausePanel.SetActive(false); // Ensure the pause panel is hidden at the start

        if (returnButton != null)
        {
            bool inIntermission = SceneManager.GetActiveScene().name == "Intermission";
            returnButton.SetActive(!inIntermission);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Pause the game
        pausePanel.SetActive(true); // Show the pause panel
        
    }
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume the game
        pausePanel.SetActive(false); // Hide the pause panel
        
    }
    public void ReturnToIntermission()
    {
        isPaused = false;
        Time.timeScale = 1f; // Ensure the game is unpaused before returning
        SceneManager.LoadScene("Intermission");
    }

    public void ExitGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Ensure the game is unpaused before exiting
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the editor
#endif  
    }


    private void OnDestroy()
    {
        if(isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}

