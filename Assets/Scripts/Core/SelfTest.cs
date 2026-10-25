using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Быстрые проверки контента v1.3. Запускаются из редактора (Cardboard/Run Self Tests)
/// и не зависят от сцены или запущенной игры.
/// </summary>
public static class SelfTest
{
    private static int passed;
    private static int failed;

    public static bool RunAll()
    {
        passed = 0;
        failed = 0;
        Debug.Log("[SelfTest] begin v1.3");

        CheckSkills();
        CheckPerks();
        CheckClasses();
        CheckDungeons();
        CheckSettings();
        CheckEnemies();
        CheckRooms();
        CheckEquipment();
        CheckAssets();
        CheckBalance();

        Debug.Log("[SelfTest] passed=" + passed + " failed=" + failed);
        return failed == 0;
    }

    private static void Check(bool condition, string message)
    {
        if (condition)
        {
            passed++;
        }
        else
        {
            failed++;
            Debug.LogError("[SelfTest] FAIL: " + message);
        }
    }

    private static void CheckSkills()
    {
        Check(Data.Skills.Count == 16, "skills count = 16, got " + Data.Skills.Count);
        Check(Data.MaxSkillSlots == 5, "max skill slots = 5");
        var ids = new HashSet<string>();
        foreach (var s in Data.Skills)
        {
            Check(s != null, "skill is null");
            if (s == null) continue;
            Check(!string.IsNullOrEmpty(s.Id), "skill id is empty");
            Check(ids.Add(s.Id), "duplicate skill id: " + s.Id);
            Check(!string.IsNullOrEmpty(s.Name), "skill name is empty: " + s.Id);
            Check(!string.IsNullOrEmpty(s.Desc), "skill desc is empty: " + s.Id);
            Check(s.Cooldown >= 3f && s.Cooldown <= 25f, "skill cooldown out of range: " + s.Id);
            Check(ArtExists(s.Icon), "skill icon missing: " + s.Icon);
        }

        Check(Data.Skill("fireball") != null, "fireball exists");
        Check(Data.Skill("shield") != null, "shield exists");
        Check(Data.Skill("heal") != null, "heal exists");
        Check(Data.Skill("frost") != null, "frost exists");
        Check(Data.Skill("volley") != null, "volley exists");
        Check(Data.Skill("shadowrun") != null, "shadowrun exists");
        Check(Data.Skill("cleave") != null, "cleave exists");
        Check(Data.Skill("nope") == null, "unknown skill returns null");

        Check(Mathf.Approximately(Data.Skill("frost").Cooldown, 15f), "frost cooldown is 15");
        Check(Mathf.Approximately(Data.Skill("volley").Cooldown, Data.VolleyCooldown), "volley cooldown is 8");
        Check(Mathf.Approximately(Data.Skill("shadowrun").Cooldown, Data.ShadowRunCooldown),
            "shadowrun cooldown is 9");
        Check(Mathf.Approximately(Data.Skill("cleave").Cooldown, Data.CleaveCooldown), "cleave cooldown is 8");
        Check(Mathf.Approximately(Data.EffectiveCooldown(Data.Skill("shield"), HeroClass.Swordsman),
            Data.KnightShieldCooldown), "knight shield cooldown is 10");
        Check(Data.SkillAllowedForClass("shield", HeroClass.Swordsman), "shield is available to knight");
        Check(!Data.SkillAllowedForClass("shield", HeroClass.Mage), "mage cannot learn shield");
        Check(!Data.SkillAllowedForClass("shield", HeroClass.Ranger), "ranger cannot learn shield");
        Check(Data.SkillAllowedForClass("frost", HeroClass.Ranger), "other skills stay available across classes");
        Check(Data.PerkAllowedForClass("shieldstart", HeroClass.Swordsman),
            "barrier perk is available to knight");
        Check(!Data.PerkAllowedForClass("shieldstart", HeroClass.Mage),
            "mage cannot take the shield barrier");
        Check(!Data.PerkAllowedForClass("shieldstart", HeroClass.Ranger),
            "ranger cannot take the shield barrier");
    }

