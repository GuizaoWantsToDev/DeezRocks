using UnityEngine;
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("--- MENU OR UI SOUNDS ---")]
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField] private AudioClip buttonSelectSound;
    [SerializeField] private AudioClip grassToggleSound;
    [SerializeField] private AudioClip equipSound;


    [Header("--- IN GAME SOUNDS ---")]
    [SerializeField] private AudioClip rockHitSound;
    [SerializeField] private AudioClip shockwaveSound;
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip rockThrowSound;
    [SerializeField] private AudioClip landingSound;
    [SerializeField] private AudioClip[] jumpSound;
    [SerializeField] private AudioClip[] jumpScreamSound;


    private AudioSource globalAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        globalAudioSource = GetComponent<AudioSource>();
    }
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || globalAudioSource == null) return;

        globalAudioSource.PlayOneShot(clip);
    }

    // MENU OR UI SOUNDS
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

    //IN GAME SOUNDS

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
    public void PLayJumpSound()
    {
        PlaySound(jumpSound[Random.Range(0, jumpSound.Length-1)]);
        PlaySound(jumpScreamSound[Random.Range(0, jumpScreamSound.Length-1)]);
    }
}