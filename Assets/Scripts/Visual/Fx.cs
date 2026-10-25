using System.Collections.Generic;
using UnityEngine;

public class Fx : MonoBehaviour
{
    private Art art;
    private Material lineMaterial;

    public void Init()
    {
        art = Game.I.Art;
    }

    public void Sprite(string name, Vector3 pos, float scale = 1f, float duration = 0.4f, Color? tint = null,
        int order = 1600, float rotation = 0f, float rise = 0f, float drift = 0f, bool randomFlip = false)
    {
        var go = new GameObject("fx_" + name);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = art.World(name);
        sr.color = tint ?? Color.white;
        sr.sortingOrder = order;
        if (randomFlip) sr.flipX = Random.value > 0.5f;
        var fx = go.AddComponent<FxSprite>();
        fx.Init(sr, scale, duration, rise, drift);
    }

    public void Burst(string name, Vector3 pos, int count, float speed, float duration, float scale,
        Color? tint = null, int order = 1600)
    {
        for (int i = 0; i < count; i++)
        {
            float a = Random.value * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            var go = new GameObject("fx_burst");
            go.transform.position = pos + dir * Random.Range(0f, 0.15f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.World(name);
            sr.color = tint ?? Color.white;
            sr.sortingOrder = order;
            var fx = go.AddComponent<FxSprite>();
            fx.Init(sr, scale * Random.Range(0.65f, 1.15f), duration * Random.Range(0.75f, 1.2f), 0f, 0f);
            fx.Velocity = dir * speed * Random.Range(0.6f, 1.1f);
        }
    }

    public void Dust(Vector3 pos, int count = 6, float size = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            var p = pos + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0f, 0.18f), 0f);
            var go = new GameObject("fx_dust");
            go.transform.position = p;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.World("fx_dust");
            sr.color = new Color(0.85f, 0.76f, 0.6f, 0.55f);
            sr.sortingOrder = 1550;
            var fx = go.AddComponent<FxSprite>();
            fx.Init(sr, size * Random.Range(0.55f, 1.1f), Random.Range(0.35f, 0.65f), 0.1f, 0.2f);
            fx.Velocity = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.2f, 0.8f), 0f);
        }
    }

    public void Explosion(Vector3 pos, float radius, Color? tint = null)
    {
        Color c = tint ?? new Color(1f, 0.72f, 0.35f, 1f);
        var glow = new GameObject("fx_explosion_glow");
        glow.transform.position = pos;
        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite = art.World("fx_glow");
        sr.color = new Color(c.r, c.g, c.b, 0.75f);
        sr.sortingOrder = 1580;
        var f = glow.AddComponent<FxSprite>();
        f.Init(sr, radius / 1.35f, 0.42f, 0f, 0f);
        Sprite("fx_explosion", pos, radius / 1.2f, 0.42f, c, 1610, Random.Range(0f, 360f));
        Ring(pos, c, radius, 0.35f);
        Dust(pos, 8, radius * 0.35f);
        Game.I.Shake(0.18f + radius * 0.03f, 0.3f);
    }

    public void Ring(Vector3 pos, Color color, float radius, float duration = 0.45f)
    {
        var go = new GameObject("fx_ring");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = art.World("fx_ring");
        sr.color = color;
        sr.sortingOrder = 1570;
        float w = Mathf.Max(0.001f, sr.sprite.bounds.size.x);
        var fx = go.AddComponent<FxSprite>();
        fx.Init(sr, radius * 2f / w, duration, 0f, 0f);
        fx.Grow = radius * 0.55f;
    }

    public void Slash(Vector3 pos, Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Sprite("fx_slash", pos, 0.8f, 0.18f, new Color(1f, 0.96f, 0.85f, 0.95f), 1660, angle, 0f, 0f);
    }

    public void Heal(Vector3 pos)
    {
        Ring(pos, new Color(0.5f, 0.95f, 0.55f, 0.9f), 0.7f, 0.6f);
        Burst("fx_flame", pos, 6, 1.1f, 0.55f, 0.45f, new Color(0.6f, 1f, 0.65f, 0.9f), 1650);
        FloatingText(pos + Vector3.up * 1.1f, "+", new Color(0.6f, 1f, 0.65f), 0.7f);
    }

    public void Shield(Transform target, float duration)
    {
        var go = new GameObject("fx_shield");
        go.transform.SetParent(target, false);
        go.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = art.World("fx_shield");
        sr.color = new Color(0.7f, 0.9f, 1f, 0.9f);
        sr.sortingOrder = 1590;
        float w = sr.sprite.bounds.size.x;
        go.transform.localScale = Vector3.one * (1.15f / w);
        var pulse = go.AddComponent<ShieldFx>();
        pulse.Init(duration);
    }

    public void Afterimage(SpriteRenderer source)
    {
        if (source == null) return;
        var go = new GameObject("afterimage");
        go.transform.position = source.transform.position;
        go.transform.rotation = source.transform.rotation;
        go.transform.localScale = source.transform.lossyScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.color = new Color(0.45f, 0.75f, 0.9f, 0.35f);
        sr.sortingOrder = source.sortingOrder - 1;
        var fx = go.AddComponent<FxSprite>();
        fx.Init(sr, 1f, 0.25f, 0f, 0f);
    }

    public void FloatingText(Vector3 pos, string text, Color color, float size = 1f, bool crit = false)
    {
        var go = new GameObject("dmg");
        go.transform.position = pos + new Vector3(Random.Range(-0.12f, 0.12f), 0.2f, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = art.Serif;
        tm.fontSize = crit ? 80 : 64;
        tm.characterSize = 0.075f * size;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = art.Serif.material;
        mr.sortingOrder = 2000;
        var f = go.AddComponent<FxText>();
        f.Init(tm, crit ? 1.5f : 1f);
    }

    public void Line(Vector3 a, Vector3 b, Color color, float width = 0.12f, float duration = 0.25f)
    {
        var go = new GameObject("fx_line");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = width;
        lr.endWidth = width * 0.4f;
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lineMaterial.name = "Cardboard Fx Line";
        }
        lr.sharedMaterial = lineMaterial;
        lr.startColor = color;
        lr.endColor = new Color(color.r, color.g, color.b, 0.2f);
        lr.sortingOrder = 1700;
        var f = go.AddComponent<LineFx>();
        f.Init(lr, duration);
    }

    public void TelegraphedCircle(Vector3 pos, float radius, float duration, Color? tint = null)
    {
        Color c = tint ?? new Color(1f, 0.35f, 0.3f, 0.8f);
        var go = new GameObject("fx_telegraph");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = art.World("fx_glow");
        sr.color = new Color(c.r, c.g, c.b, 0.25f);
        sr.sortingOrder = 1560;
        float w = sr.sprite.bounds.size.x;
        go.transform.localScale = Vector3.one * (radius * 2f / w);
        var f = go.AddComponent<TelegraphFx>();
        f.Init(sr, duration, radius);
    }

    private void OnDestroy()
    {
        if (lineMaterial != null) Destroy(lineMaterial);
    }
}

