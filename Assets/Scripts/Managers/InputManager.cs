using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private bool keyboardJoined;
    private List<Gamepad> controllersJoined = new();
    private PlayerInputManager inputManager;

    private void Start()
    {
        inputManager = GetComponent<PlayerInputManager>();
    }

    private void Update()
    {
        if (inputManager.playerCount >= inputManager.maxPlayerCount) return;

        if (!keyboardJoined && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlayerInput.Instantiate(GameManager.Instance.spawnablePlayers[inputManager.playerCount], controlScheme: "Keyboard", pairWithDevice: Keyboard.current);
            keyboardJoined = true;

            if (inputManager.playerCount >= inputManager.maxPlayerCount) return;
        }

        foreach (var controller in Gamepad.all)
        {
            if (!controllersJoined.Contains(controller) && controller.buttonSouth.wasPressedThisFrame)
            {
                PlayerInput.Instantiate(GameManager.Instance.spawnablePlayers[inputManager.playerCount], controlScheme: "Controller", pairWithDevice: controller);
                controllersJoined.Add(controller);

                if (inputManager.playerCount >= inputManager.maxPlayerCount) return;
            }
        }
    }
}