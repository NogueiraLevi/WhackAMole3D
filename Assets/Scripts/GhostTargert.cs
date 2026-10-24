using System.Collections;
using UnityEngine;

public class GhostTarget : MonoBehaviour
{
    private bool canBeHit = true;
    private bool hasEscaped = false;
    private bool lateWindowActive = false;

    private Collider[] targetColliders;

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

        // Busca os colisores caso ainda não tenham sido encontrados.
        if (targetColliders == null || targetColliders.Length == 0)
        {
            targetColliders = GetComponentsInChildren<Collider>(true);
        }

        // Reativa todos os colisores quando o fantasma reaparece.
        foreach (Collider targetCollider in targetColliders)
        {
            if (targetCollider != null)
            {
                targetCollider.enabled = true;
            }
        }
    }


    public bool TryHit()
    {
        if (hasEscaped)
        {
            return false;
        }

        if (!canBeHit)
        {
            Debug.Log("Este fantasma já foi acertado!");
            return false;
        }

        canBeHit = false;
        Debug.Log("Acerto registrado no GhostTarget!");
        return true;
    }

    public void MarkEscaped()
    {
        if (hasEscaped || !canBeHit)
        {
            return;
        }

        hasEscaped = true;
        canBeHit = false;
        lateWindowActive = true;

        StartCoroutine(EndLateWindow());
    }

    private IEnumerator EndLateWindow()
    {
        yield return new WaitForSeconds(0.6f);

        lateWindowActive = false;

        foreach (Collider targetCollider in targetColliders)
        {
            if (targetCollider != null)
            {
                targetCollider.enabled = false;
            }
        }
    }
}
