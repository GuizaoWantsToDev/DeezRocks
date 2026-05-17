using UnityEngine;

// Singleton pattern to manage global sounds.
// We use a 2D AudioSource to play sounds at full volume everywhere (Brackeys method).
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("=== SOUND CLIPS ===")]
    // The sound file for rock impacts
    [SerializeField] private AudioClip rockHitSound;
    [SerializeField] private AudioClip playerHitSound;

    // The AudioSource component attached to this GameObject
    private AudioSource globalAudioSource;

    private void Awake()
    {
        // Ensure only one SoundManager exists in the game
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Get the AudioSource component from this GameObject
        globalAudioSource = GetComponent<AudioSource>();
    }

    // Plays a sound as a 2D effect (full volume regardless of camera distance)
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || globalAudioSource == null) return;

        // PlayOneShot allows multiple sounds to play at the same time without cutting each other off
        globalAudioSource.PlayOneShot(clip);
    }

    // Helper method to easily play the rock hit sound from anywhere
    public void PlayRockHit()
    {
        PlaySound(rockHitSound);
    }
    public void PlayPlayerHit()
    {
        PlaySound(playerHitSound);
    }
}