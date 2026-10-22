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
    public bool Control = true;
    public float PoisonTimer;
    public float PoisonDps;
    public float RageTimer;

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
    public float RageMult => RageTimer > 0f ? 1.5f : 1f;

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
        Stats.hp = Stats.maxHp;
        Shield = 0f;
        ShieldTimer = 0f;
        Invuln = 0f;
        DashCd = 0f;
        Dead = false;
        Control = true;
        PoisonTimer = 0f;
        RageTimer = 0f;
        autoTimer = 0f;
        currentTarget = null;
        transform.rotation = Quaternion.identity;
        if (dashRoutine != null) StopCoroutine(dashRoutine);
        dashRoutine = null;
        Visual.ResetVisual();
        Visual.SetScale(1f);
        Visual.SetTint(Color.white);
        if (Skills != null) Skills.ResetForRun();
    }

    public void ShowInMenu()
    {
        transform.position = new Vector3(3.9f, -1.45f, 0f);
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
        Invuln = 0.4f;
        if (Skills != null) Skills.ResetCooldowns();
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

        UpdatePoison(dt);
        UpdateTint();

        moveInput = Vector2.zero;
        if (Control)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveInput.y += 1f;
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            facing = moveInput.normalized;
            Visual.Face(moveInput.x);
            Visual.Walking = true;
            float speed = Stats.speed;
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

        if (Control && Input.GetKeyDown(KeyCode.Space) && DashCd <= 0f)
        {
            Vector2 dir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : facing;
            dashRoutine = StartCoroutine(Dash(dir));
        }

        if (Control)
        {
            currentTarget = Game.I.Rooms.NearestEnemy(transform.position, Stats.range);
            if (currentTarget != null && autoTimer <= 0f)
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
        if (PoisonTimer > 0f && RageTimer > 0f) c = new Color(0.8f, 0.75f, 0.5f);
        else if (PoisonTimer > 0f) c = new Color(0.65f, 0.95f, 0.6f);
        else if (RageTimer > 0f) c = new Color(1f, 0.72f, 0.65f);
        Visual.SetTint(c);
    }

    private void FireAt(Enemy target)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.75f;
        Vector2 dir = ((Vector2)target.transform.position + Vector2.up * 0.5f - origin).normalized;
        facing = dir;
        Visual.Face(dir.x);
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

    private IEnumerator Dash(Vector2 dir)
    {
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
    }

    public void TakeDamage(float amount, Vector2 source, bool ignoreInvuln = false, bool isDot = false,
        Enemy attacker = null)
    {
        if (Dead) return;
        if (!ignoreInvuln && Invuln > 0f) return;
        amount *= 1f - Mathf.Clamp(Stats.damageReduction, 0f, 0.7f);

        if (Stats.thorns > 0f && attacker != null && !attacker.Dead)
        {
            attacker.TakeDamage(Stats.thorns, transform.position, true, false);
        }

        if (Shield > 0f)
        {
            float absorbed = Mathf.Min(Shield, amount);
            Shield -= absorbed;
            amount -= absorbed;
            Game.I.Fx.FloatingText(transform.position + Vector3.up * 1.4f, "-" + Mathf.RoundToInt(absorbed),
                new Color(0.6f, 0.85f, 1f), 0.8f);
            if (Shield <= 0f)
            {
                Shield = 0f;
                ShieldTimer = 0f;
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
                Stats.hp = 1f;
                Shield = 12f;
                ShieldTimer = 6f;
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

    public void AddShield(float amount, float duration = 6f)
    {
        if (amount <= 0f) return;
        Shield += amount;
        ShieldTimer = Mathf.Max(ShieldTimer, duration);
        Game.I.Fx.Shield(transform, duration);
        Game.I.Fx.Ring(transform.position + Vector3.up * 0.6f, new Color(0.55f, 0.8f, 1f), 0.7f, 0.4f);
        Game.I.Audio.Play("sfx_shield", 0.5f);
    }

    public void Poison(float duration, float dps)
    {
        PoisonTimer = Mathf.Max(PoisonTimer, duration);
        PoisonDps = Mathf.Max(PoisonDps, dps);
        poisonTick = Mathf.Min(poisonTick, 0.2f);
    }

    public void Rage(float duration)
    {
        RageTimer = Mathf.Max(RageTimer, duration);
        Game.I.Fx.Ring(transform.position, new Color(1f, 0.45f, 0.35f), 1.1f, 0.5f);
        Game.I.Audio.Play("sfx_cast", 0.7f);
    }

    public float DamageOut(float baseDamage)
    {
        return baseDamage * Stats.skillPower * RageMult;
    }

    public void KillHeal(float dealtDamage)
    {
        if (Stats.lifesteal <= 0f) return;
        Heal(dealtDamage * Stats.lifesteal);
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