    private static void CheckPerks()
    {
        Check(Data.Perks.Count == 19, "perks count = 19, got " + Data.Perks.Count);
        var ids = new HashSet<string>();
        int unique = 0;
        foreach (var p in Data.Perks)
        {
            Check(p != null, "perk is null");
            if (p == null) continue;
            Check(!string.IsNullOrEmpty(p.Id), "perk id is empty");
            Check(ids.Add(p.Id), "duplicate perk id: " + p.Id);
            Check(!string.IsNullOrEmpty(p.Name), "perk name is empty: " + p.Id);
            Check(!string.IsNullOrEmpty(p.Desc), "perk desc is empty: " + p.Id);
            Check(p.Weight > 0f, "perk weight must be positive: " + p.Id);
            Check(ArtExists(p.Icon), "perk icon missing: " + p.Icon);
            if (p.Unique) unique++;
        }

        Check(unique >= 1, "at least one unique perk");
        Check(Data.Perk("damage") != null, "damage perk exists");
        Check(Data.Perk("slot") != null && !Data.Perk("slot").Unique, "slot perk is repeatable");
        Check(Data.Perk("secondwind") != null && Data.Perk("secondwind").Unique, "secondwind is unique");
        Check(Data.Perk("lifesteal") != null && !Data.Perk("lifesteal").Unique, "lifesteal is repeatable");
        Check(Data.Perk("nope") == null, "unknown perk returns null");
    }

    private static void CheckClasses()
    {
        Check(Data.HeroClasses.Count == 3, "hero classes = 3, got " + Data.HeroClasses.Count);
        var ids = new HashSet<HeroClass>();
        var styles = new HashSet<HeroAttackStyle>();
        foreach (var hero in Data.HeroClasses)
        {
            Check(hero != null, "hero class is null");
            if (hero == null) continue;
            Check(ids.Add(hero.Id), "duplicate hero class: " + hero.Id);
            Check(styles.Add(hero.Style), "duplicate hero attack style: " + hero.Id);
            Check(!string.IsNullOrEmpty(hero.Name), "hero name is empty: " + hero.Id);
            Check(!string.IsNullOrEmpty(hero.Title), "hero title is empty: " + hero.Id);
            Check(!string.IsNullOrEmpty(hero.Desc), "hero desc is empty: " + hero.Id);
            Check(ArtExists(hero.Sprite), "hero sprite missing: " + hero.Sprite);
            Check(hero.Hp > 60f, "hero hp too low: " + hero.Id);
            Check(hero.Damage > 0f && hero.AttackInterval > 0.2f, "hero attack stats invalid: " + hero.Id);
            Check(hero.Speed > 3f && hero.Range > 0.5f, "hero movement stats invalid: " + hero.Id);
        }

        Check(Data.Hero(HeroClass.Mage).Style == HeroAttackStyle.Bolt, "mage uses bolts");
        Check(Data.Hero(HeroClass.Swordsman).Style == HeroAttackStyle.Slash, "swordsman uses slashes");
        Check(Data.Hero(HeroClass.Ranger).Style == HeroAttackStyle.Arrow, "ranger uses arrows");
        Check(Data.Hero(HeroClass.Swordsman).Hp > Data.Hero(HeroClass.Mage).Hp, "swordsman is tougher than mage");
        Check(Data.Hero(HeroClass.Ranger).AttackInterval < Data.Hero(HeroClass.Swordsman).AttackInterval,
            "ranger attacks faster than swordsman");

        CheckStarter(HeroClass.Mage, "fireball", "frost");
        CheckStarter(HeroClass.Swordsman, "shield", "cleave");
        CheckStarter(HeroClass.Ranger, "volley", "shadowrun");
    }

    private static void CheckStarter(HeroClass hero, string first, string second)
    {
        var starter = Data.StarterSkills(hero);
        Check(starter != null && starter.Length == 2, "starter set must have two skills: " + hero);
        if (starter == null || starter.Length != 2) return;
        Check(starter[0] == first && starter[1] == second,
            "wrong starter skills for " + hero + ": " + starter[0] + ", " + starter[1]);
        Check(Data.Skill(starter[0]) != null && Data.Skill(starter[1]) != null,
            "starter skill is missing for " + hero);
    }

