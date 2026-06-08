using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// Manages the pause state, UI navigation, and input detection
public class PauseMenu : UnityEngine.MonoBehaviour
{
    public static bool gameIsPaused = false;

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject optionsMenuUI;
    [SerializeField] private GameObject quitMenuUI;


    public void OnPause(InputAction.CallbackContext context)
    {
        if (optionsMenuUI.activeSelf || quitMenuUI.activeSelf && gameIsPaused)
        {
            pauseMenuUI.SetActive(true);
            optionsMenuUI.SetActive(false);
            quitMenuUI.SetActive(false);
        }
        else if (gameIsPaused)
        {
            Resume();
        }
        else if(!gameIsPaused)
        {
            Pause();
        }
    }
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        optionsMenuUI.SetActive(false);
        quitMenuUI.SetActive(false);
        Time.timeScale = 1f;
        gameIsPaused = false;
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        gameIsPaused = true;
    }

    // Restores time scale to prevent a frozen Main Menu, then loads the scene
    public void LeaveGame()
    {
        Time.timeScale = 1f; // Failsafe: Ensures the next scene doesn't start frozen!
        gameIsPaused = false;
        SceneManager.LoadScene("MainMenu");
    }
}