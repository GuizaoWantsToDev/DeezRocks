using System.Collections;
using UnityEngine;

public class Debris : MonoBehaviour, IPooledObject
{
    [Header("--- SPAWN_BOOST ---")]
    [SerializeField] private float debrisSpawnBoost;

    [Header("--- SIZE ---")]
    [SerializeField] private float maxDebrisSize;
    [SerializeField] private float minDebrisSize;

    [Header("--- ENERGY ---")]
    [SerializeField] private float baseEnergyAmount;
    private float finalEnergy;

    private float delay = 1f;
    private Rigidbody2D debrisRigidBody2D;
    private PolygonCollider2D debrisPollygonCollider2D;

    public void OnObjectSpawn()
    {
        debrisRigidBody2D = GetComponent<Rigidbody2D>();
        debrisPollygonCollider2D = GetComponent<PolygonCollider2D>();

        float boostX = Random.Range(-debrisSpawnBoost, debrisSpawnBoost);
        float boostY = Random.Range(debrisSpawnBoost/2f, debrisSpawnBoost);
        Vector2 force = new Vector2(boostX, boostY);
        
        float size = Random.Range(minDebrisSize, maxDebrisSize);
        Vector3 newSize = new Vector3(size, size, 0);

        StartCoroutine(DisableCollision());

        debrisRigidBody2D.AddForce( force, ForceMode2D.Impulse);
        transform.localScale = newSize;

        finalEnergy = baseEnergyAmount * size;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.TryGetComponent<PlayerEnergy>(out PlayerEnergy playerEnergy) && other is CapsuleCollider2D)
        {
            playerEnergy.RefillEnergy(finalEnergy);
            gameObject.SetActive(false);
        }
    }

    private IEnumerator DisableCollision()
    {
        debrisPollygonCollider2D.enabled = false;
        yield return new WaitForSeconds(delay);
        debrisPollygonCollider2D.enabled=true;
    }
}