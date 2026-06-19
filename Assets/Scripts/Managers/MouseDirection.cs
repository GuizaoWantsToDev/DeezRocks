using UnityEngine;
using UnityEngine.InputSystem;

public class MouseDirection : MonoBehaviour
{
    public static MouseDirection Instance { get; private set; }

    public Vector2 direction;
    private Vector2 mousePosition;
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

    private void Update()
    {
        if (player == null)
        {
            return;
        }
        else
        {
            GetMouseDirection();
        }
    }

    private void GetMouseDirection()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        direction = (mousePosition - (Vector2)player.transform.position).normalized;
    }
}