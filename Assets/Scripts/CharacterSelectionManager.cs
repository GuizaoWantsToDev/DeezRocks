using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class CharacterSelectionManager : MonoBehaviour
{
    public static CharacterSelectionManager Instance { get; private set; }

    [Header("Colors")]
    public Color defaultColor = Color.white;
    public Color greenColor = new Color(0.2f, 0.8f, 0.2f);
    public Color purpleColor = new Color(0.7f, 0.2f, 0.8f);
    public static Color p1Color = Color.white;
    public static Color p2Color = Color.white;

    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float countdownStart = 5f;

    [Header("Player Prefabs")]
    public GameObject greenPlayerPrefab;
    public GameObject purplePlayerPrefab;

    [Header("Player 1 Settings")]
    public Button p1GreenHatButton;
    public Button p1PurpleHatButton;
    public TMP_InputField inputNamePlayer1;
    public UnityEvent p1CancelAction;

    [Header("Player 2 Settings")]
    public Button p2GreenHatButton;
    public Button p2PurpleHatButton;
    public TMP_InputField inputNamePlayer2;
    public UnityEvent p2CancelAction;

    [Header("Menu Settings")]
    public Button mainBackButton;

    public static InputDevice p1Device;
    public static InputDevice p2Device;
    public static GameObject p1SelectedPrefab;
    public static GameObject p2SelectedPrefab;

    public static string customNamePlayer1 = "";
    public static string customNamePlayer2 = "";

    private bool p1Ready = false;
    private bool p2Ready = false;
    private int p1HatIndex = 0;
    private int p2HatIndex = 0;

    private bool timerRunning = false;
    private float currentTimer;
    private bool canAcceptInput = false;

    [SerializeField] public UnityEvent onPlay;

    private void Awake()
    {
        if (CharacterSelectionManager.Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        p1GreenHatButton.onClick.AddListener(() => OnHatClickedFromMouse(1, 0));
        p1PurpleHatButton.onClick.AddListener(() => OnHatClickedFromMouse(1, 1));
        p2GreenHatButton.onClick.AddListener(() => OnHatClickedFromMouse(2, 0));
        p2PurpleHatButton.onClick.AddListener(() => OnHatClickedFromMouse(2, 1));

        inputNamePlayer1.onEndEdit.AddListener(SavePlayer1Name);
        inputNamePlayer2.onEndEdit.AddListener(SavePlayer2Name);
    }

    private void OnEnable()
    {
        timerText.gameObject.SetActive(false);
        p1Device = null;
        p2Device = null;
        p1Ready = false;
        p2Ready = false;
        canAcceptInput = false;

        p1HatIndex = 0;
        p2HatIndex = 0;
        p1SelectedPrefab = greenPlayerPrefab;
        p2SelectedPrefab = greenPlayerPrefab;

        p1Color = defaultColor;
        p2Color = defaultColor;
        customNamePlayer1 = "";
        customNamePlayer2 = "";

        inputNamePlayer1.text = "";
        inputNamePlayer2.text = "";
        inputNamePlayer1.interactable = true;
        inputNamePlayer2.interactable = true;

        ChangeInputFieldColor(inputNamePlayer1, p1Color);
        ChangeInputFieldColor(inputNamePlayer2, p2Color);

        if (p1CancelAction != null)
        {
            p1CancelAction.Invoke();
        }

        if (p2CancelAction != null)
        {
            p2CancelAction.Invoke();
        }

        DisableButtonNavigation(p1GreenHatButton);
        DisableButtonNavigation(p1PurpleHatButton);
        DisableButtonNavigation(p2GreenHatButton);
        DisableButtonNavigation(p2PurpleHatButton);

        p1GreenHatButton.GetComponent<ButtonHover>().ForceSelect();
        p1PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect();
        p2GreenHatButton.GetComponent<ButtonHover>().ForceSelect();
        p2PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect();

        UpdateInteractableStates();
        StartCoroutine(InputCooldown());
    }

    private void ChangeInputFieldColor(TMP_InputField inputField, Color newColor)
    {
        if (inputField != null)
        {
            if (inputField.textComponent != null)
            {
                inputField.textComponent.color = newColor;
            }

            if (inputField.placeholder != null)
            {
                inputField.placeholder.color = newColor;
            }
        }
    }

    private void SavePlayer1Name(string input)
    {
        customNamePlayer1 = input;
    }

    private void SavePlayer2Name(string input)
    {
        customNamePlayer2 = input;
    }

    private IEnumerator InputCooldown()
    {
        canAcceptInput = false;
        yield return new WaitForSeconds(0.15f);
        canAcceptInput = true;
    }

    private void DisableButtonNavigation(Button btn)
    {
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
    }

    private void Update()
    {
        if (inputNamePlayer1.isFocused || inputNamePlayer2.isFocused)
        {
            return;
        }

        if (canAcceptInput)
        {
            CheckInputs();
        }

        if (p1Ready && p2Ready)
        {
            if (!timerRunning)
            {
                timerRunning = true;
                currentTimer = countdownStart;
                timerText.gameObject.SetActive(true);

                inputNamePlayer1.interactable = false;
                inputNamePlayer2.interactable = false;

                if (string.IsNullOrWhiteSpace(inputNamePlayer1.text))
                {
                    inputNamePlayer1.text = "PLAYER 1";
                    customNamePlayer1 = "PLAYER 1";
                }

                if (string.IsNullOrWhiteSpace(inputNamePlayer2.text))
                {
                    inputNamePlayer2.text = "PLAYER 2";
                    customNamePlayer2 = "PLAYER 2";
                }
            }

            currentTimer -= Time.deltaTime;
            timerText.text = Mathf.CeilToInt(currentTimer).ToString();

            if (currentTimer <= 0)
            {
                MainMenu.Instance.PlayGame();
            }
        }
        else
        {
            if (timerRunning)
            {
                timerRunning = false;
                timerText.gameObject.SetActive(false);

                inputNamePlayer1.interactable = true;
                inputNamePlayer2.interactable = true;
            }
        }
    }

    private void CheckInputs()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        foreach (Gamepad pad in Gamepad.all)
        {
            if (!canAcceptInput)
            {
                return;
            }

            if (pad.buttonSouth.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == pad)
                {
                    HandleP1JoinOrReady(pad);
                }
                else if (p2Device == null || p2Device == pad)
                {
                    HandleP2JoinOrReady(pad);
                }
            }
            else if (pad.buttonEast.wasPressedThisFrame)
            {
                HandleUnifiedBack();
            }
            else if (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == pad)
                {
                    HandleP1Hat(-1, pad);
                }
                else if (p2Device == null || p2Device == pad)
                {
                    HandleP2Hat(-1, pad);
                }
            }
            else if (pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == pad)
                {
                    HandleP1Hat(1, pad);
                }
                else if (p2Device == null || p2Device == pad)
                {
                    HandleP2Hat(1, pad);
                }
            }
        }

        if (Keyboard.current != null)
        {
            if (!canAcceptInput)
            {
                return;
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == Keyboard.current)
                {
                    HandleP1JoinOrReady(Keyboard.current);
                }
                else if (p2Device == null || p2Device == Keyboard.current)
                {
                    HandleP2JoinOrReady(Keyboard.current);
                }
            }
            else if (Keyboard.current.backspaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
            {
                HandleUnifiedBack();
            }
            else if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == Keyboard.current)
                {
                    HandleP1Hat(-1, Keyboard.current);
                }
                else if (p2Device == null || p2Device == Keyboard.current)
                {
                    HandleP2Hat(-1, Keyboard.current);
                }
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == Keyboard.current)
                {
                    HandleP1Hat(1, Keyboard.current);
                }
                else if (p2Device == null || p2Device == Keyboard.current)
                {
                    HandleP2Hat(1, Keyboard.current);
                }
            }
        }
    }

    private void OnHatClickedFromMouse(int player, int hatIndex)
    {
        if (!canAcceptInput)
        {
            return;
        }

        if (player == 1 && !p1Ready)
        {
            if (p1Device == null)
            {
                p1Device = Keyboard.current;
            }

            p1HatIndex = hatIndex;

            if (hatIndex == 0)
            {
                p1SelectedPrefab = greenPlayerPrefab;
                p1Color = greenColor;
            }
            else
            {
                p1SelectedPrefab = purplePlayerPrefab;
                p1Color = purpleColor;
            }

            ChangeInputFieldColor(inputNamePlayer1, p1Color);

            p1Ready = true;
            UpdateInteractableStates();
            StartCoroutine(InputCooldown());
        }
        else if (player == 2 && !p2Ready)
        {
            if (p2Device == null)
            {
                p2Device = Keyboard.current;
            }

            p2HatIndex = hatIndex;

            if (hatIndex == 0)
            {
                p2SelectedPrefab = greenPlayerPrefab;
                p2Color = greenColor;
            }
            else
            {
                p2SelectedPrefab = purplePlayerPrefab;
                p2Color = purpleColor;
            }

            ChangeInputFieldColor(inputNamePlayer2, p2Color);

            p2Ready = true;
            UpdateInteractableStates();
            StartCoroutine(InputCooldown());
        }
    }

    private void HandleUnifiedBack()
    {
        if (p1Ready || p2Ready)
        {
            p1Ready = false;
            p2Ready = false;

            p1Color = defaultColor;
            p2Color = defaultColor;

            inputNamePlayer1.text = "";
            inputNamePlayer2.text = "";
            customNamePlayer1 = "";
            customNamePlayer2 = "";

            ChangeInputFieldColor(inputNamePlayer1, p1Color);
            ChangeInputFieldColor(inputNamePlayer2, p2Color);

            p1CancelAction.Invoke();
            p2CancelAction.Invoke();

            UpdateInteractableStates();
            StartCoroutine(InputCooldown());
        }
        else
        {
            p1Device = null;
            p2Device = null;
            p1HatIndex = 0;
            p2HatIndex = 0;

            p1Color = defaultColor;
            p2Color = defaultColor;

            ChangeInputFieldColor(inputNamePlayer1, p1Color);
            ChangeInputFieldColor(inputNamePlayer2, p2Color);

            p1GreenHatButton.GetComponent<ButtonHover>().ForceDeselect();
            p1PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect();
            p2GreenHatButton.GetComponent<ButtonHover>().ForceDeselect();
            p2PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect();

            if (mainBackButton != null)
            {
                mainBackButton.onClick.Invoke();
            }

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(InputCooldown());
            }
        }
    }

    private void HandleP1JoinOrReady(InputDevice device)
    {
        if (p1Device == null)
        {
            p1Device = device;
        }

        if (!p1Ready)
        {
            Button btnToClick;

            if (p1HatIndex == 0)
            {
                btnToClick = p1GreenHatButton;
            }
            else
            {
                btnToClick = p1PurpleHatButton;
            }

            if (btnToClick.interactable)
            {
                p1Ready = true;
                btnToClick.onClick.Invoke();
                UpdateInteractableStates();
                StartCoroutine(InputCooldown());
            }
        }
    }

    private void HandleP2JoinOrReady(InputDevice device)
    {
        if (p2Device == null)
        {
            p2Device = device;
        }

        if (!p2Ready)
        {
            Button btnToClick;

            if (p2HatIndex == 0)
            {
                btnToClick = p2GreenHatButton;
            }
            else
            {
                btnToClick = p2PurpleHatButton;
            }

            if (btnToClick.interactable)
            {
                p2Ready = true;
                btnToClick.onClick.Invoke();
                UpdateInteractableStates();
                StartCoroutine(InputCooldown());
            }
        }
    }

    private void HandleP1Hat(int direction, InputDevice device)
    {
        if (p1Device == null)
        {
            p1Device = device;
        }

        if (!p1Ready)
        {
            p1HatIndex = Mathf.Clamp(p1HatIndex + direction, 0, 1);

            if (p1HatIndex == 0)
            {
                p1SelectedPrefab = greenPlayerPrefab;
                p1Color = greenColor;
                p1GreenHatButton.GetComponent<ButtonHover>().ForceSelect();
                p1PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect();
            }
            else
            {
                p1SelectedPrefab = purplePlayerPrefab;
                p1Color = purpleColor;
                p1GreenHatButton.GetComponent<ButtonHover>().ForceDeselect();
                p1PurpleHatButton.GetComponent<ButtonHover>().ForceSelect();
            }

            ChangeInputFieldColor(inputNamePlayer1, p1Color);
            StartCoroutine(InputCooldown());
        }
    }

    private void HandleP2Hat(int direction, InputDevice device)
    {
        if (p2Device == null)
        {
            p2Device = device;
        }

        if (!p2Ready)
        {
            p2HatIndex = Mathf.Clamp(p2HatIndex + direction, 0, 1);

            if (p2HatIndex == 0)
            {
                p2SelectedPrefab = greenPlayerPrefab;
                p2Color = greenColor;
                p2GreenHatButton.GetComponent<ButtonHover>().ForceSelect();
                p2PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect();
            }
            else
            {
                p2SelectedPrefab = purplePlayerPrefab;
                p2Color = purpleColor;
                p2GreenHatButton.GetComponent<ButtonHover>().ForceDeselect();
                p2PurpleHatButton.GetComponent<ButtonHover>().ForceSelect();
            }

            ChangeInputFieldColor(inputNamePlayer2, p2Color);
            StartCoroutine(InputCooldown());
        }
    }

    private void UpdateInteractableStates()
    {
        p1GreenHatButton.interactable = true;
        p1PurpleHatButton.interactable = true;
        p2GreenHatButton.interactable = true;
        p2PurpleHatButton.interactable = true;

        if (p1Ready)
        {
            if (p1HatIndex == 0)
            {
                p1PurpleHatButton.interactable = false;
                p2GreenHatButton.interactable = false;
            }
            else
            {
                p1GreenHatButton.interactable = false;
                p2PurpleHatButton.interactable = false;
            }
        }

        if (p2Ready)
        {
            if (p2HatIndex == 0)
            {
                p2PurpleHatButton.interactable = false;
                p1GreenHatButton.interactable = false;
            }
            else
            {
                p2GreenHatButton.interactable = false;
                p1PurpleHatButton.interactable = false;
            }
        }
    }
}