    private static void CheckDungeons()
    {
        Check(Data.Dungeons.Count == 3, "dungeons = 3, got " + Data.Dungeons.Count);
        var ids = new HashSet<string>();
        float lastHp = 0f;
        float lastDamage = 0f;
        float lastDrop = 0f;
        int lastTier = 0;
        for (int i = 0; i < Data.Dungeons.Count; i++)
        {
            var d = Data.Dungeons[i];
            Check(d != null, "dungeon is null");
            if (d == null) continue;
            Check(ids.Add(d.Id), "duplicate dungeon id: " + d.Id);
            Check(!string.IsNullOrEmpty(d.Name), "dungeon name is empty: " + d.Id);
            Check(!string.IsNullOrEmpty(d.Tagline), "dungeon tagline is empty: " + d.Id);
            Check(!string.IsNullOrEmpty(d.Lore), "dungeon lore is empty: " + d.Id);
            Check(d.Difficulty == (DungeonDifficulty)i, "dungeon order/difficulty mismatch: " + d.Id);
            Check(ArtExists("ui_dungeon_" + d.Id), "dungeon emblem missing: " + d.Id);
            Check(ArtExists(d.FloorA) && ArtExists(d.FloorB), "dungeon floor missing: " + d.Id);
            Check(ArtExists(d.Wall), "dungeon wall missing: " + d.Id);
            Check(d.Rooms != null && d.Rooms.Length == Data.RoomCount, "dungeon room count mismatch: " + d.Id);
            Check(d.Pool != null && d.Pool.Length >= 4, "dungeon enemy pool too small: " + d.Id);

            var boss = Data.Enemies.ContainsKey(d.BossKind) ? Data.Enemies[d.BossKind] : null;
            Check(boss != null && boss.Boss, "dungeon boss invalid: " + d.Id);
            Check(boss != null && d.BossName == boss.Name, "dungeon boss name mismatch: " + d.Id);
            if (boss != null) Check(ArtExists(boss.Sprite), "dungeon boss sprite missing: " + d.Id);

            for (int j = 0; j < d.Pool.Length; j++)
            {
                Check(Data.Enemies.ContainsKey(d.Pool[j]), "dungeon pool has unknown enemy: " + d.Id);
            }
            Check(d.HpScale > lastHp, "dungeon hp scale must grow: " + d.Id);
            Check(d.DamageScale > lastDamage, "dungeon damage scale must grow: " + d.Id);
            Check(d.DropChance > lastDrop, "dungeon drop chance must grow: " + d.Id);
            Check(d.LootTier == lastTier + 1, "dungeon loot tier must grow: " + d.Id);
            Check(d.HpScale > 0.5f && d.DamageScale > 0.5f, "dungeon scaling too low: " + d.Id);
            Check(d.PerRoomHp >= 0f && d.PerRoomDamage >= 0f, "dungeon per-room scaling invalid: " + d.Id);
            lastHp = d.HpScale;
            lastDamage = d.DamageScale;
            lastDrop = d.DropChance;
            lastTier = d.LootTier;
        }

        Check(Data.FindDungeon("crypt") != null, "find dungeon by known id");
        Check(Data.FindDungeon("missing_dungeon") == null, "find dungeon rejects unknown id");
        Check(Data.Dungeon("missing_dungeon").Id == "crypt", "unknown dungeon falls back to crypt");
        Check(Data.Dungeon("crypt").Difficulty == DungeonDifficulty.Easy, "crypt is easy");
        Check(Data.Dungeon("web").Difficulty == DungeonDifficulty.Medium, "web is medium");
        Check(Data.Dungeon("throne").Difficulty == DungeonDifficulty.Hard, "throne is hard");
        Check(Mathf.Abs(Data.Dungeons[0].DropChance - 0.45f) < 0.001f, "easy drop chance is 45%");
        Check(Mathf.Abs(Data.Dungeons[1].DropChance - 0.65f) < 0.001f, "medium drop chance is 65%");
        Check(Mathf.Abs(Data.Dungeons[2].DropChance - 0.85f) < 0.001f, "hard drop chance is 85%");
    }

