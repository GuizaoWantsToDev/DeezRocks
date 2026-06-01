using System.Runtime.CompilerServices;
using UnityEngine;

public class MobilityAndCombatStats : MonoBehaviour
{

    public static MobilityAndCombatStats Instance { get; private set; } = null;

    [Header("--- SHOTGUN ---")]
    [SerializeField] public float shotgunDamage;
    [SerializeField] public float shotgunKnockbackForce;
    [SerializeField] public float shotgunEnergyCost;

    [Header("--- OTHER ---")]
    [SerializeField] public float baseRecoil;
    [SerializeField] public float extraRecoilPerLevel;
    [SerializeField] public float abilityCooldown;

    [Header("--- PLAYER_STATS ---")]
    [SerializeField] public float knockedTimer;
    [SerializeField] public bool isKnocked;
    
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
