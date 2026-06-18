using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("=== UI BARS ===")]
    public Image screenSpaceHealthBar;

    public float currentHealth;
    private Animator playerAnimator;

    private void Start()
    {
        currentHealth = HealthManager.Instance.maxHealth;
        UpdateBar();
        playerAnimator = GetComponent<Animator>();
    }

    public void Damage(float damageAmount)
    {
        currentHealth -= damageAmount;
        UpdateBar();

        if (SoundManager.Instance != null && playerAnimator != null)
        {
            playerAnimator.SetTrigger("Damaged");
            SoundManager.Instance.PlayPlayerHit();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateBar()
    {
        float fillAmount = currentHealth / HealthManager.Instance.maxHealth;

        if (screenSpaceHealthBar != null)
            screenSpaceHealthBar.fillAmount = fillAmount;
    }

    public void Die()
    {
        // 1. Força a vida a 0 (útil caso ele morra por cair num buraco com a vida cheia)
        currentHealth = 0f;
        UpdateBar();

        // 2. Vai buscar a energia e seca-a completamente
        if (TryGetComponent<PlayerEnergy>(out PlayerEnergy playerEnergy))
        {
            playerEnergy.UseEnergy(playerEnergy.currentEnergy); // Gasta a energia toda que sobra
            playerEnergy.StopPassiveRegen(); // Impede que a energia volte a subir enquanto ele está morto
        }

        if (GameManager.Instance != null)
            GameManager.Instance.HandlePlayerDeath(gameObject);

        gameObject.SetActive(false);
    }
}