using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class HammerInput : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private HitManager hitManager;

    private void Update()
    {
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Ignora cliques sobre a interface (UI).
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            DetectHit();
        }
    }


    private void DetectHit()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning(
                "HammerInput: nenhuma câmera foi configurada."
            );
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Raycast atingiu: " + hit.collider.name);
            
            GhostTarget ghost =
                hit.collider.GetComponentInParent<GhostTarget>();

            if (ghost != null)
            {
                // O fantasma escapou recentemente.
                if (ghost.HasEscaped && ghost.LateWindowActive)
                {
                    if (hitManager != null)
                    {
                        hitManager.RegisterTargetTooLate();
                    }

                    return;
                }

                // O fantasma ainda está presente.
                if (!ghost.HasEscaped)
                {
                    bool registered = ghost.TryHit();

                    if (registered)
                    {
                        Debug.Log("ACERTOU O FANTASMA!");

                        if (hitManager != null)
                        {
                            hitManager.RegisterHit();
                        }
                    }
                }

                // Não registra erro de mira para esse clique.
                return;
            }
        }

        // Só chega aqui se não encontrou um fantasma reconhecido.
        if (hitManager != null)
        {
            hitManager.RegisterMissedTarget();
        }
    }

}
