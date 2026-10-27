using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Combat,
    Elite,
    Treasure,
    Rest,
    Boss
}

public enum EnemyKind
{
    Imp,
    Skeleton,
    Boar,
    Bomber,
    Guard,
    Shaman,
    Spider,
    Lich,
    Wraith,
    Executioner,
    PlagueDoctor,
    Hound,
    BoneKing,
    SpiderQueen
}

public class SkillDef
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Desc;
    public readonly float Cooldown;
    public readonly string Icon;
    public readonly Color Color;

    public SkillDef(string id, string name, string desc, float cooldown, string icon, Color color)
    {
        Id = id;
        Name = name;
        Desc = desc;
        Cooldown = cooldown;
        Icon = icon;
        Color = color;
    }
}

public class PerkDef
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Desc;
    public readonly string Icon;
    public readonly bool Unique;
    public readonly float Weight;

    public PerkDef(string id, string name, string desc, string icon, bool unique = false, float weight = 1f)
    {
        Id = id;
        Name = name;
        Desc = desc;
        Icon = icon;
        Unique = unique;
        Weight = weight;
    }
}

public class EnemyDef
{
    public EnemyKind Kind;
    public string Name;
    public string Sprite;
    public float Hp;
    public float Speed;
    public float Damage;
    public float Range;
    public float Cooldown;
    public float Scale;
    public bool Boss;
    public string Projectile = "proj_skull";
    public string Special;

    public EnemyDef Clone()
    {
        return (EnemyDef)MemberwiseClone();
    }
}

public class RoomDef
{
    public readonly RoomType Type;
    public readonly string Name;
    public readonly string Subtitle;
    public readonly int Waves;
    public readonly string Theme;

    public RoomDef(RoomType type, string name, string subtitle, int waves, string theme = null)
    {
        Type = type;
        Name = name;
        Subtitle = subtitle;
        Waves = waves;
        Theme = theme;
    }
}

public static class Pal
{
    public static readonly Color Cream = new Color32(242, 227, 198, 255);
    public static readonly Color Paper = new Color32(232, 217, 184, 255);
    public static readonly Color Ink = new Color32(42, 27, 22, 255);
    public static readonly Color Gold = new Color32(232, 184, 109, 255);
    public static readonly Color GoldDeep = new Color32(190, 140, 72, 255);
    public static readonly Color Red = new Color32(201, 79, 69, 255);
    public static readonly Color RedDeep = new Color32(142, 48, 44, 255);
    public static readonly Color Blue = new Color32(78, 125, 181, 255);
    public static readonly Color Teal = new Color32(79, 179, 166, 255);
    public static readonly Color Purple = new Color32(139, 107, 177, 255);
    public static readonly Color Green = new Color32(111, 168, 100, 255);
    public static readonly Color Dark = new Color32(26, 20, 32, 255);
    public static readonly Color Dark2 = new Color32(42, 32, 51, 255);
}

public static partial class Data
{
    public const int RoomCount = 12;
    public const int MaxSkillSlots = 5;

    public const float LifestealPerPerk = 0.03f;
    public const float LifestealCap = 0.12f;
    public const float RoomTransitionHeal = 0.20f;
    public const float KnightShieldPercent = 0.30f;
    public const float ShieldDuration = 5f;
    public const float KnightShieldCooldown = 10f;
    public const float FrostDuration = 2f;
    public const float FrostDamagePercent = 0.20f;
    public const float VolleyCooldown = 8f;
    public const float ShadowRunDuration = 3f;
    public const float ShadowRunCooldown = 9f;
    public const float CleaveCooldown = 8f;

