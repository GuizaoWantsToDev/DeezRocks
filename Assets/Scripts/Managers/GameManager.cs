using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } = null;

    [Header("Player OutOffBounds")]
    [SerializeField]
    private BoxCollider2D mapLimit;

    public GameObject[] spawnablePlayers;

    public List<GameObject> playersList = new List<GameObject>();
    private int maxPlayers = 2;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private CinemachineTargetGroup targetGroup;

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

    public void OnRestart(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene("Prototype 2");
    }
    IEnumerator ReloadGame()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Prototype 2");
    }

    public void AddPlayer(GameObject player)
    {
        if(playersList.Count <= maxPlayers)
        {
            playersList.Add(player);
            targetGroup.AddMember(player.transform,1,1);
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

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //playerController.Die();
        }
        Destroy(other.gameObject);
    }  
}