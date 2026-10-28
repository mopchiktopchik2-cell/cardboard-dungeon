using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillSystem : MonoBehaviour
{
    public class Slot
    {
        public SkillDef Def;
        public float Cd;
        public float MaxCd;
        public int Level = 1;

        public bool Mastery => Level >= EndlessContent.MaxSkillLevel;
        public float Power => 1f + Mathf.Max(0, Level - 1) * 0.15f;
        public float Area => 1f + Mathf.Max(0, Level - 1) * 0.06f;
        public float Duration => 1f + Mathf.Max(0, Level - 1) * 0.08f;
    }

    public Slot[] Slots = new Slot[5];
    public int MaxSlots = 2;
    public Player Player;
    public bool CdrPulse;

    public void Init(Player player)
    {
        Player = player;
        for (int i = 0; i < Slots.Length; i++) Slots[i] = new Slot();
        ResetForRun();
    }

    public void ResetForRun()
    {
        MaxSlots = 2;
        for (int i = 0; i < Slots.Length; i++)
        {
            Slots[i].Def = null;
            Slots[i].Cd = 0f;
            Slots[i].MaxCd = 0f;
            Slots[i].Level = 1;
        }
        var starter = Data.StarterSkills(MetaProgress.SelectedClass);
        for (int i = 0; i < starter.Length && i < MaxSlots; i++)
        {
            Slots[i].Def = Data.Skill(starter[i]);
        }
        // Любой класс должен остаться с рабочим набором, даже если контент поменяется.
        if (Slots[0].Def == null) Slots[0].Def = Data.Skill("fireball");
        if (Slots[1].Def == null) Slots[1].Def = Data.Skill("heal");
    }

    public void ResetCooldowns()
    {
        foreach (var s in Slots) s.Cd = 0f;
    }

    public void UnlockSlot()
    {
        MaxSlots = Mathf.Clamp(MaxSlots + 1, 2, Data.MaxSkillSlots);
    }

    public bool AddSkill(string id)
    {
        var def = Data.Skill(id);
        if (def == null) return false;
        if (!Data.SkillAllowedForClass(id, MetaProgress.SelectedClass)) return false;
        foreach (var s in Slots)
        {
            if (s.Def != null && s.Def.Id == id) return false;
        }
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].Def == null && i < MaxSlots)
            {
                Slots[i].Def = def;
                Slots[i].Cd = 0f;
                Slots[i].Level = 1;
                if (Game.I != null && Game.I.Run != null) Game.I.Run.EnsureSkillLevel(id);
                Game.I.UI.RefreshSkills();
                return true;
            }
        }
        return false;
    }

    public bool HasSkill(string id)
    {
        foreach (var s in Slots) if (s.Def != null && s.Def.Id == id) return true;
        return false;
    }

    public void SetLevel(string id, int level)
    {
        if (string.IsNullOrEmpty(id)) return;
        int value = Mathf.Clamp(level, 1, EndlessContent.MaxSkillLevel);
        foreach (var s in Slots)
        {
            if (s.Def != null && s.Def.Id == id) s.Level = value;
        }
    }

    private void Update()
    {
        if (Game.I == null || Player == null) return;
        float dt = Time.deltaTime;
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].Cd > 0f) Slots[i].Cd = Mathf.Max(0f, Slots[i].Cd - dt);
        }
        if (Game.I.State != GameState.Combat || Player.Dead) return;
        for (int i = 0; i < Data.MaxSkillSlots; i++)
        {
            bool pressed = Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i);
            if (pressed) TryCast(i);
        }
    }

    public void TryCast(int index)
    {
        if (index < 0 || index >= Slots.Length) return;
        var slot = Slots[index];
        if (slot.Def == null || slot.Cd > 0f) return;
        if (index >= MaxSlots) return;
        if (Player.StunTimer > 0f) return;
        if (!Cast(slot)) return;
        float cd = Data.EffectiveCooldown(slot.Def, MetaProgress.SelectedClass) *
                   (1f - Mathf.Clamp(Player.Stats.cdr, 0f, 0.55f)) *
                   (1f - Mathf.Max(0, slot.Level - 1) * 0.05f) *
                   (slot.Mastery ? 0.9f : 1f);
        slot.Cd = cd;
        slot.MaxCd = cd;
        Game.I.Audio.Play("sfx_cast", 0.55f, 1f, 0.08f);
    }

    private bool Cast(Slot slot)
    {
        var def = slot.Def;
        if (def == null) return false;
        var rooms = Game.I.Rooms;
        var player = Player;
        Vector2 pos = player.transform.position;
        Vector2 origin = pos + Vector2.up * 0.8f;
        Enemy target = rooms.NearestEnemy(pos, 13f);
        float power = slot.Power;
        float area = slot.Area;

        switch (def.Id)
        {
            case "fireball":
            {
                if (target == null) return false;
                int count = slot.Mastery ? 3 : 1;
                var targets = slot.Mastery ? rooms.NearestEnemies(pos, 14f, 3) : null;
                for (int i = 0; i < count; i++)
                {
                    Enemy t = targets != null && targets.Count > 0 ? targets[i % targets.Count] : target;
                    if (t == null) t = target;
                    Vector2 aim = ((Vector2)t.transform.position + Vector2.up * 0.5f - origin).normalized;
                    if (i > 0) aim = Rotate(aim, (i - 1) * 13f);
                    Projectile.Player(origin, aim, player.DamageOut(28f * power), 11.5f, "proj_fire",
                        new Color(1f, 0.62f, 0.25f), 2.2f, 0,
                        2.2f * area * (slot.Mastery ? 1.35f : 1f), false, false, false, false, 2.2f);
                }
                Game.I.Audio.Play("sfx_fireball", 0.75f);
                Game.I.Fx.Sprite("fx_flame", origin, 0.6f, 0.25f, new Color(1f, 0.7f, 0.3f), 1550);
                return true;
            }
            case "frost":
            {
                float radius = slot.Mastery ? 99f : 4.1f * area;
                float duration = Data.FrostDuration * slot.Duration;
                float percent = slot.Mastery ? 0.35f : Data.FrostDamagePercent;
                var frozen = rooms.EnemiesInRadius(pos, radius);
                Game.I.Fx.Ring(pos, new Color(0.55f, 0.9f, 1f), Mathf.Min(radius, 12f), 0.55f);
                Game.I.Fx.Burst("fx_flame", pos, slot.Mastery ? 30 : 16, 4.2f, 0.55f, 0.55f,
                    new Color(0.7f, 0.95f, 1f), 1610);
                foreach (var e in frozen)
                {
                    if (e != null && !e.Dead) e.Freeze(duration, percent);
                }
                Game.I.Audio.Play("sfx_ice", 0.9f);
                Game.I.Shake(slot.Mastery ? 0.25f : 0.14f, 0.32f);
                return true;
            }
            case "volley":
            {
                int count = slot.Mastery ? 12 : 7;
                var targets = rooms.NearestEnemies(pos, 14f, count);
                if (targets.Count == 0) return false;
                Vector2 baseDir = ((Vector2)targets[0].transform.position + Vector2.up * 0.5f - origin).normalized;
                for (int i = 0; i < count; i++)
                {
                    Vector2 aim = baseDir;
                    var t = targets[i % targets.Count];
                    if (t != null) aim = ((Vector2)t.transform.position + Vector2.up * 0.5f - origin).normalized;
                    aim = Rotate(aim, (i - (count - 1) * 0.5f) * (slot.Mastery ? 4f : 5f));
                    Projectile.Player(origin, aim, player.DamageOut(10f * power), 17.5f, "proj_arrow",
                        new Color(0.72f, 1f, 0.62f), 1.15f, slot.Mastery ? 2 : 1,
                        slot.Mastery ? 1.5f * area : 0f, false, false, false, true, 3.4f);
                }
                Game.I.Fx.Sprite("fx_glow", origin, 0.8f, 0.18f,
                    new Color(0.65f, 1f, 0.5f, 0.65f), 1660);
                Game.I.Audio.Play("sfx_shoot", 0.75f, 1.05f);
                return true;
            }
            case "shadowrun":
            {
                player.StartShadowRun(Data.ShadowRunDuration * slot.Duration);
                if (slot.Mastery) ResetCooldowns();
                return true;
            }
            case "cleave":
            {
                StartCoroutine(CleaveRoutine(player, slot));
                return true;
            }
            case "shield":
            {
                float amount = player.Stats.maxHp * (slot.Mastery ? 0.50f : Data.KnightShieldPercent) *
                               (1f + Mathf.Max(0, slot.Level - 1) * 0.04f);
                player.AddShield(amount, Data.ShieldDuration * slot.Duration, slot.Mastery);
                return true;
            }
            case "nova":
            {
                float radius = 3.6f * area;
                NovaPulse(pos, radius, player.DamageOut(18f * power), player);
                if (slot.Mastery) StartCoroutine(NovaSecondPulse(pos, radius * 1.3f, player, slot));
                Game.I.Audio.Play("sfx_ice", 0.8f);
                Game.I.Shake(0.12f, 0.3f);
                return true;
            }
            case "chain":
            {
                if (target == null) return false;
                int count = slot.Mastery ? 5 : 3;
                var targets = rooms.NearestEnemies(pos, 12f, count);
                Vector2 from = origin;
                for (int i = 0; i < targets.Count; i++)
                {
                    var e = targets[i];
                    if (e == null) continue;
                    float mult = slot.Mastery ? 1f : i == 0 ? 1f : i == 1 ? 0.7f : 0.45f;
                    Game.I.Fx.Line(from, e.transform.position + Vector3.up * 0.6f,
                        new Color(1f, 0.9f, 0.45f), 0.16f, 0.28f);
                    e.TakeDamage(player.DamageOut(24f * power * mult), pos, true, false);
                    from = e.transform.position + Vector3.up * 0.6f;
                }
                Game.I.Audio.Play("sfx_lightning", 0.85f);
                Game.I.Fx.Burst("fx_flame", target.transform.position, slot.Mastery ? 7 : 4, 2.5f, 0.3f,
                    0.35f, new Color(1f, 0.9f, 0.4f), 1620);
                return true;
            }
            case "heal":
            {
                if (player.Stats.hp >= player.Stats.maxHp - 1f) return false;
                float amount = player.Stats.maxHp * (slot.Mastery ? 0.48f : 0.30f + 0.03f * (slot.Level - 1));
                player.Heal(amount);
                if (slot.Mastery)
                {
                    player.PoisonTimer = 0f;
                    player.StunTimer = 0f;
                    player.SlowTimer = 0f;
                    player.SlowFactor = 1f;
                    player.Invuln = Mathf.Max(player.Invuln, 1.5f);
                    Game.I.Fx.Ring(pos, new Color(0.65f, 1f, 0.7f), 2.2f, 0.6f);
                }
                Game.I.Fx.Heal(pos);
                Game.I.Audio.Play("sfx_heal", 0.8f);
                return true;
            }
            case "poison":
            {
                if (target == null) return false;
                float radius = 2.3f * area;
                float duration = 6f * slot.Duration;
                float dps = 6f * power;
                Hazard.Poison(target.transform.position, radius, dps, duration, true);
                Game.I.Fx.Dust(target.transform.position, 8, 0.8f);
                if (slot.Mastery)
                {
                    var extra = rooms.NearestEnemies(target.transform.position, 7f, 3);
                    int spawned = 0;
                    foreach (var e in extra)
                    {
                        if (e == null || e == target || spawned >= 2) continue;
                        Hazard.Poison(e.transform.position, radius * 0.8f, dps * 0.75f, duration * 0.8f, true);
                        Game.I.Fx.Dust(e.transform.position, 5, 0.6f);
                        spawned++;
                    }
                }
                Game.I.Audio.Play("sfx_poison", 0.7f);
                return true;
            }
            case "meteor":
            {
                if (target == null) return false;
                Vector2 p = target.transform.position;
                float radius = 2.4f * area;
                float damage = player.DamageOut(55f * power);
                Game.I.Fx.TelegraphedCircle(p, radius, 1.0f, new Color(1f, 0.5f, 0.25f));
                StartCoroutine(MeteorRoutine(p, 1.0f, damage, radius, slot.Mastery));
                Game.I.Audio.Play("sfx_wave", 0.6f);
                return true;
            }
            case "blink":
            {
                Vector2 d = player.Facing;
                if (d.sqrMagnitude < 0.1f) d = Vector2.right;
                Vector2 dest = rooms.Clamp(pos + d * 3.6f, 0.4f);
                for (int i = 0; i < 4; i++)
                {
                    Game.I.Fx.Afterimage(player.Visual.Main);
                    player.transform.position = Vector2.Lerp(pos, dest, (i + 1) / 4f);
                }
                player.transform.position = dest;
                player.Invuln = Mathf.Max(player.Invuln, slot.Mastery ? 0.6f : 0.35f);
                Game.I.Fx.Ring(pos, new Color(0.5f, 0.9f, 0.9f), 0.9f, 0.35f);
                Game.I.Fx.Ring(dest, new Color(0.5f, 0.9f, 0.9f), 1.0f, 0.35f);
                if (slot.Mastery)
                {
                    float radius = 2.4f * area;
                    float damage = player.DamageOut(30f * power);
                    Game.I.Fx.Explosion(pos, radius, new Color(0.45f, 0.9f, 1f));
                    Game.I.Fx.Explosion(dest, radius, new Color(0.45f, 0.9f, 1f));
                    rooms.DamageEnemies(pos, radius, damage);
                    rooms.DamageEnemies(dest, radius, damage);
                }
                Game.I.Audio.Play("sfx_dash", 0.8f);
                return true;
            }
            case "missiles":
            {
                int count = slot.Mastery ? 10 : 5;
                var targets = rooms.NearestEnemies(pos, 14f, count);
                for (int i = 0; i < count; i++)
                {
                    Vector2 d;
                    if (targets.Count > 0)
                    {
                        var t = targets[i % targets.Count];
                        d = t != null
                            ? ((Vector2)t.transform.position + Vector2.up * 0.5f - origin).normalized
                            : Random.insideUnitCircle.normalized;
                    }
                    else
                    {
                        d = Random.insideUnitCircle.normalized;
                    }
                    Projectile.Player(origin, d, player.DamageOut(9f * power), 8f, "proj_bolt",
                        new Color(0.8f, 0.7f, 1f), 1.5f, slot.Mastery ? 1 : 0, 0f,
                        false, false, false, true, 3f);
                }
                Game.I.Audio.Play("sfx_cast", 0.7f, 1.25f);
                return true;
            }
            case "slow":
            {
                if (slot.Mastery)
                {
                    foreach (var e in rooms.Enemies)
                    {
                        if (e != null && !e.Dead) e.Freeze(2.2f, 0.12f);
                    }
                    Projectile.ClearEnemyProjectiles();
                    Game.I.Fx.Ring(pos, new Color(0.75f, 0.6f, 1f), 8f, 0.9f);
                }
                else
                {
                    foreach (var e in rooms.Enemies)
                    {
                        if (e != null && !e.Dead) e.Slow(3f, 0.5f);
                    }
                    Game.I.Fx.Ring(pos, new Color(0.75f, 0.6f, 1f), 4.5f, 0.7f);
                }
                Game.I.Audio.Play("sfx_ice", 0.7f, 0.7f);
                return true;
            }
            case "golem":
            {
                int count = slot.Mastery ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    float side = i == 0 ? 1f : -1f;
                    Ally.Summon(pos + Vector2.right * (1.1f * side));
                }
                Game.I.Audio.Play("sfx_spawn", 0.8f);
                return true;
            }
            case "rage":
            {
                player.Rage(6f * slot.Duration, slot.Mastery);
                Game.I.Fx.Ring(pos, new Color(1f, 0.4f, 0.3f), 1.5f, 0.4f);
                return true;
            }
        }
        return false;
    }

    private void NovaPulse(Vector2 pos, float radius, float damage, Player player)
    {
        Game.I.Fx.Ring(pos, new Color(0.55f, 0.9f, 1f), Mathf.Min(radius, 12f), 0.5f);
        Game.I.Fx.Burst("fx_flame", pos, 12, 3.8f, 0.5f, 0.5f, new Color(0.65f, 0.95f, 1f), 1600);
        Game.I.Rooms.DamageEnemies(pos, radius, damage);
        foreach (var e in Game.I.Rooms.EnemiesInRadius(pos, radius))
        {
            if (e != null && !e.Dead) e.Slow(3f, 0.5f);
        }
    }

    private IEnumerator NovaSecondPulse(Vector2 pos, float radius, Player player, Slot slot)
    {
        yield return new WaitForSeconds(0.42f);
        if (Game.I == null || Game.I.State != GameState.Combat || player == null || player.Dead) yield break;
        Game.I.Fx.Ring(pos, new Color(0.75f, 0.95f, 1f), Mathf.Min(radius, 12f), 0.55f);
        Game.I.Fx.Burst("fx_flame", pos, 20, 4.8f, 0.55f, 0.6f, new Color(0.75f, 0.95f, 1f), 1610);
        Game.I.Rooms.DamageEnemies(pos, radius, player.DamageOut(22f * slot.Power));
        foreach (var e in Game.I.Rooms.EnemiesInRadius(pos, radius))
        {
            if (e != null && !e.Dead) e.Freeze(1.1f, 0.08f);
        }
        Game.I.Audio.Play("sfx_ice", 0.8f, 1.15f);
    }

    private IEnumerator CleaveRoutine(Player player, Slot slot)
    {
        player.CastLock = Mathf.Max(player.CastLock, 0.56f);
        player.Visual.Windup(0.3f);
        player.Visual.Flash(new Color(1f, 0.86f, 0.55f), 0.16f);
        Game.I.Audio.Play("sfx_cast", 0.45f, 0.7f);
        yield return new WaitForSeconds(0.22f);
        if (Game.I == null || Game.I.State != GameState.Combat || player == null || player.Dead) yield break;

        Vector2 center = player.transform.position;
        float radius = 2.7f * slot.Area;
        Game.I.Fx.Ring(center + Vector2.up * 0.5f, new Color(1f, 0.82f, 0.45f, 0.8f), radius, 0.4f);
        Vector2[] dirs =
        {
            Vector2.right, Vector2.up, Vector2.left, Vector2.down,
            new Vector2(1f, 1f).normalized, new Vector2(-1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized, new Vector2(1f, -1f).normalized
        };
        foreach (var d in dirs)
        {
            Game.I.Fx.Slash(center + d * 1.15f + Vector2.up * 0.55f, d);
        }
        Game.I.Rooms.DamageEnemies(center, radius, player.DamageOut(38f * slot.Power));
        Game.I.Audio.Play("sfx_hit", 0.9f, 0.65f);
        Game.I.Shake(0.2f, 0.32f);
        Game.I.Hitstop(0.045f);

        if (slot.Mastery)
        {
            yield return new WaitForSeconds(0.3f);
            if (Game.I == null || Game.I.State != GameState.Combat || player == null || player.Dead) yield break;
            float shockRadius = radius * 1.65f;
            Game.I.Fx.Ring(center + Vector2.up * 0.45f, new Color(1f, 0.7f, 0.3f, 0.9f), shockRadius, 0.45f);
            Game.I.Fx.Burst("fx_dust", center + Vector2.up * 0.3f, 18, 5f, 0.5f, 0.8f,
                new Color(1f, 0.8f, 0.45f, 0.8f), 1610);
            Game.I.Rooms.DamageEnemies(center, shockRadius, player.DamageOut(26f * slot.Power));
            Game.I.Shake(0.3f, 0.4f);
            Game.I.Audio.Play("sfx_explosion", 0.6f, 1.3f);
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    private IEnumerator MeteorRoutine(Vector2 p, float delay, float damage, float radius, bool mastery)
    {
        yield return new WaitForSeconds(delay);
        if (Game.I == null || Game.I.State != GameState.Combat) yield break;
        MeteorImpact(p, damage, radius);
        if (!mastery) yield break;
        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForSeconds(0.18f);
            if (Game.I == null || Game.I.State != GameState.Combat) yield break;
            Vector2 offset = Random.insideUnitCircle * 2.2f;
            MeteorImpact(p + offset, damage * 0.55f, radius * 0.82f);
        }
    }

    private void MeteorImpact(Vector2 p, float damage, float radius)
    {
        Vector3 center = new Vector3(p.x, p.y, 0f);
        Game.I.Fx.Explosion(center, radius, new Color(1f, 0.55f, 0.2f));
        Game.I.Rooms.DamageEnemies(p, radius, damage);
        Game.I.Audio.Play("sfx_explosion", 0.9f);
        Game.I.Shake(0.25f, 0.45f);
    }
}
