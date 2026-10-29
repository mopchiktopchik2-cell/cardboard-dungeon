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
    public bool Endless;
    public int EndlessSeed;
    public int Coins;
    public readonly Dictionary<string, int> PerkCounts = new Dictionary<string, int>();
    public readonly List<PerkDef> TakenPerks = new List<PerkDef>();
    public readonly Dictionary<string, int> SkillLevels = new Dictionary<string, int>();

    private readonly Dictionary<int, RoomDef> endlessRooms = new Dictionary<int, RoomDef>();

    public int RoomCount => Endless
        ? EndlessContent.RoomCap
        : Dungeon != null ? Dungeon.Rooms.Length : Data.RoomCount;

    public EnemyKind BossKind => Endless
        ? EndlessContent.BossFor(RoomIndex)
        : Dungeon != null ? Dungeon.BossKind : EnemyKind.Lich;

    public string BossName => Endless
        ? Data.Enemy(BossKind).Name
        : Dungeon != null ? Dungeon.BossName : "ЛИЧ-ПЕРЕПЛЁТ";

    public float EnemyHpScale => Endless
        ? EndlessContent.EnemyHpScale(RoomIndex)
        : (Dungeon != null ? Dungeon.HpScale : 1f) *
          (1f + RoomIndex * (Dungeon != null ? Dungeon.PerRoomHp : 0.16f));

    public float EnemyDamageScale => Endless
        ? EndlessContent.EnemyDamageScale(RoomIndex)
        : (Dungeon != null ? Dungeon.DamageScale : 1f) *
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

    public void StartRun(bool endless = false)
    {
        RunId++;
        Endless = endless;
        Dungeon = endless ? EndlessContent.Dungeon : MetaProgress.SelectedDungeon;
        Hero = MetaProgress.SelectedClass;
        EndlessSeed = endless ? Random.Range(1, int.MaxValue) : 0;
        RoomIndex = 0;
        Kills = 0;
        DamageDealt = 0f;
        DamageTaken = 0f;
        RunTime = 0f;
        Coins = endless ? EndlessContent.StartCoins : 0;
        VictoryLoot = null;
        PerkCounts.Clear();
        TakenPerks.Clear();
        SkillLevels.Clear();
        endlessRooms.Clear();
        Game.I.Player.ResetForRun();
        Game.I.Skills.ResetForRun();
        foreach (var slot in Game.I.Skills.Slots)
        {
            if (slot.Def != null) SkillLevels[slot.Def.Id] = 1;
        }
        Game.I.UI.RefreshRoom();
        Game.I.UI.RefreshSkills();
        Game.I.UI.RefreshCoins();
    }

    public RoomDef RoomAt(int index)
    {
        if (!Endless)
        {
            if (Dungeon == null || Dungeon.Rooms == null || Dungeon.Rooms.Length == 0) return null;
            return Dungeon.Rooms[Mathf.Clamp(index, 0, Dungeon.Rooms.Length - 1)];
        }
        index = Mathf.Max(0, index);
        if (index >= EndlessContent.RoomCap) return null;
        RoomDef room;
        if (!endlessRooms.TryGetValue(index, out room))
        {
            room = EndlessContent.RoomFor(index, EndlessSeed);
            endlessRooms[index] = room;
        }
        return room;
    }

    public bool IsBossRoom(int index)
    {
        return Endless ? EndlessContent.IsBoss(index) : index == RoomCount - 1;
    }

    public int SkillLevel(string id)
    {
        if (string.IsNullOrEmpty(id)) return 1;
        int level;
        return SkillLevels.TryGetValue(id, out level) ? Mathf.Clamp(level, 1, EndlessContent.MaxSkillLevel) : 1;
    }

    public void EnsureSkillLevel(string id)
    {
        if (string.IsNullOrEmpty(id) || SkillLevels.ContainsKey(id)) return;
        SkillLevels[id] = 1;
    }

    public void SetSkillLevel(string id, int level)
    {
        if (string.IsNullOrEmpty(id)) return;
        SkillLevels[id] = Mathf.Clamp(level, 1, EndlessContent.MaxSkillLevel);
        if (Game.I != null && Game.I.Skills != null) Game.I.Skills.SetLevel(id, SkillLevels[id]);
        if (Game.I != null && Game.I.UI != null) Game.I.UI.RefreshSkills();
    }

    public void AddCoins(int amount)
    {
        if (!Endless || amount <= 0) return;
        Coins += amount;
        if (Game.I != null && Game.I.UI != null) Game.I.UI.RefreshCoins();
    }

    public LootRoll RollVictoryLoot()
    {
        VictoryLoot = MetaProgress.RollDungeonDrop(Dungeon);
        return VictoryLoot;
    }

    public void OnKill(Enemy enemy)
    {
        Kills++;
        if (Endless && enemy != null)
        {
            int drop = EndlessContent.CoinDrop(enemy.Boss, enemy.Elite, RoomIndex);
            CoinPickup.Drop(enemy.transform.position, drop);
        }
    }

    public void OnRoomCleared()
    {
        if (Endless)
        {
            CoinPickup.CollectAll();
            AddCoins(EndlessContent.RoomClearBonus(RoomIndex));
        }
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
            if (!Data.PerkAllowedForClass(perk.Id, Hero)) continue;
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
            if (!Data.SkillAllowedForClass(s.Id, Hero)) continue;
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
        if (!Data.PerkAllowedForClass(perk.Id, Hero)) return false;
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
            if (!Data.SkillAllowedForClass(skill.Id, Hero)) continue;
            if (!Game.I.Skills.HasSkill(skill.Id)) return true;
        }
        return false;
    }

    private bool HasEmptySlot()
    {
        var skills = Game.I != null ? Game.I.Skills : null;
        if (skills == null) return false;
        for (int i = 0; i < skills.Slots.Length; i++)
        {
            if (i < skills.MaxSlots && skills.Slots[i].Def == null) return true;
        }
        return false;
    }

    public List<ShopOffer> BuildShopOffers(int count)
    {
        var result = new List<ShopOffer>();
        if (!Endless || Game.I == null || Game.I.Skills == null) return result;

        var upgrades = new List<ShopOffer>();
        var newSkills = new List<ShopOffer>();
        var utility = new List<ShopOffer>();

        foreach (var slot in Game.I.Skills.Slots)
        {
            if (slot == null || slot.Def == null) continue;
            int level = SkillLevel(slot.Def.Id);
            if (level >= EndlessContent.MaxSkillLevel) continue;
            upgrades.Add(UpgradeOffer(slot.Def, level + 1));
        }

        bool emptySlot = HasEmptySlot();
        foreach (var skill in Data.Skills)
        {
            if (!Data.SkillAllowedForClass(skill.Id, Hero)) continue;
            if (Game.I.Skills.HasSkill(skill.Id)) continue;
            if (!emptySlot && Game.I.Skills.MaxSlots >= Data.MaxSkillSlots) continue;
            newSkills.Add(NewSkillOffer(skill, !emptySlot));
        }

        var player = Game.I.Player;
        if (player != null && player.Stats.hp < player.Stats.maxHp * 0.92f)
        {
            utility.Add(HealOffer());
        }

        var perkRoll = RollPerks(1);
        if (perkRoll.Count > 0) utility.Add(PerkOffer(perkRoll[0]));
        utility.Add(StatOffer("damage"));
        utility.Add(StatOffer("hp"));
        utility.Add(StatOffer("skillpower"));

        var rng = new System.Random(RunId * 977 + RoomIndex * 131 + 7);
        ShopOffer guaranteedUpgrade = PickOne(upgrades, rng);
        if (guaranteedUpgrade != null) result.Add(guaranteedUpgrade);
        ShopOffer guaranteedSkill = PickOne(newSkills, rng);
        if (guaranteedSkill != null && !result.Contains(guaranteedSkill)) result.Add(guaranteedSkill);

        var pool = new List<ShopOffer>();
        pool.AddRange(upgrades);
        pool.AddRange(newSkills);
        pool.AddRange(utility);
        while (result.Count < count && pool.Count > 0)
        {
            int idx = rng.Next(0, pool.Count);
            var offer = pool[idx];
            pool.RemoveAt(idx);
            if (offer != null && !result.Contains(offer)) result.Add(offer);
        }

        // Страховка: в лавке всегда должно быть чем потратить монеты.
        while (result.Count < count)
        {
            result.Add(StatOffer(result.Count % 2 == 0 ? "damage" : "hp"));
        }
        return result;
    }

    private static ShopOffer PickOne(List<ShopOffer> list, System.Random rng)
    {
        if (list == null || list.Count == 0) return null;
        return list[rng.Next(0, list.Count)];
    }

    private ShopOffer UpgradeOffer(SkillDef skill, int targetLevel)
    {
        bool mastery = targetLevel >= EndlessContent.MaxSkillLevel;
        return new ShopOffer
        {
            Kind = ShopOfferKind.Upgrade,
            Skill = skill,
            TargetLevel = targetLevel,
            Cost = EndlessContent.UpgradeCost(targetLevel, RoomIndex),
            Title = skill.Name + " · УР. " + targetLevel,
            Desc = EndlessContent.LevelEffect(skill.Id, targetLevel),
            Badge = mastery ? "МАСТЕРИЯ" : "УЛУЧШЕНИЕ",
            Icon = skill.Icon,
            Accent = mastery ? Pal.Purple : skill.Color
        };
    }

    private ShopOffer NewSkillOffer(SkillDef skill, bool needsSlot)
    {
        return new ShopOffer
        {
            Kind = ShopOfferKind.NewSkill,
            Skill = skill,
            NeedsSlot = needsSlot,
            Cost = EndlessContent.NewSkillCost(needsSlot, RoomIndex),
            Title = skill.Name,
            Desc = (needsSlot ? "Открыть новый слот и изучить навык.\n" : "Изучить новый навык.\n") +
                   "<color=#BFAF96>" + skill.Desc + "</color>",
            Badge = needsSlot ? "СЛОТ + НАВЫК" : "НОВЫЙ НАВЫК",
            Icon = skill.Icon,
            Accent = skill.Color
        };
    }

    private ShopOffer PerkOffer(PerkDef perk)
    {
        return new ShopOffer
        {
            Kind = ShopOfferKind.Perk,
            Perk = perk,
            Cost = EndlessContent.PerkCost(RoomIndex),
            Title = perk.Name,
            Desc = perk.Desc,
            Badge = "ПЕРК",
            Icon = perk.Icon,
            Accent = Pal.Gold
        };
    }

    private ShopOffer HealOffer()
    {
        return new ShopOffer
        {
            Kind = ShopOfferKind.Heal,
            Cost = EndlessContent.HealCost(RoomIndex),
            Title = "ПЕРЕВЯЗКА",
            Desc = "Восстановить 45% максимального здоровья.",
            Badge = "ЛЕЧЕНИЕ",
            Icon = "icon_heal",
            Accent = Pal.Green
        };
    }

    private ShopOffer StatOffer(string id)
    {
        string title;
        string desc;
        string icon;
        switch (id)
        {
            case "hp":
                title = "ПЛОТНАЯ БУМАГА";
                desc = "+25 к максимальному здоровью.";
                icon = "icon_perk_hp";
                break;
            case "skillpower":
                title = "СИЛЬНАЯ МАГИЯ";
                desc = "+10% силы всех навыков.";
                icon = "icon_perk_armor";
                break;
            default:
                title = "ЗАКАЛКА";
                desc = "+10% урона автоатаки.";
                icon = "icon_perk_damage";
                break;
        }
        return new ShopOffer
        {
            Kind = ShopOfferKind.Stat,
            StatId = id,
            Cost = EndlessContent.StatCost(RoomIndex),
            Title = title,
            Desc = desc,
            Badge = "УСИЛЕНИЕ",
            Icon = icon,
            Accent = Pal.Cream
        };
    }

    public bool TryBuy(ShopOffer offer, out string error)
    {
        error = null;
        if (!Endless)
        {
            error = "Лавка доступна только в бесконечном режиме.";
            return false;
        }
        if (offer == null)
        {
            error = "Товар уже распродан.";
            return false;
        }
        if (Coins < offer.Cost)
        {
            error = "Не хватает " + (offer.Cost - Coins) + " монет.";
            return false;
        }

        var player = Game.I.Player;
        switch (offer.Kind)
        {
            case ShopOfferKind.Upgrade:
            {
                if (offer.Skill == null || !Game.I.Skills.HasSkill(offer.Skill.Id))
                {
                    error = "Этот навык больше не в слотах.";
                    return false;
                }
                int current = SkillLevel(offer.Skill.Id);
                if (offer.TargetLevel != current + 1 || offer.TargetLevel > EndlessContent.MaxSkillLevel)
                {
                    error = "Уровень уже изменился.";
                    return false;
                }
                Coins -= offer.Cost;
                SetSkillLevel(offer.Skill.Id, offer.TargetLevel);
                if (offer.TargetLevel >= EndlessContent.MaxSkillLevel)
                {
                    Game.I.Fx.Ring(player.transform.position, Pal.Purple, 2.2f, 0.7f);
                    Game.I.Audio.Play("sfx_levelup", 0.9f);
                }
                break;
            }
            case ShopOfferKind.NewSkill:
            {
                if (offer.Skill == null || Game.I.Skills.HasSkill(offer.Skill.Id))
                {
                    error = "Этот навык уже изучен.";
                    return false;
                }
                bool needsSlot = !HasEmptySlot();
                if (needsSlot && Game.I.Skills.MaxSlots >= Data.MaxSkillSlots)
                {
                    error = "Все слоты навыков заняты.";
                    return false;
                }
                if (needsSlot) Game.I.Skills.UnlockSlot();
                if (!Game.I.Skills.AddSkill(offer.Skill.Id))
                {
                    error = "Не удалось добавить навык.";
                    return false;
                }
                Coins -= offer.Cost;
                EnsureSkillLevel(offer.Skill.Id);
                break;
            }
            case ShopOfferKind.Perk:
            {
                if (!AddPerk(offer.Perk))
                {
                    error = "Этот перк больше недоступен.";
                    return false;
                }
                Coins -= offer.Cost;
                break;
            }
            case ShopOfferKind.Heal:
            {
                Coins -= offer.Cost;
                player.Heal(player.Stats.maxHp * 0.45f);
                Game.I.Fx.Heal(player.transform.position);
                Game.I.Audio.Play("sfx_heal", 0.8f);
                break;
            }
            case ShopOfferKind.Stat:
            {
                Coins -= offer.Cost;
                var stats = player.Stats;
                if (offer.StatId == "hp")
                {
                    stats.maxHp += 25f;
                    player.Heal(25f);
                }
                else if (offer.StatId == "skillpower")
                {
                    stats.skillPower += 0.10f;
                }
                else
                {
                    stats.damage *= 1.10f;
                }
                break;
            }
        }

        Game.I.UI.RefreshCoins();
        Game.I.UI.RefreshSkills();
        return true;
    }

    public void SaveRecord(bool victory = false)
    {
        if (Dungeon == null) return;
        if (Endless)
        {
            MetaProgress.RecordEndless(Mathf.Clamp(RoomIndex + 1, 1, RoomCount), Kills, Coins);
            return;
        }
        int reached = victory ? RoomCount : Mathf.Clamp(RoomIndex + 1, 1, RoomCount);
        MetaProgress.RecordRun(Dungeon.Id, reached, Kills, victory);
    }

    public int BestRoom => Endless
        ? MetaProgress.BestEndlessRoom
        : Dungeon != null ? MetaProgress.BestRoom(Dungeon.Id) : 0;

    public int BestKills => Endless
        ? MetaProgress.BestEndlessKills
        : Dungeon != null ? MetaProgress.BestKills(Dungeon.Id) : 0;

    public int Clears => Dungeon != null ? MetaProgress.Clears(Dungeon.Id) : 0;
}
