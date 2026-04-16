using UnityEngine;
using UnityEngine.InputSystem;

public class MouseDirection : MonoBehaviour
{
    public static MouseDirection Instance { get; private set; } = null;

    private Vector2 mousePosition = new();
    public Vector2 direction = new();
    private GameObject player;
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

        player = GameObject.FindWithTag("Player");
    }
    private void GetMouseDirection()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        direction = (mousePosition - (Vector2)player.transform.position).normalized;
    }
    private void Update()
    {
        if (player == null)
            return;
        else
            GetMouseDirection();
    }
}