    private static void CheckSettings()
    {
        Check(ScreenSettings.Resolutions.Length == 3, "three screen resolutions");
        Check(ScreenSettings.FpsNames.Length == 4, "four frame-rate modes");
        Check(ScreenSettings.ResolutionName(0) == "1280×720", "resolution label uses width×height");
    }

    private static void CheckEnemies()
    {
        Check(Data.Enemies.Count == 14, "enemy count = 14, got " + Data.Enemies.Count);
        var sprites = new HashSet<string>();
        var specials = new HashSet<string>();
        var bossKinds = new HashSet<EnemyKind>
        {
            EnemyKind.BoneKing, EnemyKind.SpiderQueen, EnemyKind.Lich
        };
        int bosses = 0;
        float strongestRegular = 0f;
        foreach (EnemyKind kind in System.Enum.GetValues(typeof(EnemyKind)))
        {
            Check(Data.Enemies.ContainsKey(kind), "enemy definition missing: " + kind);
            if (!Data.Enemies.ContainsKey(kind)) continue;
            var e = Data.Enemies[kind];
            Check(e.Kind == kind, "enemy kind mismatch: " + kind);
            Check(!string.IsNullOrEmpty(e.Name), "enemy name is empty: " + kind);
            Check(e.Hp > 0f, "enemy hp must be positive: " + kind);
            Check(e.Speed > 0f, "enemy speed must be positive: " + kind);
            Check(e.Damage > 0f, "enemy damage must be positive: " + kind);
            Check(e.Range > 0f, "enemy range must be positive: " + kind);
            Check(e.Scale > 0.4f && e.Scale < 2.5f, "enemy scale out of range: " + kind);
            Check(ArtExists(e.Sprite), "enemy sprite missing: " + e.Sprite);
            Check(sprites.Add(e.Sprite), "enemy sprite reused: " + e.Sprite);
            Check(!string.IsNullOrEmpty(e.Special), "enemy special missing: " + kind);
            Check(specials.Add(e.Special), "enemy special reused: " + e.Special);
            Check(e.Boss == bossKinds.Contains(kind), "enemy boss flag mismatch: " + kind);
            if (e.Boss)
            {
                bosses++;
                Check(e.Hp >= 900f, "boss hp too low: " + kind);
                Check(!string.IsNullOrEmpty(e.Projectile) && ArtExists(e.Projectile),
                    "boss projectile missing: " + kind);
            }
            else
            {
                strongestRegular = Mathf.Max(strongestRegular, e.Hp);
            }
        }

        Check(bosses == 3, "exactly three bosses, got " + bosses);
        foreach (var kind in bossKinds)
        {
            var boss = Data.Enemy(kind);
            Check(boss != null && boss.Hp > strongestRegular * 4f,
                "boss must be much tougher than regular enemies: " + kind);
        }
        Check(Data.Enemy(EnemyKind.Lich).Hp > Data.Enemy(EnemyKind.SpiderQueen).Hp, "lich is the final boss");
        Check(Data.Enemy(EnemyKind.SpiderQueen).Hp > Data.Enemy(EnemyKind.BoneKing).Hp,
            "spider queen is harder than bone king");
        Check(Data.Enemy(EnemyKind.Executioner).Hp > Data.Enemy(EnemyKind.Guard).Hp,
            "executioner is tougher than guard");
        Check(Data.Enemy(EnemyKind.Guard).Special == "shield_charge", "guard has a dodgeable charge");
        Check(Data.Enemy(EnemyKind.Spider).Special == "web_spit", "spider has a web ability");
        Check(Data.Enemy(EnemyKind.PlagueDoctor).Special == "plague_pool", "plague doctor leaves a pool");
        Check(Data.Enemy(EnemyKind.Hound).Special == "pounce", "hound has a pounce");
    }