    public static readonly List<SkillDef> Skills = new List<SkillDef>
    {
        new SkillDef("fireball", "ОГНЕННЫЙ ШАР", "Шар летит в ближайшего врага и взрывается по площади.", 4f,
            "icon_fireball", Pal.Red),
        new SkillDef("shield", "ЩИТ РЫЦАРЯ", "Только рыцарь. Барьер на 30% максимального HP, держится 5 секунд.", 10f,
            "icon_shield", Pal.Blue),
        new SkillDef("nova", "ЛЕДЯНАЯ НОВА", "Волна холода вокруг мага: урон и замедление врагов.", 8f,
            "icon_nova", Pal.Teal),
        new SkillDef("chain", "ЦЕПНАЯ МОЛНИЯ", "Молния бьёт до трёх врагов, урон падает с каждым прыжком.", 8f,
            "icon_chain", Pal.Gold),
        new SkillDef("heal", "ИСЦЕЛЕНИЕ", "Восстанавливает 30% максимального здоровья.", 15f,
            "icon_heal", Pal.Red),
        new SkillDef("poison", "ЯДОВИТОЕ ОБЛАКО", "Лужа яда на месте ближайшего врага. Держится 6 секунд.", 9f,
            "icon_poison", Pal.Green),
        new SkillDef("meteor", "МЕТЕОР", "Через секунду с неба падает метеор. Большой урон по площади.", 13f,
            "icon_meteor", Pal.Red),
        new SkillDef("blink", "ПРЫЖОК", "Маг мгновенно перемещается и становится неуязвимым на миг.", 8f,
            "icon_blink", Pal.Teal),
        new SkillDef("missiles", "МАГИЧЕСКИЕ СНАРЯДЫ", "Пять самонаводящихся снарядов в ближайших врагов.", 7f,
            "icon_missiles", Pal.Purple),
        new SkillDef("slow", "ЗАМЕДЛЕНИЕ ВРЕМЕНИ", "Все враги двигаются вдвое медленнее 3 секунды.", 17f,
            "icon_clock", Pal.Purple),
        new SkillDef("golem", "КАРТОННЫЙ ГОЛЕМ", "Голем сражается на вашей стороне 12 секунд.", 20f,
            "icon_golem", Pal.Paper),
        new SkillDef("rage", "БЕРСЕРК", "6 секунд: +50% урона и +39% скорости атаки.", 18f,
            "icon_rage", Pal.RedDeep),
        new SkillDef("frost", "ЛЕДЯНАЯ ТЮРЬМА", "Заморозка врагов вокруг на 2 секунды. За это время они теряют 20% максимального HP.", 15f,
            "icon_perk_freeze", Pal.Teal),
        new SkillDef("volley", "СЕМЬ СТРЕЛ", "Залп из 7 самонаводящихся стрел по ближайшим врагам.", 8f,
            "icon_missiles", Pal.Green),
        new SkillDef("shadowrun", "ТЕНЕВОЙ БЕГ", "3 секунды неуязвимости. Можно стрелять на бегу.", 9f,
            "icon_blink", Pal.Green),
        new SkillDef("cleave", "КРУГОВОЙ УДАР", "Герой замирает и бьёт всех вокруг с повышенным уроном.", 8f,
            "icon_perk_damage", Pal.Gold)
    };

