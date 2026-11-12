using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MoleRandomizer : MonoBehaviour
{
    [Header("Bichos já posicionados na cena")]
    [SerializeField] private GameObject[] moles = new GameObject[0];

    [Header("Tempos de aparição")]
    [SerializeField, Min(0f)] private float minDelayBeforeNextMole = 0.35f;
    [SerializeField, Min(0f)] private float maxDelayBeforeNextMole = 0.9f;
    [SerializeField, Min(0.1f)] private float visibleDuration = 1.25f;

    [Header("Animação de recolhimento")]
    [SerializeField, Min(0.1f)] private float retractDuration = 0.5f;

    [Header("Janela de clique atrasado")]
    [SerializeField, Min(0.1f)] private float lateClickWindow = 0.6f;

    [Header("Comportamento")]
    [SerializeField] private bool avoidSameMoleTwiceInARow = true;
    [SerializeField] private bool startAutomatically = true;

    [Header("Integração com pontuação")]
    [SerializeField] private UnityEvent onMoleHit = new UnityEvent();

    private Coroutine spawnRoutine;
    private GameObject currentMole;
    private int previousMoleIndex = -1;
    private bool currentMoleWasHit;

    public GameObject CurrentMole => currentMole;
    public bool IsRunning => spawnRoutine != null;

    private void Awake()
    {
        HideAllMoles();
    }

    private void OnEnable()
    {
        if (startAutomatically)
            StartSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    public void StartSpawning()
    {
        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnLoop());
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

    public bool TryRegisterHit(Collider hitCollider)
    {
        return hitCollider != null && TryRegisterHit(hitCollider.gameObject);
    }

    public bool TryRegisterHit(GameObject hitObject)
    {
        if (hitObject == null || currentMole == null ||
            currentMoleWasHit || !currentMole.activeInHierarchy)
            return false;

        Transform hitTransform = hitObject.transform;
        Transform currentTransform = currentMole.transform;

        if (hitTransform != currentTransform &&
            !hitTransform.IsChildOf(currentTransform))
            return false;

        GhostTarget target = currentMole.GetComponent<GhostTarget>();

        if (target == null || !target.TryHit())
            return false;

        currentMoleWasHit = true;
        onMoleHit?.Invoke();

        return true;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float minDelay = Mathf.Max(0f, minDelayBeforeNextMole);
            float maxDelay = Mathf.Max(minDelay, maxDelayBeforeNextMole);

            yield return new WaitForSeconds(
                Random.Range(minDelay, maxDelay)
            );

            int selectedIndex = ChooseNextMoleIndex();

            if (selectedIndex < 0)
            {
                Debug.LogError("MoleRandomizer: nenhum bicho atribuído.", this);
                spawnRoutine = null;
                yield break;
            }

            GameObject selectedMole = moles[selectedIndex];

            if (selectedMole == null)
                continue;

            currentMole = selectedMole;
            currentMoleWasHit = false;

            selectedMole.SetActive(true);

            float elapsed = 0f;

            while (elapsed < Mathf.Max(0.1f, visibleDuration)
                   && !currentMoleWasHit)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            GhostTarget ghostTarget =
                selectedMole.GetComponent<GhostTarget>();

            // Caso o jogador tenha acertado o fantasma.
            if (currentMoleWasHit)
            {
                if (ghostTarget != null)
                    ghostTarget.PlayRetreatAnimation();

                yield return new WaitForSeconds(retractDuration);

                selectedMole.SetActive(false);

                currentMole = null;
                currentMoleWasHit = false;
                previousMoleIndex = selectedIndex;

                continue;
            }

            // Caso o tempo de exposição tenha acabado.
            if (ghostTarget != null)
            {
                ghostTarget.MarkEscaped(lateClickWindow);
                ghostTarget.PlayRetreatAnimation();
            }

            currentMole = null;

            // Mantém o objeto ativo durante a animação
            // e durante a janela de clique atrasado.
            float waitDuration = Mathf.Max(
                retractDuration,
                lateClickWindow
            );

            yield return new WaitForSeconds(waitDuration);

            selectedMole.SetActive(false);

            currentMole = null;
            previousMoleIndex = selectedIndex;
        }
    }

    private int ChooseNextMoleIndex()
    {
        List<int> availableIndices = new List<int>();

        if (moles == null)
            return -1;

        for (int i = 0; i < moles.Length; i++)
        {
            if (moles[i] != null)
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
            return -1;

        if (avoidSameMoleTwiceInARow && availableIndices.Count > 1)
            availableIndices.Remove(previousMoleIndex);

        return availableIndices[
            Random.Range(0, availableIndices.Count)
        ];
    }

    private void HideAllMoles()
    {
        if (moles == null)
            return;

        foreach (GameObject mole in moles)
        {
            if (mole != null)
                mole.SetActive(false);
        }
    }

    private void OnValidate()
    {
        minDelayBeforeNextMole = Mathf.Max(0f, minDelayBeforeNextMole);
        maxDelayBeforeNextMole = Mathf.Max(
            minDelayBeforeNextMole,
            maxDelayBeforeNextMole
        );

        visibleDuration = Mathf.Max(0.1f, visibleDuration);
        retractDuration = Mathf.Max(0.1f, retractDuration);
        lateClickWindow = Mathf.Max(0.1f, lateClickWindow);
    }
}
