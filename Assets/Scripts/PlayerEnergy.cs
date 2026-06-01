using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerEnergy : UnityEngine.MonoBehaviour
{
    [Header("--- UI ----")]
    [SerializeField] private Image energyBar;
    [SerializeField] GameObject energyWarning;
    public float currentEnergy;

    private float lowEnough = 0.30f;

    private Coroutine passiveRegenCoroutine;
    private Coroutine warningCoroutine;
    private float blinkDelay = 0.6f;

    private void Start()
    {
        currentEnergy = EnergyManager.Instance.maxEnergy;
        UpdateBars();
        StartPassiveRegen();
    }

    private void Update()
    {
        if (currentEnergy == 0f)
            energyWarning.SetActive(true);
    }
    private bool LowOnEnergy()
    {
        if(currentEnergy/EnergyManager.Instance.maxEnergy < lowEnough)
            return true;
        else
            return false;
    }

    private IEnumerator Warning()
    { 
        while (true && currentEnergy != 0)
        {
            energyWarning.SetActive(true);
            yield return new WaitForSeconds(blinkDelay);
            energyWarning.SetActive(false);
            yield return new WaitForSeconds(blinkDelay);
        }
    }
    public void UseEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Max(currentEnergy, 0f);
        UpdateBars();

        if (LowOnEnergy() && warningCoroutine == null)
            warningCoroutine = StartCoroutine(Warning());
    }

    public void RefillEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Min(currentEnergy, EnergyManager.Instance.maxEnergy);
        UpdateBars();

        if (warningCoroutine != null && currentEnergy/EnergyManager.Instance.maxEnergy > lowEnough)
        {
            energyWarning.SetActive(false);
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }
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

        if (energyBar != null)
            energyBar.fillAmount = fillAmount;
    }
}