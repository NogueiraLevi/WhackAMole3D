using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class HammerInput : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private HitManager hitManager;
    [SerializeField] private MoleRandomizer moleRandomizer;

    private void Awake()
    {
        if (moleRandomizer == null)
            moleRandomizer = FindFirstObjectByType<MoleRandomizer>();
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        DetectHit();
    }

    private void DetectHit()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("HammerInput: câmera não configurada.", this);
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GhostTarget ghost = hit.collider.GetComponentInParent<GhostTarget>();
            if (ghost != null)
            {
                if (ghost.HasEscaped)
                {
                    // Cada fuga penaliza no máximo uma martelada atrasada.
                    if (ghost.TryRegisterLateHit())
                        hitManager?.RegisterTargetTooLate();
                    return;
                }

                // O randomizador é a fonte de verdade sobre qual fantasma está ativo.
                if (moleRandomizer != null && moleRandomizer.TryRegisterHit(hit.collider))
                {
                    hitManager?.RegisterHit();
                    Debug.Log("ACERTOU O FANTASMA!");
                }
                return;
            }
        }

        hitManager?.RegisterMissedTarget();
    }
}
