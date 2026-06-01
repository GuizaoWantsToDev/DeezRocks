using UnityEngine;
public class SoundManager : UnityEngine.MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("=== SOUND CLIPS ===")]
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField] private AudioClip buttonSelectSound;

    private AudioSource globalAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
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

    public void PlayButtonHover()
    {
        PlaySound(buttonHoverSound);
    }
    public void PlayButtonSelect()
    {
        PlaySound(buttonSelectSound);
    }
}