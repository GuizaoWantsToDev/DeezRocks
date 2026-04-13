using UnityEngine.UI;
using UnityEngine;
using System.Collections;

public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; } = null;

    [SerializeField] private Image energyBar;
    [SerializeField] private float maxEnergy;

    [SerializeField] private float passiveCooldown;
    [SerializeField] private float passiveEnergyAmount;

    public float currentEnergy;

    [SerializeField] private RockThrow rockThrow;

    private Coroutine passiveEnergyCoroutine;
    //private void Awake()
    //{
    //    if (Instance == null)
    //    {
    //        Instance = this;
    //    }
    //    else
    //    {
    //        Destroy(gameObject);
    //    }
    //}
    private void Start()
    {
        StartPassiveRegen();
        currentEnergy = maxEnergy;
    }
    public void StartPassiveRegen()
    {
        StopPassiveRegen();
        passiveEnergyCoroutine = StartCoroutine(PassiveEnergy());
    }

    public void StopPassiveRegen()
    {
        if (passiveEnergyCoroutine != null)
        {
            StopCoroutine(passiveEnergyCoroutine);
            passiveEnergyCoroutine = null;
        }
    }

    private IEnumerator PassiveEnergy()
    {
        while (true)
        {
            yield return new WaitForSeconds (passiveCooldown);

            if (currentEnergy < maxEnergy)
            {
                RefillEnergy(passiveEnergyAmount);
            }
        }
    }
    public void UseEnergy(float energy)
    {
        currentEnergy -= energy;
        UpdateEnergyBar();
    }
    public void RefillEnergy(float energy)
    {
        currentEnergy += energy;
        UpdateEnergyBar();
    }
    private void UpdateEnergyBar()
    {
        energyBar.fillAmount = currentEnergy / maxEnergy;
    }
}