using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("=== UI DO TIMER ===")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float countdownStart = 5f;

    [Header("=== PREFABS DOS PERSONAGENS (IN-GAME) ===")]
    public GameObject greenPlayerPrefab;
    public GameObject purplePlayerPrefab;

    [Header("=== P1 - BOTOES E CANCELAR ===")]
    public Button p1GreenHatButton;
    public Button p1PurpleHatButton;
    public UnityEvent p1CancelAction;

    [Header("=== P2 - BOTOES E CANCELAR ===")]
    public Button p2GreenHatButton;
    public Button p2PurpleHatButton;
    public UnityEvent p2CancelAction;

    [Header("=== BOTAO VOLTAR AO MENU ===")]
    public Button mainBackButton;

    public static InputDevice p1Device;
    public static InputDevice p2Device;
    public static GameObject p1SelectedPrefab;
    public static GameObject p2SelectedPrefab;

    private bool p1Ready = false;
    private bool p2Ready = false;
    private int p1HatIndex = 0;
    private int p2HatIndex = 0;

    private bool timerRunning = false;
    private float currentTimer;
    private bool canAcceptInput = false;

    [SerializeField] private UnityEvent onPlay;

    private void Awake()
    {
        // A MAGIA DO RATO: Liga as funções de rato aos botões automaticamente!
        p1GreenHatButton.onClick.AddListener(() => OnHatClickedFromMouse(1, 0));
        p1PurpleHatButton.onClick.AddListener(() => OnHatClickedFromMouse(1, 1));
        p2GreenHatButton.onClick.AddListener(() => OnHatClickedFromMouse(2, 0));
        p2PurpleHatButton.onClick.AddListener(() => OnHatClickedFromMouse(2, 1));
    }

    private void OnEnable()
    {
        timerText.gameObject.SetActive(false);
        p1Device = null; p2Device = null;
        p1Ready = false; p2Ready = false;
        canAcceptInput = false;

        p1HatIndex = 0;
        p2HatIndex = 0;
        p1SelectedPrefab = greenPlayerPrefab;
        p2SelectedPrefab = greenPlayerPrefab;

        if (p1CancelAction != null) p1CancelAction.Invoke();
        if (p2CancelAction != null) p2CancelAction.Invoke();

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
            }

            currentTimer -= Time.deltaTime;
            timerText.text = Mathf.CeilToInt(currentTimer).ToString();

            if (currentTimer <= 0)
            {
                onPlay.Invoke();
                MainMenu.Instance.PlayGame();
            }
        }
        else
        {
            timerRunning = false;
            timerText.gameObject.SetActive(false);
        }
    }

    private void CheckInputs()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        // --- GAMEPADS ---
        foreach (Gamepad pad in Gamepad.all)
        {
            if (!canAcceptInput) return;

            if (pad.buttonSouth.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == pad) HandleP1JoinOrReady(pad);
                else if (p2Device == null || p2Device == pad) HandleP2JoinOrReady(pad);
            }
            else if (pad.buttonEast.wasPressedThisFrame)
            {
                HandleUnifiedBack();
            }
            else if (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == pad) HandleP1Hat(-1, pad);
                else if (p2Device == null || p2Device == pad) HandleP2Hat(-1, pad);
            }
            else if (pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == pad) HandleP1Hat(1, pad);
                else if (p2Device == null || p2Device == pad) HandleP2Hat(1, pad);
            }
        }

        // --- TECLADO E RATO ---
        if (Keyboard.current != null)
        {
            if (!canAcceptInput) return;

            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == Keyboard.current) HandleP1JoinOrReady(Keyboard.current);
                else if (p2Device == null || p2Device == Keyboard.current) HandleP2JoinOrReady(Keyboard.current);
            }
            // Adicionado o CLIQUE DIREITO DO RATO para Cancelar/Sair
            else if (Keyboard.current.backspaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame ||
                    (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
            {
                HandleUnifiedBack();
            }
            else if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == Keyboard.current) HandleP1Hat(-1, Keyboard.current);
                else if (p2Device == null || p2Device == Keyboard.current) HandleP2Hat(-1, Keyboard.current);
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                if (p1Device == null || p1Device == Keyboard.current) HandleP1Hat(1, Keyboard.current);
                else if (p2Device == null || p2Device == Keyboard.current) HandleP2Hat(1, Keyboard.current);
            }
        }
    }

    // ==========================================
    // FUNÇÃO DEDICADA AO CLIQUE DO RATO
    // ==========================================
    private void OnHatClickedFromMouse(int player, int hatIndex)
    {
        if (!canAcceptInput) return;

        if (player == 1 && !p1Ready)
        {
            if (p1Device == null) p1Device = Keyboard.current; // Assume teclado!
            p1HatIndex = hatIndex;
            p1SelectedPrefab = (hatIndex == 0) ? greenPlayerPrefab : purplePlayerPrefab;
            p1Ready = true;
            UpdateInteractableStates();
            StartCoroutine(InputCooldown());
        }
        else if (player == 2 && !p2Ready)
        {
            if (p2Device == null) p2Device = Keyboard.current; // Assume o mesmo teclado para o P2 poder testar sozinho!
            p2HatIndex = hatIndex;
            p2SelectedPrefab = (hatIndex == 0) ? greenPlayerPrefab : purplePlayerPrefab;
            p2Ready = true;
            UpdateInteractableStates();
            StartCoroutine(InputCooldown());
        }
    }

    // ==========================================
    // FUNÇÕES NATIVAS (Teclado/Comando)
    // ==========================================
    private void HandleUnifiedBack()
    {
        if (p1Ready || p2Ready)
        {
            p1Ready = false;
            p2Ready = false;

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

            p1GreenHatButton.GetComponent<ButtonHover>().ForceDeselect();
            p1PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect();
            p2GreenHatButton.GetComponent<ButtonHover>().ForceDeselect();
            p2PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect();

            if (mainBackButton != null)
            {
                mainBackButton.onClick.Invoke();
            }
            StartCoroutine(InputCooldown());
        }
    }

    private void HandleP1JoinOrReady(InputDevice device)
    {
        if (p1Device == null) p1Device = device;

        if (!p1Ready)
        {
            Button btnToClick = (p1HatIndex == 0) ? p1GreenHatButton : p1PurpleHatButton;
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
        if (p2Device == null) p2Device = device;

        if (!p2Ready)
        {
            Button btnToClick = (p2HatIndex == 0) ? p2GreenHatButton : p2PurpleHatButton;
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
        if (p1Device == null) p1Device = device;

        if (!p1Ready)
        {
            p1HatIndex = Mathf.Clamp(p1HatIndex + direction, 0, 1);
            if (p1HatIndex == 0) { p1SelectedPrefab = greenPlayerPrefab; p1GreenHatButton.GetComponent<ButtonHover>().ForceSelect(); p1PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect(); }
            else { p1SelectedPrefab = purplePlayerPrefab; p1GreenHatButton.GetComponent<ButtonHover>().ForceDeselect(); p1PurpleHatButton.GetComponent<ButtonHover>().ForceSelect(); }
            StartCoroutine(InputCooldown());
        }
    }

    private void HandleP2Hat(int direction, InputDevice device)
    {
        if (p2Device == null) p2Device = device;

        if (!p2Ready)
        {
            p2HatIndex = Mathf.Clamp(p2HatIndex + direction, 0, 1);
            if (p2HatIndex == 0) { p2SelectedPrefab = greenPlayerPrefab; p2GreenHatButton.GetComponent<ButtonHover>().ForceSelect(); p2PurpleHatButton.GetComponent<ButtonHover>().ForceDeselect(); }
            else { p2SelectedPrefab = purplePlayerPrefab; p2GreenHatButton.GetComponent<ButtonHover>().ForceDeselect(); p2PurpleHatButton.GetComponent<ButtonHover>().ForceSelect(); }
            StartCoroutine(InputCooldown());
        }
    }

    private void UpdateInteractableStates()
    {
        p1GreenHatButton.interactable = true; p1PurpleHatButton.interactable = true;
        p2GreenHatButton.interactable = true; p2PurpleHatButton.interactable = true;

        if (p1Ready) { if (p1HatIndex == 0) { p1PurpleHatButton.interactable = false; p2GreenHatButton.interactable = false; } else { p1GreenHatButton.interactable = false; p2PurpleHatButton.interactable = false; } }
        if (p2Ready) { if (p2HatIndex == 0) { p2PurpleHatButton.interactable = false; p1GreenHatButton.interactable = false; } else { p2GreenHatButton.interactable = false; p1PurpleHatButton.interactable = false; } }
    }
}