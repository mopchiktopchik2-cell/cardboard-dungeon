using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Быстрые проверки контента. Запускаются из редактора (Cardboard/Run Self Tests)
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
        Debug.Log("[SelfTest] begin");

        CheckSkills();
        CheckPerks();
        CheckEnemies();
        CheckRooms();
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
        Check(Data.Skills.Count == 12, "skills count = 12, got " + Data.Skills.Count);
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
        Check(Data.Skill("nope") == null, "unknown skill returns null");
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

        Check(unique >= 2, "at least two unique perks");
        Check(Data.Perk("damage") != null, "damage perk exists");
        Check(Data.Perk("slot") != null && Data.Perk("slot").Unique, "slot perk is unique");
        Check(Data.Perk("secondwind") != null && Data.Perk("secondwind").Unique, "secondwind is unique");
        Check(Data.Perk("nope") == null, "unknown perk returns null");
    }

    private static void CheckEnemies()
    {
        Check(Data.Enemies.Count == 8, "enemy count = 8, got " + Data.Enemies.Count);
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
            Check(ArtExists(e.Sprite), "enemy sprite missing: " + e.Sprite);
        }

        var boss = Data.Enemy(EnemyKind.Lich);
        float strongest = 0f;
        foreach (var e in Data.Enemies.Values)
        {
            if (e.Kind != EnemyKind.Lich) strongest = Mathf.Max(strongest, e.Hp);
        }
        Check(boss.Hp > strongest * 3f, "boss must be much tougher than regular enemies");
        Check(boss.Hp >= 500f, "boss hp must be at least 500");
    }

    private static void CheckRooms()
    {
        Check(Data.Rooms.Length == Data.RoomCount, "room defs match RoomCount");
        Check(Data.RoomCount == 12, "room count = 12");
        Check(Data.Rooms[Data.Rooms.Length - 1].Type == RoomType.Boss, "last room is boss room");

        var names = new HashSet<string>();
        int combat = 0;
        foreach (var r in Data.Rooms)
        {
            Check(r != null, "room is null");
            if (r == null) continue;
            Check(!string.IsNullOrEmpty(r.Name), "room name is empty");
            Check(names.Add(r.Name), "duplicate room name: " + r.Name);
            Check(!string.IsNullOrEmpty(r.Subtitle), "room subtitle is empty: " + r.Name);
            Check(r.Waves >= 0 && r.Waves <= 4, "room waves out of range: " + r.Name);
            if (r.Type == RoomType.Combat || r.Type == RoomType.Elite)
            {
                combat++;
                Check(r.Waves > 0, "combat room must have waves: " + r.Name);
            }
            if (r.Type == RoomType.Treasure || r.Type == RoomType.Rest)
            {
                Check(r.Waves == 0, "non-combat room must have zero waves: " + r.Name);
            }
        }
        Check(combat >= 8, "at least eight combat/elite rooms");
        Check(Data.Rooms[4].Type == RoomType.Treasure, "room 5 is treasure");
        Check(Data.Rooms[7].Type == RoomType.Rest, "room 8 is rest");
    }

    private static void CheckAssets()
    {
        Check(ArtExists("wizard"), "player sprite exists");
        Check(ArtExists("table_bg"), "table background exists");
        Check(ArtExists("floor"), "floor exists");
        Check(ArtExists("floor_boss"), "boss floor exists");
        Check(ArtExists("prop_chest"), "chest exists");
        Check(ArtExists("prop_campfire"), "campfire exists");
        Check(ArtExists("fx_slash"), "slash effect exists");
        Check(ArtExists("fx_glow"), "glow effect exists");
        Check(ArtExists("ui_menu_bg"), "v1.1 menu backdrop exists");
        Check(ArtExists("ui_menu_frame"), "v1.1 menu frame exists");
        Check(ArtExists("ui_rune"), "v1.1 rune exists");
        Check(ArtExists("ui_divider"), "v1.1 divider exists");
        Check(ArtExists("fx_ember"), "ember particle exists");
        Check(ArtExists("fx_mote"), "ambient mote exists");

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

        var baseStats = new PlayerStats();
        float playerDps = baseStats.damage / baseStats.attackInterval;
        var imp = Data.Enemy(EnemyKind.Imp);
        var guard = Data.Enemy(EnemyKind.Guard);
        var lich = Data.Enemy(EnemyKind.Lich);
        Check(imp.Hp / playerDps > 1.2f && imp.Hp / playerDps < 3f, "first enemies take a few shots");
        Check(guard.Hp > imp.Hp * 3f, "guard is much tougher than imp");
        Check(lich.Hp / playerDps > 60f, "boss takes a real fight");
        Check(Data.Enemy(EnemyKind.Spider).Speed > imp.Speed, "spiders are the fastest basic enemy");
    }

    private static bool ArtExists(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (Resources.Load<Texture2D>("Art/" + name) != null) return true;
        return Resources.Load<Sprite>("Art/" + name) != null;
    }
}
