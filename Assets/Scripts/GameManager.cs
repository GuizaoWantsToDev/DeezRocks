using System.Collections;
using System.Collections.Generic;
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

    [SerializeField]
    public List<GameObject> players = new List<GameObject>();

    [SerializeField]
    private PlayerController playerController;

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

    private void Update()
    {
        if(players.Count <= 1)
        {
            StartCoroutine(ReloadGame());
        }
    }

    public void OnRestart(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene("Prototype");
    }
    IEnumerator ReloadGame()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("Prototype");
    }

    public void AddPlayer(GameObject player)
    {
        players.Add(player);
    }
    public void RemovePlayer(GameObject player)
    {
        players.Remove(player);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerController.Die();
        }
        Destroy(other.gameObject);
    }  
}