using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
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

    private IEnumerator ReloadGame()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Prototype 2");
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

        if (playersList.Count <= 1 && gameObject.activeInHierarchy)
        {
            StartCoroutine(ReloadGame());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!gameObject.activeInHierarchy) return;

        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>().Die();
        }
        Destroy(other.gameObject);
    }
}