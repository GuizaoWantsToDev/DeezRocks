using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [SerializeField] private GameObject bodyGreen;
    [SerializeField] private GameObject bodyPurple;
    [SerializeField] private GameObject greenHatNormal;
    [SerializeField] private GameObject greeHatSelected;
    [SerializeField] private GameObject purpleHatNormal;
    [SerializeField] private GameObject purpleHatSelected;
    [SerializeField] private GameObject ready;
    

    public void OnCancel(InputAction.CallbackContext context)
    {
        bodyGreen.SetActive(false);
        bodyPurple.SetActive(false);
        greenHatNormal.SetActive(true);
        greeHatSelected.SetActive(false);
        purpleHatNormal.SetActive(true);
        purpleHatSelected.SetActive(false);
        ready.SetActive(false);

        greenHatNormal.GetComponent<Button>().interactable = true;
        purpleHatNormal.GetComponent<Button>().interactable = true;
        MainMenu.Instance.PlayerReady(false);
        MainMenu.Instance.GetComponent<PlayerInput>().enabled = true;
    }
}
