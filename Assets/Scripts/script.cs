using UnityEngine;
// IMPORTANTE: Adiciona esta linha para usar o novo Input System
using UnityEngine.InputSystem;

public class ArrastarObjeto2D : MonoBehaviour
{
    private Camera cam;
    private TargetJoint2D targetJoint;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Se o rato não estiver conectado, não faz nada
        if (Mouse.current == null) return;

        // Novo Input System: Lê a posição do rato no ecrã
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // Converte para a posição do mundo 2D
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        // 1. Clicar com o botão esquerdo (Equivalente ao antigo GetMouseButtonDown)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                Rigidbody2D rb = hit.collider.GetComponentInParent<Rigidbody2D>();

                if (rb != null)
                {
                    targetJoint = rb.gameObject.AddComponent<TargetJoint2D>();
                    targetJoint.dampingRatio = 1f;
                    targetJoint.frequency = 5f;

                    targetJoint.target = mousePos2D;
                }
            }
        }

        // 2. Enquanto mantiveres o botão premido (Equivalente ao antigo GetMouseButton)
        if (Mouse.current.leftButton.isPressed && targetJoint != null)
        {
            targetJoint.target = mousePos2D;
        }

        // 3. Largar o botão esquerdo (Equivalente ao antigo GetMouseButtonUp)
        if (Mouse.current.leftButton.wasReleasedThisFrame && targetJoint != null)
        {
            Destroy(targetJoint);
        }
    }
}