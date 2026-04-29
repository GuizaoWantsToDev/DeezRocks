using UnityEngine;
using UnityEngine.InputSystem;

public class RockThrow : MonoBehaviour
{
    private PlayerController player;
    private PlayerInput playerInput;
    private PlayerEnergy playerEnergy;

    [Header("=== STATE & AIMING ===")]
    public bool inThrowState;
    public Vector2 aimDirection = Vector2.right;
    private Vector2 rawAimInput;

    [Header("=== SHOTGUN UPGRADE ===")]
    public bool isShotgunActive = false;
    public Transform[] shotgunShootPoints;
    public WeaponUIManager weaponUI;
    [SerializeField] private float shotgunEnergyMultiplier = 3f;

    [Header("=== VISUALS & EFFECTS ===")]
    [SerializeField] private SpriteRenderer armRender;
    [SerializeField] private ParticleSystem particleSystems;
    [SerializeField] private SpriteRenderer shotgunConeIndicator;

    [Header("=== ROCK SETUP & PREFABS ===")]
    [SerializeField] private Rock rock;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject throwPoint;
    [SerializeField] private Transform throwDirection;
    [SerializeField] private LineRenderer trajectoryLine;

    private GameObject rockInst;

    [Header("=== RECOIL (EXPONENTIAL) ===")]
    [SerializeField] private float baseRecoilForce = 1f;
    [SerializeField] private float recoilGrowthRate = 1.8f;

    [Header("=== COOLDOWN SETTINGS ===")]
    [SerializeField] private float throwCooldown = 1f;
    private float nextThrowTime;

    [Header("=== SPAWN VALIDATION ===")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float spawnCheckRadius = 0.3f;

    public float holdTime { get; private set; }

    private void Start()
    {
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        playerEnergy = GetComponent<PlayerEnergy>();

        // O Cone reflete a arma selecionada no início
        if (shotgunConeIndicator != null) shotgunConeIndicator.enabled = isShotgunActive;
    }

    private void Update()
    {
        HandleAimDirection();
        player.myAnimator.SetBool("ThrowState", inThrowState);

        if (inThrowState && !isShotgunActive)
        {
            holdTime += Time.deltaTime;

            if (rockInst != null)
            {
                rockInst.transform.rotation = throwPoint.transform.rotation;
            }
        }
        else
        {
            holdTime = 0f;
        }
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        rawAimInput = context.ReadValue<Vector2>();
    }

    public void OnSwapWeapon(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState)
        {
            isShotgunActive = !isShotgunActive;
            if (weaponUI != null) weaponUI.UpdateWeaponUI(isShotgunActive);

            // LIGA/DESLIGA O CONE VISUAL CONSOANTE A ARMA
            if (shotgunConeIndicator != null) shotgunConeIndicator.enabled = isShotgunActive;
            if (trajectoryLine != null) trajectoryLine.enabled = !isShotgunActive && inThrowState;
        }
    }

    private void HandleAimDirection()
    {
        if (playerInput.currentControlScheme == "Keyboard")
        {
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                aimDirection = (mousePosition - (Vector2)transform.position).normalized;
            }
        }
        else
        {
            if (rawAimInput.magnitude > 0.25f)
                aimDirection = rawAimInput.normalized;
        }

        throwPoint.transform.right = aimDirection;
        armRender.flipY = aimDirection.x < 0;

