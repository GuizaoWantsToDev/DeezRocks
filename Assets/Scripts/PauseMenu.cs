using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// Manages the pause state, UI navigation, and input detection
public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject optionsMenuUI;
    [SerializeField] private GameObject quitMenuUI;

    private void Update()
    {
        // Check if the Escape key was pressed on the keyboard
        bool keyboardPause = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        // Check if the Options (PlayStation) or Start (Xbox) button was pressed on the gamepad
        bool gamepadPause = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        // Toggle the pause state if either input is detected
        if (keyboardPause || gamepadPause)
        {
            if (gameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // Hides UI menus, restores game time scale, and updates the boolean state
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        optionsMenuUI.SetActive(false);
        quitMenuUI.SetActive(false);
        Time.timeScale = 1f;
        gameIsPaused = false;
    }

    // Displays the pause menu, freezes game time, and updates the boolean state
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