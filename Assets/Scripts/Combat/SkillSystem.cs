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
        }
        Slots[0].Def = Data.Skill("fireball");
        Slots[1].Def = Data.Skill("shield");
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
        if (!Cast(slot.Def)) return;
        float cd = slot.Def.Cooldown * (1f - Mathf.Clamp(Player.Stats.cdr, 0f, 0.55f));
        slot.Cd = cd;
        slot.MaxCd = cd;
        Game.I.Audio.Play("sfx_cast", 0.55f, 1f, 0.08f);
    }

    private bool Cast(SkillDef def)
    {
        var rooms = Game.I.Rooms;
        var player = Player;
        Vector2 pos = player.transform.position;
        Vector2 origin = pos + Vector2.up * 0.8f;
        Enemy target = rooms.NearestEnemy(pos, 13f);

        switch (def.Id)
        {
            case "fireball":
            {
                if (target == null) return false;
                Vector2 dir = ((Vector2)target.transform.position + Vector2.up * 0.5f - origin).normalized;
                Projectile.Player(origin, dir, player.DamageOut(28f), 11.5f, "proj_fire",
                    new Color(1f, 0.62f, 0.25f), 2.2f, 0, 2.2f, false, false, false, false, 2.2f);
                Game.I.Audio.Play("sfx_fireball", 0.75f);
                Game.I.Fx.Sprite("fx_flame", origin, 0.6f, 0.25f, new Color(1f, 0.7f, 0.3f), 1550);
                return true;
            }
            case "shield":
                player.AddShield(35f, 8f);
                return true;
            case "nova":
            {
                Game.I.Fx.Ring(pos, new Color(0.55f, 0.9f, 1f), 3.6f, 0.5f);
                Game.I.Fx.Burst("fx_flame", pos, 12, 3.8f, 0.5f, 0.5f, new Color(0.65f, 0.95f, 1f), 1600);
                rooms.DamageEnemies(pos, 3.6f, player.DamageOut(18f));
                foreach (var e in rooms.EnemiesInRadius(pos, 3.6f)) e.Slow(3f, 0.5f);
                Game.I.Audio.Play("sfx_ice", 0.8f);
                Game.I.Shake(0.12f, 0.3f);
                return true;
            }
            case "chain":
            {
                if (target == null) return false;
                var targets = rooms.NearestEnemies(pos, 12f, 3);
                float[] mult = { 1f, 0.7f, 0.45f };
                Vector2 from = origin;
                for (int i = 0; i < targets.Count; i++)
                {
                    var e = targets[i];
                    Game.I.Fx.Line(from, e.transform.position + Vector3.up * 0.6f,
                        new Color(1f, 0.9f, 0.45f), 0.16f, 0.28f);
                    e.TakeDamage(player.DamageOut(24f * mult[i]), pos, true, false);
                    from = e.transform.position + Vector3.up * 0.6f;
                }
                Game.I.Audio.Play("sfx_lightning", 0.85f);
                Game.I.Fx.Burst("fx_flame", target.transform.position, 4, 2.5f, 0.3f, 0.35f,
                    new Color(1f, 0.9f, 0.4f), 1620);
                return true;
            }
            case "heal":
            {
                if (player.Stats.hp >= player.Stats.maxHp - 1f) return false;
                player.Heal(player.Stats.maxHp * 0.30f);
                Game.I.Fx.Heal(pos);
                Game.I.Audio.Play("sfx_heal", 0.8f);
                return true;
            }
            case "poison":
            {
                if (target == null) return false;
                Hazard.Poison(target.transform.position, 2.3f, 6f, 6f, true);
                Game.I.Fx.Dust(target.transform.position, 8, 0.8f);
                Game.I.Audio.Play("sfx_poison", 0.7f);
                return true;
            }
            case "meteor":
            {
                if (target == null) return false;
                Vector2 p = target.transform.position;
                Game.I.Fx.TelegraphedCircle(p, 2.4f, 1.0f, new Color(1f, 0.5f, 0.25f));
                StartCoroutine(MeteorRoutine(p, 1.0f));
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
                player.Invuln = Mathf.Max(player.Invuln, 0.35f);
                Game.I.Fx.Ring(pos, new Color(0.5f, 0.9f, 0.9f), 0.9f, 0.35f);
                Game.I.Fx.Ring(dest, new Color(0.5f, 0.9f, 0.9f), 1.0f, 0.35f);
                Game.I.Audio.Play("sfx_dash", 0.8f);
                return true;
            }
            case "missiles":
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 d = Random.insideUnitCircle.normalized;
                    Projectile.Player(origin, d, player.DamageOut(9f), 8f, "proj_bolt",
                        new Color(0.8f, 0.7f, 1f), 1.5f, 0, 0f, false, false, false, true, 3f);
                }
                Game.I.Audio.Play("sfx_cast", 0.7f, 1.25f);
                return true;
            }
            case "slow":
            {
                foreach (var e in rooms.Enemies) e.Slow(3f, 0.5f);
                Game.I.Fx.Ring(pos, new Color(0.75f, 0.6f, 1f), 4.5f, 0.7f);
                Game.I.Audio.Play("sfx_ice", 0.7f, 0.7f);
                return true;
            }
            case "golem":
            {
                Ally.Summon(pos + Vector2.right * 1.1f);
                Game.I.Audio.Play("sfx_spawn", 0.8f);
                return true;
            }
            case "rage":
            {
                player.Rage(6f);
                Game.I.Fx.Ring(pos, new Color(1f, 0.4f, 0.3f), 1.5f, 0.4f);
                return true;
            }
        }
        return false;
    }

    private IEnumerator MeteorRoutine(Vector2 p, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (Game.I == null || Game.I.State != GameState.Combat) yield break;
        Vector3 center = new Vector3(p.x, p.y, 0f);
        Game.I.Fx.Explosion(center, 2.5f, new Color(1f, 0.55f, 0.2f));
        Game.I.Rooms.DamageEnemies(p, 2.4f, Player.DamageOut(55f));
        Game.I.Audio.Play("sfx_explosion", 0.9f);
        Game.I.Shake(0.25f, 0.45f);
    }
}
