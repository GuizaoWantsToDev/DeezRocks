using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class TitleScreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "MainMenu";

    private void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += LoadNextScene;
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(nextSceneName);
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            SceneManager.LoadScene(nextSceneName);
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void LoadNextScene(VideoPlayer vp)
    {
        SceneManager.LoadScene(nextSceneName);
    }
}