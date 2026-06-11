using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float sizeMultiplier = 1.1f;
    private Vector2 originalSize;
    private RectTransform rectTransform;
    private Button thisButton;

    private void Awake()
    {
        thisButton = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.localScale;
    }


    public void OnBackInput(InputAction.CallbackContext context)
    {
        ForceButtonAction();
    }
    public void ForceButtonAction()
    {
        thisButton.onClick.Invoke();
    }
    // Mouse enters the button
    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.localScale = originalSize * sizeMultiplier;
        SoundManager.Instance.PlayButtonHover();
    }

    // Mouse leaves the button
    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.localScale = originalSize;
    }

    // Gamepad navigates TO this button
    public void OnSelect(BaseEventData eventData)
    {
        ForceSelect();
    }

    public void ForceSelect()
    {
        rectTransform.localScale = originalSize * sizeMultiplier;
        SoundManager.Instance.PlayButtonHover();
    }

    // Gamepad navigates AWAY from this button
    public void OnDeselect(BaseEventData eventData)
    {
        ForceDeselect();
    }

    public void ForceDeselect()
    {
        rectTransform.localScale = originalSize;
    }

    public void OnChangeInteraction()
    {
        thisButton.interactable = false;
    }

    
}