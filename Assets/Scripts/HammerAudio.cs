using UnityEngine;

public class HammerSounds : MonoBehaviour
{
    [SerializeField] private HammerCursor hammer;

    private void OnEnable()
    {
        hammer.OnGhostHit += _ => GameAudio.Instance.PlayHit();
        hammer.OnMiss += () => GameAudio.Instance.PlayMiss();
    }

    private void OnDisable()
    {
        // se preferir, guarde as lambdas em variáveis para remover aqui
    }
}