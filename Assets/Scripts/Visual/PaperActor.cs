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
    public bool StepDust;
    public float YSortBias;
    public float PoseTilt;

    public float SpriteScale => baseScale.y;
    public float BobAmount = 0.025f;
    public float BobSpeed = 3f;
    public float TiltAmount = 1.6f;

    // v1.1 micro-animation tuning: a real stride, paper squash and hit reactions.
    public float StrideAmount = 0.055f;
    public float StrideLift = 0.055f;
    public float StrideSpeed = 10.5f;
    public float SquashAmount = 0.045f;

    private float seed;
    private Color baseColor;
    private Vector3 baseScale;
    private Vector3 shadowBaseScale;
    private float spawnScale = 1f;
    private Coroutine flashRoutine;
    private Coroutine actionRoutine;
    private Coroutine popRoutine;
    private Coroutine pulseRoutine;
    private Coroutine deathRoutine;
    private float popScale = 1f;
    private float pulseScale = 1f;
    private float chargeScale = 1f;
    private Vector3 actionOffset;
    private float actionTilt;
    private Vector3 actionScale = Vector3.one;
    private float walkPhase;
    private float dustTimer;

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
        walkPhase = seed;

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
            Shadow.color = new Color(0.04f, 0.03f, 0.04f, 0.5f);
            Shadow.sortingOrder = 999;
            float sw = Main.sprite.bounds.size.x;
            float shW = Shadow.sprite.bounds.size.x;
            float k = sw * 0.72f / Mathf.Max(0.01f, shW);
            shadowBaseScale = new Vector3(k, k * 0.95f, 1f);
            sh.transform.localScale = shadowBaseScale * scale;
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
        if (Shadow != null) Shadow.transform.localScale = shadowBaseScale * scale;
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
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        if (popRoutine != null) StopCoroutine(popRoutine);
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        if (deathRoutine != null) StopCoroutine(deathRoutine);
        actionRoutine = null;
        popRoutine = null;
        pulseRoutine = null;
        flashRoutine = null;
        deathRoutine = null;
        PoseTilt = 0f;
        popScale = 1f;
        pulseScale = 1f;
        chargeScale = 1f;
        actionOffset = Vector3.zero;
        actionTilt = 0f;
        actionScale = Vector3.one;
        dustTimer = 0f;
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
        if (Visual == null) return;
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine(duration));
    }

    private IEnumerator PopRoutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.18f;
            popScale = Mathf.Lerp(0.25f, 1f, EaseOutBack(k)) * s;
            yield return null;
        }
        popScale = 1f;
        popRoutine = null;
    }

    public void Pulse(float amount = 0.15f, float duration = 0.18f)
    {
        if (Visual == null) return;
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine(amount, duration));
    }

    private IEnumerator PulseRoutine(float amount, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            pulseScale = 1f + k * amount;
            yield return null;
        }
        pulseScale = 1f;
        pulseRoutine = null;
    }

    /// <summary>Forward lunge + recoil for a shot or a melee swing.</summary>
    public void Attack(Vector2 dir, float power = 1f)
    {
        if (Visual == null) return;
        Face(dir.x);
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        actionRoutine = StartCoroutine(AttackRoutine(dir, power));
    }

    private IEnumerator AttackRoutine(Vector2 dir, float power)
    {
        float sx = dir.x >= 0f ? 1f : -1f;
        float duration = 0.26f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float punch = Mathf.Sin(Mathf.Clamp01(k / 0.42f) * Mathf.PI * 0.5f);
            float settle = Mathf.Clamp01((k - 0.4f) / 0.6f);
            float recoil = Mathf.Sin(settle * Mathf.PI);
            actionOffset = new Vector3(
                (sx * 0.13f * punch - sx * 0.055f * recoil) * power,
                (0.035f * punch - 0.012f * recoil) * power, 0f);
            actionTilt = (-sx * 8f * punch + sx * 4f * recoil) * power;
            actionScale = new Vector3(1f + 0.07f * punch - 0.025f * recoil,
                1f - 0.055f * punch + 0.02f * recoil, 1f);
            yield return null;
        }
        actionOffset = Vector3.zero;
        actionTilt = 0f;
        actionScale = Vector3.one;
        actionRoutine = null;
    }

    /// <summary>Paper crunch when the actor is hit. Direction points away from the source.</summary>
    public void Hit(Vector2 dir, float power = 1f)
    {
        if (Visual == null) return;
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        actionRoutine = StartCoroutine(HitRoutine(dir, power));
    }

    private IEnumerator HitRoutine(Vector2 dir, float power)
    {
        Vector2 d = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
        float duration = 0.2f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float hit = Mathf.Sin(k * Mathf.PI) * power;
            actionOffset = new Vector3(d.x * 0.075f * hit, d.y * 0.05f * hit, 0f);
            actionTilt = -d.x * 7f * hit;
            actionScale = new Vector3(1f + 0.08f * hit, 1f - 0.055f * hit, 1f);
            yield return null;
        }
        actionOffset = Vector3.zero;
        actionTilt = 0f;
        actionScale = Vector3.one;
        actionRoutine = null;
    }

    /// <summary>Crouch and tremble before an enemy attack, making telegraphs readable.</summary>
    public void Windup(float duration)
    {
        if (Visual == null) return;
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        actionRoutine = StartCoroutine(WindupRoutine(duration));
    }

    private IEnumerator WindupRoutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            float charge = Mathf.Sin(k * Mathf.PI * 0.5f);
            actionOffset = new Vector3(Mathf.Sin(Time.time * 46f) * 0.018f * charge, -0.035f * charge, 0f);
            actionTilt = Mathf.Sin(Time.time * 38f) * 2.4f * charge;
            actionScale = new Vector3(1f - 0.07f * charge, 1f + 0.1f * charge, 1f);
            yield return null;
        }
        actionOffset = Vector3.zero;
        actionTilt = 0f;
        actionScale = Vector3.one;
        actionRoutine = null;
    }

    /// <summary>Lean into the dash and stretch like a paper cutout caught by wind.</summary>
    public void Dash()
    {
        if (Visual == null) return;
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        actionRoutine = StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        float duration = 0.24f;
        float t = 0f;
        actionOffset = Vector3.zero;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float dash = Mathf.Sin(k * Mathf.PI);
            actionTilt = 18f * dash;
            actionScale = new Vector3(1f - 0.12f * dash, 1f + 0.12f * dash, 1f);
            yield return null;
        }
        actionOffset = Vector3.zero;
        actionTilt = 0f;
        actionScale = Vector3.one;
        actionRoutine = null;
    }

    /// <summary>Used by the bomber: swelling paper body before the fuse burns out.</summary>
    public void SetCharge(float amount)
    {
        chargeScale = 1f + Mathf.Max(0f, amount);
    }

    public void DeathAnim(Vector2 dir, float duration = 0.55f)
    {
        IsDead = true;
        if (Shadow != null) Shadow.enabled = false;
        if (Aura != null) Aura.enabled = false;
        if (deathRoutine != null) StopCoroutine(deathRoutine);
        deathRoutine = StartCoroutine(DeathRoutine(dir, duration));
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
        deathRoutine = null;
    }

    private void LateUpdate()
    {
        if (IsDead || Visual == null || Main == null) return;
        float dt = Time.deltaTime;
        float time = Time.time;

        if (Walking) walkPhase += dt * StrideSpeed;
        float cycle = Mathf.Sin(walkPhase);
        float lift = Mathf.Abs(cycle);
        float idleBob = Mathf.Sin(time * BobSpeed + seed) * BobAmount * (Walking ? 0.35f : 0.7f);
        float idleTilt = Mathf.Sin(time * BobSpeed * 0.85f + seed) * TiltAmount * (Walking ? 0.2f : 0.32f);

        Vector3 walkOffset = Walking
            ? new Vector3(cycle * StrideAmount, lift * StrideLift, 0f)
            : Vector3.zero;
        float walkTilt = Walking ? -cycle * TiltAmount * 1.25f : 0f;
        float squash = Walking ? (1f - lift) * SquashAmount : 0f;

        Visual.localPosition = walkOffset + new Vector3(0f, idleBob, 0f) + actionOffset;
        Visual.localRotation = Quaternion.Euler(0f, 0f, walkTilt + idleTilt + actionTilt + PoseTilt);

        float scaleX = (1f + squash) * actionScale.x;
        float scaleY = (1f - squash * 0.78f) * actionScale.y;
        float extra = popScale * pulseScale * chargeScale;
        Visual.localScale = new Vector3(baseScale.x * scaleX * extra, baseScale.y * scaleY * extra, baseScale.z);

        if (StepDust && Walking && Game.I != null && Game.I.Fx != null)
        {
            dustTimer -= dt;
            if (dustTimer <= 0f)
            {
                dustTimer = 0.31f;
                Game.I.Fx.Dust(transform.position + Vector3.down * 0.02f, 1, 0.34f);
            }
        }

        int order = 1000 - Mathf.RoundToInt((transform.position.y + YSortBias) * 80f);
        Main.sortingOrder = order;
        if (Shadow != null)
        {
            Shadow.sortingOrder = order - 2;
            Shadow.transform.localPosition = new Vector3(0f, 0.02f + idleBob * 0.2f, 0.02f);
            float shadowShrink = Walking ? lift * 0.07f : 0f;
            Shadow.transform.localScale = new Vector3(shadowBaseScale.x * (1f + shadowShrink),
                shadowBaseScale.y * (1f - shadowShrink), 1f);
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
