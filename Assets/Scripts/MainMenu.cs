using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject tutorialMenu;
    [SerializeField] private GameObject quitMenu;

    private bool playerOneReady = false;
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public void OnGoBack(InputAction.CallbackContext context)
    {
        if (!playerOneReady)
        {
            SoundManager.Instance.PlayButtonSelect();
            mainMenu.SetActive(true);
            playMenu.SetActive(false);
            optionsMenu.SetActive(false);
            quitMenu.SetActive(false);
            tutorialMenu.SetActive(false);
        }
    }

    public void PlayerReady(bool isReady) => playerOneReady = isReady;


    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("QUIT!!!!!!");

#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#endif
    }
}
