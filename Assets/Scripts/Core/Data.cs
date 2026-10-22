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
    Lich
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

    public RoomDef(RoomType type, string name, string subtitle, int waves)
    {
        Type = type;
        Name = name;
        Subtitle = subtitle;
        Waves = waves;
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

public static class Data
{
    public const int RoomCount = 12;
    public const int MaxSkillSlots = 5;

    public static readonly List<SkillDef> Skills = new List<SkillDef>
    {
        new SkillDef("fireball", "ОГНЕННЫЙ ШАР", "Шар летит в ближайшего врага и взрывается по площади.", 4f,
            "icon_fireball", Pal.Red),
        new SkillDef("shield", "ЩИТ", "Барьер поглощает 35 урона в течение 8 секунд.", 11f,
            "icon_shield", Pal.Blue),
        new SkillDef("nova", "ЛЕДЯНАЯ НОВА", "Волна холода вокруг мага: урон и замедление врагов.", 8f,
            "icon_nova", Pal.Teal),
        new SkillDef("chain", "ЦЕПНАЯ МОЛНИЯ", "Молния бьёт до трёх врагов, урон падает с каждым прыжком.", 8f,
            "icon_chain", Pal.Gold),
        new SkillDef("heal", "ИСЦЕЛЕНИЕ", "Восстанавливает 35% максимального здоровья.", 15f,
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
        new SkillDef("rage", "БЕРСЕРК", "6 секунд: +50% урона и +30% скорости атаки.", 18f,
            "icon_rage", Pal.RedDeep)
    };

    public static readonly List<PerkDef> Perks = new List<PerkDef>
    {
        new PerkDef("damage", "ОСТРЫЙ КРАЙ", "+18% урона автоатаки.", "icon_perk_damage", false, 1.2f),
        new PerkDef("attackspeed", "БЫСТРЫЕ РУКИ", "+14% скорости автоатаки.", "icon_perk_speed", false, 1.1f),
        new PerkDef("movespeed", "ЛЁГКИЕ НОГИ", "+8% скорости передвижения.", "icon_perk_speed", false, 1.0f),
        new PerkDef("hp", "ПЛОТНАЯ БУМАГА", "+25 к максимальному здоровью и лечение на 25.", "icon_perk_hp", false, 1.1f),
        new PerkDef("lifesteal", "ВАМПИРИЗМ", "6% нанесённого урона возвращается здоровьем.", "icon_perk_lifesteal", false, 0.9f),
        new PerkDef("crit", "МЕТКА РОКА", "+12% шанс крита. Крит наносит 180% урона.", "icon_perk_crit", false, 1.0f),
        new PerkDef("pierce", "ПРОБОЙ", "Снаряды автоатаки пробивают +1 врага.", "icon_perk_pierce", false, 0.9f),
        new PerkDef("multi", "ВЕЕР", "+1 снаряд в автоатаке.", "icon_perk_multi", false, 0.8f),
        new PerkDef("bomb", "ВЗРЫВНАЯ КОНЧИНА", "Побеждённые враги взрываются и бьют соседей.", "icon_perk_bomb", false, 0.7f),
        new PerkDef("shieldstart", "БУМАЖНЫЙ БАРЬЕР", "В начале каждой комнаты даётся щит на 15.", "icon_perk_shield", false, 0.9f),
        new PerkDef("cdr", "УСКОРЕНИЕ КАСТА", "-15% перезарядки навыков.", "icon_perk_cd", false, 1.0f),
        new PerkDef("slot", "НОВЫЙ СЛОТ", "+1 слот навыка и выбор нового навыка.", "icon_perk_slot", true, 0.55f),
        new PerkDef("freeze", "ИНЕЙ НА КОНЧИКЕ", "Автоатака замедляет врагов на 1 секунду.", "icon_perk_freeze", false, 0.8f),
        new PerkDef("poison", "ТОКСИЧНАЯ КРАСКА", "Автоатака отравляет врагов на 3 секунды.", "icon_perk_poison", false, 0.8f),
        new PerkDef("thorns", "ШИПЫ", "Атакующий получает 10 урона.", "icon_perk_thorns", false, 0.8f),
        new PerkDef("healroom", "ПЕРЕДЫШКА", "После каждой комнаты восстанавливается 15% здоровья.", "icon_perk_heal", false, 0.7f),
        new PerkDef("secondwind", "ВТОРОЕ ДЫХАНИЕ", "Один раз за забег смертельный удар оставляет 1 HP и щит.", "icon_perk_phoenix", true, 0.45f),
        new PerkDef("skillpower", "СИЛЬНАЯ МАГИЯ", "+25% урона навыков.", "icon_perk_armor", false, 0.9f),
        new PerkDef("armor", "ПЛОТНЫЙ КАРТОН", "-10% получаемого урона.", "icon_perk_armor", false, 0.85f)
    };

    public static readonly Dictionary<EnemyKind, EnemyDef> Enemies = new Dictionary<EnemyKind, EnemyDef>
    {
        [EnemyKind.Imp] = new EnemyDef
        {
            Kind = EnemyKind.Imp, Name = "БУМАЖНЫЙ БЕС", Sprite = "imp",
            Hp = 16, Speed = 3.1f, Damage = 8, Range = 0.95f, Cooldown = 1.15f, Scale = 1.0f
        },
        [EnemyKind.Skeleton] = new EnemyDef
        {
            Kind = EnemyKind.Skeleton, Name = "СКЕЛЕТ-ЛУЧНИК", Sprite = "skeleton",
            Hp = 22, Speed = 2.25f, Damage = 7, Range = 5.2f, Cooldown = 2.0f, Scale = 1.0f
        },
        [EnemyKind.Boar] = new EnemyDef
        {
            Kind = EnemyKind.Boar, Name = "КАРТОННЫЙ КАБАН", Sprite = "boar",
            Hp = 42, Speed = 2.5f, Damage = 14, Range = 5.0f, Cooldown = 3.2f, Scale = 1.05f
        },
        [EnemyKind.Bomber] = new EnemyDef
        {
            Kind = EnemyKind.Bomber, Name = "БОМБЕР", Sprite = "bomber",
            Hp = 18, Speed = 2.7f, Damage = 26, Range = 1.8f, Cooldown = 1.6f, Scale = 1.0f
        },
        [EnemyKind.Guard] = new EnemyDef
        {
            Kind = EnemyKind.Guard, Name = "СТРАЖ-ЩИТОНОСЕЦ", Sprite = "guard",
            Hp = 64, Speed = 1.55f, Damage = 12, Range = 1.2f, Cooldown = 1.7f, Scale = 1.08f
        },
        [EnemyKind.Shaman] = new EnemyDef
        {
            Kind = EnemyKind.Shaman, Name = "ШАМАН", Sprite = "shaman",
            Hp = 28, Speed = 1.9f, Damage = 6, Range = 6.5f, Cooldown = 2.7f, Scale = 1.0f
        },
        [EnemyKind.Spider] = new EnemyDef
        {
            Kind = EnemyKind.Spider, Name = "ПАУК", Sprite = "spider",
            Hp = 14, Speed = 3.7f, Damage = 6, Range = 0.95f, Cooldown = 0.85f, Scale = 0.92f
        },
        [EnemyKind.Lich] = new EnemyDef
        {
            Kind = EnemyKind.Lich, Name = "ЛИЧ-ПЕРЕПЛЁТ", Sprite = "lich",
            Hp = 760, Speed = 1.35f, Damage = 16, Range = 8f, Cooldown = 2.3f, Scale = 1.45f
        }
    };

    public static readonly RoomDef[] Rooms =
    {
        new RoomDef(RoomType.Combat, "ПОГРЕБ", "Здесь пахнет клеем и старой бумагой.", 2),
        new RoomDef(RoomType.Combat, "КЛАДОВАЯ", "Кто-то оставил здесь свои кости.", 2),
        new RoomDef(RoomType.Elite, "СТРАЖНИЦА", "Щит не пропустит просто так.", 1),
        new RoomDef(RoomType.Combat, "КОРИДОР КОСТЕЙ", "Стены сложены из чьих-то рёбер.", 3),
        new RoomDef(RoomType.Treasure, "СОКРОВИЩНИЦА", "Сундук ждал именно тебя.", 0),
        new RoomDef(RoomType.Combat, "ЗАЛ ПАУТИНЫ", "Не трогай липкие нити.", 2),
        new RoomDef(RoomType.Elite, "АЛТАРЬ", "Кто-то здесь ещё молится.", 2),
        new RoomDef(RoomType.Rest, "ПРИВАЛ", "Можно перевести дух.", 0),
        new RoomDef(RoomType.Combat, "ТЁМНЫЙ ПРЕДЕЛ", "Свет факелов едва достаёт до стен.", 3),
        new RoomDef(RoomType.Combat, "ЗАЛ ПЕЧАТЕЙ", "Каждый шаг отдаётся эхом.", 3),
        new RoomDef(RoomType.Elite, "ТРОННЫЙ ПРЕДДВЕР", "За дверью что-то шевелится.", 2),
        new RoomDef(RoomType.Boss, "ТРОН ЛИЧА", "Он ждал тебя все эти страницы.", 1)
    };

    public static SkillDef Skill(string id)
    {
        return Skills.Find(s => s.Id == id);
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
