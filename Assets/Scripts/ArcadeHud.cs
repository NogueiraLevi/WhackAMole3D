using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArcadeHud : MonoBehaviour
{
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Graphic[] lifeHearts;

    private RectTransform bar;
    private readonly Color mutedText = new Color(0.65f, 0.61f, 0.73f);
    private readonly Color amber = new Color(1f, 0.72f, 0.38f);

    private void Awake()
    {
        bar = CreateRect("Arcade HUD", hudCanvas.transform, Vector2.zero, Vector2.zero);
        bar.anchorMin = new Vector2(0, 1);
        bar.anchorMax = Vector2.one;
        bar.pivot = new Vector2(0.5f, 1);
        bar.offsetMin = new Vector2(48, -156);
        bar.offsetMax = new Vector2(-48, -32);
        bar.gameObject.AddComponent<Image>().color = new Color(0.045f, 0.035f, 0.075f, 1f);

        RectTransform accent = CreateRect("Neon underline", bar, Vector2.zero, Vector2.zero);
        accent.anchorMin = Vector2.zero;
        accent.anchorMax = new Vector2(1, 0);
        accent.pivot = new Vector2(0.5f, 0);
        accent.sizeDelta = new Vector2(0, 2);
        var accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = new Color(0.52f, 0.30f, 0.67f, 0.7f);
        accentImage.raycastTarget = false;

        CreateLabel("TEMPO DE JOGO", 0.13f);
        PlaceValue(timerText, 0.13f, 270, 44, new Color(0.65f, 1f, 0.87f));
        CreateLabel("PONTOS", 0.48f).color = amber;
        PlaceValue(scoreText, 0.48f, 440, 54, amber);
        scoreText.characterSpacing = 5;

        CreateLabel("VIDAS", 0.76f);
        for (int i = 0; i < lifeHearts.Length; i++)
        {
            RectTransform heart = lifeHearts[i].rectTransform;
            heart.SetParent(bar, false);
            heart.anchorMin = heart.anchorMax = new Vector2(0.76f, 1);
            heart.pivot = new Vector2(0.5f, 0.5f);
            heart.anchoredPosition = new Vector2((i - 1) * 48, -78);
            heart.sizeDelta = new Vector2(34, 34);
            lifeHearts[i].raycastTarget = false;
        }

        CreateSeparator(0.28f);
        CreateSeparator(0.65f);
        CreateSeparator(0.86f);
    }

    public void PlacePauseButton(Button button)
    {
        var rect = (RectTransform)button.transform;
        rect.SetParent(bar, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.93f, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -65);
        rect.sizeDelta = new Vector2(140, 52);
        button.GetComponent<Image>().color = new Color(0.13f, 0.10f, 0.18f);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.rectTransform.sizeDelta = rect.sizeDelta;
        label.fontSize = 21;
        label.fontStyle = FontStyles.Normal;
        label.color = new Color(0.85f, 0.81f, 0.91f);
    }

    public bool ContainsScreenPoint(Vector2 position)
    {
        Camera camera = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(bar, position, camera);
    }

    private TMP_Text CreateLabel(string text, float horizontalPosition)
    {
        RectTransform rect = CreateRect(text, bar, new Vector2(0, -28), new Vector2(280, 30));
        rect.anchorMin = rect.anchorMax = new Vector2(horizontalPosition, 1);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = timerText.font;
        label.text = text;
        label.fontSize = 20;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 3;
        label.alignment = TextAlignmentOptions.Center;
        label.color = mutedText;
        label.raycastTarget = false;
        return label;
    }

    private void PlaceValue(TMP_Text text, float horizontalPosition, float width, float fontSize, Color color)
    {
        RectTransform rect = text.rectTransform;
        rect.SetParent(bar, false);
        rect.anchorMin = rect.anchorMax = new Vector2(horizontalPosition, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -78);
        rect.sizeDelta = new Vector2(width, 64);
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = color;
        text.raycastTarget = false;
    }

    private void CreateSeparator(float horizontalPosition)
    {
        RectTransform rect = CreateRect("Divider", bar, new Vector2(0, -62), new Vector2(1, 64));
        rect.anchorMin = rect.anchorMax = new Vector2(horizontalPosition, 1);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.4f, 0.3f, 0.5f, 0.35f);
        image.raycastTarget = false;
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
