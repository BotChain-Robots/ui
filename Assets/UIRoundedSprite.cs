using UnityEngine;

/// <summary>
/// Shared utility for creating a rounded-rectangle sprite used by UI panels and buttons.
/// </summary>
public static class UIRoundedSprite
{
    private static Sprite _roundedRectSprite;

    /// <summary>
    /// Returns a cached white rounded-rectangle sprite (9-slice friendly, scales well).
    /// </summary>
    public static Sprite GetRoundedRectSprite()
    {
        if (_roundedRectSprite != null)
            return _roundedRectSprite;

        const int size = 64;
        const int radius = 12;
        var tex = new Texture2D(size, size);
        var pixels = new Color32[size * size];
        float r = radius - 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x < r ? r - x : (x >= size - r ? x - (size - 1 - r) : 0);
            float dy = y < r ? r - y : (y >= size - r ? y - (size - 1 - r) : 0);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(255 * (1f - Mathf.Clamp01((d - r) / 1.5f))), 0, 255);
            pixels[y * size + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply(true, true);
        tex.filterMode = FilterMode.Bilinear;
        // Border for 9-slice: keeps corners fixed so they don't stretch on tall/wide panels
        var border = new Vector4(radius, radius, radius, radius);
        _roundedRectSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        return _roundedRectSprite;
    }
}