    public static readonly List<PerkDef> Perks = new List<PerkDef>
    {
        new PerkDef("damage", "ОСТРЫЙ КРАЙ", "+18% урона автоатаки.", "icon_perk_damage", false, 1.2f),
        new PerkDef("attackspeed", "БЫСТРЫЕ РУКИ", "+16% скорости автоатаки.", "icon_perk_speed", false, 1.1f),
        new PerkDef("movespeed", "ЛЁГКИЕ НОГИ", "+8% скорости передвижения.", "icon_perk_speed", false, 1.0f),
        new PerkDef("hp", "ПЛОТНАЯ БУМАГА", "+20 к максимальному здоровью и лечение на 20.", "icon_perk_hp", false, 1.1f),
        new PerkDef("lifesteal", "ВАМПИРИЗМ", "3% нанесённого урона возвращается здоровьем (максимум 12%).", "icon_perk_lifesteal", false, 0.9f),
        new PerkDef("crit", "МЕТКА РОКА", "+10% шанс крита. Крит наносит 180% урона.", "icon_perk_crit", false, 1.0f),
        new PerkDef("pierce", "ПРОБОЙ", "Снаряды автоатаки пробивают +1 врага.", "icon_perk_pierce", false, 0.9f),
        new PerkDef("multi", "ВЕЕР", "+1 снаряд в автоатаке.", "icon_perk_multi", false, 0.8f),
        new PerkDef("bomb", "ВЗРЫВНАЯ КОНЧИНА", "Побеждённые враги взрываются и бьют соседей.", "icon_perk_bomb", false, 0.7f),
        new PerkDef("shieldstart", "БУМАЖНЫЙ БАРЬЕР", "Только рыцарь. В начале каждой комнаты даётся щит на 10.", "icon_perk_shield", false, 0.9f),
        new PerkDef("cdr", "УСКОРЕНИЕ КАСТА", "-15% перезарядки навыков.", "icon_perk_cd", false, 1.0f),
        new PerkDef("slot", "НОВЫЙ СЛОТ", "+1 слот навыка и выбор нового навыка.", "icon_perk_slot", false, 0.8f),
        new PerkDef("freeze", "ИНЕЙ НА КОНЧИКЕ", "Автоатака замедляет врагов на 1 секунду.", "icon_perk_freeze", false, 0.8f),
        new PerkDef("poison", "ТОКСИЧНАЯ КРАСКА", "Автоатака отравляет врагов на 3 секунды.", "icon_perk_poison", false, 0.8f),
        new PerkDef("thorns", "ШИПЫ", "Атакующий получает 8 урона.", "icon_perk_thorns", false, 0.8f),
        new PerkDef("healroom", "ПЕРЕДЫШКА", "После каждой комнаты восстанавливается 12% здоровья.", "icon_perk_heal", false, 0.7f),
        new PerkDef("secondwind", "ВТОРОЕ ДЫХАНИЕ", "Один раз за забег смертельный удар оставляет 12% HP и краткую неуязвимость.", "icon_perk_phoenix", true, 0.45f),
        new PerkDef("skillpower", "СИЛЬНАЯ МАГИЯ", "+25% урона навыков.", "icon_perk_armor", false, 0.9f),
        new PerkDef("armor", "ПЛОТНЫЙ КАРТОН", "-8% получаемого урона.", "icon_perk_armor", false, 0.85f)
    };

