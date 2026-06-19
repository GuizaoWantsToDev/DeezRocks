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
        InitIfNeeded();
    }

    private void InitIfNeeded()
    {
        if (rectTransform == null)
        {
            thisButton = GetComponent<Button>();
            rectTransform = GetComponent<RectTransform>();
            originalSize = rectTransform.localScale;
        }
    }

    public void OnBackInput(InputAction.CallbackContext context)
    {
        ForceButtonAction();
    }

    public void ForceButtonAction()
    {
        if (thisButton != null)
        {
            thisButton.onClick.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        InitIfNeeded();
        rectTransform.localScale = originalSize * sizeMultiplier;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonHover();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InitIfNeeded();
        rectTransform.localScale = originalSize;
    }

    public void OnSelect(BaseEventData eventData)
    {
        ForceSelect();
    }

    public void ForceSelect()
    {
        InitIfNeeded();
        rectTransform.localScale = originalSize * sizeMultiplier;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonHover();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ForceDeselect();
    }

    public void ForceDeselect()
    {
        InitIfNeeded();
        rectTransform.localScale = originalSize;
    }

    public void OnChangeInteraction()
    {
        if (thisButton != null)
        {
            thisButton.interactable = false;
        }
    }
}