using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectAutoScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 10f;

    [Header("Force Select")]
    [SerializeField] private GameObject buttonToSelectAfter;

    private bool mouseOver = false;
    private List<Selectable> selectables = new List<Selectable>();
    private ScrollRect scrollRect;
    private Vector2 targetScrollPosition = Vector2.up;
    private GameObject lastSelected;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Start()
    {
        RefreshSelectablesList();
        ScrollToSelected(true);
    }

    private void OnEnable()
    {
        RefreshSelectablesList();
    }

    private void Update()
    {
        GameObject currentSelected = null;

        if (EventSystem.current != null)
        {
            currentSelected = EventSystem.current.currentSelectedGameObject;
        }

        if (currentSelected != null && currentSelected != lastSelected)
        {
            lastSelected = currentSelected;

            if (!mouseOver)
            {
                RefreshSelectablesList();
                ScrollToSelected(false);
            }
        }

        if (!mouseOver)
        {
            scrollRect.normalizedPosition = Vector2.Lerp(scrollRect.normalizedPosition, targetScrollPosition, scrollSpeed * Time.unscaledDeltaTime);
        }
        else
        {
            targetScrollPosition = scrollRect.normalizedPosition;
        }
    }

    private void RefreshSelectablesList()
    {
        if (scrollRect == null)
        {
            return;
        }

        scrollRect.content.GetComponentsInChildren(selectables);

        foreach (Selectable selectable in selectables)
        {
            ColorBlock colors = selectable.colors;
            colors.selectedColor = colors.highlightedColor;
            selectable.colors = colors;
        }
    }

    private void ScrollToSelected(bool instant)
    {
        if (EventSystem.current == null)
        {
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        Selectable selectedElement = null;

        if (selectedObject != null)
        {
            selectedElement = selectedObject.GetComponent<Selectable>();
        }

        if (selectedElement == null)
        {
            return;
        }

        int selectedIndex = selectables.IndexOf(selectedElement);

        if (selectedIndex < 0)
        {
            return;
        }

        float normalizedIndex = 1f - (selectedIndex / Mathf.Max(1f, (float)selectables.Count - 1));
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

    public void ForceSelectTarget()
    {
        if (buttonToSelectAfter != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(buttonToSelectAfter);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;

        if (lastSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }

        ScrollToSelected(false);
    }
}