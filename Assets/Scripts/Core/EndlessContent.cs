using System.Collections.Generic;
using UnityEngine;

public enum ShopOfferKind
{
    Upgrade,
    NewSkill,
    Perk,
    Heal,
    Stat
}

/// <summary>Одна карточка в лавке Бесконечного разлома.</summary>
public class ShopOffer
{
    public ShopOfferKind Kind;
    public SkillDef Skill;
    public PerkDef Perk;
    public string StatId;
    public int Cost;
    public int TargetLevel;
    public bool NeedsSlot;
    public string Title;
    public string Desc;
    public string Badge;
    public string Icon;
    public Color Accent;
}

/// <summary>
/// Контент отдельного бесконечного режима. Сюжетные подземелья не меняются:
/// здесь комнаты, темы и боссы собираются процедурно на каждый забег.
/// </summary>
public static class EndlessContent
{
    public const string DungeonId = "endless";
    public const int RoomCap = 999;
    public const int BossEvery = 5;
    public const int MaxSkillLevel = 5;
    public const int ShopOfferCount = 3;
    public const int StartCoins = 25;

    private static DungeonDef dungeon;
    private static List<string> themeIds;

    private static readonly EnemyKind[] BossRotation =
    {
        EnemyKind.BoneKing,
        EnemyKind.SpiderQueen,
        EnemyKind.Lich
    };

    private static readonly EnemyKind[] NormalPool =
    {
        EnemyKind.Imp,
        EnemyKind.Skeleton,
        EnemyKind.Boar,
        EnemyKind.Bomber,
        EnemyKind.Guard,
        EnemyKind.Shaman,
        EnemyKind.Spider,
        EnemyKind.Wraith,
        EnemyKind.Executioner,
        EnemyKind.PlagueDoctor,
        EnemyKind.Hound
    };

    private static readonly string[] RoomNames =
    {
        "ТРЕЩИНА В КАРТОНЕ",
        "ЗАЛ ОБРЫВКОВ",
        "КОРИДОР БЕЗ КОНЦА",
        "СВАЛКА МИРОВ",
        "ТИХАЯ ГЛУБИНА",
        "ЧЕРНИЛЬНЫЙ ПРОХОД",
        "ПЕПЕЛЬНЫЙ ЯРУС",
        "ЗАБЫТАЯ СТРАНИЦА",
        "ПРОРЫВ ТЬМЫ",
        "ГНИЮЩАЯ ГАЛЕРЕЯ",
        "ЛАБИРИНТ ИЗ КЛЕЯ",
        "ПОСЛЕДНИЙ ОБРЫВОК"
    };

    public static DungeonDef Dungeon
    {
        get
        {
            if (dungeon == null) dungeon = BuildDungeon();
            return dungeon;
        }
    }

    private static DungeonDef BuildDungeon()
    {
        return new DungeonDef
        {
            Id = DungeonId,
            Name = "БЕСКОНЕЧНЫЙ РАЗЛОМ",
            Tagline = "Чем глубже, тем громче шёпот",
            Lore = "Картонные стены закончились. За ними — бесконечная свалка чужих подземелий, " +
                   "где стражи меняются местами, а глубина измеряется не комнатами, а страхом.",
            Difficulty = DungeonDifficulty.Medium,
            Accent = new Color32(186, 132, 232, 255),
            FloorA = "floor_crypt_a",
            FloorB = "floor_web_a",
            Wall = "wall_h",
            BossKind = EnemyKind.BoneKing,
            BossName = "СТРАЖ ГЛУБИНЫ",
            HpScale = 1f,
            DamageScale = 1f,
            PerRoomHp = 0.11f,
            PerRoomDamage = 0.065f,
            DropChance = 0.5f,
            LootTier = 2,
            Pool = NormalPool,
            Adds = NormalPool,
            Rooms = new RoomDef[0]
        };
    }

    public static bool IsBoss(int index)
    {
        if (index < 0) return false;
        if (index >= RoomCap - 1) return index == RoomCap - 1;
        return (index + 1) % BossEvery == 0;
    }

    public static EnemyKind BossFor(int index)
    {
        int cycle = Mathf.Max(0, index) / BossEvery;
        return BossRotation[cycle % BossRotation.Length];
    }

    public static RoomDef RoomFor(int index, int seed)
    {
        bool boss = IsBoss(index);
        bool elite = !boss && index > 0 && (index + 1) % 3 == 0;
        var rng = new System.Random(seed * 397 + index * 7919 + 17);
        string themeId = boss ? BossTheme(BossFor(index)) : PickTheme(rng);
        RoomType type = boss ? RoomType.Boss : elite ? RoomType.Elite : RoomType.Combat;
        int waves = boss ? 1 : elite ? (index >= 12 ? 2 : 1) : 2 + Mathf.Min(2, index / 8);
        string name = boss
            ? "ПЕЧАТЬ ГЛУБИНЫ " + (index + 1)
            : RoomNames[Mathf.Abs(rng.Next()) % RoomNames.Length];
        string subtitle = boss
            ? "Страж этого слоя уже почуял тебя."
            : "Бесконечный разлом · глубина " + (index + 1);
        return new RoomDef(type, name, subtitle, waves, themeId);
    }

