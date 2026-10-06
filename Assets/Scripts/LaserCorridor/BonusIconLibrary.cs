using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural pixel-art icons for bonus panels (no downloaded assets).
/// Boot = speed, cross = heal, shield = armor, bubble = invulnerability.
/// Sprites are cached and shared by all panels.
/// </summary>
public static class BonusIconLibrary
{
    static readonly Dictionary<BonusPanel.BonusType, Sprite> cache = new Dictionary<BonusPanel.BonusType, Sprite>();

    // Palette: K outline, W white, B boot brown, Y gold, S steel blue.
    static readonly Dictionary<char, Color32> palette = new Dictionary<char, Color32>
    {
        { 'K', new Color32(25, 25, 30, 255) },
        { 'W', new Color32(255, 255, 255, 255) },
        { 'B', new Color32(150, 95, 45, 255) },
        { 'Y', new Color32(255, 200, 40, 255) },
        { 'S', new Color32(150, 200, 235, 255) },
    };

    public static Sprite GetIcon(BonusPanel.BonusType type)
    {
        if (!cache.TryGetValue(type, out var sprite))
        {
            sprite = Build(RowsFor(type));
            cache[type] = sprite;
        }
        return sprite;
    }

    /// <summary>Creates the floating icon above a panel. Returns the icon object.</summary>
    public static GameObject AttachIcon(Transform panel, BonusPanel.BonusType type, float height = 1.35f, float size = 1.15f)
    {
        var sprite = GetIcon(type);
        Transform parent = panel.parent != null ? panel.parent : panel;
        var go = new GameObject($"Icon_{type}");
        go.transform.SetParent(parent, false);
        go.transform.position = panel.position + Vector3.up * height;
        go.transform.localScale = Vector3.one * size;
        var rend = go.AddComponent<SpriteRenderer>();
        rend.sprite = sprite;
        rend.color = Color.white;
        rend.enabled = true;
        rend.sortingOrder = 10;
        // Ensure it renders in URP + always faces camera via BonusIconFloat.
        var floaty = go.AddComponent<BonusIconFloat>();
        floaty.follow = panel;
        floaty.height = height;
        floaty.size = size;
        go.SetActive(true);
        return go;
    }

    static string[] RowsFor(BonusPanel.BonusType type)
    {
        switch (type)
        {
            case BonusPanel.BonusType.SpeedBoost: return Boot;
            case BonusPanel.BonusType.Heal: return Cross;
            case BonusPanel.BonusType.DamageReduction: return Shield;
            case BonusPanel.BonusType.Invulnerability: return Bubble;
            default: return Cross;
        }
    }

    // White health cross.
    static readonly string[] Cross = new string[]
    {
        "................",
        ".....WWWW.......",
        ".....WWWW.......",
        ".....WWWW.......",
        ".....WWWW.......",
        "..WWWWWWWWWW....",
        "..WWWWWWWWWW....",
        "..WWWWWWWWWW....",
        "..WWWWWWWWWW....",
        ".....WWWW.......",
        ".....WWWW.......",
        ".....WWWW.......",
        ".....WWWW.......",
        "................",
        "................",
        "................",
    };

    // Brown boot, side profile facing right.
    static readonly string[] Boot = new string[]
    {
        "................",
        "..KKKK..........",
        "..KBBBK.........",
        "..KBBBK.........",
        "..KBBBK.........",
        "..KBBBK.........",
        "..KBBBK.........",
        "..KBBBK.........",
        "..KBBBK.........",
        "..KBBBBBKKKK....",
        "..KBBBBBBBBBK...",
        "..KBBBBBBBBBBK..",
        "..KYYYYYYYYYYK..",
        "..KKKKKKKKKKKK..",
        "................",
        "................",
    };

    // Steel armor shield with highlight.
    static readonly string[] Shield = new string[]
    {
        "................",
        "..KKKKKKKKKKKK..",
        ".KSSSSSSSSSSSSK.",
        ".KSWWSSSSSSSSSK.",
        ".KSWWSSSSSSSSSK.",
        ".KSSSSSSSSSSSSK.",
        ".KSSSSSSSSSSSSK.",
        "..KSSSSSSSSSSK..",
        "..KSSSSSSSSSSK..",
        "...KSSSSSSSSK...",
        "....KSSSSSSK....",
        "....KSSSSSSK....",
        ".....KSSSSK.....",
        ".....KSSSSK.....",
        "......KSSK......",
        ".......KK.......",
    };

    // Gold invulnerability bubble.
    static readonly string[] Bubble = new string[]
    {
        "................",
        ".....KKKKKK.....",
        "...KKYYYYYYKK...",
        "..KYWWYYYYYYYK..",
        "..KYWYYYYYYYYK..",
        ".KYWYYYYYYYYYYK.",
        ".KYWYYYYYYYYYYK.",
        ".KYWYYYYYYYYYYK.",
        ".KYYYYYYYYYYYYK.",
        ".KYYYYYYYYYYYYK.",
        ".KYYYYYYYYYYYYK.",
        "..KYYYYYYYYYYK..",
        "..KYYYYYYYYYYK..",
        "...KKYYYYYYKK...",
        ".....KKKKKK.....",
        "................",
    };

    static Sprite Build(string[] rows)
    {
        int h = rows.Length;
        int w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int r = 0; r < h; r++)
        {
            string row = rows[r];
            for (int x = 0; x < w && x < row.Length; x++)
            {
                char c = row[x];
                Color32 col = c == '.' ? new Color32(0, 0, 0, 0)
                    : palette.TryGetValue(c, out var mapped) ? mapped : new Color32(255, 0, 255, 255);
                tex.SetPixel(x, h - 1 - r, col);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}
