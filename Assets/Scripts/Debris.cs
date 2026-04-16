using UnityEngine;

public class Debris : MonoBehaviour
{
    [Header("Little jump when the debris spawn.")]
    [SerializeField] private float debrisSpawnBoost = 3f;

    [SerializeField] private float energyAmount = 2f;

    private Rigidbody2D debrisRigidBody2D;

    void Start()
    {
        debrisRigidBody2D = GetComponent<Rigidbody2D>();
        debrisRigidBody2D.AddForce(Vector2.up * debrisSpawnBoost, ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerEnergy playerEnergy = collision.GetComponent<PlayerEnergy>();

            if (playerEnergy != null)
                playerEnergy.RefillEnergy(energyAmount);

            Destroy(gameObject);
        }
    }
}