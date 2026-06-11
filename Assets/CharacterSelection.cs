using UnityEngine;
using UnityEngine.Events;
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

    [SerializeField] private UnityEvent backInputEnable;
    

    public void OnCancel(InputAction.CallbackContext context)
    {
        CancelSelection();
    }

    public void CancelSelection()
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

        backInputEnable.Invoke();
    }
}
