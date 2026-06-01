using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : UnityEngine.MonoBehaviour, IDamageable
{
    [Header("=== UI BARS ===")]
    [SerializeField] private Image screenSpaceHealthBar;

    public float currentHealth;

    private void Start()
    {
        currentHealth = HealthManager.Instance.maxHealth;
        UpdateBar();
    }
    public void Damage(float damageAmount)
    {
        currentHealth -= damageAmount;
        UpdateBar();

        //if (SoundManager.Instance != null)
       //     SoundManager.Instance.PlayPlayerHit();

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
        if (GameManager.Instance != null) 
            GameManager.Instance.RemovePlayer(gameObject);

        gameObject.SetActive(false);
    }
}
