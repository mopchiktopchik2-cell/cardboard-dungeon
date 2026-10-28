using System.Collections;
using UnityEngine;

public class PlayerStats
{
    public float maxHp = 100f;
    public float hp = 100f;
    public float speed = 4.9f;
    public float damage = 9f;
    public float attackInterval = 0.75f;
    public float range = 6.9f;
    public float critChance = 0.04f;
    public float critMult = 1.8f;
    public float lifesteal = 0f;
    public float cdr = 0f;
    public float skillPower = 1f;
    public float damageReduction = 0f;
    public int pierce = 0;
    public int multishot = 1;
    public bool freezeShot;
    public bool poisonShot;
    public bool killBombs;
    public bool secondWind;
    public bool secondWindUsed;
    public float thorns;
    public float roomHeal;
    public float startShield;
    public int extraSlots;
}

public class Player : MonoBehaviour
{
    public PlayerStats Stats = new PlayerStats();
    public PaperActor Visual;
    public SkillSystem Skills;
    public ShieldFx ActiveShieldFx;
    public float Shield;
    public float ShieldTimer;
    public float Invuln;
    public float DashCd;
    public float DashCooldown = 1.35f;
    public bool Dead;
    public bool QaInvincible;
    public bool VictoryLocked;
    public bool Control = true;
    public float PoisonTimer;
    public float PoisonDps;
    public float RageTimer;
    public bool RageMastery;
    public bool ShieldRetaliate;
    public float StunTimer;
    public float CastLock;
    public float SlowTimer;
    public float SlowFactor = 1f;
    public float ShadowRunTimer;

    private float shadowTrailTimer;
    private bool dashing;
    private Vector2 moveInput;
    private Vector2 facing = Vector2.right;
    private float autoTimer;
    private float poisonTick;
    private float stepTimer;
    private Coroutine dashRoutine;
    private Enemy currentTarget;
    private Art art;

    public Enemy CurrentTarget => currentTarget;
    public Vector2 Facing => facing;
    public float RageMult => RageTimer > 0f ? (RageMastery ? 1.75f : 1.5f) : 1f;

    public static Player Create(Transform parent)
    {
        var go = new GameObject("Player");
        go.transform.SetParent(parent, false);
        var p = go.AddComponent<Player>();
        p.Init();
        return p;
    }

    private void Init()
    {
        art = Game.I.Art;
        Visual = PaperActor.Create(transform, art, "wizard", 1.0f, Color.white, true, false);
        Visual.StepDust = true;
        Stats.hp = Stats.maxHp;
        gameObject.layer = 0;
    }

    public void ResetForRun()
    {
        Stats = new PlayerStats();
        var hero = Data.Hero(MetaProgress.SelectedClass);
        Stats.maxHp = hero.Hp;
        Stats.speed = hero.Speed;
        Stats.damage = hero.Damage;
        Stats.attackInterval = hero.AttackInterval;
        Stats.range = hero.Range;
        Stats.critChance = hero.Crit;
        Stats.damageReduction = hero.Armor;
        Stats.skillPower = hero.SkillPower;
        MetaProgress.ApplyLoadout(Stats);
        Stats.hp = Stats.maxHp;
        Shield = 0f;
        ShieldTimer = 0f;
        Invuln = 0f;
        DashCd = 0f;
        Dead = false;
        VictoryLocked = false;
        dashing = false;
        Control = true;
        PoisonTimer = 0f;
        RageTimer = 0f;
        RageMastery = false;
        ShieldRetaliate = false;
        StunTimer = 0f;
        CastLock = 0f;
        SlowTimer = 0f;
        SlowFactor = 1f;
        ShadowRunTimer = 0f;
        shadowTrailTimer = 0f;
        autoTimer = 0f;
        currentTarget = null;
        transform.rotation = Quaternion.identity;
        if (dashRoutine != null) StopCoroutine(dashRoutine);
        dashRoutine = null;
        SetClassVisual(MetaProgress.SelectedClass);
        Visual.ResetVisual();
        Visual.SetScale(1f);
        Visual.SetTint(Color.white);
        if (Skills != null) Skills.ResetForRun();
    }