public class FxSprite : MonoBehaviour
{
    private SpriteRenderer sr;
    private float life;
    private float age;
    private float rise;
    private float drift;
    private float grow;
    private Vector3 startScale;
    private Color color;
    private float spin;

    public Vector3 Velocity;

    public float Grow
    {
        set => grow = value;
    }

    public void Init(SpriteRenderer renderer, float scale, float duration, float riseSpeed, float driftSpeed)
    {
        sr = renderer;
        life = Mathf.Max(0.01f, duration);
        age = 0f;
        rise = riseSpeed;
        drift = driftSpeed;
        startScale = Vector3.one * scale;
        transform.localScale = startScale;
        color = sr.color;
        spin = Random.Range(-40f, 40f);
    }

    private void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / life);
        transform.position += new Vector3(Velocity.x, Velocity.y + rise, 0f) * Time.deltaTime;
        Velocity = Vector3.Lerp(Velocity, new Vector3(Random.Range(-drift, drift), Velocity.y * 0.9f, 0f), Time.deltaTime * 2f);
        transform.Rotate(0f, 0f, spin * Time.deltaTime);
        transform.localScale = startScale * (1f + grow * k);
        var c = color;
        c.a = color.a * (1f - k);
        sr.color = c;
        if (age >= life) Destroy(gameObject);
    }
}

public class FxText : MonoBehaviour
{
    private TextMesh text;
    private float age;
    private float life = 0.8f;
    private float size = 1f;
    private Color color;
    private Vector3 start;

    public void Init(TextMesh tm, float scale)
    {
        text = tm;
        size = scale;
        color = tm.color;
        start = transform.position;
        life = 0.75f;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / life);
        transform.position = start + Vector3.up * (k * 0.8f);
        var c = color;
        c.a = 1f - k * k;
        text.color = c;
        transform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.18f * size);
        if (age >= life) Destroy(gameObject);
    }
}

public class ShieldFx : MonoBehaviour
{
    private float duration;
    private float age;
    private SpriteRenderer sr;

    public void Init(float time)
    {
        duration = time;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / duration);
        float a = Mathf.Sin(Time.time * 5f) * 0.12f + 0.35f;
        sr.color = new Color(0.7f, 0.9f, 1f, k > 0.85f ? (1f - k) * 2f : a);
        transform.localScale = Vector3.one * (1.15f / Mathf.Max(0.01f, sr.sprite.bounds.size.x)) * (1f + Mathf.Sin(Time.time * 4f) * 0.03f);
        if (age >= duration) Destroy(gameObject);
    }
}

public class LineFx : MonoBehaviour
{
    private LineRenderer lr;
    private float duration;
    private float age;
    private Color a;
    private Color b;

    public void Init(LineRenderer line, float time)
    {
        lr = line;
        duration = time;
        a = lr.startColor;
        b = lr.endColor;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / duration);
        lr.startColor = new Color(a.r, a.g, a.b, a.a * (1f - k));
        lr.endColor = new Color(b.r, b.g, b.b, b.a * (1f - k));
        if (age >= duration) Destroy(gameObject);
    }
}

public class TelegraphFx : MonoBehaviour
{
    private SpriteRenderer sr;
    private float duration;
    private float age;

    public void Init(SpriteRenderer renderer, float time, float radius)
    {
        sr = renderer;
        duration = Mathf.Max(0.05f, time);
    }

    private void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / duration);
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.16f + k * 0.25f);
        if (age >= duration) Destroy(gameObject);
    }
}
