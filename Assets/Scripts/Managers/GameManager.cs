using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } = null;

    [Header("--- SPAWN POINTS ---")]
    public Transform p1SpawnPoint;
    public Transform p2SpawnPoint; 

    [Header("--- PLAYERS ---")]
    public List<GameObject> playersList = new();

    [SerializeField] private CinemachineTargetGroup targetGroup;

    private static bool alreadyPlayed = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playersList.Clear();
        SpawnPlayersAutomatic();
    }

    private void SpawnPlayersAutomatic()
    {
        // SPAWN DO PLAYER 1
        if (CharacterSelectionManager.p1Device != null)
        {
            string scheme1 = CharacterSelectionManager.p1Device is Keyboard ? "Keyboard" : "Controller";

            // O Unity spawna o prefab selecionado e tranca-o ao comando do P1!
            PlayerInput p1Input = PlayerInput.Instantiate(
                CharacterSelectionManager.p1SelectedPrefab,
                controlScheme: scheme1,
                pairWithDevice: CharacterSelectionManager.p1Device
            );

            // Coloca o jogador no lado esquerdo do mapa
            p1Input.transform.position = p1SpawnPoint.position;
            AddPlayer(p1Input.gameObject);
        }

        // SPAWN DO PLAYER 2
        if (CharacterSelectionManager.p2Device != null)
        {
            string scheme2 = CharacterSelectionManager.p2Device is Keyboard ? "Keyboard" : "Controller";

            PlayerInput p2Input = PlayerInput.Instantiate(
                CharacterSelectionManager.p2SelectedPrefab,
                controlScheme: scheme2,
                pairWithDevice: CharacterSelectionManager.p2Device
            );

            // Coloca o jogador no lado direito do mapa
            p2Input.transform.position = p2SpawnPoint.position;
            AddPlayer(p2Input.gameObject);
        }
    }

    public void AddPlayer(GameObject player)
    {
        if (playersList.Contains(player))
            return;

        playersList.Add(player);
        targetGroup.AddMember(player.transform, 1, 1);
    }

    public void RemovePlayer(GameObject player)
    {
        playersList.Remove(player);
        targetGroup.RemoveMember(player.transform);
        if (playersList.Count <= 1)
        {
            StartCoroutine(ReloadGame());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(playersList.Contains(collision.gameObject))
            {
                RemovePlayer(collision.gameObject);
                return;
            }
        }
        else
        {
            collision.gameObject.SetActive(false);
        }

    }

    private IEnumerator ReloadGame()
    {
        yield return new WaitForSeconds(1f);
        alreadyPlayed = true;
        // O Reload da cena vai chamar o Start() de novo, 
        // e eles vão spawnar outra vez nos seus lugares perfeitamente!
        SceneManager.LoadScene("Prototype 2");
    }
}