using System.Collections.Generic;
using UnityEngine;

public class Art
{
    public const float WorldPpu = 190f;
    public const float UiPpu = 100f;

    private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private readonly HashSet<string> missingWarned = new HashSet<string>();
    private Texture2D missingTexture;

    public Font Serif;
    public Font SerifRegular;
    public Font Sans;
    public Font SansBold;

    public Sprite WhiteWorld { get; private set; }
    public Sprite WhiteUi { get; private set; }
    public Sprite Shadow { get; private set; }

    public void Init()
    {
        foreach (var tex in Resources.LoadAll<Texture2D>("Art"))
        {
            textures[tex.name] = tex;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 1;
        }

        Serif = LoadFont("Fonts/PT_Serif-Bold");
        SerifRegular = LoadFont("Fonts/PT_Serif-Regular");
        Sans = LoadFont("Fonts/PT_Sans-Regular");
        SansBold = LoadFont("Fonts/PT_Sans-Bold");
        if (Serif == null) Serif = BuiltinFont();
        if (SerifRegular == null) SerifRegular = Serif;
        if (Sans == null) Sans = Serif;
        if (SansBold == null) SansBold = Serif;

        WhiteWorld = World("white");
        WhiteUi = UI("white");
        Shadow = World("fx_shadow");
        Debug.Log("[Cardboard] Art loaded: " + textures.Count + " textures");
    }

    private static Font LoadFont(string path)
    {
        var f = Resources.Load<Font>(path);
        if (f == null) Debug.LogWarning("[Cardboard] Missing font " + path);
        return f;
    }

    private static Font BuiltinFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    public Texture2D Tex(string name)
    {
        if (textures.TryGetValue(name, out var t)) return t;
        if (name == "white") return Texture2D.whiteTexture;

        if (missingWarned.Add(name))
        {
            Debug.LogError("[Cardboard] Missing texture: " + name);
        }
        if (missingTexture == null) missingTexture = CreateMissingTexture();
        return missingTexture;
    }

    private static Texture2D CreateMissingTexture()
    {
        const int size = 8;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "missing_texture";
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool light = ((x / 2) + (y / 2)) % 2 == 0;
                tex.SetPixel(x, y, light ? new Color(1f, 0f, 1f, 1f) : new Color(0.12f, 0f, 0.12f, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    public Sprite World(string name)
    {
        return Make(name, WorldPpu, new Vector2(0.5f, 0.5f), Vector4.zero, false);
    }

    public Sprite Actor(string name)
    {
        return Make(name, WorldPpu, new Vector2(0.5f, 0.08f), Vector4.zero, false);
    }

    public Sprite Prop(string name)
    {
        return Make(name, WorldPpu, new Vector2(0.5f, 0.10f), Vector4.zero, false);
    }

    public Sprite Pivot(string name, float x, float y)
    {
        return Make(name, WorldPpu, new Vector2(x, y), Vector4.zero, false);
    }

    public Sprite Tiled(string name)
    {
        var s = Make(name, WorldPpu, new Vector2(0.5f, 0.5f), Vector4.zero, true);
        return s;
    }

    public Sprite UI(string name, Vector4 border = default)
    {
        return Make(name, UiPpu, new Vector2(0.5f, 0.5f), border, false);
    }

    private Sprite Make(string name, float ppu, Vector2 pivot, Vector4 border, bool repeat)
    {
        string key = name + "|" + ppu + "|" + pivot.x + "|" + pivot.y + "|" + border + "|" + repeat;
        if (sprites.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = Tex(name);
        if (tex == null)
        {
            Debug.LogError("[Cardboard] Missing texture: " + name);
            return null;
        }
        if (repeat)
        {
            tex.wrapMode = TextureWrapMode.Repeat;
        }
        var rect = new Rect(0, 0, tex.width, tex.height);
        var sprite = Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect, border);
        sprite.name = name;
        sprites[key] = sprite;
        return sprite;
    }

    public Color ColorOf(string id)
    {
        switch (id)
        {
            case "fireball":
            case "meteor":
            case "rage":
                return Pal.Red;
            case "shield":
                return Pal.Blue;
            case "nova":
            case "blink":
                return Pal.Teal;
            case "chain":
                return Pal.Gold;
            case "heal":
                return Pal.Red;
            case "poison":
                return Pal.Green;
            case "missiles":
            case "slow":
                return Pal.Purple;
            default:
                return Pal.Cream;
        }
    }
}
