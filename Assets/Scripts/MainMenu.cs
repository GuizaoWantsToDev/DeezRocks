using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [Header("--- MENUS ---")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject tutorialMenu;
    [SerializeField] private GameObject quitMenu;

    [Header("--- 1ST BUTTONS ---")]
    [SerializeField] private GameObject firstMainMenuButton;
    [SerializeField] private GameObject firstPlayMenuButton;
    [SerializeField] private GameObject firstOptionsMenuButton;
    [SerializeField] private GameObject firstQuitMenuButton;

    [Header("--- 1ST BUTTONS IN GAME ---")]
    [SerializeField] private GameObject firstPauseMenuButton;
    [SerializeField] private GameObject firstOptionsMenuIGButton;
    [SerializeField] private GameObject firstQuitMenuIGButton;

    private GameObject currentFirstButton;

    private enum InputMode { Mouse, KeyboardOrGamepad }
    private InputMode currentInputMode = InputMode.KeyboardOrGamepad;

    // A nossa trava para impedir o spam do analógico
    private bool requireStickReset = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
        // Variáveis para sabermos o estado dos analógicos de forma fácil
        float leftStickMag = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue().magnitude : 0f;
        float rightStickMag = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue().magnitude : 0f;

        bool isStickPushed = leftStickMag >= 0.5f || rightStickMag >= 0.5f;
        bool isStickCentered = leftStickMag < 0.2f && rightStickMag < 0.2f;

        // 1. RATO: Passa para rato se mexeres o rato ou clicares
        bool mouseIsMoving = Mouse.current != null && Mouse.current.delta.ReadValue().magnitude > 0.1f; // Podes aumentar isto se a secretária tremer muito
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if ((mouseIsMoving || mouseClicked) && currentInputMode != InputMode.Mouse)
        {
            SwitchToMouseMode();

            // Se mudou para o rato mas o analógico ainda estava empurrado, ativa a trava!
            if (isStickPushed)
            {
                requireStickReset = true;
            }
        }

        // 2. COMANDO: Lê as ações do comando
        if (Gamepad.current != null)
        {
            // Se o analógico voltou ao centro (largaste o dedo), tira a trava
            if (isStickCentered)
            {
                requireStickReset = false;
            }

            bool gamepadIsActive =
                (isStickPushed && !requireStickReset) || // <-- Só aceita o analógico se NÃO estiver trancado
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

        // 3. TECLADO: Passa para teclado se carregares nalguma tecla
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

        // Limpa a seleção para o EventSystem não chatear o hover do rato
        EventSystem.current.SetSelectedGameObject(null);
        Cursor.visible = true;
    }

    private void SwitchToKeyboardGamepadMode()
    {
        currentInputMode = InputMode.KeyboardOrGamepad;

        // Seleciona o botão para o analógico/dpad conseguir navegar logo
        if (EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(currentFirstButton);

        Cursor.visible = false;
    }


    #region First Button Setters

    public void ChangeFirstToMainMenu()
    {
        currentFirstButton = firstMainMenuButton;
        if (currentInputMode == InputMode.KeyboardOrGamepad)
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
    }

    public void ChangeFirstToPlayMenu()
    {
        currentFirstButton = firstPlayMenuButton;
        if (currentInputMode == InputMode.KeyboardOrGamepad)
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
    }

    public void ChangeFirstToOptionMenu()
    {
        currentFirstButton = firstOptionsMenuButton;
        if (currentInputMode == InputMode.KeyboardOrGamepad)
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
    }

    public void ChangeFirstToQuitMenu()
    {
        currentFirstButton = firstQuitMenuButton;
        if (currentInputMode == InputMode.KeyboardOrGamepad)
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
    }

    public void ChangeFirstToPauseMenu()
    {
        currentFirstButton = firstPauseMenuButton;
        if (currentInputMode == InputMode.KeyboardOrGamepad)
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
    }

    public void ChangeFirstToOptionsMenuIG()
    {
        currentFirstButton = firstOptionsMenuIGButton;
        if (currentInputMode == InputMode.KeyboardOrGamepad)
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
    }

    public void ChangeFirstToQuitMenuIG()
    {
        currentFirstButton = firstQuitMenuIGButton;
        if (currentInputMode == InputMode.KeyboardOrGamepad)
            EventSystem.current.SetSelectedGameObject(currentFirstButton);
    }

    #endregion

    public void PlayGame()
    {
        Loader.Load(Loader.Scene.Prototype2);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("QUIT!");

#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#endif
    }
}