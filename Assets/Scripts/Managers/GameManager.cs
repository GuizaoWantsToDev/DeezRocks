using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } = null;

    [Header("--- PLAYERS ---")]
    public GameObject[] spawnablePlayers;
    public List<GameObject> playersList = new();
    private int maxPlayers = 2;

    private static bool alreadyPlayed = false;

    [SerializeField] private CinemachineTargetGroup targetGroup;

    [SerializeField] private InputManager inputManager;

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

        if (alreadyPlayed)
        {
            inputManager.SpawnMNK();
            inputManager.SpawnController();
        }
    }

    public void AddPlayer(GameObject player)
    {
        if (playersList.Count < maxPlayers)
        {
            playersList.Add(player);
            targetGroup.AddMember(player.transform, 1, 1);
        }
        else
            Destroy(player);
    }

    public void RemovePlayer(GameObject player)
    {
        playersList.Remove(player);

        if (playersList.Count <= 1)
        {
            StartCoroutine(ReloadGame());
        }
    }
    private IEnumerator ReloadGame()
    {
        yield return new WaitForSeconds(1f);
        alreadyPlayed = true;
        SceneManager.LoadScene("Prototype 2");
    }

    

    private void OnTriggerEnter2D(Collider2D other)
    {
      if(other.TryGetComponent<PlayerHealth>(out PlayerHealth player))
            player.Die();
      
      other.gameObject.SetActive(false);
    }
}