    private static string PickTheme(System.Random rng)
    {
        var ids = Themes();
        return ids[Mathf.Abs(rng.Next()) % ids.Count];
    }

    private static string BossTheme(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.SpiderQueen: return "web_boss";
            case EnemyKind.Lich: return "throne_boss";
            default: return "crypt_boss";
        }
    }

    private static List<string> Themes()
    {
        if (themeIds != null && themeIds.Count > 0) return themeIds;
        themeIds = new List<string>();
        foreach (var d in Data.Dungeons)
        {
            if (d == null || d.Rooms == null) continue;
            foreach (var room in d.Rooms)
            {
                if (room == null || string.IsNullOrEmpty(room.Theme)) continue;
                if (!themeIds.Contains(room.Theme)) themeIds.Add(room.Theme);
            }
        }
        if (themeIds.Count == 0) themeIds.Add("crypt_cellar");
        return themeIds;
    }

    public static float EnemyHpScale(int index)
    {
        float depth = Mathf.Max(0, index);
        return 1f + 0.11f * depth + 0.0025f * depth * depth;
    }

    public static float EnemyDamageScale(int index)
    {
        float depth = Mathf.Max(0, index);
        return 1f + 0.065f * depth + 0.0008f * depth * depth;
    }

    public static int CoinDrop(bool boss, bool elite, int depth)
    {
        if (boss) return 15 + depth;
        if (elite) return 4 + depth / 3;
        return Random.Range(1, 3) + depth / 6;
    }

    public static int UpgradeCost(int targetLevel, int depth)
    {
        int baseCost;
        switch (targetLevel)
        {
            case 2: baseCost = 24; break;
            case 3: baseCost = 38; break;
            case 4: baseCost = 56; break;
            default: baseCost = 85; break;
        }
        return baseCost + Mathf.Max(0, depth) * Mathf.Max(1, targetLevel - 1) * 2;
    }

    public static int NewSkillCost(bool needsSlot, int depth)
    {
        return 50 + Mathf.Max(0, depth) * 2 + (needsSlot ? 35 : 0);
    }

    public static int PerkCost(int depth)
    {
        return 60 + Mathf.Max(0, depth) * 3;
    }

    public static int HealCost(int depth)
    {
        return 25 + Mathf.Max(0, depth);
    }

    public static int StatCost(int depth)
    {
        return 45 + Mathf.Max(0, depth) * 2;
    }

    public static string LevelEffect(string skillId, int level)
    {
        if (level >= MaxSkillLevel)
        {
            return "МАСТЕРИЯ: " + MasteryDescription(skillId);
        }
        return "Уровень " + level + ": +15% силы, +6% радиуса и -5% перезарядки.";
    }

    public static string MasteryDescription(string skillId)
    {
        switch (skillId)
        {
            case "fireball": return "шар расщепляется на три огненных снаряда.";
            case "shield": return "щит обжигает атакующих ответным уроном.";
            case "nova": return "волна холода проходит вторым, более мощным импульсом.";
            case "chain": return "молния бьёт до пяти целей без падения урона.";
            case "heal": return "лечение снимает яд, оглушение и даёт краткую неуязвимость.";
            case "poison": return "яд расползается ещё в два облака по соседним врагам.";
            case "meteor": return "метеор вызывает два дополнительных обломка.";
            case "blink": return "прыжок взрывается в точке входа и выхода.";
            case "missiles": return "десять снарядов сами ищут цели.";
            case "slow": return "время замирает: враги скованы льдом, их снаряды гаснут.";
            case "golem": return "на поле выходят два картонных голема.";
            case "rage": return "берсерк усиливает урон и возвращает здоровье с каждого удара.";
            case "frost": return "тюрьма накрывает всю арену и вытягивает больше HP.";
            case "volley": return "стрелы пробивают врагов и взрываются.";
            case "shadowrun": return "теневой бег обнуляет перезарядку остальных навыков.";
            case "cleave": return "после удара расходится широкая ударная волна.";
            default: return "навык получает новый боевой эффект.";
        }
    }

    public static string MasteryBadge(string skillId)
    {
        return string.IsNullOrEmpty(skillId) ? "МАСТЕРИЯ" : "МАСТЕРИЯ · " + MasteryDescription(skillId).ToUpperInvariant();
    }
}
