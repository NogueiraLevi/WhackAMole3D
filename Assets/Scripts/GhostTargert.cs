using System.Collections;
using UnityEngine;

// O nome do arquivo foi preservado para evitar alterações desnecessárias nos assets.
public class GhostTarget : MonoBehaviour
{
    private bool canBeHit = true;
    private bool hasEscaped;
    private bool lateWindowActive;
    private bool lateHitAlreadyCounted;
    private Collider[] targetColliders;
    private Coroutine lateWindowRoutine;

    public bool HasEscaped => hasEscaped;
    public bool LateWindowActive => lateWindowActive;

    private void Awake()
    {
        targetColliders = GetComponentsInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        canBeHit = true;
        hasEscaped = false;
        lateWindowActive = false;
        lateHitAlreadyCounted = false;
        if (targetColliders == null || targetColliders.Length == 0)
            targetColliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider c in targetColliders)
            if (c != null) c.enabled = true;
    }

    private void OnDisable()
    {
        if (lateWindowRoutine != null)
        {
            StopCoroutine(lateWindowRoutine);
            lateWindowRoutine = null;
        }
    }

    public bool TryHit()
    {
        if (hasEscaped || !canBeHit)
            return false;

        canBeHit = false;
        Debug.Log("Acerto registrado no GhostTarget!");
        return true;
    }

    // A janela é definida pelo MoleRandomizer, sem valor duplicado aqui.
    public void MarkEscaped(float windowDuration)
    {
        if (hasEscaped || !canBeHit) return;
        hasEscaped = true;
        canBeHit = false;
        lateWindowActive = true;
        lateHitAlreadyCounted = false;
        lateWindowRoutine = StartCoroutine(EndLateWindow(Mathf.Max(0.1f, windowDuration)));
    }

    // Evita que vários cliques no mesmo fantasma escapado gerem vários erros.
    public bool TryRegisterLateHit()
    {
        if (!hasEscaped || !lateWindowActive || lateHitAlreadyCounted)
            return false;

        lateHitAlreadyCounted = true;
        return true;
    }

    private IEnumerator EndLateWindow(float duration)
    {
        yield return new WaitForSeconds(duration);
        lateWindowActive = false;
        foreach (Collider c in targetColliders)
            if (c != null) c.enabled = false;
        lateWindowRoutine = null;
    }
}