    private static void CheckRooms()
    {
        Check(Data.RoomCount == 12, "room count = 12");
        Check(Data.Rooms.Length == Data.RoomCount, "room defs match RoomCount");

        var allNames = new HashSet<string>();
        var allThemes = new HashSet<string>();
        var checkedAssets = new HashSet<string>();
        int totalRooms = 0;
        foreach (var dungeon in Data.Dungeons)
        {
            Check(dungeon.Rooms != null && dungeon.Rooms.Length == Data.RoomCount,
                "dungeon must have 12 rooms: " + dungeon.Id);
            if (dungeon.Rooms == null) continue;
            Check(dungeon.Rooms[dungeon.Rooms.Length - 1].Type == RoomType.Boss,
                "last room is boss room: " + dungeon.Id);

            bool treasure = false;
            bool rest = false;
            foreach (var room in dungeon.Rooms)
            {
                totalRooms++;
                Check(room != null, "room is null");
                if (room == null) continue;
                Check(!string.IsNullOrEmpty(room.Name), "room name is empty");
                Check(allNames.Add(room.Name), "duplicate room name across dungeons: " + room.Name);
                Check(!string.IsNullOrEmpty(room.Subtitle), "room subtitle is empty: " + room.Name);
                Check(room.Waves >= 0 && room.Waves <= 4, "room waves out of range: " + room.Name);
                Check(!string.IsNullOrEmpty(room.Theme), "room theme is empty: " + room.Name);
                var theme = Data.Theme(room.Theme);
                Check(theme != null && theme.Id == room.Theme, "room theme missing: " + room.Theme);
                Check(allThemes.Add(room.Theme), "room theme reused: " + room.Theme);
                if (room.Type == RoomType.Combat || room.Type == RoomType.Elite)
                {
                    Check(room.Waves > 0, "combat room must have waves: " + room.Name);
                }
                if (room.Type == RoomType.Treasure)
                {
                    treasure = true;
                    Check(room.Waves == 0, "treasure room must have zero waves: " + room.Name);
                }
                if (room.Type == RoomType.Rest)
                {
                    rest = true;
                    Check(room.Waves == 0, "rest room must have zero waves: " + room.Name);
                }
                if (theme != null)
                {
                    CheckThemeAssets(theme, checkedAssets);
                }
            }
            Check(treasure, "dungeon has a treasure room: " + dungeon.Id);
            Check(rest, "dungeon has a rest room: " + dungeon.Id);
        }

        Check(totalRooms == 36, "total named rooms = 36, got " + totalRooms);
        Check(allNames.Count == 36, "all room names are unique, got " + allNames.Count);
        Check(allThemes.Count == 36, "all room themes are unique, got " + allThemes.Count);
        Check(Data.Rooms[Data.Rooms.Length - 1].Type == RoomType.Boss, "easy dungeon ends with boss");
    }

    private static void CheckThemeAssets(RoomTheme theme, HashSet<string> seen)
    {
        if (theme == null) return;
        if (seen.Add(theme.Floor)) Check(ArtExists(theme.Floor), "theme floor missing: " + theme.Floor);
        if (seen.Add(theme.Wall)) Check(ArtExists(theme.Wall), "theme wall missing: " + theme.Wall);
        if (!string.IsNullOrEmpty(theme.Centerpiece) && seen.Add(theme.Centerpiece))
        {
            Check(ArtExists(theme.Centerpiece), "theme centerpiece missing: " + theme.Centerpiece);
        }
        Check(theme.Props != null && theme.Props.Length >= 3, "theme needs at least 3 props: " + theme.Id);
        if (theme.Props != null)
        {
            foreach (var prop in theme.Props)
            {
                if (!seen.Add(prop)) continue;
                Check(ArtExists(prop), "theme prop missing: " + prop);
            }
        }
        Check(theme.PropCount >= 2 && theme.PropCount <= 8, "theme prop count out of range: " + theme.Id);
    }

