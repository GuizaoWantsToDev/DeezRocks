using UnityEngine.UI;
using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; } = null;

    [SerializeField] private Image energyBar;
    [SerializeField] private float maxEnergy = 100f;

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
    }

    public void UseEnergy(float energy)
    {
        currentEnergy -= energy;
        UpdateEnergyBar();
    }
    public void RefiillEnergy(float energy)
    {
        currentEnergy += energy;
        UpdateEnergyBar();
    }
    private void UpdateEnergyBar()
    {
        energyBar.fillAmount = currentEnergy / maxEnergy;
    }
}
