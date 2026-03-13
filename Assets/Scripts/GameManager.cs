using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

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

    public void AddPlayer(GameObject player)
    {
        players.Add(player);
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