    private static void CheckEquipment()
    {
        Check(Data.Equipment.Count == 12, "equipment count = 12, got " + Data.Equipment.Count);
        var ids = new HashSet<string>();
        var names = new HashSet<string>();
        var slots = new Dictionary<EquipSlot, int>();
        var rarities = new Dictionary<Rarity, int>();
        foreach (var item in Data.Equipment)
        {
            Check(item != null, "equipment item is null");
            if (item == null) continue;
            Check(!string.IsNullOrEmpty(item.Id), "equipment id is empty");
            Check(ids.Add(item.Id), "duplicate equipment id: " + item.Id);
            Check(!string.IsNullOrEmpty(item.Name), "equipment name is empty: " + item.Id);
            Check(names.Add(item.Name), "duplicate equipment name: " + item.Id);
            Check(!string.IsNullOrEmpty(item.Desc), "equipment desc is empty: " + item.Id);
            Check(ArtExists(item.Icon), "equipment icon missing: " + item.Icon);
            Check(item.Tier >= 1 && item.Tier <= 3, "equipment tier out of range: " + item.Id);
            Check(item.Weight > 0f, "equipment weight must be positive: " + item.Id);
            Check(!string.IsNullOrEmpty(item.StatLine()), "equipment has no stats: " + item.Id);

            if (!slots.ContainsKey(item.Slot)) slots[item.Slot] = 0;
            slots[item.Slot]++;
            if (!rarities.ContainsKey(item.Rarity)) rarities[item.Rarity] = 0;
            rarities[item.Rarity]++;
        }

        Check(slots.Count == 3, "all three equipment slots are used");
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
        {
            Check(slots.ContainsKey(slot) && slots[slot] >= 3, "slot has at least three items: " + slot);
        }
        Check(rarities.ContainsKey(Rarity.Common) && rarities[Rarity.Common] >= 3, "common items exist");
        Check(rarities.ContainsKey(Rarity.Rare) && rarities[Rarity.Rare] >= 3, "rare items exist");
        Check(rarities.ContainsKey(Rarity.Epic) && rarities[Rarity.Epic] >= 2, "epic items exist");
        Check(rarities.ContainsKey(Rarity.Legendary) && rarities[Rarity.Legendary] >= 2, "legendary items exist");
        Check(Data.Equipment.Find(e => e.Id == "kingsbane") != null, "kingsbane exists");
        Check(Data.Equipment.Find(e => e.Id == "lich_seal") != null, "lich seal exists");
    }

    private static void CheckAssets()
    {
        string[] textures =
        {
            "wizard", "swordsman", "ranger",
            "imp", "skeleton", "boar", "bomber", "guard", "shaman", "spider",
            "wraith", "executioner", "plague_doctor", "hound",
            "bone_king", "spider_queen", "lich_abyss",
            "floor_crypt_a", "floor_crypt_b", "floor_web_a", "floor_web_b",
            "floor_throne_a", "floor_throne_b", "wall_crypt", "wall_web", "wall_throne",
            "proj_soul", "proj_bone", "proj_skull", "proj_poison", "proj_arrow",
            "fx_soul", "fx_shadow", "fx_slash", "fx_glow", "fx_ember", "fx_mote",
            "ui_menu_bg", "ui_menu_frame", "ui_rune", "ui_divider", "ui_logo",
            "ui_card_dark", "ui_panel_dark", "ui_panel",
            "ui_dungeon_crypt", "ui_dungeon_web", "ui_dungeon_throne",
            "eq_rusty_blade", "eq_grave_cleaver", "eq_widow_fang", "eq_kingsbane",
            "eq_patched_cloak", "eq_web_carapace", "eq_royal_shroud",
            "eq_bone_charm", "eq_ember_eye", "eq_spider_heart", "eq_lich_seal", "eq_crown_of_ashes",
            "prop_chest", "prop_campfire", "prop_torch", "prop_column", "prop_rock", "prop_bones",
            "prop_barrel", "prop_crate", "prop_moss", "prop_sarcophagus", "prop_rack", "prop_shield",
            "prop_chain", "prop_cage", "prop_skullpile", "prop_ribs", "prop_candles", "prop_web",
            "prop_cocoon", "prop_altar", "prop_banner", "prop_throne", "prop_bone_arch", "prop_eggs",
            "prop_roots", "prop_acid", "prop_web_throne", "prop_gate", "prop_blood", "prop_mirror",
            "prop_cauldron", "table_bg", "floor", "floor_boss"
        };
        foreach (var name in textures)
        {
            Check(ArtExists(name), "art missing: " + name);
        }

        Check(Resources.Load<Font>("Fonts/PT_Serif-Bold") != null, "PT Serif Bold font exists");
        Check(Resources.Load<Font>("Fonts/PT_Sans-Regular") != null, "PT Sans Regular font exists");

        string[] audio =
        {
            "sfx_hit", "sfx_shoot", "sfx_heal", "sfx_boss_roar", "music_dungeon", "music_boss"
        };
        foreach (var a in audio)
        {
            Check(Resources.Load<AudioClip>("Audio/" + a) != null, "audio clip missing: " + a);
        }
    }

