using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [Header("Original Variables")]
    [SerializeField] private GameObject bodyGreen;
    [SerializeField] private GameObject bodyPurple;
    [SerializeField] private GameObject greenHatNormal;
    [SerializeField] private GameObject greeHatSelected;
    [SerializeField] private GameObject purpleHatNormal;
    [SerializeField] private GameObject purpleHatSelected;
    [SerializeField] private UnityEvent backInputEnable;

    [Header("UI Names")]
    public GameObject readyText;
    public GameObject nameInputField;

    public void ConfirmSelection()
    {
        if (readyText != null)
        {
            readyText.SetActive(false);
        }

        if (nameInputField != null)
        {
            nameInputField.SetActive(true);
        }
    }

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
        greenHatNormal.GetComponent<Button>().interactable = true;
        purpleHatNormal.GetComponent<Button>().interactable = true;

        backInputEnable.Invoke();

        if (nameInputField != null)
        {
            nameInputField.SetActive(false);
        }

        if (readyText != null)
        {
            readyText.SetActive(true);
        }
    }
}