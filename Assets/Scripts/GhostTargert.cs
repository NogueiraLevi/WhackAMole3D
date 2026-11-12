using System.Collections;
using UnityEngine;

public class GhostTarget : MonoBehaviour
{
    [Header("Animação")]
    [SerializeField] private Animator animator;
    [SerializeField] private string retreatTrigger = "Recolher";

    private bool canBeHit = true;
    private bool hasEscaped;
    private bool lateWindowActive;
    private bool lateHitAlreadyCounted;

    private Collider[] targetColliders;
    private Renderer[] targetRenderers;
    private Coroutine lateWindowRoutine;

    public bool HasEscaped => hasEscaped;
    public bool LateWindowActive => lateWindowActive;

    public bool IsHittable
    {
        get
        {
            if (!isActiveAndEnabled || hasEscaped || !canBeHit)
                return false;

            foreach (Renderer visual in targetRenderers)
            {
                if (visual != null &&
                    visual.enabled &&
                    visual.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        targetColliders = GetComponentsInChildren<Collider>(true);
        targetRenderers = GetComponentsInChildren<Renderer>(true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
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
        {
            if (c != null)
                c.enabled = true;
        }
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
        if (!IsHittable)
            return false;

        canBeHit = false;

        Debug.Log("Acerto registrado no GhostTarget!");

        return true;
    }

    public void PlayRetreatAnimation()
    {
        if (animator != null)
            animator.SetTrigger(retreatTrigger);
        else
            Debug.LogWarning("GhostTarget: Animator não encontrado!", this);
    }

    public void MarkEscaped(float windowDuration)
    {
        if (hasEscaped || !canBeHit)
            return;

        hasEscaped = true;
        canBeHit = false;

        lateWindowActive = true;
        lateHitAlreadyCounted = false;

        lateWindowRoutine = StartCoroutine(
            EndLateWindow(Mathf.Max(0.1f, windowDuration))
        );
    }

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
        lateWindowRoutine = null;

        foreach (Collider c in targetColliders)
        {
            if (c != null)
                c.enabled = false;
        }
    }
}
