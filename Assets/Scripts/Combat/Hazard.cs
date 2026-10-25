using UnityEngine;

public class Hazard : MonoBehaviour
{
    private float radius;
    private float dps;
    private float life;
    private float age;
    private float tick;
    private SpriteRenderer sr;
    private Color color;

    public static Hazard Poison(Vector2 pos, float radius, float dps, float duration, bool fromPlayer)
    {
        var go = new GameObject("poison_puddle");
        go.transform.position = pos;
        var h = go.AddComponent<Hazard>();
        h.radius = radius;
        h.dps = dps;
        h.life = duration;
        h.sr = go.AddComponent<SpriteRenderer>();
        h.sr.sprite = Game.I.Art.World("fx_poison");
        h.color = fromPlayer ? new Color(0.65f, 0.95f, 0.5f, 0.85f) : new Color(0.55f, 0.85f, 0.45f, 0.8f);
        h.sr.color = h.color;
        h.sr.sortingOrder = 700;
        float w = h.sr.sprite.bounds.size.x;
        go.transform.localScale = Vector3.one * (radius * 2f / w);
        h.fromPlayer = fromPlayer;
        return h;
    }

    private bool fromPlayer;

    /// <summary>Немедленно выключает лужу, не дожидаясь Destroy в конце кадра.</summary>
    public void Defuse()
    {
        enabled = false;
        Destroy(gameObject);
    }

    private void Update()
    {
        if (Game.I == null || Game.I.State != GameState.Combat) return;
        age += Time.deltaTime;
        tick -= Time.deltaTime;
        if (tick <= 0f)
        {
            tick = 0.45f;
            if (fromPlayer)
            {
                var enemies = Game.I.Rooms.EnemiesInRadius(transform.position, radius);
                foreach (var e in enemies)
                {
                    if (e != null && !e.Dead) e.TakeDamage(dps * 0.45f, transform.position, true, false);
                }
            }
            else
            {
                var p = Game.I.Player;
                if (p != null && !p.Dead && Vector2.Distance(p.transform.position, transform.position) < radius + 0.3f)
                {
                    p.TakeDamage(dps * 0.45f, transform.position, true, true);
                    p.Poison(1.2f, 2f);
                }
            }
        }
        float k = Mathf.Clamp01(age / life);
        sr.color = new Color(color.r, color.g, color.b, color.a * (k > 0.8f ? (1f - k) * 5f : 1f));
        transform.Rotate(0f, 0f, Time.deltaTime * 6f);
        if (age >= life) Destroy(gameObject);
    }
}