    public static readonly Dictionary<EnemyKind, EnemyDef> Enemies = new Dictionary<EnemyKind, EnemyDef>
    {
        [EnemyKind.Imp] = new EnemyDef
        {
            Kind = EnemyKind.Imp, Name = "БУМАЖНЫЙ БЕС", Sprite = "imp",
            Hp = 24, Speed = 3.45f, Damage = 11, Range = 0.95f, Cooldown = 1.05f, Scale = 1.0f,
            Special = "fire_spit"
        },
        [EnemyKind.Skeleton] = new EnemyDef
        {
            Kind = EnemyKind.Skeleton, Name = "СКЕЛЕТ-ЛУЧНИК", Sprite = "skeleton",
            Hp = 34, Speed = 2.45f, Damage = 10, Range = 5.2f, Cooldown = 1.85f, Scale = 1.0f,
            Projectile = "proj_arrow", Special = "triple_shot"
        },
        [EnemyKind.Boar] = new EnemyDef
        {
            Kind = EnemyKind.Boar, Name = "КАРТОННЫЙ КАБАН", Sprite = "boar",
            Hp = 62, Speed = 2.85f, Damage = 19, Range = 5.0f, Cooldown = 2.9f, Scale = 1.05f,
            Special = "boar_charge"
        },
        [EnemyKind.Bomber] = new EnemyDef
        {
            Kind = EnemyKind.Bomber, Name = "БОМБЕР", Sprite = "bomber",
            Hp = 28, Speed = 3.0f, Damage = 40, Range = 1.8f, Cooldown = 1.5f, Scale = 1.0f,
            Special = "self_destruct"
        },
        [EnemyKind.Guard] = new EnemyDef
        {
            Kind = EnemyKind.Guard, Name = "СТРАЖ-ЩИТОНОСЕЦ", Sprite = "guard",
            Hp = 105, Speed = 1.7f, Damage = 18, Range = 1.2f, Cooldown = 1.55f, Scale = 1.08f,
            Special = "shield_charge"
        },
        [EnemyKind.Shaman] = new EnemyDef
        {
            Kind = EnemyKind.Shaman, Name = "ШАМАН", Sprite = "shaman",
            Hp = 44, Speed = 2.15f, Damage = 9, Range = 6.5f, Cooldown = 2.45f, Scale = 1.0f,
            Special = "healing_ritual"
        },
        [EnemyKind.Spider] = new EnemyDef
        {
            Kind = EnemyKind.Spider, Name = "ПАУК", Sprite = "spider",
            Hp = 19, Speed = 4.05f, Damage = 8, Range = 0.95f, Cooldown = 0.78f, Scale = 0.92f,
            Special = "web_spit"
        },
        [EnemyKind.Lich] = new EnemyDef
        {
            Kind = EnemyKind.Lich, Name = "ЛИЧ-ПЕРЕПЛЁТ", Sprite = "lich_abyss",
            Hp = 1680, Speed = 1.5f, Damage = 27, Range = 8f, Cooldown = 2.1f, Scale = 1.5f,
            Boss = true, Projectile = "proj_skull", Special = "soul_storm"
        },
        [EnemyKind.Wraith] = new EnemyDef
        {
            Kind = EnemyKind.Wraith, Name = "ПРИЗРАК-ЛУЧНИК", Sprite = "wraith",
            Hp = 58, Speed = 2.35f, Damage = 13, Range = 6.4f, Cooldown = 2.15f, Scale = 1.05f,
            Projectile = "proj_soul", Special = "blink_volley"
        },
        [EnemyKind.Executioner] = new EnemyDef
        {
            Kind = EnemyKind.Executioner, Name = "ПАЛАЧ", Sprite = "executioner",
            Hp = 168, Speed = 1.55f, Damage = 29, Range = 1.35f, Cooldown = 2.15f, Scale = 1.18f,
            Special = "execution_slam"
        },
        [EnemyKind.PlagueDoctor] = new EnemyDef
        {
            Kind = EnemyKind.PlagueDoctor, Name = "ЧУМНОЙ ДОКТОР", Sprite = "plague_doctor",
            Hp = 76, Speed = 2.0f, Damage = 11, Range = 6.2f, Cooldown = 2.7f, Scale = 1.02f,
            Projectile = "proj_poison", Special = "plague_pool"
        },
        [EnemyKind.Hound] = new EnemyDef
        {
            Kind = EnemyKind.Hound, Name = "ГОЛОДНЫЙ ГОНЧИЙ", Sprite = "hound",
            Hp = 46, Speed = 4.25f, Damage = 14, Range = 1.15f, Cooldown = 2.1f, Scale = 0.98f,
            Special = "pounce"
        },
        [EnemyKind.BoneKing] = new EnemyDef
        {
            Kind = EnemyKind.BoneKing, Name = "КОСТЯНОЙ КОРОЛЬ", Sprite = "bone_king",
            Hp = 980, Speed = 1.4f, Damage = 20, Range = 7.4f, Cooldown = 2.2f, Scale = 1.45f,
            Boss = true, Projectile = "proj_bone", Special = "bone_storm"
        },
        [EnemyKind.SpiderQueen] = new EnemyDef
        {
            Kind = EnemyKind.SpiderQueen, Name = "МАТКА ПАУКОВ", Sprite = "spider_queen",
            Hp = 1260, Speed = 1.65f, Damage = 23, Range = 7.2f, Cooldown = 2.0f, Scale = 1.5f,
            Boss = true, Projectile = "proj_poison", Special = "brood_call"
        }
    };

