using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Randomly shows one of the character instances already placed in the Game scene.
/// This keeps the current scene positions and models created by the team.
/// </summary>
public class MoleRandomizer : MonoBehaviour
{
    [Header("Bichos já posicionados na cena")]
    [Tooltip("Arraste aqui os cinco objetos Ghost_kid da cena Assets/Scenes/Game.unity.")]
    [SerializeField] private GameObject[] moles = new GameObject[0];

    [Header("Tempos de aparição")]
    [Tooltip("Tempo mínimo de espera antes de aparecer o próximo bicho, em segundos.")]
    [SerializeField, Min(0f)] private float minDelayBeforeNextMole = 0.35f;

    [Tooltip("Tempo máximo de espera antes de aparecer o próximo bicho, em segundos.")]
    [SerializeField, Min(0f)] private float maxDelayBeforeNextMole = 0.9f;

    [Tooltip("Quanto tempo o bicho fica disponível antes de desaparecer, em segundos.")]
    [SerializeField, Min(0.1f)] private float visibleDuration = 1.25f;

    [Header("Comportamento")]
    [Tooltip("Evita escolher o mesmo bicho duas vezes seguidas, quando houver mais de um válido.")]
    [SerializeField] private bool avoidSameMoleTwiceInARow = true;

    [Tooltip("Inicia o sorteio automaticamente quando a cena Game começa.")]
    [SerializeField] private bool startAutomatically = true;

    [Header("Integração com pontuação")]
    [Tooltip("Evento disparado quando TryRegisterHit confirma um acerto. Pode ser conectado à pontuação no Inspector.")]
    [SerializeField] private UnityEvent onMoleHit = new UnityEvent();

    private Coroutine spawnRoutine;
    private GameObject currentMole;
    private int previousMoleIndex = -1;
    private bool currentMoleWasHit;

    /// <summary>The currently visible character, or null when none is visible.</summary>
    public GameObject CurrentMole => currentMole;

    public bool IsRunning => spawnRoutine != null;

    private void Awake()
    {
        HideAllMoles();
    }

    private void OnEnable()
    {
        if (startAutomatically)
        {
            StartSpawning();
        }
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    public void StartSpawning()
    {
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        currentMole = null;
        currentMoleWasHit = false;
        HideAllMoles();
    }

    /// <summary>
    /// Call this from the hammer/raycast code. Returns true only when the hit
    /// belongs to the character that is currently visible.
    /// </summary>
    public bool TryRegisterHit(Collider hitCollider)
    {
        return hitCollider != null && TryRegisterHit(hitCollider.gameObject);
    }

    /// <summary>GameObject overload for callers that do not have a Collider reference.</summary>
    public bool TryRegisterHit(GameObject hitObject)
    {
        if (hitObject == null || currentMole == null || currentMoleWasHit || !currentMole.activeInHierarchy)
        {
            return false;
        }

        Transform hitTransform = hitObject.transform;
        Transform currentTransform = currentMole.transform;

        // The collider may be on a child object inside the character prefab.
        if (hitTransform != currentTransform && !hitTransform.IsChildOf(currentTransform))
        {
            return false;
        }

        currentMoleWasHit = true;
        currentMole.SetActive(false);
        onMoleHit?.Invoke();
        return true;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float minDelay = Mathf.Max(0f, minDelayBeforeNextMole);
            float maxDelay = Mathf.Max(minDelay, maxDelayBeforeNextMole);
            float delay = Random.Range(minDelay, maxDelay);

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            int selectedIndex = ChooseNextMoleIndex();
            if (selectedIndex < 0)
            {
                Debug.LogError(
                    "MoleRandomizer: nenhum bicho foi atribuído. Adicione os objetos Ghost_kid no array Moles do Inspector.",
                    this);
                spawnRoutine = null;
                yield break;
            }

            GameObject selectedMole = moles[selectedIndex];
            if (selectedMole == null)
            {
                continue;
            }

            currentMole = selectedMole;
            currentMoleWasHit = false;
            selectedMole.SetActive(true);

            float elapsed = 0f;
            float allowedVisibleTime = Mathf.Max(0.1f, visibleDuration);

            while (elapsed < allowedVisibleTime && !currentMoleWasHit)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (selectedMole != null)
            {
                selectedMole.SetActive(false);
            }

            currentMole = null;
            previousMoleIndex = selectedIndex;
        }
    }

    private int ChooseNextMoleIndex()
    {
        List<int> availableIndices = new List<int>();

        if (moles == null)
        {
            return -1;
        }

        for (int i = 0; i < moles.Length; i++)
        {
            if (moles[i] != null)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0)
        {
            return -1;
        }

        if (avoidSameMoleTwiceInARow && availableIndices.Count > 1)
        {
            availableIndices.Remove(previousMoleIndex);
        }

        return availableIndices[Random.Range(0, availableIndices.Count)];
    }

    private void HideAllMoles()
    {
        if (moles == null)
        {
            return;
        }

        foreach (GameObject mole in moles)
        {
            if (mole != null)
            {
                mole.SetActive(false);
            }
        }
    }

    private void OnValidate()
    {
        minDelayBeforeNextMole = Mathf.Max(0f, minDelayBeforeNextMole);
        maxDelayBeforeNextMole = Mathf.Max(minDelayBeforeNextMole, maxDelayBeforeNextMole);
        visibleDuration = Mathf.Max(0.1f, visibleDuration);
    }
}
