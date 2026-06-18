using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [Header("=== ORIGINAL VARIABLES ===")]
    [SerializeField] private GameObject bodyGreen;
    [SerializeField] private GameObject bodyPurple;
    [SerializeField] private GameObject greenHatNormal;
    [SerializeField] private GameObject greeHatSelected;
    [SerializeField] private GameObject purpleHatNormal;
    [SerializeField] private GameObject purpleHatSelected;

    [SerializeField] private UnityEvent backInputEnable;

    [Header("=== NEW UI ELEMENTS (NOMES) ===")]
    public GameObject readyText;       // Arrasta o texto "Ready" para aqui
    public GameObject nameInputField;  // Arrasta o teu InputField novo para aqui

    // ---> NOVA FUNÇÃO: Chama isto no OnClick do botão quando o jogador ESCOLHE O CHAPÉU
    public void ConfirmSelection()
    {
        // Esconde o texto Ready original e acende o Input para escrever
        if (readyText != null) readyText.SetActive(false);
        if (nameInputField != null) nameInputField.SetActive(true);
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        CancelSelection();
    }

    public void CancelSelection()
    {
        // --- O TEU CÓDIGO ORIGINAL INTACTO ---
        bodyGreen.SetActive(false);
        bodyPurple.SetActive(false);
        greenHatNormal.SetActive(true);
        greeHatSelected.SetActive(false);
        purpleHatNormal.SetActive(true);
        purpleHatSelected.SetActive(false);

        greenHatNormal.GetComponent<Button>().interactable = true;
        purpleHatNormal.GetComponent<Button>().interactable = true;

        backInputEnable.Invoke();

        // --- A ADIÇÃO PARA OS NOMES ---
        // Se o gajo cancelar, esconde o InputField e volta a ligar o "Ready"
        if (nameInputField != null) nameInputField.SetActive(false);
        if (readyText != null) readyText.SetActive(true);
    }
}