    public static RoomDef[] Rooms => Dungeons[0].Rooms;

    public static SkillDef Skill(string id)
    {
        return Skills.Find(s => s.Id == id);
    }

    public static string[] StarterSkills(HeroClass hero)
    {
        switch (hero)
        {
            case HeroClass.Swordsman: return new[] { "shield", "cleave" };
            case HeroClass.Ranger: return new[] { "volley", "shadowrun" };
            default: return new[] { "fireball", "frost" };
        }
    }

    public static float EffectiveCooldown(SkillDef skill, HeroClass hero)
    {
        if (skill == null) return 0f;
        if (skill.Id == "shield" && hero == HeroClass.Swordsman)
        {
            return KnightShieldCooldown;
        }
        return skill.Cooldown;
    }

    // Классовые пулы разделены полностью: общими остаются только лечение,
    // голем и базовые перки, которые не относятся к чужой школе.
    private static readonly string[] MageSkillIds =
    {
        "fireball", "nova", "chain", "meteor", "blink", "missiles", "slow", "frost", "heal", "golem"
    };

    private static readonly string[] SwordsmanSkillIds =
    {
        "shield", "cleave", "rage", "heal", "golem"
    };

    private static readonly string[] RangerSkillIds =
    {
        "volley", "shadowrun", "poison", "heal", "golem"
    };

    private static readonly string[] SharedPerkIds =
    {
        "damage", "attackspeed", "movespeed", "hp", "lifesteal", "crit", "bomb", "healroom",
        "secondwind", "slot"
    };

    private static readonly string[] MagePerkIds =
    {
        "cdr", "skillpower", "freeze"
    };

    private static readonly string[] SwordsmanPerkIds =
    {
        "shieldstart", "thorns", "armor"
    };

    private static readonly string[] RangerPerkIds =
    {
        "pierce", "multi", "poison"
    };

    private static bool InPool(string[] pool, string id)
    {
        return !string.IsNullOrEmpty(id) && System.Array.IndexOf(pool, id) >= 0;
    }

    public static bool SkillAllowedForClass(string skillId, HeroClass hero)
    {
        switch (hero)
        {
            case HeroClass.Mage:
                return InPool(MageSkillIds, skillId);
            case HeroClass.Swordsman:
                return InPool(SwordsmanSkillIds, skillId);
            case HeroClass.Ranger:
                return InPool(RangerSkillIds, skillId);
            default:
                return false;
        }
    }

    public static bool PerkAllowedForClass(string perkId, HeroClass hero)
    {
        if (InPool(SharedPerkIds, perkId)) return true;
        switch (hero)
        {
            case HeroClass.Mage:
                return InPool(MagePerkIds, perkId);
            case HeroClass.Swordsman:
                return InPool(SwordsmanPerkIds, perkId);
            case HeroClass.Ranger:
                return InPool(RangerPerkIds, perkId);
            default:
                return false;
        }
    }

    public static PerkDef Perk(string id)
    {
        return Perks.Find(p => p.Id == id);
    }

    public static EnemyDef Enemy(EnemyKind kind)
    {
        return Enemies[kind].Clone();
    }

    public static string SkillKey(int index)
    {
        return index == 0 ? "1" : index == 1 ? "2" : index == 2 ? "3" : index == 3 ? "4" : "5";
    }

    public static Color RoomAccent(RoomType type)
    {
        switch (type)
        {
            case RoomType.Elite: return Pal.Gold;
            case RoomType.Treasure: return Pal.Gold;
            case RoomType.Rest: return Pal.Green;
            case RoomType.Boss: return Pal.Red;
            default: return Pal.Cream;
        }
    }
}
