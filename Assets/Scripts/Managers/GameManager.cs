using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } = null;

    [Header("--- SPAWN POINTS ---")]
    public Transform p1SpawnPoint;
    public Transform p2SpawnPoint;

    [Header("--- UI PLAYER 1 (ESQUERDA) ---")]
    public TextMeshProUGUI textNamePlayer1;
    public Image healthBarPlayer1;
    public Image energyBarPlayer1;
    public Animator heartAnimatorPlayer1;

    [Header("--- UI PLAYER 2 (DIREITA) ---")]
    public TextMeshProUGUI textNamePlayer2;
    public Image healthBarPlayer2;
    public Image energyBarPlayer2;
    public Animator heartAnimatorPlayer2;

    [Header("--- PLAYERS ---")]
    public List<GameObject> playersList = new();
    public GameObject instantiatedPlayer1;
    public GameObject instantiatedPlayer2;

    [SerializeField] private CinemachineTargetGroup targetGroup;
    private static bool alreadyPlayed = false;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        isGameOver = false;
        playersList.Clear();
        SpawnPlayersAutomatic();
    }

    private void SpawnPlayersAutomatic()
    {
        if (CharacterSelectionManager.p1Device != null)
        {
            string scheme1 = CharacterSelectionManager.p1Device is Keyboard ? "Keyboard" : "Controller";

            PlayerInput p1Input = PlayerInput.Instantiate(
                CharacterSelectionManager.p1SelectedPrefab,
                controlScheme: scheme1,
                pairWithDevice: CharacterSelectionManager.p1Device
            );

            instantiatedPlayer1 = p1Input.gameObject;
            instantiatedPlayer1.transform.position = p1SpawnPoint.position;

            // Atribuir Nome e COR ao Player 1
            textNamePlayer1.text = CharacterSelectionManager.customNamePlayer1;
            textNamePlayer1.color = CharacterSelectionManager.p1Color;

            PlayerHealth health1 = instantiatedPlayer1.GetComponent<PlayerHealth>();
            health1.screenSpaceHealthBar = healthBarPlayer1;

            PlayerEnergy energy1 = instantiatedPlayer1.GetComponent<PlayerEnergy>();
            energy1.energyBar = energyBarPlayer1;

            AddPlayer(instantiatedPlayer1);
        }

        if (CharacterSelectionManager.p2Device != null)
        {
            string scheme2 = CharacterSelectionManager.p2Device is Keyboard ? "Keyboard" : "Controller";

            PlayerInput p2Input = PlayerInput.Instantiate(
                CharacterSelectionManager.p2SelectedPrefab,
                controlScheme: scheme2,
                pairWithDevice: CharacterSelectionManager.p2Device
            );

            instantiatedPlayer2 = p2Input.gameObject;
            instantiatedPlayer2.transform.position = p2SpawnPoint.position;

            // Atribuir Nome e COR ao Player 2
            textNamePlayer2.text = CharacterSelectionManager.customNamePlayer2;
            textNamePlayer2.color = CharacterSelectionManager.p2Color;

            PlayerHealth health2 = instantiatedPlayer2.GetComponent<PlayerHealth>();
            health2.screenSpaceHealthBar = healthBarPlayer2;

            PlayerEnergy energy2 = instantiatedPlayer2.GetComponent<PlayerEnergy>();
            energy2.energyBar = energyBarPlayer2;

            AddPlayer(instantiatedPlayer2);
        }
    }
    public void AddPlayer(GameObject player)
    {
        if (playersList.Contains(player)) return;

        playersList.Add(player);
        targetGroup.AddMember(player.transform, 1, 1);
    }

    public void HandlePlayerDeath(GameObject deadPlayer)
    {
        if (isGameOver) return;
        isGameOver = true;

        playersList.Remove(deadPlayer);
        targetGroup.RemoveMember(deadPlayer.transform);

        if (deadPlayer == instantiatedPlayer1)
        {
            heartAnimatorPlayer1.SetTrigger("BreakHeart");
        }
        else if (deadPlayer == instantiatedPlayer2)
        {
            heartAnimatorPlayer2.SetTrigger("BreakHeart");
        }

        StartCoroutine(ReloadGameAfterDeath());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && playersList.Contains(collision.gameObject))
        {
            collision.gameObject.GetComponent<PlayerHealth>().Die();
        }
        else
        {
            collision.gameObject.SetActive(false);
        }
    }

    private IEnumerator ReloadGameAfterDeath()
    {
        // Espera tempo suficiente para a animação do coração acabar + 1 segundo extra
        yield return new WaitForSeconds(2.5f);
        alreadyPlayed = true;
        Loader.Load(Loader.Scene.Prototype2);
    }
}