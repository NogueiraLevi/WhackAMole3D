using UnityEngine;

public class GhostParticleController : MonoBehaviour
{
    [Header("Partículas")]
    [SerializeField] private Transform particleParent;

    [SerializeField] private ParticleSystem[] particles;

    private ParticleSystem[] activeParticles;

    private void Awake()
    {
        // Se um objeto pai foi atribuído,
        // busca todos os Particle Systems dentro dele.
        if (particleParent != null)
        {
            activeParticles =
                particleParent.GetComponentsInChildren<ParticleSystem>(true);
        }
        // Caso contrário, utiliza as partículas
        // atribuídas individualmente no Inspector.
        else
        {
            activeParticles = particles;
        }
    }

    public void PlaySkullParticles()
    {
        if (activeParticles == null || activeParticles.Length == 0)
        {
            Debug.LogWarning("Nenhuma partícula foi configurada!");
            return;
        }

        foreach (ParticleSystem particle in activeParticles)
        {
            if (particle == null)
                continue;

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            particle.Play();
        }
    }
}