    private static void CheckBalance()
    {
        Check(Data.Skills.Count > Data.MaxSkillSlots, "more skills than slots");
        Check(Data.Perks.Count >= 15, "enough perk variety");
        Check(Data.RoomCount > 8, "long enough run");
        Check(Data.Dungeons.Count == 3, "three difficulty tiers");
        Check(Mathf.Approximately(Data.LifestealPerPerk, 0.03f), "lifesteal per perk is 3%");
        Check(Mathf.Approximately(Data.LifestealCap, 0.12f), "lifesteal cap is 12%");
        Check(Mathf.Approximately(Data.KnightShieldPercent, 0.30f), "knight shield is 30% hp");
        Check(Mathf.Approximately(Data.ShieldDuration, 5f), "shield lasts 5 seconds");
        Check(Mathf.Approximately(Data.FrostDuration, 2f), "frost lasts 2 seconds");
        Check(Mathf.Approximately(Data.FrostDamagePercent, 0.20f), "frost deals 20% max hp");
        Check(Mathf.Approximately(Data.ShadowRunDuration, 3f), "shadow run lasts 3 seconds");
        Check(Data.LifestealPerPerk * 4f >= Data.LifestealCap - 0.001f, "four lifesteal perks reach the cap");

        var mage = Data.Hero(HeroClass.Mage);
        var swordsman = Data.Hero(HeroClass.Swordsman);
        var ranger = Data.Hero(HeroClass.Ranger);
        float mageDps = mage.Damage / mage.AttackInterval;
        float swordDps = swordsman.Damage / swordsman.AttackInterval;
        float rangerDps = ranger.Damage / ranger.AttackInterval;

        var imp = Data.Enemy(EnemyKind.Imp);
        var guard = Data.Enemy(EnemyKind.Guard);
        var executioner = Data.Enemy(EnemyKind.Executioner);
        var boneKing = Data.Enemy(EnemyKind.BoneKing);
        var spiderQueen = Data.Enemy(EnemyKind.SpiderQueen);
        var lich = Data.Enemy(EnemyKind.Lich);

        Check(mageDps > 8f && mageDps < 20f, "mage dps in range");
        Check(swordDps > mageDps && swordDps < 40f, "swordsman deals more dps than mage");
        Check(rangerDps > 10f && rangerDps < 30f, "ranger dps in range");
        Check(imp.Hp / mageDps > 1.2f && imp.Hp / mageDps < 3f, "first enemies take a few shots");
        Check(guard.Hp > imp.Hp * 3f, "guard is much tougher than imp");
        Check(executioner.Hp > guard.Hp, "executioner is a late-game threat");
        Check(lich.Hp / mageDps > 60f, "final boss takes a real fight");
        Check(boneKing.Hp > executioner.Hp * 4f, "bone king is a boss, not an elite");
        Check(spiderQueen.Hp > boneKing.Hp, "boss hp grows with difficulty");
        Check(lich.Hp > spiderQueen.Hp, "final boss is the toughest");
        Check(Data.Enemy(EnemyKind.Spider).Speed > imp.Speed, "spiders are faster than imps");
        Check(Data.Enemy(EnemyKind.Hound).Speed > Data.Enemy(EnemyKind.Skeleton).Speed,
            "hounds are faster than skeletons");
    }

    private static bool ArtExists(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (Resources.Load<Texture2D>("Art/" + name) != null) return true;
        return Resources.Load<Sprite>("Art/" + name) != null;
    }
}
