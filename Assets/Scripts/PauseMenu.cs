using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject optionsMenuUI;
    [SerializeField] private GameObject quitMenuUI;
    [SerializeField] private UnityEvent pauseSelect;
    [SerializeField] private UnityEvent resumeEvent;


    public void OnPause(InputAction.CallbackContext context)
    {
        if (gameIsPaused)
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
        ResetInputs();
        pauseMenuUI.SetActive(false);
        optionsMenuUI.SetActive(false);
        quitMenuUI.SetActive(false);
        Time.timeScale = 1f;
        gameIsPaused = false;
    }

    public void ResetInputs()
    {
        resumeEvent.Invoke();
    }
    private void Pause()
    {
        pauseSelect.Invoke();
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
    private void Update()
    {
        Debug.Log(gameIsPaused);
    }
}