    public void SetClassVisual(HeroClass id)
    {
        if (Visual == null || Game.I == null || Game.I.Art == null) return;
        var hero = Data.Hero(id);
        Visual.SetSprite(Game.I.Art.Actor(hero.Sprite));
        Visual.SetTint(Color.white);
    }

    public void ShowInMenu()
    {
        transform.position = new Vector3(3.9f, -1.45f, 0f);
        SetClassVisual(MetaProgress.SelectedClass);
        Visual.ResetVisual();
        Visual.SetVisible(true);
        Visual.Walking = false;
        Visual.Face(-1f);
        Visual.SetScale(1.05f);
        Visual.PoseTilt = 2f;
        Control = false;
        Dead = false;
    }

    public void RevivePosition(Vector2 pos)
    {
        transform.position = pos;
        Visual.ResetVisual();
        Visual.transform.localRotation = Quaternion.identity;
        Visual.SetVisible(true);
        Visual.SetScale(1f);
        Visual.Walking = false;
        Dead = false;
        dashing = false;
        Invuln = Mathf.Max(Invuln, 0.35f);
        gameObject.SetActive(true);
    }

    public void SetVisible(bool visible)
    {
        Visual.SetVisible(visible);
    }

    public void SetControl(bool value)
    {
        Control = value;
        if (!value) Visual.Walking = false;
    }

    public void OnRoomStart()
    {
        if (Stats.startShield > 0f)
        {
            AddShield(Stats.startShield, 8f);
        }
        PoisonTimer = 0f;
        RageTimer = 0f;
        RageMastery = false;
        ShieldRetaliate = false;
        StunTimer = 0f;
        CastLock = 0f;
        SlowTimer = 0f;
        SlowFactor = 1f;
        ShadowRunTimer = 0f;
        shadowTrailTimer = 0f;
        Invuln = 0.4f;
        VictoryLocked = false;
        dashing = false;
        if (Skills != null) Skills.ResetCooldowns();
    }

    public void LockVictory()
    {
        VictoryLocked = true;
        Invuln = Mathf.Max(Invuln, 2f);
    }

    private void Update()
    {
        if (Dead || Game.I == null) return;
        if (Game.I.State != GameState.Combat) return;

        float dt = Time.deltaTime;
        if (Invuln > 0f) Invuln -= dt;
        if (ShieldTimer > 0f)
        {
            ShieldTimer -= dt;
            if (ShieldTimer <= 0f)
            {
                ShieldTimer = 0f;
                Shield = 0f;
            }
        }
        if (DashCd > 0f) DashCd -= dt;
        if (autoTimer > 0f) autoTimer -= dt;
        if (RageTimer > 0f) RageTimer -= dt;
        if (StunTimer > 0f) StunTimer -= dt;
        if (CastLock > 0f) CastLock -= dt;
        if (SlowTimer > 0f)
        {
            SlowTimer -= dt;
            if (SlowTimer <= 0f) SlowFactor = 1f;
        }
        if (ShadowRunTimer > 0f)
        {
            ShadowRunTimer -= dt;
            shadowTrailTimer -= dt;
            if (shadowTrailTimer <= 0f)
            {
                shadowTrailTimer = 0.09f;
                Game.I.Fx.Afterimage(Visual.Main);
            }
        }

        UpdatePoison(dt);
        UpdateTint();

        moveInput = Vector2.zero;
        if (StunTimer > 0f || CastLock > 0f)
        {
            Visual.Walking = false;
            return;
        }

        if (Control)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveInput.y += 1f;
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
        }

