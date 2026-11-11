using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class LeaderboardScreen
{
    public static void Show()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.transform.Find("LeaderboardScreen") != null)
            return;

        GameObject previousSelection = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;
        TMP_FontAsset font = canvas.GetComponentInChildren<TMP_Text>(true).font;

        RectTransform screen = CreateRect("LeaderboardScreen", canvas.transform, Vector2.zero, Vector2.zero);
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        var background = screen.gameObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.04f, 0.10f, 0.98f);

        AddText(screen, font, "GHOST BONK!", 330, 30, new Color(0.65f, 1f, 0.87f));
        AddText(screen, font, "MELHORES PONTUAÇÕES", 235, 56, new Color(1f, 0.71f, 0.37f));

        int[] scores = LocalLeaderboard.GetScores();
        for (int i = 0; i < scores.Length; i++)
        {
            float y = 85 - i * 110;
            RectTransform row = CreateRect("Rank " + (i + 1), screen, new Vector2(0, y), new Vector2(760, 86));
            row.gameObject.AddComponent<Image>().color = new Color(0.19f, 0.13f, 0.27f);
            AddText(row, font, $"{i + 1}º LUGAR        {scores[i]} PONTOS", 0, 36, Color.white);
        }

        AddText(screen, font, "SEUS 3 MELHORES RESULTADOS NESTE DISPOSITIVO", -245, 22,
            new Color(0.68f, 0.57f, 0.76f));

        RectTransform back = CreateRect("BackButton", screen, new Vector2(0, -340), new Vector2(530, 82));
        Image backImage = back.gameObject.AddComponent<Image>();
        backImage.color = new Color(0.89f, 0.47f, 0.15f);
        Button button = back.gameObject.AddComponent<Button>();
        button.targetGraphic = backImage;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        AddText(back, font, "VOLTAR", 0, 30, Color.white);
        button.onClick.AddListener(() =>
        {
            Object.Destroy(screen.gameObject);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection);
        });
        button.Select();
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

    private static void AddText(Transform parent, TMP_FontAsset font, string text, float y, float size, Color color)
    {
        RectTransform rect = CreateRect(text, parent, new Vector2(0, y), new Vector2(1150, 80));
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }
}
