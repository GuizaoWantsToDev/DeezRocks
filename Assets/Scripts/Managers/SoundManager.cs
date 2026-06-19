using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Menu Or UI Sounds")]
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField] private AudioClip buttonSelectSound;
    [SerializeField] private AudioClip grassToggleSound;
    [SerializeField] private AudioClip equipSound;

    [Header("In Game Sounds")]
    [SerializeField] private AudioClip rockHitSound;
    [SerializeField] private AudioClip shockwaveSound;
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip rockThrowSound;
    [SerializeField] private AudioClip landingSound;
    [SerializeField] private AudioClip[] jumpSound;
    [SerializeField] private AudioClip[] jumpScreamSound;
    [SerializeField] private AudioClip debrisSound;
    [SerializeField] private AudioClip switchToRockSound;
    [SerializeField] private AudioClip switchToShotgunSound;

    private AudioSource globalAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        globalAudioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null || globalAudioSource == null)
        {
            return;
        }

        globalAudioSource.PlayOneShot(clip);
    }

    public void PlayButtonHover()
    {
        PlaySound(buttonHoverSound);
    }

    public void PlayButtonSelect()
    {
        PlaySound(buttonSelectSound);
    }

    public void PlayGrassToggle()
    {
        PlaySound(grassToggleSound);
    }

    public void PlayEquipSound()
    {
        PlaySound(equipSound);
    }

    public void PlayRockHit()
    {
        PlaySound(rockHitSound);
    }

    public void PlayShockwave()
    {
        PlaySound(shockwaveSound);
    }

    public void PlayPlayerHit()
    {
        PlaySound(playerHitSound);
    }

    public void PlayDashSound()
    {
        PlaySound(dashSound);
    }

    public void PlayRockThrow()
    {
        PlaySound(rockThrowSound);
    }

    public void PlayLandingSound()
    {
        PlaySound(landingSound);
    }

    public void PlayJumpSound()
    {
        PlaySound(jumpSound[Random.Range(0, jumpSound.Length - 1)]);
        PlaySound(jumpScreamSound[Random.Range(0, jumpScreamSound.Length - 1)]);
    }
    public void ShotgunSound()
    {
        PlaySound(switchToShotgunSound);
    }
    public void RockSound()
    {
        PlaySound(switchToRockSound);
    }
    public void PlayDebrisSound()
    {
        PlaySound(debrisSound);
    }
}