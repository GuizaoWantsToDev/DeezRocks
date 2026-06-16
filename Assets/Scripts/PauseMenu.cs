using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    [Header("=== MENUS UI ===")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject pauseMENU;
    [SerializeField] private GameObject optionsMenuUI;
    [SerializeField] private GameObject quitMenuUI;

    [Header("=== PRIMEIROS BOTÕES ===")]
    [SerializeField] private GameObject firstPauseButton;
    [SerializeField] private GameObject firstOptionsButton;
    [SerializeField] private GameObject firstQuitButton;

    private enum PauseState { Closed, Main, Options, Quit }
    private PauseState currentState = PauseState.Closed;

    private void Start()
    {
        gameIsPaused = false;
        currentState = PauseState.Closed;
        pauseMenuUI.SetActive(false);
        optionsMenuUI.SetActive(false);
        quitMenuUI.SetActive(false);
    }

    private void Update()
    {
        CheckPauseInputs();
    }

    private void CheckPauseInputs()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                if (currentState == PauseState.Closed) OpenPauseMenu();
                else if (currentState == PauseState.Main) Resume();
                else CloseSubMenu();
                return;
            }
        }

        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad.startButton.wasPressedThisFrame)
            {
                if (currentState == PauseState.Closed) OpenPauseMenu();
                else Resume();
                return;
            }

            if (pad.buttonEast.wasPressedThisFrame)
            {
                if (currentState == PauseState.Main) Resume();
                else if (currentState != PauseState.Closed) CloseSubMenu();
                return;
            }
        }
    }

    public void OpenPauseMenu()
    {
        currentState = PauseState.Main;
        gameIsPaused = true;
        Time.timeScale = 0f;

        pauseMenuUI.SetActive(true);
        optionsMenuUI.SetActive(false);
        quitMenuUI.SetActive(false);

        if (GameManager.Instance != null)
        {
            foreach (GameObject player in GameManager.Instance.playersList)
            {
                if (player != null && player.TryGetComponent<PlayerController>(out var controller))
                    controller.canMove = false;
            }
        }

        StartCoroutine(SelectButtonSafely(firstPauseButton));
    }

    public void Resume()
    {
        currentState = PauseState.Closed;
        gameIsPaused = false;
        Time.timeScale = 1f;

        pauseMenuUI.SetActive(false);
        optionsMenuUI.SetActive(false);
        quitMenuUI.SetActive(false);

        if (GameManager.Instance != null)
        {
            foreach (GameObject player in GameManager.Instance.playersList)
            {
                if (player != null && player.TryGetComponent<PlayerController>(out var controller))
                    controller.canMove = true;
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OpenOptions()
    {
        currentState = PauseState.Options;
        pauseMenuUI.SetActive(false);
        optionsMenuUI.SetActive(true);
        StartCoroutine(SelectButtonSafely(firstOptionsButton));
    }

    public void OpenQuitMenu()
    {
        currentState = PauseState.Quit;
        pauseMenuUI.SetActive(false);
        quitMenuUI.SetActive(true);
        StartCoroutine(SelectButtonSafely(firstQuitButton));
    }

    public void CloseSubMenu()
    {
        currentState = PauseState.Main;
        optionsMenuUI.SetActive(false);
        quitMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        StartCoroutine(SelectButtonSafely(firstPauseButton));
    }

    public void LeaveGame()
    {
        pauseMENU.SetActive(false);
        Time.timeScale = 1f;
        gameIsPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator SelectButtonSafely(GameObject buttonToSelect)
    {
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(null);
        if (buttonToSelect != null)
        {
            EventSystem.current.SetSelectedGameObject(buttonToSelect);
        }
    }
}