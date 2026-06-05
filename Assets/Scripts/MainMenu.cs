using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : UnityEngine.MonoBehaviour
{
    
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
