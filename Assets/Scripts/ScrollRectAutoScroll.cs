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

    [Header("=== FORCE SELECT ===")]
    [Tooltip("O botão/Dropdown que deve receber o foco após uma seleção.")]
    [SerializeField] private GameObject buttonToSelectAfter;

    private bool mouseOver = false;
    private List<Selectable> selectables = new List<Selectable>();
    private ScrollRect scrollRect;
    private Vector2 targetScrollPosition = Vector2.up;

    // Rastreador super útil para saber se a seleção mudou
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
        // O EventSystem já sabe o que estás a selecionar, não precisamos de ler os analógicos à mão!
        GameObject currentSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        // Se a seleção mudou e não foi com o rato
        if (currentSelected != null && currentSelected != lastSelected)
        {
            lastSelected = currentSelected;

            if (!mouseOver)
            {
                // Atualiza a lista AQUI. Como o Dropdown gera as opções na hora, 
                // isto garante que a lista está sempre correta!
                RefreshSelectablesList();
                ScrollToSelected(false);
            }
        }

        // Movimento suave do Scroll
        if (!mouseOver)
        {
            scrollRect.normalizedPosition = Vector2.Lerp(
                scrollRect.normalizedPosition,
                targetScrollPosition,
                scrollSpeed * Time.unscaledDeltaTime
            );
        }
        else
        {
            targetScrollPosition = scrollRect.normalizedPosition;
        }
    }

    private void RefreshSelectablesList()
    {
        if (scrollRect == null) return;

        scrollRect.content.GetComponentsInChildren(selectables);

        // Garante que o hover e o selected color são os mesmos para navegação com comando
        foreach (Selectable selectable in selectables)
        {
            ColorBlock colors = selectable.colors;
            colors.selectedColor = colors.highlightedColor;
            selectable.colors = colors;
        }
    }

    private void ScrollToSelected(bool instant)
    {
        if (EventSystem.current == null) return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        Selectable selectedElement = selectedObject != null ? selectedObject.GetComponent<Selectable>() : null;

        if (selectedElement == null) return;

        int selectedIndex = selectables.IndexOf(selectedElement);
        if (selectedIndex < 0) return; // Se a opção não estiver na lista, aborta

        // Calcula a posição (1 = topo, 0 = fundo)
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

    // Chama esta função a partir do evento OnValueChanged do teu Dropdown!
    public void ForceSelectTarget()
    {
        if (buttonToSelectAfter != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(buttonToSelectAfter);
        }
        else
        {
            Debug.LogWarning("Nenhum botão definido no 'Button To Select After' ou EventSystem ausente.");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;

        // Quando o rato sai, volta a focar onde o comando estava
        if (lastSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
        ScrollToSelected(false);
    }
}