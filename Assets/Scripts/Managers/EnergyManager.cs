using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    [Header("Energy Stats")]
    public float maxEnergy = 100f;
    public float passiveCooldown = 2f;
    public float passiveRegenAmount = 5f;

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
}