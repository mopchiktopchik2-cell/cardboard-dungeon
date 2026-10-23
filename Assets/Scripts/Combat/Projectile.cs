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
        var p = Create(pos, direction, dmg, spd, sprite, Color.white, scale, lifetime);
        p.enemyTeam = true;
        p.radius = radiusValue;
        p.enemyPoison = poisonOnHit;
        p.enemySlow = slowOnHit;
    }

    private static Projectile Create(Vector2 pos, Vector2 direction, float dmg, float spd, string sprite, Color color,
        float scaleValue, float lifetime)
    {
        var go = new GameObject("projectile_" + sprite);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Game.I.Art.World(sprite);
        sr.color = color;
        sr.sortingOrder = 1450;
        var p = go.AddComponent<Projectile>();
        p.enemyTeam = false;
        p.damage = dmg;
        p.speed = spd;
        p.radius = 0.16f;
        p.life = lifetime;
        p.dir = direction.normalized;
        p.spriteName = sprite;
        p.tint = color;
        p.scale = scaleValue;
        p.sr = sr;
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
        for (int i = All.Count - 1; i >= 0; i--)
        {
            if (All[i] != null) Destroy(All[i].gameObject);
        }
        All.Clear();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
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

        transform.position += (Vector3)(dir * speed * dt);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        trailTimer -= dt;
        if (trailTimer <= 0f)
        {
            trailTimer = 0.055f;
            Game.I.Fx.Sprite(spriteName, transform.position, scale * 0.55f, 0.14f,
                new Color(tint.r, tint.g, tint.b, 0.35f), 1440, 0f, 0f, 0f, false);
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
