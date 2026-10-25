using System.Collections.Generic;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public int RoomIndex;
    public int RunId { get; private set; }
    public int Kills;
    public float DamageDealt;
    public float DamageTaken;
    public float RunTime;
    public DungeonDef Dungeon;
    public HeroClass Hero;
    public LootRoll VictoryLoot;
    public readonly Dictionary<string, int> PerkCounts = new Dictionary<string, int>();
    public readonly List<PerkDef> TakenPerks = new List<PerkDef>();

    public int RoomCount => Dungeon != null ? Dungeon.Rooms.Length : Data.RoomCount;
    public string BossName => Dungeon != null ? Dungeon.BossName : "ЛИЧ-ПЕРЕПЛЁТ";

    public float EnemyHpScale =>
        (Dungeon != null ? Dungeon.HpScale : 1f) * (1f + RoomIndex * (Dungeon != null ? Dungeon.PerRoomHp : 0.16f));

    public float EnemyDamageScale =>
        (Dungeon != null ? Dungeon.DamageScale : 1f) *
        (1f + RoomIndex * (Dungeon != null ? Dungeon.PerRoomDamage : 0.10f));

    public void Init()
    {
        Dungeon = MetaProgress.SelectedDungeon;
        Hero = MetaProgress.SelectedClass;
    }

    private void Update()
    {
        if (Game.I != null && Game.I.InRun && Game.I.State != GameState.Pause)
        {
            RunTime += Time.deltaTime;
        }
    }

    public void StartRun()
    {
        RunId++;
        Dungeon = MetaProgress.SelectedDungeon;
        Hero = MetaProgress.SelectedClass;
        RoomIndex = 0;
        Kills = 0;
        DamageDealt = 0f;
        DamageTaken = 0f;
        RunTime = 0f;
        VictoryLoot = null;
        PerkCounts.Clear();
        TakenPerks.Clear();
        Game.I.Player.ResetForRun();
        Game.I.Skills.ResetForRun();
        Game.I.UI.RefreshRoom();
        Game.I.UI.RefreshSkills();
    }

    public LootRoll RollVictoryLoot()
    {
        VictoryLoot = MetaProgress.RollDungeonDrop(Dungeon);
        return VictoryLoot;
    }

    public void OnKill(Enemy enemy)
    {
        Kills++;
    }

    public void OnRoomCleared()
    {
        var p = Game.I.Player;
        if (p.Stats.roomHeal > 0f)
        {
            p.Heal(p.Stats.maxHp * p.Stats.roomHeal);
            Game.I.Fx.Heal(p.transform.position);
        }
    }

    public List<PerkDef> RollPerks(int count)
    {
        var candidates = new List<PerkDef>();
        foreach (var perk in Data.Perks)
        {
            if (perk.Unique)
            {
                int owned = PerkCounts.TryGetValue(perk.Id, out var c) ? c : 0;
                if (owned > 0) continue;
            }
            if (IsPerkCapped(perk)) continue;
            if (!Data.PerkAllowedForClass(perk.Id, MetaProgress.SelectedClass)) continue;
            candidates.Add(perk);
        }

        var result = new List<PerkDef>();
        for (int n = 0; n < count && candidates.Count > 0; n++)
        {
            float total = 0f;
            foreach (var c in candidates) total += c.Weight;
            float roll = Random.value * total;
            PerkDef picked = candidates[candidates.Count - 1];
            foreach (var c in candidates)
            {
                roll -= c.Weight;
                if (roll <= 0f)
                {
                    picked = c;
                    break;
                }
            }
            result.Add(picked);
            candidates.Remove(picked);
        }
        return result;
    }

    public List<SkillDef> RollSkills(int count)
    {
        var candidates = new List<SkillDef>();
        foreach (var s in Data.Skills)
        {
            if (!Data.SkillAllowedForClass(s.Id, MetaProgress.SelectedClass)) continue;
            if (!Game.I.Skills.HasSkill(s.Id)) candidates.Add(s);
        }
        var result = new List<SkillDef>();
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            result.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }
        return result;
    }

    public bool AddPerk(PerkDef perk)
    {
        if (perk == null) return false;
        if (!Data.PerkAllowedForClass(perk.Id, MetaProgress.SelectedClass)) return false;
        if (perk.Unique && PerkCounts.TryGetValue(perk.Id, out var owned) && owned > 0) return false;
        if (IsPerkCapped(perk)) return false;
        if (perk.Id == "slot" &&
            (Game.I == null || Game.I.Skills == null || Game.I.Skills.MaxSlots >= Data.MaxSkillSlots))
        {
            return false;
        }

        PerkCounts[perk.Id] = (PerkCounts.TryGetValue(perk.Id, out var c) ? c : 0) + 1;
        TakenPerks.Add(perk);
        var s = Game.I.Player.Stats;
        switch (perk.Id)
        {
            case "damage":
                s.damage *= 1.18f;
                break;
            case "attackspeed":
                s.attackInterval = Mathf.Max(0.18f, s.attackInterval * 0.86f);
                break;
            case "movespeed":
                s.speed *= 1.08f;
                break;
            case "hp":
                s.maxHp += 20f;
                Game.I.Player.Heal(20f);
                break;
            case "lifesteal":
                s.lifesteal = Mathf.Min(Data.LifestealCap, s.lifesteal + Data.LifestealPerPerk);
                break;
            case "crit":
                s.critChance = Mathf.Min(0.75f, s.critChance + 0.10f);
                break;
            case "pierce":
                s.pierce += 1;
                break;
            case "multi":
                s.multishot = Mathf.Min(5, s.multishot + 1);
                break;
            case "bomb":
                s.killBombs = true;
                break;
            case "shieldstart":
                s.startShield += 10f;
                break;
            case "cdr":
                s.cdr = Mathf.Min(0.55f, s.cdr + 0.15f);
                break;
            case "slot":
                Game.I.Skills.UnlockSlot();
                break;
            case "freeze":
                s.freezeShot = true;
                break;
            case "poison":
                s.poisonShot = true;
                break;
            case "thorns":
                s.thorns += 8f;
                break;
            case "healroom":
                s.roomHeal += 0.12f;
                break;
            case "secondwind":
                s.secondWind = true;
                break;
            case "skillpower":
                s.skillPower += 0.25f;
                break;
            case "armor":
                s.damageReduction = Mathf.Min(0.4f, s.damageReduction + 0.08f);
                break;
        }
        Game.I.UI.RefreshSkills();
        return true;
    }

    private bool IsPerkCapped(PerkDef perk)
    {
        if (Game.I == null || Game.I.Player == null || Game.I.Player.Stats == null) return false;
        var s = Game.I.Player.Stats;
        var skills = Game.I.Skills;
        switch (perk.Id)
        {
            case "attackspeed": return s.attackInterval <= 0.181f;
            case "crit": return s.critChance >= 0.749f;
            case "multi": return s.multishot >= 5;
            case "cdr": return s.cdr >= 0.549f;
            case "armor": return s.damageReduction >= 0.399f;
            case "bomb": return s.killBombs;
            case "freeze": return s.freezeShot;
            case "poison": return s.poisonShot;
            case "secondwind": return s.secondWind;
            case "lifesteal": return s.lifesteal >= Data.LifestealCap - 0.001f;
            case "slot": return skills == null || skills.MaxSlots >= Data.MaxSkillSlots || !HasUnlearnedSkills();
        }
        return false;
    }

    public bool HasUnlearnedSkills()
    {
        if (Game.I == null || Game.I.Skills == null) return true;
        foreach (var skill in Data.Skills)
        {
            if (!Data.SkillAllowedForClass(skill.Id, MetaProgress.SelectedClass)) continue;
            if (!Game.I.Skills.HasSkill(skill.Id)) return true;
        }
        return false;
    }

    public void SaveRecord(bool victory = false)
    {
        if (Dungeon == null) return;
        int reached = victory ? RoomCount : Mathf.Clamp(RoomIndex + 1, 1, RoomCount);
        MetaProgress.RecordRun(Dungeon.Id, reached, Kills, victory);
    }

    public int BestRoom => Dungeon != null ? MetaProgress.BestRoom(Dungeon.Id) : 0;
    public int BestKills => Dungeon != null ? MetaProgress.BestKills(Dungeon.Id) : 0;
    public int Clears => Dungeon != null ? MetaProgress.Clears(Dungeon.Id) : 0;
}
