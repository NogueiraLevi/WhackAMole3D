using UnityEngine;
using UnityEngine.UI;

// Draws a heart directly in the HUD, independently of the font's available glyphs.
public class HeartIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        const int segments = 64;
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        vertexHelper.AddVert(center, color, Vector2.zero);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float sine = Mathf.Sin(angle);
            float x = 16f * sine * sine * sine;
            float y = 13f * Mathf.Cos(angle) - 5f * Mathf.Cos(2f * angle)
                - 2f * Mathf.Cos(3f * angle) - Mathf.Cos(4f * angle);

            Vector2 point = center + new Vector2(x / 32f * rect.width, (y + 2f) / 34f * rect.height);
            vertexHelper.AddVert(point, color, Vector2.zero);
            vertexHelper.AddTriangle(0, i + 1, (i + 1) % segments + 1);
        }
    }
}
