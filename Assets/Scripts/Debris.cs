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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
            EnergyManager.Instance.RefiillEnergy(energyAmount);
        }
        if (collision.gameObject.CompareTag("Bounds"))
        {
            Destroy(gameObject);
        }
    }
}