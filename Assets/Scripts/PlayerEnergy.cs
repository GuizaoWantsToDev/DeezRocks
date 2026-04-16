using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerEnergy : MonoBehaviour
{
    [Header("=== UI BARS ===")]
    [SerializeField] private Image screenSpaceEnergyBar;
    [SerializeField] private Image worldSpaceEnergyBar;

    public float currentEnergy;

    private Coroutine passiveRegenCoroutine;

    private void Start()
    {
        currentEnergy = EnergyManager.Instance.maxEnergy;
        UpdateBars();
        StartPassiveRegen();
    }

    public void UseEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Max(currentEnergy, 0f);
        UpdateBars();
    }

    public void RefillEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Min(currentEnergy, EnergyManager.Instance.maxEnergy);
        UpdateBars();
    }

    public bool HasEnough(float amount)
    {
        return currentEnergy >= amount;
    }

    public void StartPassiveRegen()
    {
        StopPassiveRegen();
        passiveRegenCoroutine = StartCoroutine(PassiveRegenCoroutine());
    }

    public void StopPassiveRegen()
    {
        if (passiveRegenCoroutine != null)
        {
            StopCoroutine(passiveRegenCoroutine);
            passiveRegenCoroutine = null;
        }
    }

    private IEnumerator PassiveRegenCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(EnergyManager.Instance.passiveCooldown);

            if (currentEnergy < EnergyManager.Instance.maxEnergy)
                RefillEnergy(EnergyManager.Instance.passiveRegenAmount);
        }
    }

    private void UpdateBars()
    {
        float fillAmount = currentEnergy / EnergyManager.Instance.maxEnergy;

        if (screenSpaceEnergyBar != null)
            screenSpaceEnergyBar.fillAmount = fillAmount;

        if (worldSpaceEnergyBar != null)
            worldSpaceEnergyBar.fillAmount = fillAmount;
    }
}