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
        currentEnergy = maxEnergy;

        StartCoroutine(PassiveEnergy());
    }

    private void Update()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
    }
    IEnumerator PassiveEnergy()
    {
        while(true)
        {
            yield return new WaitForSeconds(passiveCooldown);
            RefillEnergy(passiveEnergyAmount);
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
