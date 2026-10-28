using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private static readonly List<Projectile> All = new List<Projectile>();

    private bool enemyTeam;
    private float damage;
    private float speed;
    private float radius;
    private float aoe;
    private float life;
    private int pierce;
    private bool crit;
    private bool slow;
    private bool poison;
    private bool homing;
    private bool enemyPoison;
    private bool enemySlow;
    private string spriteName;
    private Color tint;
    private float scale;
    private Vector2 dir;
    private SpriteRenderer sr;
    private SpriteRenderer glow;
    private float trailTimer;
    private float homingTimer;
    private Enemy homingTarget;
    private readonly HashSet<Enemy> hit = new HashSet<Enemy>();

    public static void Player(Vector2 pos, Vector2 direction, float dmg, float spd, string sprite, Color color,
        float scale = 1f, int pierceCount = 0, float aoeRadius = 0f, bool isCrit = false, bool slows = false,
        bool poisons = false, bool homes = false, float lifetime = 2.2f)
    {
        var p = Create(pos, direction, dmg, spd, sprite, color, scale, lifetime);
        p.pierce = pierceCount;
        p.aoe = aoeRadius;
        p.crit = isCrit;
        p.slow = slows;
        p.poison = poisons;
        p.homing = homes;
    }

    public static void Enemy(Vector2 pos, Vector2 direction, float dmg, float spd, string sprite, float scale = 1f,
        float radiusValue = 0.22f, float lifetime = 4f, bool poisonOnHit = false, bool slowOnHit = false)
    {
        var p = Create(pos, direction, dmg, spd, sprite, EnemyTint(sprite), scale, lifetime);
        p.enemyTeam = true;
        p.radius = radiusValue;
        p.enemyPoison = poisonOnHit;
        p.enemySlow = slowOnHit;
    }

    private static Color EnemyTint(string sprite)
    {
        switch (sprite)
        {
            case "proj_arrow": return new Color(1f, 0.82f, 0.32f);
            case "proj_bone": return new Color(0.95f, 0.86f, 0.66f);
            case "proj_skull": return new Color(0.78f, 0.65f, 1f);
            case "proj_soul": return new Color(0.45f, 0.9f, 1f);
            case "proj_poison": return new Color(0.55f, 1f, 0.42f);
            case "proj_web": return new Color(0.85f, 0.92f, 1f);
            case "proj_ice": return new Color(0.5f, 0.9f, 1f);
            case "proj_fire": return new Color(1f, 0.48f, 0.2f);
            default: return new Color(1f, 0.58f, 0.32f);
        }
    }

    private static Projectile Create(Vector2 pos, Vector2 direction, float dmg, float spd, string sprite, Color color,
        float scaleValue, float lifetime)
    {
        var go = new GameObject("projectile_" + sprite);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Game.I.Art.World(sprite);
        Color bright = Color.Lerp(color, Color.white, 0.18f);
        sr.color = bright;
        sr.sortingOrder = 1520;
        var glowGo = new GameObject("glow");
        glowGo.transform.SetParent(go.transform, false);
        glowGo.transform.localScale = Vector3.one * 1.7f;
        var glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = Game.I.Art.World("fx_glow");
        Color halo = Color.Lerp(bright, Color.white, 0.45f);
        halo.a = 0.38f;
        glowSr.color = halo;
        glowSr.sortingOrder = 1505;

        var p = go.AddComponent<Projectile>();
        p.enemyTeam = false;
        p.damage = dmg;
        p.speed = spd;
        p.radius = 0.16f;
        p.life = lifetime;
        p.dir = direction.normalized;
        p.spriteName = sprite;
        p.tint = bright;
        p.scale = scaleValue;
        p.sr = sr;
        p.glow = glowSr;
        go.transform.localScale = Vector3.one * scaleValue;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        return p;
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public static void ClearAll()
    {
        var copy = new List<Projectile>(All);
        All.Clear();
        foreach (var projectile in copy)
        {
            if (projectile == null) continue;
            projectile.enabled = false;
            Destroy(projectile.gameObject);
        }
    }

    public static void ClearEnemyProjectiles()
    {
        var copy = new List<Projectile>(All);
        foreach (var projectile in copy)
        {
            if (projectile == null || !projectile.enemyTeam) continue;
            projectile.enabled = false;
            Destroy(projectile.gameObject);
        }
    }

    private void Update()
    {
        if (Game.I == null)
        {
            Destroy(gameObject);
            return;
        }
        if (Game.I.State == GameState.Pause) return;
        if (Game.I.State != GameState.Combat)
        {
            // Не даём снарядам убить игрока после зачистки или во время перехода.
            Destroy(gameObject);
            return;
        }

        float dt = Time.deltaTime;
        life -= dt;
        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (glow != null)
        {
            Color halo = Color.Lerp(tint, Color.white, 0.45f);
            halo.a = 0.3f + Mathf.Sin(Time.time * 15f) * 0.1f;
            glow.color = halo;
        }

        if (homing)
        {
            homingTimer -= dt;
            if (homingTimer <= 0f || homingTarget == null || homingTarget.Dead)
            {
                homingTimer = 0.18f;
                homingTarget = Game.I.Rooms.NearestEnemy(transform.position, 9f);
            }
            if (homingTarget != null && !homingTarget.Dead)
            {
                Vector2 want = ((Vector2)homingTarget.transform.position + Vector2.up * 0.5f - (Vector2)transform.position).normalized;
                dir = Vector2.Lerp(dir, want, dt * 5.5f).normalized;
            }
        }

        Vector2 from = transform.position;
        Vector2 to = from + dir * speed * dt;
        Rect arena = Game.I.Rooms.Arena;
        if (to.x < arena.xMin + 0.05f || to.x > arena.xMax - 0.05f ||
            to.y < arena.yMin + 0.05f || to.y > arena.yMax - 0.05f ||
            Game.I.Rooms.SegmentBlocked(from, to, radius))
        {
            ImpactObstacle();
            return;
        }
        transform.position = to;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        trailTimer -= dt;
        if (trailTimer <= 0f)
        {
            trailTimer = 0.055f;
            Game.I.Fx.Sprite(spriteName, transform.position, scale * 0.62f, 0.16f,
                new Color(tint.r, tint.g, tint.b, 0.55f), 1480, 0f, 0f, 0f, false);
        }

        if (enemyTeam)
        {
            var player = Game.I.Player;
            if (player != null && !player.Dead &&
                Vector2.Distance(player.transform.position, transform.position) < radius + 0.32f)
            {
                player.TakeDamage(damage, transform.position);
                if (enemyPoison) player.Poison(3.2f, 3.5f);
                if (enemySlow) player.Webbed(2.8f, 0.5f);
                Game.I.Fx.Sprite(spriteName, transform.position, scale * 0.8f, 0.18f, tint, 1600);
                Game.I.Audio.Play("sfx_hit", 0.35f);
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            var list = Game.I.Rooms.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.Dead || hit.Contains(e)) continue;
                float d = Vector2.Distance(e.transform.position + Vector3.up * 0.5f, transform.position);
                if (d < radius + 0.38f)
                {
                    HitEnemy(e);
                    if (this == null) return;
                    if (pierce <= 0) break;
                }
            }
        }

        Vector2 pos = transform.position;
        if (pos.x < -14f || pos.x > 14f || pos.y < -9f || pos.y > 9f)
        {
            Destroy(gameObject);
        }
    }

    private void ImpactObstacle()
    {
        if (Game.I != null && Game.I.Fx != null)
        {
            Game.I.Fx.Sprite(spriteName, transform.position, scale * 0.7f, 0.16f,
                new Color(tint.r, tint.g, tint.b, 0.55f), 1620);
            Game.I.Audio.Play("sfx_hit", 0.22f, 1.25f);
        }
        Destroy(gameObject);
    }

    private void HitEnemy(Enemy e)
    {
        hit.Add(e);
        e.TakeDamage(damage, transform.position, true, crit);
        if (slow) e.Slow(1.1f, 0.55f);
        if (poison) e.Poison(3f, 3f);
        Game.I.Fx.Sprite(spriteName, transform.position, scale * 0.9f, 0.2f, tint, 1620);
        Game.I.Fx.Burst("fx_flame", transform.position, 2, 1.2f, 0.25f, 0.25f, tint, 1600);

        if (aoe > 0f)
        {
            Game.I.Fx.Explosion(transform.position, aoe, tint);
            Game.I.Rooms.DamageEnemies(transform.position, aoe, damage * 0.8f, crit);
        }
        else
        {
            Game.I.Audio.Play("sfx_hit", 0.3f, 1.2f, 0.1f);
        }

        if (pierce <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            pierce--;
        }
    }
}
