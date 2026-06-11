using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectAutoScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("=== SCROLL SETTINGS ===")]
    [SerializeField] private float scrollSpeed = 10f;

    private bool mouseOver = false;
    private List<Selectable> selectables = new List<Selectable>();
    private ScrollRect scrollRect;
    private Vector2 targetScrollPosition = Vector2.up;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Start()
    {
        if (scrollRect)
            scrollRect.content.GetComponentsInChildren(selectables);

        // Make selected color match highlighted color for all buttons
        // This fixes the missing highlight when navigating with keyboard or controller
        foreach (Selectable selectable in selectables)
        {
            ColorBlock colors = selectable.colors;
            colors.selectedColor = colors.highlightedColor;
            selectable.colors = colors;
        }

        ScrollToSelected(true);
    }

    private void OnEnable()
    {
        if (scrollRect)
            scrollRect.content.GetComponentsInChildren(selectables);
    }

    private void Update()
    {
        CheckForNavigationInput();

        if (!mouseOver)
        {
            // Smoothly lerp toward the target scroll position
            scrollRect.normalizedPosition = Vector2.Lerp(
                scrollRect.normalizedPosition,
                targetScrollPosition,
                scrollSpeed * Time.unscaledDeltaTime
            );
        }
        else
        {
            // Mouse is over — follow scroll position directly
            targetScrollPosition = scrollRect.normalizedPosition;
        }
    }

    // Checks if the player is navigating with keyboard or controller
    private void CheckForNavigationInput()
    {
        if (selectables.Count == 0) return;

        bool isNavigatingWithKeyboard = Keyboard.current != null &&
            (Keyboard.current.upArrowKey.isPressed ||
             Keyboard.current.downArrowKey.isPressed ||
             Keyboard.current.wKey.isPressed ||
             Keyboard.current.sKey.isPressed);

        bool isNavigatingWithStick = Gamepad.current != null &&
            Gamepad.current.leftStick.ReadValue().magnitude > 0.3f;

        // D-pad is digital so it needs to be checked separately from the analog stick
        bool isNavigatingWithDpad = Gamepad.current != null &&
            (Gamepad.current.dpad.up.isPressed ||
             Gamepad.current.dpad.down.isPressed);

        if (isNavigatingWithKeyboard || isNavigatingWithStick || isNavigatingWithDpad)
            ScrollToSelected(false);
    }

    // Calculates the correct scroll position to keep the selected item visible
    private void ScrollToSelected(bool instant)
    {
        if (EventSystem.current == null) return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        Selectable selectedElement = selectedObject != null
            ? selectedObject.GetComponent<Selectable>()
            : null;

        if (selectedElement == null) return;

        int selectedIndex = selectables.IndexOf(selectedElement);
        if (selectedIndex < 0) return;

        // Calculate normalized position (1 = top, 0 = bottom)
        float normalizedIndex = 1f - (selectedIndex / ((float)selectables.Count - 1));
        Vector2 newPosition = new Vector2(0f, normalizedIndex);

        if (instant)
        {
            scrollRect.normalizedPosition = newPosition;
            targetScrollPosition = newPosition;
        }
        else
        {
            targetScrollPosition = newPosition;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;
        ScrollToSelected(false);
    }
}