using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemyDef Def;
    public float Hp;
    public float MaxHp;
    public float Damage;
    public bool Elite;
    public bool Boss;
    public bool Dead;
    public PaperActor Visual;
    public WorldBar Bar;

    private float radius = 0.34f;
    private float attackCd;
    private float windup;
    private float windupTime;
    private bool winding;
    private float spawnTimer;
    private float slowTimer;
    private float slowFactor = 1f;
    private float stun;
    private float specialCd;
    private float fuse;
    private bool exploding;
    private bool dashing;
    private float dashTime;
    private Vector2 dashDir;
    private bool dashHit;
    private float teleportTimer;
    private float beepTimer;
    private int pattern;
    private bool phase2;
    private bool casting;
    private Vector2 knockback;
    private Color baseTint = Color.white;
    private float contactCd;
    private float dotTimer;
    private float dotDps;
    private float dotTick;

    public static Enemy Spawn(EnemyKind kind, Vector2 pos, float hpScale, float damageScale, bool elite = false)
    {
        var go = new GameObject("Enemy_" + kind);
        go.transform.position = pos;
        var e = go.AddComponent<Enemy>();
        e.Build(kind, hpScale, damageScale, elite);
        return e;
    }

    private void Build(EnemyKind kind, float hpScale, float damageScale, bool elite)
    {
        Def = Data.Enemy(kind);
        Elite = elite;
        Boss = Def.Boss;
        MaxHp = Def.Hp * hpScale * (elite ? 2.1f : 1f);
        Hp = MaxHp;
        Damage = Def.Damage * damageScale * (elite ? 1.25f : 1f);
        float scale = Def.Scale * (elite ? 1.14f : 1f);
        radius = 0.34f * scale;
        if (Boss)
        {
            scale = Def.Scale * 1.25f;
            radius = 0.55f;
            Damage *= 1.15f;
        }

        baseTint = elite ? new Color(1f, 0.92f, 0.7f) : Color.white;
        Visual = PaperActor.Create(transform, Game.I.Art, Def.Sprite, scale, baseTint, true, false);
        Visual.StepDust = Boss || elite;
        Visual.Main.sortingOrder = 1000;
        Visual.SpawnPop(0.4f);
        if (elite) Visual.AddAura(Game.I.Art, Pal.Gold, 1.35f);
        if (Boss)
        {
            Color aura = kind == EnemyKind.BoneKing ? new Color(0.65f, 1f, 0.7f) :
                kind == EnemyKind.SpiderQueen ? new Color(1f, 0.35f, 0.65f) :
                new Color(0.7f, 0.35f, 0.9f);
            Visual.AddAura(Game.I.Art, aura, 1.6f);
        }

        float h = Visual.Main.sprite.bounds.size.y * scale;
        if (!Boss)
        {
            Bar = WorldBar.Create(transform, Game.I.Art, elite ? 1.25f : 0.95f, 0.1f,
                elite ? Pal.Gold : Pal.Red, elite);
            Bar.Set(Hp / MaxHp, h + 0.25f);
        }

        spawnTimer = 0.4f;
        attackCd = Random.Range(0.25f, 0.8f);
        specialCd = 1.5f;
        Game.I.Fx.Dust(transform.position, Boss ? 12 : 5, Boss ? 1.4f : 0.8f);
        if (Boss)
        {
            Game.I.Fx.Ring(transform.position, new Color(0.75f, 0.4f, 0.9f, 0.8f), 2f, 0.8f);
        }
    }

    private void Update()
    {
        if (Dead) return;
        if (Game.I == null || Game.I.State != GameState.Combat)
        {
            Visual.Walking = false;
            return;
        }
        float dt = Time.deltaTime;
        if (spawnTimer > 0f)
        {
            spawnTimer -= dt;
            Visual.Walking = false;
            return;
        }
        if (stun > 0f)
        {
            stun -= dt;
            Visual.Walking = false;
            return;
        }
        if (slowTimer > 0f)
        {
            slowTimer -= dt;
            if (slowTimer <= 0f) slowFactor = 1f;
        }
        if (dotTimer > 0f)
        {
            dotTimer -= dt;
            dotTick -= dt;
            if (dotTick <= 0f)
            {
                dotTick = 0.7f;
                TakeDamage(dotDps * 0.7f, transform.position, false, false);
                if (Dead) return;
            }
        }
        if (contactCd > 0f) contactCd -= dt;
        if (dotTimer > 0f) Visual.SetTint(new Color(0.7f, 1f, 0.66f));
        else if (slowTimer > 0f) Visual.SetTint(new Color(0.75f, 0.9f, 1f));
        else Visual.SetTint(baseTint);

        knockback = Vector2.Lerp(knockback, Vector2.zero, dt * 8f);
        if (knockback.sqrMagnitude > 0.001f)
        {
            transform.position = Game.I.Rooms.Move(transform.position, knockback * dt, radius);
        }

        var player = Game.I.Player;
        if (player == null || player.Dead) return;
        Vector2 myPos = transform.position;
        Vector2 toPlayer = (Vector2)player.transform.position - myPos;
        float dist = toPlayer.magnitude;
        Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.right;

        attackCd -= dt;
        specialCd -= dt;

        switch (Def.Kind)
        {
            case EnemyKind.Imp:
                MeleeAI(player, dir, dist, dt, 1f);
                break;
            case EnemyKind.Guard:
                MeleeAI(player, dir, dist, dt, 0.9f);
                break;
            case EnemyKind.Spider:
                MeleeAI(player, dir, dist, dt, 1.15f, true);
                break;
            case EnemyKind.Skeleton:
                ArcherAI(player, dir, dist, dt);
                break;
            case EnemyKind.Wraith:
                WraithAI(player, dir, dist, dt);
                break;
            case EnemyKind.Shaman:
                ShamanAI(player, dir, dist, dt);
                break;
            case EnemyKind.PlagueDoctor:
                PlagueAI(player, dir, dist, dt);
                break;
            case EnemyKind.Boar:
                BoarAI(player, dir, dist, dt);
                break;
            case EnemyKind.Hound:
                BoarAI(player, dir, dist, dt);
                break;
            case EnemyKind.Executioner:
                MeleeAI(player, dir, dist, dt, 0.78f);
                break;
            case EnemyKind.Bomber:
                BomberAI(player, dir, dist, dt);
                break;
            case EnemyKind.Lich:
            case EnemyKind.BoneKing:
            case EnemyKind.SpiderQueen:
                BossAI(player, dir, dist, dt);
                break;
        }

        Separate(dt);
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (Bar == null || Boss) return;
        float h = Visual.Main.sprite.bounds.size.y * Visual.SpriteScale + 0.22f;
        Bar.Set(Hp / MaxHp, h);
        if (!BarVisible() && Hp < MaxHp) SetBar(true);
    }

    private bool barVisible;

    private void SetBar(bool v)
    {
        barVisible = v;
        if (Bar != null) Bar.SetVisible(v);
    }

    private bool BarVisible()
    {
        return barVisible || Elite;
    }

    private void MeleeAI(Player player, Vector2 dir, float dist, float dt, float speedK, bool poison = false)
    {
        if (winding)
        {
            windup -= dt;
            if (windup <= 0f)
            {
                winding = false;
                attackCd = Def.Cooldown;
                if (dist < Def.Range + 0.45f)
                {
                    Visual.Attack(dir, 1.25f);
                    player.TakeDamage(Damage, transform.position, false, false, this);
                    Game.I.Fx.Slash(player.transform.position, dir);
                    Game.I.Audio.Play("sfx_hit", 0.5f);
                    if (poison) player.Poison(2.5f, 2.5f);
                }
            }
            return;
        }

        if (dist > Def.Range)
        {
            MoveDir(dir, Def.Speed * speedK, dt);
        }
        else
        {
            Visual.Walking = false;
            Visual.Face(dir.x);
            if (attackCd <= 0f)
            {
                winding = true;
                windup = 0.3f;
                Visual.Windup(windup);
                Visual.Flash(new Color(1f, 0.55f, 0.5f), 0.18f);
            }
        }
    }

    private void ArcherAI(Player player, Vector2 dir, float dist, float dt)
    {
        if (winding)
        {
            windup -= dt;
            if (windup <= 0f)
            {
                winding = false;
                attackCd = Def.Cooldown;
                Shoot(player, dir, "proj_arrow", 10.2f, 1.0f);
            }
            return;
        }

        if (dist < 3.3f)
        {
            MoveDir(-dir, Def.Speed, dt);
        }
        else if (dist > 5.4f)
        {
            MoveDir(dir, Def.Speed, dt);
        }
        else
        {
            Visual.Walking = false;
            Vector2 side = new Vector2(-dir.y, dir.x);
            MoveDir(side * (Mathf.Sin(Time.time + transform.position.x) > 0f ? 1f : -1f), 0.55f, dt);
        }

        if (attackCd <= 0f && dist < 7f)
        {
            winding = true;
            windup = 0.36f;
            Visual.Windup(windup);
            Visual.Flash(new Color(0.9f, 0.85f, 0.7f), 0.2f);
        }
    }

    private void WraithAI(Player player, Vector2 dir, float dist, float dt)
    {
        if (specialCd <= 0f && dist < 3.6f)
        {
            specialCd = 6.2f;
            StartCoroutine(WraithBlink());
            return;
        }
        ArcherAI(player, dir, dist, dt);
    }

    private IEnumerator WraithBlink()
    {
        Visual.SetVisible(false);
        Game.I.Fx.Burst("fx_soul", transform.position, 6, 1.2f, 0.35f, 0.3f,
            new Color(0.45f, 0.85f, 1f, 0.8f), 1600);
        Game.I.Audio.Play("sfx_dash", 0.55f, 0.8f);
        yield return new WaitForSeconds(0.22f);
        if (Dead || Game.I == null) yield break;
        Vector2 p = Game.I.Rooms.RandomArenaPoint(1.8f);
        if (Vector2.Distance(p, Game.I.Player.transform.position) < 2.4f)
        {
            p = Game.I.Rooms.RandomArenaPoint(2.2f);
        }
        transform.position = p;
        Visual.SetVisible(true);
        Visual.SpawnPop(0.3f);
        Game.I.Fx.Explosion(transform.position, 1.1f, new Color(0.4f, 0.8f, 1f));
    }

    private void PlagueAI(Player player, Vector2 dir, float dist, float dt)
    {
        if (winding)
        {
            windup -= dt;
            if (windup <= 0f)
            {
                winding = false;
                attackCd = Def.Cooldown;
                Visual.Attack(dir, 0.85f);
                Shoot(player, dir, "proj_poison", 9.2f, 1.05f, true);
                Game.I.Audio.Play("sfx_cast", 0.5f);
            }
            return;
        }

        if (dist < 4.4f) MoveDir(-dir, Def.Speed, dt);
        else if (dist > 6.8f) MoveDir(dir, Def.Speed, dt);
        else
        {
            Vector2 side = new Vector2(-dir.y, dir.x);
            MoveDir(side * (Mathf.Sin(Time.time * 0.7f) > 0f ? 1f : -1f), 0.45f, dt);
        }

        if (attackCd <= 0f && dist < 7.5f)
        {
            winding = true;
            windup = 0.48f;
            Visual.Windup(windup);
            Visual.Flash(new Color(0.7f, 1f, 0.6f), 0.2f);
        }
    }

    private void ShamanAI(Player player, Vector2 dir, float dist, float dt)
    {
        if (winding)
        {
            windup -= dt;
            if (windup <= 0f)
            {
                winding = false;
                attackCd = Def.Cooldown;
                if (specialCd <= 0f)
                {
                    specialCd = 8f;
                    HealAllies();
                }
                else
                {
                    Visual.Attack(dir, 0.85f);
                    Shoot(player, dir, "proj_skull", 8.2f, 1.0f);
                    Game.I.Audio.Play("sfx_cast", 0.5f);
                }
            }
            return;
        }

        if (dist < 4.6f) MoveDir(-dir, Def.Speed, dt);
        else if (dist > 6.6f) MoveDir(dir, Def.Speed, dt);
        else
        {
            Vector2 side = new Vector2(-dir.y, dir.x);
            MoveDir(side * (Mathf.Sin(Time.time * 0.8f) > 0f ? 1f : -1f), 0.4f, dt);
        }

        if (attackCd <= 0f)
        {
            winding = true;
            windup = 0.5f;
            Visual.Windup(windup);
        }
    }

    private void HealAllies()
    {
        Game.I.Fx.Ring(transform.position, new Color(0.55f, 1f, 0.6f), 1.6f, 0.6f);
        Game.I.Audio.Play("sfx_heal", 0.5f);
        foreach (var e in Game.I.Rooms.Enemies)
        {
            if (e == null || e.Dead || e == this) continue;
            if (Vector2.Distance(e.transform.position, transform.position) < 6f)
            {
                e.Heal(e.MaxHp * 0.08f);
                Game.I.Fx.Heal(e.transform.position);
            }
        }
    }

    private void BoarAI(Player player, Vector2 dir, float dist, float dt)
    {
        if (dashing)
        {
            dashTime -= dt;
            transform.position = Game.I.Rooms.Move(transform.position, dashDir * 11.5f * dt, radius);
            Visual.Walking = true;
            Game.I.Fx.Dust(transform.position, 1, 0.4f);
            if (!dashHit && Vector2.Distance(transform.position, player.transform.position) < 0.85f)
            {
                dashHit = true;
                player.TakeDamage(Damage, transform.position, false, false, this);
                Game.I.Shake(0.2f, 0.3f);
                Game.I.Audio.Play("sfx_hit", 0.8f);
            }
            Vector2 p = transform.position;
            if (p.x <= Game.I.Rooms.Arena.xMin + 0.4f || p.x >= Game.I.Rooms.Arena.xMax - 0.4f ||
                p.y <= Game.I.Rooms.Arena.yMin + 0.4f || p.y >= Game.I.Rooms.Arena.yMax - 0.4f)
            {
                dashing = false;
                stun = 0.7f;
                Game.I.Shake(0.18f, 0.3f);
                Game.I.Fx.Dust(transform.position, 6, 1.1f);
            }
            if (dashTime <= 0f) dashing = false;
            return;
        }

        if (winding)
        {
            windup -= dt;
            Visual.Face(dir.x);
            if (windup <= 0f)
            {
                winding = false;
                dashing = true;
                dashHit = false;
                dashTime = 0.52f;
                dashDir = dir;
                attackCd = Def.Cooldown;
                Visual.Attack(dir, 1.5f);
                Game.I.Audio.Play("sfx_dash", 0.6f);
            }
            return;
        }

        if (dist < 5f && attackCd <= 0f)
        {
            winding = true;
            windup = 0.48f;
            Visual.Windup(windup);
            Visual.Flash(new Color(1f, 0.55f, 0.45f), 0.5f);
            Game.I.Fx.Ring(transform.position, new Color(1f, 0.4f, 0.3f, 0.8f), 1.0f, 0.5f);
            return;
        }
        MoveDir(dir, Def.Speed, dt);
    }

    private void BomberAI(Player player, Vector2 dir, float dist, float dt)
    {
        if (exploding)
        {
            fuse -= dt;
            beepTimer -= dt;
            float k = 1f - Mathf.Clamp01(fuse / 1.15f);
            Visual.SetCharge(0.12f + Mathf.Sin(Time.time * 22f) * 0.08f + k * 0.12f);
            if (beepTimer <= 0f)
            {
                beepTimer = Mathf.Lerp(0.18f, 0.05f, k);
                Game.I.Audio.Play("sfx_bomb", 0.12f, 1.4f, 0.1f);
                Visual.Flash(new Color(1f, 0.5f, 0.3f), 0.06f);
            }
            if (fuse <= 0f)
            {
                Explode(player);
            }
            return;
        }

        if (dist < 1.9f && attackCd <= 0f)
        {
            exploding = true;
            fuse = 1.15f;
            beepTimer = 0f;
            Visual.Flash(new Color(1f, 0.65f, 0.35f), 0.2f);
            return;
        }
        MoveDir(dir, Def.Speed, dt);
    }

    private void Explode(Player player)
    {
        Dead = true;
        Vector3 pos = transform.position;
        Game.I.Fx.Explosion(pos, 2.2f, new Color(1f, 0.55f, 0.3f));
        Game.I.Audio.Play("sfx_bomb", 0.9f);
        if (Vector2.Distance(player.transform.position, pos) < 2.3f)
        {
            player.TakeDamage(Damage, pos, false, false, this);
        }
        Game.I.Run.OnKill(this);
        Game.I.Rooms.NotifyKilled(this);
        Visual.DeathAnim(Vector2.right, 0.35f);
        Destroy(gameObject, 0.4f);
    }

    private void BossAI(Player player, Vector2 dir, float dist, float dt)
    {
        if (!phase2 && Hp < MaxHp * 0.5f)
        {
            phase2 = true;
            Color phaseTint = Def.Kind == EnemyKind.BoneKing ? new Color(0.7f, 1f, 0.72f) :
                Def.Kind == EnemyKind.SpiderQueen ? new Color(1f, 0.55f, 0.78f) :
                new Color(0.9f, 0.62f, 0.85f);
            Visual.SetTint(phaseTint);
            baseTint = phaseTint;
            Game.I.Fx.Explosion(transform.position, 2.2f, phaseTint);
            Game.I.Audio.Play("sfx_boss_roar", 1f);
            Game.I.Shake(0.35f, 0.8f);
            Game.I.Hitstop(0.08f);
            Game.I.UI.BossPhase();
        }

        if (casting) return;

        if (dist > 5.5f) MoveDir(dir, Def.Speed * (phase2 ? 1.4f : 1f), dt);
        else if (dist < 3f) MoveDir(-dir, Def.Speed * (phase2 ? 1.4f : 1f), dt);
        else
        {
            Visual.Walking = false;
            Visual.Face(dir.x);
        }

        if (specialCd <= 0f)
        {
            specialCd = phase2 ? 2.1f : 2.9f;
            StartCoroutine(BossAttack(player, pattern++ % 4));
        }
    }

    private IEnumerator BossAttack(Player player, int which)
    {
        casting = true;
        Visual.Pulse(0.08f, 0.25f);
        Vector2 castDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        Visual.Face(castDir.x);
        Visual.Windup(0.45f);
        Game.I.Fx.Ring(transform.position, new Color(0.7f, 0.4f, 0.9f, 0.7f), 1.6f, 0.35f);

        if (which == 0)
        {
            yield return new WaitForSeconds(0.5f);
            Visual.Attack(castDir, 1f);
            int count = phase2 ? 14 : 9;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count + Time.time * 0.3f;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Projectile.Enemy(transform.position + Vector3.up * 0.9f, d, Damage * 0.75f, 5.6f,
                    Def.Projectile, 1.4f, 0.26f, 5f);
            }
            Game.I.Audio.Play("sfx_cast", 0.8f);
        }
        else if (which == 1)
        {
            yield return new WaitForSeconds(0.55f);
            Visual.Attack(castDir, 0.8f);
            int adds = phase2 ? 3 : 2;
            Game.I.Rooms.SpawnAdds(adds);
            Game.I.Fx.Explosion(transform.position, 1.6f, new Color(0.7f, 0.5f, 0.9f));
            Game.I.Audio.Play("sfx_spawn", 0.8f);
        }
        else if (which == 2)
        {
            yield return new WaitForSeconds(0.45f);
            Visual.Attack(castDir, 1f);
            int count = phase2 ? 5 : 4;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = (Vector2)player.transform.position + Random.insideUnitCircle * 3.2f;
                p = Game.I.Rooms.Clamp(p, 0.6f);
                Game.I.Fx.TelegraphedCircle(p, 1.5f, 1.15f, new Color(1f, 0.35f, 0.3f));
                Game.I.Rooms.StartCoroutine(MeteorAt(p, 1.15f, Damage * 1.2f));
            }
            Game.I.Audio.Play("sfx_wave", 0.5f);
        }
        else
        {
            Vector2 target = Game.I.Rooms.RandomArenaPoint(2f);
            Visual.SetVisible(false);
            yield return new WaitForSeconds(0.35f);
            transform.position = target;
            Visual.SetVisible(true);
            Visual.SpawnPop(0.35f);
            Game.I.Fx.Explosion(target, 1.3f, new Color(0.7f, 0.4f, 0.95f));
            Game.I.Audio.Play("sfx_dash", 0.7f);
            yield return new WaitForSeconds(0.3f);
            int count = phase2 ? 10 : 7;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Projectile.Enemy(transform.position + Vector3.up * 0.9f, new Vector2(Mathf.Cos(a), Mathf.Sin(a)),
                    Damage * 0.65f, 6f, Def.Projectile, 1.3f, 0.26f, 5f);
            }
        }

        yield return new WaitForSeconds(0.25f);
        casting = false;
    }

    private IEnumerator MeteorAt(Vector2 p, float delay, float damage)
    {
        yield return new WaitForSeconds(delay);
        if (Game.I == null || Game.I.State != GameState.Combat) yield break;
        Vector3 center = new Vector3(p.x, p.y, 0f);
        Game.I.Fx.Explosion(center, 1.55f, new Color(1f, 0.5f, 0.25f));
        Game.I.Audio.Play("sfx_explosion", 0.55f);
        if (Vector2.Distance(Game.I.Player.transform.position, p) < 1.65f)
        {
            Game.I.Player.TakeDamage(damage, p);
        }
    }

    private void MoveDir(Vector2 dir, float speed, float dt)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        Vector2 d = dir.normalized * speed * slowFactor * dt;
        transform.position = Game.I.Rooms.Move(transform.position, d, radius);
        Visual.Face(dir.x);
        Visual.Walking = true;
    }

    private void Shoot(Player player, Vector2 dir, string sprite, float speed, float scale, bool poison = false)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.8f;
        Vector2 d = ((Vector2)player.transform.position + Vector2.up * 0.55f - origin).normalized;
        Projectile.Enemy(origin, d, Damage, speed, sprite, scale, 0.2f, 5f, poison);
        Game.I.Audio.Play(sprite == "proj_arrow" ? "sfx_shoot" : "sfx_cast", 0.5f, 0.85f);
    }

    private void Separate(float dt)
    {
        Vector2 push = Vector2.zero;
        var list = Game.I.Rooms.Enemies;
        for (int i = 0; i < list.Count; i++)
        {
            var other = list[i];
            if (other == null || other == this || other.Dead) continue;
            Vector2 d = (Vector2)transform.position - (Vector2)other.transform.position;
            float dist = d.magnitude;
            float min = radius + other.radius;
            if (dist > 0.001f && dist < min)
            {
                push += d / dist * (min - dist);
            }
        }
        if (push.sqrMagnitude > 0.0001f)
        {
            transform.position = Game.I.Rooms.Move(transform.position, push * dt * 4f, radius);
        }
    }

    public void Heal(float amount)
    {
        if (Dead) return;
        Hp = Mathf.Min(MaxHp, Hp + amount);
        if (Bar != null) Bar.Set(Hp / MaxHp, Visual.Main.sprite.bounds.size.y * Visual.SpriteScale + 0.22f);
    }

    public void Slow(float duration, float factor = 0.5f)
    {
        slowTimer = Mathf.Max(slowTimer, duration);
        slowFactor = Mathf.Min(slowFactor, factor);
        Visual.Flash(new Color(0.6f, 0.9f, 1f), 0.15f);
    }

    public void Poison(float duration, float dps)
    {
        dotTimer = Mathf.Max(dotTimer, duration);
        dotDps = Mathf.Max(dotDps, dps);
        dotTick = Mathf.Min(dotTick, 0.2f);
        Visual.Flash(new Color(0.7f, 1f, 0.6f), 0.15f);
    }

    public void TakeDamage(float amount, Vector2 source, bool playerSource = true, bool crit = false)
    {
        if (Dead) return;
        if (Def.Kind == EnemyKind.Guard) amount *= 0.68f;
        if (Def.Kind == EnemyKind.Executioner) amount *= 0.86f;
        Hp -= amount;
        if (playerSource)
        {
            Game.I.Run.DamageDealt += amount;
            Game.I.Player.KillHeal(amount);
        }
        if (Bar != null && !BarVisible()) SetBar(true);
        Visual.Flash(crit ? new Color(1f, 0.9f, 0.55f) : new Color(1f, 0.75f, 0.7f), 0.1f);
        Visual.Hit((Vector2)transform.position - source, crit ? 1.3f : 0.8f);
        Game.I.Fx.FloatingText(transform.position + Vector3.up * (1.5f * Visual.SpriteScale),
            Mathf.RoundToInt(amount).ToString(), crit ? new Color(1f, 0.85f, 0.35f) : Pal.Cream,
            crit ? 1.35f : 0.9f, crit);
        if (playerSource && !crit) Game.I.Hitstop(0.012f);
        knockback += (Vector2)(transform.position - (Vector3)source).normalized * 1.6f;
        Game.I.Audio.Play("sfx_enemy_hit", 0.35f, 1.1f, 0.08f);
        if (Hp <= 0f)
        {
            Die(source);
        }
    }

    private void Die(Vector2 source)
    {
        if (Dead) return;
        Dead = true;
        Hp = 0f;
        Vector2 dir = (Vector2)transform.position - source;
        Visual.Walking = false;
        Visual.DeathAnim(dir, Boss ? 1.2f : 0.55f);
        if (Bar != null) Bar.SetVisible(false);
        Game.I.Run.OnKill(this);
        Game.I.Rooms.NotifyKilled(this);
        Game.I.Audio.Play("sfx_death", Boss ? 1f : 0.55f, Boss ? 0.65f : 1.1f);

        if (Def.Kind == EnemyKind.Spider)
        {
            Hazard.Poison(transform.position, 1.35f, 4f, 7f, false);
        }
        if (Game.I.Player.Stats.killBombs)
        {
            Game.I.Fx.Explosion(transform.position, 1.3f, new Color(1f, 0.6f, 0.3f));
            Game.I.Rooms.DamageEnemies(transform.position, 1.7f, Game.I.Player.DamageOut(10f));
        }
        if (Boss)
        {
            Game.I.Fx.Explosion(transform.position, 3.2f, new Color(0.75f, 0.45f, 0.95f));
            Game.I.Shake(0.4f, 1f);
            Game.I.Rooms.BossDefeated();
        }
        Destroy(gameObject, Boss ? 1.6f : 0.8f);
    }
}
