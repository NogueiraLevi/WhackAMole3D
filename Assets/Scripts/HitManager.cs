using UnityEngine;

public class HitManager : MonoBehaviour
{
    [Header("Contadores")]
    private int missedTargets = 0;
    private int targetsTooLate = 0;
    private int successfulHits = 0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip missedTargetClip;
    [SerializeField] private AudioClip targetTooLateClip;
    [SerializeField] private AudioClip hitClip;

    public void RegisterMissedTarget()
    {
        missedTargets++;

        Debug.Log("MissedTarget! Erros de mira: " + missedTargets);

        PlaySound(missedTargetClip);
    }

    public void RegisterTargetTooLate()
    {
        targetsTooLate++;

        Debug.Log("TargetTooLate! Cliques atrasados: " + targetsTooLate);

        PlaySound(targetTooLateClip);
    }

    public void RegisterHit()
    {
        successfulHits++;

        Debug.Log("Acerto! Total de acertos: " + successfulHits);

        PlaySound(hitClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogWarning(
                "HitManager: configure o AudioSource no Inspector."
            );
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning(
                "HitManager: falta configurar um clipe de áudio."
            );
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    public int GetMissedTargets()
    {
        return missedTargets;
    }

    public int GetTargetsTooLate()
    {
        return targetsTooLate;
    }

    public int GetSuccessfulHits()
    {
        return successfulHits;
    }
}
