using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Canvas hudCanvas;
    private ArcadeHud arcadeHud;
    [SerializeField] private Behaviour[] gameplayControls;

    private GameObject panel;
    private Button pauseButton;
    private Button continueButton;
    private TMP_FontAsset font;
    private bool[] previousControlStates;
    private float previousTimeScale;
    private bool previousAudioPause;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private bool gameplayCursorVisible;

    public bool IsPaused { get; private set; }

    private void Start()
    {
        arcadeHud = GetComponent<ArcadeHud>();
        font = hudCanvas.GetComponentInChildren<TMP_Text>(true).font;
        gameplayCursorVisible = Cursor.visible;
        pauseButton = CreateButton(hudCanvas.transform, "PauseButton", "PAUSAR", Vector2.zero, Pause);
        arcadeHud.PlacePauseButton(pauseButton);

        RectTransform panelRect = CreateRect("PausePanel", hudCanvas.transform, Vector2.zero, Vector2.zero);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.10f, 0.96f);
        panel = panelRect.gameObject;
        CreateText(panel.transform, "PARTIDA PAUSADA", new Vector2(0, 165), 58);
        continueButton = CreateButton(panel.transform, "ContinueButton", "CONTINUAR PARTIDA", new Vector2(0, 10), Resume);
        CreateButton(panel.transform, "LeaveButton", "SAIR DA PARTIDA", new Vector2(0, -100), LeaveMatch);
        panel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (IsPaused || Mouse.current == null)
            return;

        // A marreta fica limitada às paredes da máquina. No HUD, mostre o cursor
        // real para que o jogador consiga localizar e clicar no botão de pausa.
        bool pointerInHud = arcadeHud.ContainsScreenPoint(Mouse.current.position.ReadValue());
        Cursor.visible = pointerInHud || gameplayCursorVisible;
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        previousControlStates = new bool[gameplayControls.Length];
        for (int i = 0; i < gameplayControls.Length; i++)
        {
            if (gameplayControls[i] == null)
                continue;

            previousControlStates[i] = gameplayControls[i].enabled;
            gameplayControls[i].enabled = false;
        }

        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseButton.interactable = false;
        panel.SetActive(true);
        continueButton.Select();
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        panel.SetActive(false);
        RestoreGameState();
        pauseButton.interactable = true;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
    }

    public void LeaveMatch()
    {
        RestoreGameState();
        SceneManager.LoadScene("MainMenu");
    }

    private void RestoreGameState()
    {
        if (!IsPaused)
            return;

        IsPaused = false;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        for (int i = 0; i < gameplayControls.Length; i++)
            if (gameplayControls[i] != null)
                gameplayControls[i].enabled = previousControlStates[i];

        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLock;
    }

    private void OnDisable()
    {
        RestoreGameState();
    }

    private void OnDestroy()
    {
        if (panel != null) Destroy(panel);
        if (pauseButton != null) Destroy(pauseButton.gameObject);
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 position,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateRect(name, parent, position, new Vector2(560, 82));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.45f, 0.25f, 0.60f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        CreateText(rect, label, Vector2.zero, 28);
        return button;
    }

    private void CreateText(Transform parent, string text, Vector2 position, float fontSize)
    {
        RectTransform rect = CreateRect(text, parent, position, new Vector2(1100, 80));
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.92f, 0.84f);
        label.raycastTarget = false;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var element = new GameObject(name, typeof(RectTransform));
        element.layer = parent.gameObject.layer;
        var rect = (RectTransform)element.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
