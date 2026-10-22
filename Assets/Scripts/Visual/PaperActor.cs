using System.Collections;
using UnityEngine;

public class PaperActor : MonoBehaviour
{
    public SpriteRenderer Main;
    public SpriteRenderer Shadow;
    public SpriteRenderer Aura;
    public Transform Visual;

    public bool Walking;
    public bool IsDead;
    public float YSortBias;
    public float BobAmount = 0.025f;
    public float BobSpeed = 3f;
    public float TiltAmount = 1.6f;

    private float seed;
    private Color baseColor;
    private Vector3 baseScale;
    private Coroutine flashRoutine;
    private float spawnScale = 1f;

    public static PaperActor Create(Transform parent, Art art, string spriteName, float scale, Color tint,
        bool shadow = true, bool prop = false)
    {
        var go = new GameObject(spriteName);
        go.transform.SetParent(parent, false);
        var actor = go.AddComponent<PaperActor>();
        actor.Build(art, spriteName, scale, tint, shadow, prop);
        return actor;
    }

    public void Build(Art art, string spriteName, float scale, Color tint, bool shadow, bool prop)
    {
        seed = Random.value * 6.28f;
        baseColor = tint;
        spawnScale = scale;

        var visualGo = new GameObject("Visual");
        Visual = visualGo.transform;
        Visual.SetParent(transform, false);
        Visual.localScale = Vector3.one * scale;
        baseScale = Visual.localScale;

        Main = visualGo.AddComponent<SpriteRenderer>();
        Main.sprite = prop ? art.Prop(spriteName) : art.Actor(spriteName);
        if (Main.sprite == null) Main.sprite = art.Actor("wizard");
        Main.color = tint;
        Main.sortingOrder = 1000;

        if (shadow)
        {
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(transform, false);
            sh.transform.localPosition = new Vector3(0f, 0.035f, 0.02f);
            Shadow = sh.AddComponent<SpriteRenderer>();
            Shadow.sprite = art.Shadow;
            Shadow.color = new Color(0.05f, 0.03f, 0.05f, 0.48f);
            Shadow.sortingOrder = 999;
            float sw = Main.sprite.bounds.size.x;
            float shW = Shadow.sprite.bounds.size.x;
            float k = sw * 0.72f / Mathf.Max(0.01f, shW);
            sh.transform.localScale = new Vector3(k, k * 0.95f, 1f);
            sh.transform.localPosition = new Vector3(0f, 0.02f, 0.02f);
        }
    }

    public void AddAura(Art art, Color color, float scale = 1.3f)
    {
        if (Aura != null) return;
        var go = new GameObject("Aura");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.02f, 0.03f);
        Aura = go.AddComponent<SpriteRenderer>();
        Aura.sprite = art.World("fx_ring_gold");
        Aura.color = new Color(color.r, color.g, color.b, 0.55f);
        Aura.sortingOrder = 998;
        go.transform.localScale = new Vector3(scale, scale * 0.45f, 1f);
    }

    public void SetScale(float scale)
    {
        spawnScale = scale;
        if (Visual != null) Visual.localScale = Vector3.one * scale;
        baseScale = Vector3.one * scale;
    }

    public void SetTint(Color color)
    {
        baseColor = color;
        if (Main != null && flashRoutine == null) Main.color = color;
    }

    public void SetVisible(bool visible)
    {
        if (Main != null) Main.enabled = visible;
        if (Shadow != null) Shadow.enabled = visible && !IsDead;
        if (Aura != null) Aura.enabled = visible;
    }

    public void ResetVisual()
    {
        IsDead = false;
        if (Main != null)
        {
            Main.enabled = true;
            Main.color = baseColor;
        }
        if (Shadow != null) Shadow.enabled = true;
        if (Aura != null) Aura.enabled = true;
        if (Visual != null)
        {
            Visual.localRotation = Quaternion.identity;
            Visual.localPosition = Vector3.zero;
            Visual.localScale = baseScale;
        }
    }

    public void Face(float dirX)
    {
        if (Main != null && Mathf.Abs(dirX) > 0.05f) Main.flipX = dirX < 0f;
    }

    public void FaceTo(Vector2 target)
    {
        Face(target.x - transform.position.x);
    }

    public void Flash(Color color, float duration = 0.12f)
    {
        if (Main == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        Main.color = color;
        yield return new WaitForSeconds(duration);
        Main.color = baseColor;
        flashRoutine = null;
    }

    public void SpawnPop(float duration = 0.35f)
    {
        if (Visual != null) StartCoroutine(PopRoutine(duration));
    }

    private IEnumerator PopRoutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.18f;
            float start = Mathf.Lerp(0.25f, 1f, EaseOutBack(k));
            Visual.localScale = baseScale * (start * s);
            yield return null;
        }
        Visual.localScale = baseScale;
    }

    public void Pulse(float amount = 0.15f, float duration = 0.18f)
    {
        if (Visual != null) StartCoroutine(PulseRoutine(amount, duration));
    }

    private IEnumerator PulseRoutine(float amount, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            Visual.localScale = baseScale * (1f + k * amount);
            yield return null;
        }
        Visual.localScale = baseScale;
    }

    public void DeathAnim(Vector2 dir, float duration = 0.55f)
    {
        IsDead = true;
        if (Shadow != null) Shadow.enabled = false;
        if (Aura != null) Aura.enabled = false;
        StartCoroutine(DeathRoutine(dir, duration));
    }

    private IEnumerator DeathRoutine(Vector2 dir, float duration)
    {
        float t = 0f;
        float sign = dir.x >= 0f ? -1f : 1f;
        Color c = baseColor;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            if (Visual != null)
            {
                Visual.localRotation = Quaternion.Euler(0f, 0f, sign * 82f * k);
                Visual.localScale = baseScale * Mathf.Lerp(1f, 0.65f, k);
                Visual.localPosition = new Vector3(0f, -0.12f * k, 0f);
            }
            if (Main != null)
            {
                Main.color = new Color(c.r, c.g, c.b, 1f - k);
            }
            yield return null;
        }
        if (Main != null) Main.enabled = false;
    }

    private void LateUpdate()
    {
        if (IsDead || Visual == null || Main == null) return;
        float time = Time.time;
        float speed = Walking ? 1.7f : 1f;
        float bob = Mathf.Sin(time * BobSpeed * speed + seed) * BobAmount * (Walking ? 1.8f : 1f);
        float tilt = Mathf.Sin(time * BobSpeed * speed + seed) * TiltAmount * (Walking ? 1.3f : 0.45f);
        Visual.localPosition = new Vector3(0f, bob, 0f);
        Visual.localRotation = Quaternion.Euler(0f, 0f, tilt);

        int order = 1000 - Mathf.RoundToInt((transform.position.y + YSortBias) * 80f);
        Main.sortingOrder = order;
        if (Shadow != null)
        {
            Shadow.sortingOrder = order - 2;
            Shadow.transform.localPosition = new Vector3(0f, 0.02f + bob * 0.15f, 0.02f);
        }
        if (Aura != null)
        {
            Aura.sortingOrder = order - 3;
            float a = 0.42f + Mathf.Sin(time * 2.2f + seed) * 0.12f;
            var c = Aura.color;
            Aura.color = new Color(c.r, c.g, c.b, a);
            Aura.transform.localRotation = Quaternion.Euler(0f, 0f, time * 18f);
        }
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