        if (shotgunConeIndicator != null) shotgunConeIndicator.flipY = armRender.flipY;
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed && !inThrowState && Time.time >= nextThrowTime)
        {
            float baseCost = rock.baseEnergyCost;

            // ==========================================
            // DISPARO INSTANTÂNEO DA SHOTGUN
            // ==========================================
            if (isShotgunActive)
            {
                float totalShotgunCost = baseCost * shotgunEnergyMultiplier;

                if (playerEnergy.HasEnough(totalShotgunCost) && !player.isWalled)
                {
                    // Verificação de colisão nas paredes ANTES de disparar
                    bool spawnInsidePlatform = false;
                    foreach (Transform sPoint in shotgunShootPoints)
                    {
                        if (sPoint != null && Physics2D.OverlapCircle(sPoint.position, spawnCheckRadius, platformLayer) != null)
                        {
                            spawnInsidePlatform = true;
                            break;
                        }
                    }
                    if (spawnInsidePlatform) return;

                    // Desconta Energia
                    playerEnergy.UseEnergy(totalShotgunCost);

                    // Mostra o braço rapidamente para feedback visual do disparo
                    armRender.enabled = true;
                    Invoke(nameof(HideArm), 0.15f);

                    // Cria e dispara instantaneamente
                    for (int i = 0; i < shotgunShootPoints.Length; i++)
                    {
                        Transform sPoint = shotgunShootPoints[i];
                        if (sPoint == null) continue;

                        GameObject sgRock = Instantiate(rockPrefab, sPoint.position, sPoint.rotation);
                        Rock newRockScript = sgRock.GetComponent<Rock>();

                        if (newRockScript != null)
                        {
                            newRockScript.isShotgunRock = true;
                            newRockScript.SetOwner(this, playerEnergy, sPoint);
                            newRockScript.ReleaseRock(sPoint.right); // Fogo IMEDIATO
                        }
                    }

                    // Recoice Instantâneo
                    player.isKnockBacked = true;
                    player.myRigidBody2D.AddForce(-aimDirection * (baseRecoilForce * 2f), ForceMode2D.Impulse);
                    player.Invoke(nameof(player.CancelKnockBack), player.knockBackTime);

                    nextThrowTime = Time.time + throwCooldown;
                }
            }
            // ==========================================
            // PREPARAÇÃO DA PEDRA NORMAL (HOLD)
            // ==========================================
            else
            {
                if (playerEnergy.HasEnough(baseCost) && !player.isWalled)
                {
                    Vector2 spawnPosition = throwDirection.position;
                    if (Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, platformLayer) != null) return;

                    inThrowState = true;
                    holdTime = 0f;
                    player.ResetGravity();
                    playerEnergy.StopPassiveRegen();

                    if (trajectoryLine != null) trajectoryLine.enabled = true;

                    rockInst = Instantiate(rockPrefab, spawnPosition, throwPoint.transform.rotation);
                    if (rockInst != null)
                    {
                        Rock newRockScript = rockInst.GetComponent<Rock>();
                        playerEnergy.UseEnergy(newRockScript.baseEnergyCost);
                        newRockScript.SetOwner(this, playerEnergy, throwDirection);
                    }
                    armRender.enabled = true;
                }
            }
        }

        // LARGAR O BOTÃO (Apenas afeta a pedra normal, a Shotgun já foi disparada)
        if (context.canceled && inThrowState && !isShotgunActive)
        {
            FireRock();
        }
    }

    private void FireRock()
    {
        if (rockInst != null)
        {
            Rock normalRock = rockInst.GetComponent<Rock>();
            if (normalRock != null)
            {
                normalRock.ReleaseRock(aimDirection);
                float recoilForce = baseRecoilForce * Mathf.Pow(recoilGrowthRate, normalRock.currentRockStage);
                player.isKnockBacked = true;
                player.myRigidBody2D.AddForce(-aimDirection * recoilForce, ForceMode2D.Impulse);
                player.Invoke(nameof(player.CancelKnockBack), player.knockBackTime);
            }
        }
        rockInst = null;

        nextThrowTime = Time.time + throwCooldown;
        ResetThrowState();
        playerEnergy.StartPassiveRegen();
    }

    public void ResetThrowState()
    {
        inThrowState = false;
        holdTime = 0f;
        armRender.enabled = false;
        player.myAnimator.SetBool("ThrowState", false);

        if (trajectoryLine != null) trajectoryLine.enabled = false;

        if (rockInst != null) Destroy(rockInst);
        rockInst = null;
    }

    private void HideArm()
    {
        if (!inThrowState) armRender.enabled = false;
    }

    public void ForceRelease()
    {
        if (inThrowState && !isShotgunActive) FireRock();
    }
}