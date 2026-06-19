using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [Header("Menus")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject tutorialMenu;
    [SerializeField] private GameObject quitMenu;

    [Header("First Buttons")]
    [SerializeField] private GameObject firstMainMenuButton;
    [SerializeField] private GameObject firstPlayMenuButton;
    [SerializeField] private GameObject firstOptionsMenuButton;
    [SerializeField] private GameObject firstQuitMenuButton;

    [Header("First Buttons In Game")]
    [SerializeField] private GameObject firstPauseMenuButton;
    [SerializeField] private GameObject firstOptionsMenuIGButton;
    [SerializeField] private GameObject firstQuitMenuIGButton;

    private GameObject currentFirstButton;

    private enum InputMode { Mouse, KeyboardOrGamepad }
    private InputMode currentInputMode = InputMode.KeyboardOrGamepad;

    private bool requireStickReset = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(StartMenuSafely());
    }

    private IEnumerator StartMenuSafely()
    {
        yield return new WaitForEndOfFrame();
        ChangeFirstToMainMenu();
        Cursor.visible = false;
    }

    private void Update()
    {
        float leftStickMag = 0f;
        float rightStickMag = 0f;

        if (Gamepad.current != null)
        {
            leftStickMag = Gamepad.current.leftStick.ReadValue().magnitude;
            rightStickMag = Gamepad.current.rightStick.ReadValue().magnitude;
        }

        bool isStickPushed = leftStickMag >= 0.5f || rightStickMag >= 0.5f;
        bool isStickCentered = leftStickMag < 0.2f && rightStickMag < 0.2f;

        bool mouseIsMoving = false;
        bool mouseClicked = false;

        if (Mouse.current != null)
        {
            mouseIsMoving = Mouse.current.delta.ReadValue().magnitude > 0.1f;
            mouseClicked = Mouse.current.leftButton.wasPressedThisFrame;
        }

        if ((mouseIsMoving || mouseClicked) && currentInputMode != InputMode.Mouse)
        {
            SwitchToMouseMode();

            if (isStickPushed)
            {
                requireStickReset = true;
            }
        }

        if (Gamepad.current != null)
        {
            if (isStickCentered)
            {
                requireStickReset = false;
            }

            bool gamepadIsActive = (isStickPushed && !requireStickReset) ||
                                   Gamepad.current.dpad.ReadValue().magnitude > 0.1f ||
                                   Gamepad.current.buttonSouth.wasPressedThisFrame ||
                                   Gamepad.current.buttonEast.wasPressedThisFrame ||
                                   Gamepad.current.buttonWest.wasPressedThisFrame ||
                                   Gamepad.current.buttonNorth.wasPressedThisFrame;

            if (gamepadIsActive && currentInputMode != InputMode.KeyboardOrGamepad)
            {
                SwitchToKeyboardGamepadMode();
            }
        }

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            if (currentInputMode != InputMode.KeyboardOrGamepad)
            {
                SwitchToKeyboardGamepadMode();
            }
        }
    }

    private void SwitchToMouseMode()
    {
        currentInputMode = InputMode.Mouse;
        EventSystem.current.SetSelectedGameObject(null);
        Cursor.visible = true;
    }

    private void SwitchToKeyboardGamepadMode()
    {
        currentInputMode = InputMode.KeyboardOrGamepad;

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
        }

        Cursor.visible = false;
    }

    public void ChangeFirstToMainMenu()
    {
        currentFirstButton = firstMainMenuButton;

        if (currentInputMode == InputMode.KeyboardOrGamepad)
        {
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
        }
    }

    public void ChangeFirstToPlayMenu()
    {
        currentFirstButton = firstPlayMenuButton;

        if (currentInputMode == InputMode.KeyboardOrGamepad)
        {
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
        }
    }

    public void ChangeFirstToOptionMenu()
    {
        currentFirstButton = firstOptionsMenuButton;

        if (currentInputMode == InputMode.KeyboardOrGamepad)
        {
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
        }
    }

    public void ChangeFirstToQuitMenu()
    {
        currentFirstButton = firstQuitMenuButton;

        if (currentInputMode == InputMode.KeyboardOrGamepad)
        {
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
        }
    }

    public void ChangeFirstToPauseMenu()
    {
        currentFirstButton = firstPauseMenuButton;

        if (currentInputMode == InputMode.KeyboardOrGamepad)
        {
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
        }
    }

    public void ChangeFirstToOptionsMenuIG()
    {
        currentFirstButton = firstOptionsMenuIGButton;

        if (currentInputMode == InputMode.KeyboardOrGamepad)
        {
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
        }
    }

    public void ChangeFirstToQuitMenuIG()
    {
        currentFirstButton = firstQuitMenuIGButton;

        if (currentInputMode == InputMode.KeyboardOrGamepad)
        {
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
        }
    }

    public void PlayGame()
    {
        Loader.Load(Loader.Scene.Prototype2);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#endif
    }
}