using UnityEngine;

public class EnergyManager : UnityEngine.MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    [Header("--- ENERGY_STATS ---")]
    public float maxEnergy = 100f;
    public float passiveCooldown = 2f;
    public float passiveRegenAmount = 5f;                           

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
}