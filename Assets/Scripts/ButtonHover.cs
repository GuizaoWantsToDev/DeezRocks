using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    [SerializeField] private float sizeMultiplier;

    private Vector2 originalSize;
    private RectTransform rectTransform;
    private Button thisButton;

    private void Awake()
    {
        thisButton=GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.localScale = originalSize * sizeMultiplier;
        SoundManager.Instance.PlayButtonHover();
    }
    public void OnSelect(BaseEventData eventData)
    {
        rectTransform.localScale = originalSize * sizeMultiplier;
        SoundManager.Instance.PlayButtonHover();
    }
    public void OnDeselect(BaseEventData eventData) 
    {
        rectTransform.localScale = originalSize * sizeMultiplier;
        SoundManager.Instance.PlayButtonHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.localScale = originalSize;
    }

    public void OnChangeInteraction()
    {
        thisButton.interactable = false;
    }
}