        bool moving = moveInput.sqrMagnitude > 0.01f;
        if (moving)
        {
            facing = moveInput.normalized;
            Visual.Face(moveInput.x);
            Visual.Walking = true;
            float speed = Stats.speed * SlowFactor;
            transform.position = Game.I.Rooms.Move(transform.position, moveInput * speed * dt, 0.32f);
            stepTimer -= dt;
            if (stepTimer <= 0f)
            {
                stepTimer = 0.27f;
                Game.I.Audio.Play("sfx_step", 0.22f, 1f, 0.1f);
            }
        }
        else
        {
            Visual.Walking = false;
        }

        if (Control && Input.GetKeyDown(KeyCode.Space) && DashCd <= 0f && !dashing)
        {
            Vector2 dir = moving ? moveInput.normalized : facing;
            dashRoutine = StartCoroutine(Dash(dir));
        }

        if (Control)
        {
            currentTarget = Game.I.Rooms.NearestEnemy(transform.position, Stats.range);
            bool stationaryStyle = Data.Hero(MetaProgress.SelectedClass).Style == HeroAttackStyle.Slash;
            bool mayAttackWhileMoving = stationaryStyle || ShadowRunTimer > 0f;
            if (currentTarget != null && autoTimer <= 0f && !dashing && (!moving || mayAttackWhileMoving))
            {
                FireAt(currentTarget);
                autoTimer = Stats.attackInterval * (RageTimer > 0f ? 0.72f : 1f);
            }
        }
    }

    private void UpdatePoison(float dt)
    {
        if (PoisonTimer > 0f)
        {
            PoisonTimer -= dt;
            poisonTick -= dt;
            if (poisonTick <= 0f)
            {
                poisonTick = 0.7f;
                TakeDamage(PoisonDps * 0.7f, transform.position, true, true, null);
            }
        }
    }

    private void UpdateTint()
    {
        Color c = Color.white;
        if (StunTimer > 0f) c = new Color(1f, 0.9f, 0.55f);
        else if (ShadowRunTimer > 0f) c = new Color(0.58f, 0.9f, 1f, 0.82f);
        else if (PoisonTimer > 0f && RageTimer > 0f) c = new Color(0.8f, 0.75f, 0.5f);
        else if (PoisonTimer > 0f) c = new Color(0.65f, 0.95f, 0.6f);
        else if (SlowTimer > 0f) c = new Color(0.72f, 0.88f, 1f);
        else if (RageTimer > 0f) c = new Color(1f, 0.72f, 0.65f);
        Visual.SetTint(c);
    }

    private void FireAt(Enemy target)
    {
        Vector2 dir = ((Vector2)target.transform.position + Vector2.up * 0.5f -
                       ((Vector2)transform.position + Vector2.up * 0.75f)).normalized;
        facing = dir;
        Visual.Face(dir.x);
        var hero = Data.Hero(MetaProgress.SelectedClass);
        switch (hero.Style)
        {
            case HeroAttackStyle.Slash:
                SlashAttack(dir);
                break;
            case HeroAttackStyle.Arrow:
                FireArrow(dir);
                break;
            default:
                FireBolt(dir);
                break;
        }
    }

    private void FireBolt(Vector2 dir)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.75f;
        Visual.Attack(dir, 1f);
        Game.I.Fx.Sprite("fx_glow", origin, 0.42f, 0.13f,
            new Color(0.55f, 1f, 0.95f, 0.75f), 1660, 0f, 0f, 0f);

        bool crit = Random.value < Stats.critChance;
        float dmg = Stats.damage * RageMult * (crit ? Stats.critMult : 1f);
        int count = Mathf.Max(1, Stats.multishot);
        for (int i = 0; i < count; i++)
        {
            float spread = (i - (count - 1) * 0.5f) * 7.5f;
            Vector2 d = Rotate(dir, spread);
            Projectile.Player(origin, d, dmg, 15.5f, "proj_bolt", new Color(0.55f, 1f, 0.95f), 1.8f,
                Stats.pierce, 0f, crit, Stats.freezeShot, Stats.poisonShot, false, 1.8f);
        }
        Game.I.Audio.Play("sfx_shoot", 0.45f, 1f, 0.08f);
    }

    private void FireArrow(Vector2 dir)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.8f;
        Visual.Attack(dir, 0.85f);
        bool crit = Random.value < Stats.critChance;
        float dmg = Stats.damage * RageMult * (crit ? Stats.critMult : 1f);
        int count = Mathf.Max(1, Stats.multishot);
        for (int i = 0; i < count; i++)
        {
            float spread = (i - (count - 1) * 0.5f) * 5f;
            Vector2 d = Rotate(dir, spread);
            Projectile.Player(origin, d, dmg, 23f, "proj_arrow", new Color(0.72f, 1f, 0.62f), 1.25f,
                Stats.pierce + 1, 0f, crit, Stats.freezeShot, Stats.poisonShot, false, 2.1f);
        }
        Game.I.Fx.Sprite("fx_glow", origin, 0.28f, 0.1f,
            new Color(0.65f, 1f, 0.5f, 0.55f), 1660, 0f, 0f, 0f);
        Game.I.Audio.Play("sfx_shoot", 0.4f, 1.18f, 0.06f);
    }

    private void SlashAttack(Vector2 dir)
    {
        Visual.Attack(dir, 1.2f);
        Game.I.Fx.Slash((Vector2)transform.position + dir * 0.85f + Vector2.up * 0.55f, dir);
        Game.I.Fx.Ring((Vector2)transform.position + Vector2.up * 0.5f,
            new Color(1f, 0.86f, 0.55f, 0.38f), Stats.range, 0.22f);
        Game.I.Audio.Play("sfx_shoot", 0.32f, 0.72f, 0.05f);

        bool any = false;
        var list = Game.I.Rooms.EnemiesInRadius(transform.position, Stats.range + 0.55f);
        foreach (var e in list)
        {
            if (e == null || e.Dead) continue;
            Vector2 to = ((Vector2)e.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Dot(dir, to) < 0.12f) continue;
            bool crit = Random.value < Stats.critChance;
            float dmg = Stats.damage * RageMult * (crit ? Stats.critMult : 1f);
            e.TakeDamage(dmg, transform.position, true, crit);
            any = true;
        }
        if (any)
        {
            Game.I.Shake(0.045f, 0.08f);
            Game.I.Hitstop(0.012f);
        }
    }

    private IEnumerator Dash(Vector2 dir)
    {
        dashing = true;
        DashCd = DashCooldown;
        Invuln = Mathf.Max(Invuln, 0.28f);
        Game.I.Audio.Play("sfx_dash", 0.6f);
        Game.I.Fx.Dust(transform.position, 6);
        Visual.Dash();
        float t = 0f;
        while (t < 0.17f)
        {
            t += Time.deltaTime;
            transform.position = Game.I.Rooms.Move(transform.position, dir * 17f * Time.deltaTime, 0.32f);
            Game.I.Fx.Afterimage(Visual.Main);
            yield return null;
        }
        Visual.Walking = false;
        dashing = false;
    }

    public void TakeDamage(float amount, Vector2 source, bool ignoreInvuln = false, bool isDot = false,
        Enemy attacker = null)
    {
        if (Dead || VictoryLocked) return;
        if (QaInvincible) return;
        if (!ignoreInvuln && Invuln > 0f) return;
        amount *= 1f - Mathf.Clamp(Stats.damageReduction, 0f, 0.7f);

        // Шипы — ответный урон, а не удар игрока: не даём им
        // накручивать DamageDealt и лечить через вампиризм.
        if (Stats.thorns > 0f && attacker != null && !attacker.Dead)
        {
            attacker.TakeDamage(Stats.thorns, transform.position, false, false);
        }

        if (Shield > 0f)
        {
            float absorbed = Mathf.Min(Shield, amount);
            Shield -= absorbed;
            amount -= absorbed;
            Game.I.Fx.FloatingText(transform.position + Vector3.up * 1.4f, "-" + Mathf.RoundToInt(absorbed),
                new Color(0.6f, 0.85f, 1f), 0.8f);
            if (ShieldRetaliate && attacker != null && !attacker.Dead)
            {
                attacker.TakeDamage(Stats.maxHp * 0.18f, transform.position, false, false);
                Game.I.Fx.Line(transform.position + Vector3.up * 0.7f,
                    attacker.transform.position + Vector3.up * 0.6f, new Color(0.7f, 0.9f, 1f), 0.09f, 0.16f);
                Game.I.Fx.FloatingText(attacker.transform.position + Vector3.up * 1.2f, "ОЖОГ",
                    new Color(0.7f, 0.9f, 1f), 0.75f);
            }
            if (Shield <= 0f)
            {
                Shield = 0f;
                ShieldTimer = 0f;
                ShieldRetaliate = false;
            }
        }

        if (amount <= 0f) return;

        Stats.hp -= amount;
        Game.I.Run.DamageTaken += amount;
        Game.I.Fx.FloatingText(transform.position + Vector3.up * 1.4f, "-" + Mathf.RoundToInt(amount),
            new Color(1f, 0.45f, 0.4f), 1f);
        Game.I.UI.Flash(new Color(0.7f, 0.1f, 0.1f, 0.22f), 0.25f);
        if (!isDot)
        {
            Invuln = 0.55f;
            Visual.Flash(new Color(1f, 0.35f, 0.35f), 0.12f);
            Visual.Hit((Vector2)transform.position - source, 1f);
            Game.I.Shake(0.1f, 0.2f);
            Game.I.Hitstop(0.035f);
            Game.I.Audio.Play("sfx_hit", 0.65f);
        }

        if (Stats.hp <= 0f)
        {
            if (Stats.secondWind && !Stats.secondWindUsed)
            {
                Stats.secondWindUsed = true;
                Stats.hp = Mathf.Max(1f, Stats.maxHp * 0.12f);
                Invuln = 1.4f;
                Game.I.Fx.Heal(transform.position);
                Game.I.Fx.FloatingText(transform.position + Vector3.up * 1.7f, "ВТОРОЕ ДЫХАНИЕ",
                    new Color(1f, 0.85f, 0.4f), 1.2f);
                Game.I.Audio.Play("sfx_heal", 0.9f);
            }
            else
            {
                Die(source);
            }
        }
    }

    public void Heal(float amount)
    {
        if (Dead || amount <= 0f) return;
        float before = Stats.hp;
        Stats.hp = Mathf.Min(Stats.maxHp, Stats.hp + amount);
        float healed = Stats.hp - before;
        if (healed > 0.5f)
        {
            Game.I.Fx.FloatingText(transform.position + Vector3.up * 1.3f, "+" + Mathf.RoundToInt(healed),
                new Color(0.55f, 1f, 0.6f), 0.9f);
        }
    }

    public void Stun(float duration)
    {
        if (Dead || duration <= 0f) return;
        StunTimer = Mathf.Max(StunTimer, duration);
        CastLock = 0f;
        Visual.Walking = false;
        Visual.Flash(new Color(1f, 0.9f, 0.5f), 0.2f);
        Game.I.Fx.FloatingText(transform.position + Vector3.up * 1.55f, "ОГЛУШЁН",
            new Color(1f, 0.85f, 0.4f), 0.8f);
        Game.I.Audio.Play("sfx_hit", 0.55f, 0.75f);
    }

    public void Webbed(float duration, float factor = 0.55f)
    {
        if (Dead) return;
        SlowTimer = Mathf.Max(SlowTimer, duration);
        SlowFactor = Mathf.Min(SlowFactor, factor);
        Visual.Flash(new Color(0.75f, 0.9f, 1f), 0.18f);
        Game.I.Fx.FloatingText(transform.position + Vector3.up * 1.4f, "ПАУТИНА",
            new Color(0.75f, 0.9f, 1f), 0.7f);
    }

    public void StartShadowRun(float duration)
    {
        if (Dead || duration <= 0f) return;
        ShadowRunTimer = Mathf.Max(ShadowRunTimer, duration);
        Invuln = Mathf.Max(Invuln, duration + 0.05f);
        shadowTrailTimer = 0f;
        Game.I.Fx.Ring(transform.position + Vector3.up * 0.5f, new Color(0.55f, 0.95f, 1f), 1.4f, 0.5f);
        Game.I.Fx.Burst("fx_shadow", transform.position, 8, 2.2f, 0.45f, 0.45f,
            new Color(0.45f, 0.85f, 1f, 0.8f), 1600);
        Game.I.Audio.Play("sfx_dash", 0.7f, 1.15f);
    }

    public void AddShield(float amount, float duration = 6f, bool retaliate = false)
    {
        if (amount <= 0f) return;
        Shield += amount;
        ShieldTimer = Mathf.Max(ShieldTimer, duration);
        ShieldRetaliate = retaliate;
        Game.I.Fx.Shield(transform, duration);
        Game.I.Fx.Ring(transform.position + Vector3.up * 0.6f,
            retaliate ? new Color(1f, 0.65f, 0.35f) : new Color(0.55f, 0.8f, 1f), 0.7f, 0.4f);
        Game.I.Audio.Play("sfx_shield", 0.5f);
    }

    public void Poison(float duration, float dps)
    {
        PoisonTimer = Mathf.Max(PoisonTimer, duration);
        PoisonDps = Mathf.Max(PoisonDps, dps);
        poisonTick = Mathf.Min(poisonTick, 0.2f);
    }

    public void Rage(float duration, bool mastery = false)
    {
        RageTimer = Mathf.Max(RageTimer, duration);
        RageMastery = mastery;
        Game.I.Fx.Ring(transform.position,
            mastery ? new Color(1f, 0.28f, 0.18f) : new Color(1f, 0.45f, 0.35f), mastery ? 1.7f : 1.1f, 0.5f);
        Game.I.Audio.Play("sfx_cast", 0.7f);
    }

    public float DamageOut(float baseDamage)
    {
        return baseDamage * Stats.skillPower * RageMult;
    }

    public void KillHeal(float dealtDamage)
    {
        float lifesteal = Stats.lifesteal;
        if (RageMastery && RageTimer > 0f) lifesteal += 0.15f;
        lifesteal = Mathf.Min(lifesteal, Data.LifestealCap + 0.15f);
        if (lifesteal <= 0f || dealtDamage <= 0f) return;
        Heal(dealtDamage * lifesteal);
    }

    public IEnumerator WalkTo(Vector2 target, float duration)
    {
        Vector2 start = transform.position;
        float t = 0f;
        while (t < duration)
        {
            if (Dead || (Game.I != null && Game.I.State == GameState.Dead)) yield break;
            if (Game.I.SkipRequested)
            {
                transform.position = target;
                break;
            }
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            transform.position = Vector2.Lerp(start, target, k);
            Vector2 d = target - start;
            if (d.sqrMagnitude > 0.01f) Visual.Face(d.x);
            Visual.Walking = true;
            yield return null;
        }
        Visual.Walking = false;
    }

    private void Die(Vector2 source)
    {
        if (Dead) return;
        Dead = true;
        Control = false;
        Stats.hp = 0f;
        dashing = false;
        if (dashRoutine != null) StopCoroutine(dashRoutine);
        dashRoutine = null;
        Vector2 dir = (Vector2)transform.position - source;
        Visual.Walking = false;
        Visual.DeathAnim(dir, 0.75f);
        Game.I.Audio.Play("sfx_death", 0.9f);
        StartCoroutine(DeathFlow());
    }

    private IEnumerator DeathFlow()
    {
        yield return new WaitForSeconds(0.85f);
        Game.I.PlayerDied();
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
