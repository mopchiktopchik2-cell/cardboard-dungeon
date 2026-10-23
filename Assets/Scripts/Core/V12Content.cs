using System.Collections.Generic;
using UnityEngine;

public enum DungeonDifficulty
{
    Easy,
    Medium,
    Hard
}

public enum HeroClass
{
    Mage,
    Swordsman,
    Ranger
}

public enum HeroAttackStyle
{
    Bolt,
    Slash,
    Arrow
}

public enum EquipSlot
{
    Weapon,
    Armor,
    Charm
}

public enum Rarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public class HeroClassDef
{
    public readonly HeroClass Id;
    public readonly string Name;
    public readonly string Title;
    public readonly string Desc;
    public readonly string Sprite;
    public readonly Color Accent;
    public readonly HeroAttackStyle Style;
    public readonly float Hp;
    public readonly float Speed;
    public readonly float Damage;
    public readonly float AttackInterval;
    public readonly float Range;
    public readonly float Crit;
    public readonly float Armor;
    public readonly float SkillPower;

    public HeroClassDef(HeroClass id, string name, string title, string desc, string sprite, Color accent,
        HeroAttackStyle style, float hp, float speed, float damage, float interval, float range, float crit,
        float armor, float skillPower)
    {
        Id = id;
        Name = name;
        Title = title;
        Desc = desc;
        Sprite = sprite;
        Accent = accent;
        Style = style;
        Hp = hp;
        Speed = speed;
        Damage = damage;
        AttackInterval = interval;
        Range = range;
        Crit = crit;
        Armor = armor;
        SkillPower = skillPower;
    }
}

public class DungeonDef
{
    public string Id;
    public string Name;
    public string Tagline;
    public string Lore;
    public DungeonDifficulty Difficulty;
    public Color Accent;
    public string FloorA;
    public string FloorB;
    public string Wall;
    public EnemyKind BossKind;
    public string BossName;
    public float HpScale;
    public float DamageScale;
    public float PerRoomHp;
    public float PerRoomDamage;
    public float DropChance;
    public int LootTier;
    public RoomDef[] Rooms;
    public EnemyKind[] Pool;
    public EnemyKind[] Adds;
}

public class EquipmentDef
{
    public string Id;
    public string Name;
    public string Desc;
    public string Icon;
    public EquipSlot Slot;
    public Rarity Rarity;
    public int Tier;
    public float Hp;
    public float Damage;
    public float Speed;
    public float Crit;
    public float Armor;
    public float Lifesteal;
    public float SkillPower;
    public float Weight = 10f;

    public string StatLine()
    {
        var parts = new List<string>();
        if (Damage > 0f) parts.Add("+" + Damage.ToString("0.#") + " урона");
        if (Hp > 0f) parts.Add("+" + Hp.ToString("0") + " HP");
        if (Speed > 0f) parts.Add("+" + (Speed * 100f).ToString("0") + "% скорость");
        if (Crit > 0f) parts.Add("+" + (Crit * 100f).ToString("0") + "% крит");
        if (Armor > 0f) parts.Add("+" + (Armor * 100f).ToString("0") + "% броня");
        if (Lifesteal > 0f) parts.Add("+" + (Lifesteal * 100f).ToString("0") + "% вампиризм");
        if (SkillPower > 0f) parts.Add("+" + (SkillPower * 100f).ToString("0") + "% сила навыков");
        return string.Join(" · ", parts.ToArray());
    }
}

public class RoomTheme
{
    public readonly string Id;
    public readonly string Floor;
    public readonly string Wall;
    public readonly Color FloorTint;
    public readonly Color WallTint;
    public readonly Color FogA;
    public readonly Color FogB;
    public readonly Color Torch;
    public readonly Color Motes;
    public readonly string[] Props;
    public readonly string Centerpiece;
    public readonly int PropCount;

    public RoomTheme(string id, string floor, string wall, Color floorTint, Color wallTint, Color fogA, Color fogB,
        Color torch, Color motes, string[] props, string centerpiece, int propCount)
    {
        Id = id;
        Floor = floor;
        Wall = wall;
        FloorTint = floorTint;
        WallTint = wallTint;
        FogA = fogA;
        FogB = fogB;
        Torch = torch;
        Motes = motes;
        Props = props;
        Centerpiece = centerpiece;
        PropCount = propCount;
    }
}

public static partial class Data
{
    public static readonly List<HeroClassDef> HeroClasses = BuildHeroClasses();
    public static readonly List<DungeonDef> Dungeons = BuildDungeons();
    public static readonly List<EquipmentDef> Equipment = BuildEquipment();

    private static Dictionary<string, RoomTheme> themeCache;

    private static Color C(string hex)
    {
        Color color;
        return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.white;
    }

    private static List<HeroClassDef> BuildHeroClasses()
    {
        return new List<HeroClassDef>
        {
            new HeroClassDef(HeroClass.Mage, "МАГ", "ПЕПЕЛЬНЫЙ УЧЕНИК",
                "Дальний бой. Хрупкий, но наносит высокий урон и лучше владеет навыками.",
                "wizard", Pal.Teal, HeroAttackStyle.Bolt,
                100f, 4.9f, 9f, 0.75f, 6.9f, 0.04f, 0f, 1.15f),
            new HeroClassDef(HeroClass.Swordsman, "МЕЧНИК", "ПОСЛЕДНИЙ СТРАЖ",
                "Ближний бой. Широкий размах меча задевает сразу нескольких врагов.",
                "swordsman", Pal.Gold, HeroAttackStyle.Slash,
                155f, 5.15f, 16f, 0.62f, 2.45f, 0.05f, 0.08f, 0.92f),
            new HeroClassDef(HeroClass.Ranger, "РЕЙНДЖЕР", "СЛЕПОЙ ОХОТНИК",
                "Быстрые стрелы, большая дальность и высокий шанс критического удара.",
                "ranger", Pal.Green, HeroAttackStyle.Arrow,
                112f, 5.45f, 7.4f, 0.46f, 8.6f, 0.10f, 0f, 1f)
        };
    }

    private static List<DungeonDef> BuildDungeons()
    {
        var crypt = new DungeonDef
        {
            Id = "crypt",
            Name = "КОСТЯНОЙ СКЛЕП",
            Tagline = "Тишина, кости и первый страх",
            Lore = "Под старым картонным театром лежат залы, где когда-то хоронили неудачных героев. " +
                   "Костяной король всё ещё ждёт своего наследника.",
            Difficulty = DungeonDifficulty.Easy,
            Accent = new Color32(190, 206, 166, 255),
            FloorA = "floor_crypt_a",
            FloorB = "floor_crypt_b",
            Wall = "wall_crypt",
            BossKind = EnemyKind.BoneKing,
            BossName = "КОСТЯНОЙ КОРОЛЬ",
            HpScale = 0.82f,
            DamageScale = 0.82f,
            PerRoomHp = 0.14f,
            PerRoomDamage = 0.075f,
            DropChance = 0.45f,
            LootTier = 1,
            Pool = new[] { EnemyKind.Imp, EnemyKind.Skeleton, EnemyKind.Spider, EnemyKind.Guard },
            Adds = new[] { EnemyKind.Imp, EnemyKind.Skeleton },
            Rooms = new[]
            {
                new RoomDef(RoomType.Combat, "ПОГРЕБ", "Здесь пахнет клеем, землёй и старой бумагой.", 2, "crypt_cellar"),
                new RoomDef(RoomType.Combat, "КЛАДОВАЯ", "Кто-то оставил здесь свои кости. И не только кости.", 2, "crypt_storage"),
                new RoomDef(RoomType.Elite, "СТРАЖНИЦА", "Щит не пропустит просто так.", 1, "crypt_guard"),
                new RoomDef(RoomType.Combat, "КОРИДОР КОСТЕЙ", "Стены сложены из чьих-то рёбер.", 3, "crypt_bones"),
                new RoomDef(RoomType.Treasure, "СУНДУК В ЗАВАЛАХ", "Сундук ждал именно тебя.", 0, "crypt_treasure"),
                new RoomDef(RoomType.Combat, "ЗАЛ ПАУТИНЫ", "Не трогай липкие нити. Они уже тянутся к тебе.", 2, "crypt_web"),
                new RoomDef(RoomType.Elite, "АЛТАРЬ", "Кто-то здесь ещё молится. Свечи горят сами.", 2, "crypt_altar"),
                new RoomDef(RoomType.Rest, "ПРИВАЛ У КОСТРА", "Можно перевести дух. Ненадолго.", 0, "crypt_rest"),
                new RoomDef(RoomType.Combat, "ТЁМНЫЙ ПРЕДЕЛ", "Свет факелов едва достаёт до стен.", 3, "crypt_dark"),
                new RoomDef(RoomType.Combat, "ЗАЛ ПЕЧАТЕЙ", "Каждый шаг отдаётся эхом в пустых доспехах.", 3, "crypt_seals"),
                new RoomDef(RoomType.Elite, "ТРОННЫЙ ПРЕДДВЕР", "За дверью что-то шевелится в темноте.", 2, "crypt_vestibule"),
                new RoomDef(RoomType.Boss, "ТРОН КОСТЯНОГО КОРОЛЯ", "Он ждал тебя все эти страницы.", 1, "crypt_boss")
            }
        };

        var web = new DungeonDef
        {
            Id = "web",
            Name = "ПАУЧЬЕ ГНЕЗДО",
            Tagline = "Липкая тьма шевелится",
            Lore = "Туннели под городом обросли паутиной. В коконах ещё слышны голоса тех, " +
                   "кто спустился сюда за сокровищами. Матка пауков не любит гостей.",
            Difficulty = DungeonDifficulty.Medium,
            Accent = new Color32(190, 132, 220, 255),
            FloorA = "floor_web_a",
            FloorB = "floor_web_b",
            Wall = "wall_web",
            BossKind = EnemyKind.SpiderQueen,
            BossName = "МАТКА ПАУКОВ",
            HpScale = 1.12f,
            DamageScale = 1.06f,
            PerRoomHp = 0.16f,
            PerRoomDamage = 0.095f,
            DropChance = 0.65f,
            LootTier = 2,
            Pool = new[] { EnemyKind.Spider, EnemyKind.Bomber, EnemyKind.Shaman, EnemyKind.Boar, EnemyKind.Hound },
            Adds = new[] { EnemyKind.Spider, EnemyKind.Hound },
            Rooms = new[]
            {
                new RoomDef(RoomType.Combat, "ВХОД В ГНЕЗДО", "Паутина заклеила дверь изнутри.", 2, "web_entry"),
                new RoomDef(RoomType.Combat, "ЛИПКИЕ ТУННЕЛИ", "Стены дышат. Или это просто паутина?", 2, "web_tunnels"),
                new RoomDef(RoomType.Elite, "ВЫВОДОК", "Здесь слишком много глаз для одной комнаты.", 2, "web_brood"),
                new RoomDef(RoomType.Combat, "КОКОНЫ", "В некоторых ещё бьётся сердце.", 3, "web_cocoon"),
                new RoomDef(RoomType.Treasure, "ЛИПКАЯ КАЗНА", "Паутина не тронула только сундук.", 0, "web_treasure"),
                new RoomDef(RoomType.Combat, "ПАУЧЬЯ ЯМА", "Снизу слышно щёлканье жвал.", 2, "web_pit"),
                new RoomDef(RoomType.Elite, "МАТОЧНЫЙ ЗАЛ", "Здесь выводятся те, кто тебя ждёт.", 2, "web_broodmother"),
                new RoomDef(RoomType.Rest, "ТИХИЙ УГОЛ", "Костёр отгоняет мелких пауков.", 0, "web_rest"),
                new RoomDef(RoomType.Combat, "СЛЕПОЙ КОРИДОР", "Факел гаснет, но глаза в темноте — нет.", 3, "web_blind"),
                new RoomDef(RoomType.Combat, "КИСЛОТНЫЕ ЛУЖИ", "Пол шипит под сапогами.", 3, "web_acid"),
                new RoomDef(RoomType.Elite, "ЛОГОВО МАТКИ", "Коконы здесь размером с человека.", 2, "web_queenlair"),
                new RoomDef(RoomType.Boss, "ТРОН МАТКИ", "Паутина натянута, как струны.", 1, "web_boss")
            }
        };

        var throne = new DungeonDef
        {
            Id = "throne",
            Name = "ПРОКЛЯТЫЙ ТРОН",
            Tagline = "Здесь коронуют мёртвых",
            Lore = "Последний зал картонного королевства. Здесь правил Лич-Переплёт, пока не сшил " +
                   "себе новую корону из чужих страниц. Двор до сих пор принимает гостей.",
            Difficulty = DungeonDifficulty.Hard,
            Accent = new Color32(224, 92, 78, 255),
            FloorA = "floor_throne_a",
            FloorB = "floor_throne_b",
            Wall = "wall_throne",
            BossKind = EnemyKind.Lich,
            BossName = "ЛИЧ-ПЕРЕПЛЁТ",
            HpScale = 1.45f,
            DamageScale = 1.28f,
            PerRoomHp = 0.18f,
            PerRoomDamage = 0.11f,
            DropChance = 0.85f,
            LootTier = 3,
            Pool = new[] { EnemyKind.Wraith, EnemyKind.Executioner, EnemyKind.PlagueDoctor, EnemyKind.Hound,
                EnemyKind.Guard, EnemyKind.Skeleton },
            Adds = new[] { EnemyKind.Wraith, EnemyKind.PlagueDoctor },
            Rooms = new[]
            {
                new RoomDef(RoomType.Combat, "ПЕРЕДДВЕРЬ", "Пыль лежит толще, чем каменные плиты.", 2, "throne_gate"),
                new RoomDef(RoomType.Combat, "ЗАЛ ПАЛАЧА", "Топор прислонён к стене. Он ещё тёплый.", 2, "throne_executioner"),
                new RoomDef(RoomType.Elite, "КОРОЛЕВСКАЯ СТРАЖА", "Доспехи пусты, но всё ещё стоят на посту.", 2, "throne_royal"),
                new RoomDef(RoomType.Combat, "ГАЛЕРЕЯ ПЛАЧА", "Портреты плачут чернилами.", 3, "throne_weeping"),
                new RoomDef(RoomType.Treasure, "КАЗНА ПУСТОГО КОРОЛЯ", "Золото здесь пахнет железом.", 0, "throne_treasure"),
                new RoomDef(RoomType.Combat, "ЧУМНОЙ БЛОК", "В воздухе висит зелёная взвесь.", 3, "throne_plague"),
                new RoomDef(RoomType.Elite, "ЗЕРКАЛЬНЫЙ ЗАЛ", "В отражениях слишком много дверей.", 2, "throne_mirror"),
                new RoomDef(RoomType.Rest, "СИНИЙ КОСТЁР", "Огонь здесь горит синим, но греет.", 0, "throne_rest"),
                new RoomDef(RoomType.Combat, "ПРОКЛЯТЫЙ МОСТ", "Под мостом — только чернильная пустота.", 3, "throne_bridge"),
                new RoomDef(RoomType.Combat, "ЗАЛ ПРИЗРАКОВ", "Они повторяют твои шаги.", 3, "throne_ghost"),
                new RoomDef(RoomType.Elite, "ЗАЛ КОРОНАЦИИ", "Корона ждёт на подушке из страниц.", 2, "throne_vestibule"),
                new RoomDef(RoomType.Boss, "ТРОН ПЕРЕПЛЁТА", "Лич-Переплёт уже открыл последнюю страницу.", 1, "throne_boss")
            }
        };

        return new List<DungeonDef> { crypt, web, throne };
    }

    private static List<EquipmentDef> BuildEquipment()
    {
        return new List<EquipmentDef>
        {
            new EquipmentDef
            {
                Id = "rusty_blade", Name = "РЖАВЫЙ КЛИНОК", Icon = "eq_rusty_blade",
                Desc = "Тупой, но всё ещё тяжелее кулака.", Slot = EquipSlot.Weapon, Rarity = Rarity.Common,
                Tier = 1, Damage = 1.2f, Weight = 30f
            },
            new EquipmentDef
            {
                Id = "patched_cloak", Name = "ЛАТАНЫЙ ПЛАЩ", Icon = "eq_patched_cloak",
                Desc = "Кто-то зашил дыры чужой кожей.", Slot = EquipSlot.Armor, Rarity = Rarity.Common,
                Tier = 1, Hp = 12f, Weight = 30f
            },
            new EquipmentDef
            {
                Id = "bone_charm", Name = "КОСТЯНОЙ ОБЕРЕГ", Icon = "eq_bone_charm",
                Desc = "Тёплый, будто внутри ещё есть жизнь.", Slot = EquipSlot.Charm, Rarity = Rarity.Common,
                Tier = 1, Crit = 0.02f, Weight = 30f
            },
            new EquipmentDef
            {
                Id = "grave_cleaver", Name = "МОГИЛЬНЫЙ ТЕСАК", Icon = "eq_grave_cleaver",
                Desc = "Им рубили гробы. Иногда — вместе с содержимым.", Slot = EquipSlot.Weapon,
                Rarity = Rarity.Rare, Tier = 1, Damage = 3f, Crit = 0.01f, Weight = 15f
            },
            new EquipmentDef
            {
                Id = "widow_fang", Name = "КЛЫК ВДОВЫ", Icon = "eq_widow_fang",
                Desc = "Клинок выточен из паучьего жвала.", Slot = EquipSlot.Weapon, Rarity = Rarity.Rare,
                Tier = 2, Damage = 4.2f, Crit = 0.03f, Weight = 30f
            },
            new EquipmentDef
            {
                Id = "web_carapace", Name = "ПАНЦИРЬ ПАУКА", Icon = "eq_web_carapace",
                Desc = "Лёгкий, как бумага, прочный, как кость.", Slot = EquipSlot.Armor, Rarity = Rarity.Rare,
                Tier = 2, Hp = 22f, Armor = 0.03f, Weight = 30f
            },
            new EquipmentDef
            {
                Id = "ember_eye", Name = "ТЛЕЮЩИЙ ГЛАЗ", Icon = "eq_ember_eye",
                Desc = "Смотрит на тебя изнутри амулета.", Slot = EquipSlot.Charm, Rarity = Rarity.Rare,
                Tier = 2, SkillPower = 0.06f, Weight = 30f
            },
            new EquipmentDef
            {
                Id = "spider_heart", Name = "СЕРДЦЕ МАТКИ", Icon = "eq_spider_heart",
                Desc = "Ещё сокращается. В такт твоим шагам.", Slot = EquipSlot.Charm, Rarity = Rarity.Epic,
                Tier = 2, Speed = 0.06f, Crit = 0.04f, Weight = 12f
            },
            new EquipmentDef
            {
                Id = "kingsbane", Name = "КОРОЛЕУБИЙЦА", Icon = "eq_kingsbane",
                Desc = "На клинке выгравированы имена всех, кто им владел.", Slot = EquipSlot.Weapon,
                Rarity = Rarity.Legendary, Tier = 3, Damage = 8f, Crit = 0.06f, Speed = 0.04f, Weight = 18f
            },
            new EquipmentDef
            {
                Id = "royal_shroud", Name = "КОРОЛЕВСКИЙ САВАН", Icon = "eq_royal_shroud",
                Desc = "Ткань помнит прикосновение короны.", Slot = EquipSlot.Armor, Rarity = Rarity.Legendary,
                Tier = 3, Hp = 38f, Armor = 0.07f, Lifesteal = 0.04f, Weight = 18f
            },
            new EquipmentDef
            {
                Id = "lich_seal", Name = "ПЕЧАТЬ ЛИЧА", Icon = "eq_lich_seal",
                Desc = "Не носи её слишком долго.", Slot = EquipSlot.Charm, Rarity = Rarity.Legendary,
                Tier = 3, SkillPower = 0.10f, Damage = 1.5f, Weight = 18f
            },
            new EquipmentDef
            {
                Id = "crown_of_ashes", Name = "КОРОНА ИЗ ПЕПЛА", Icon = "eq_crown_of_ashes",
                Desc = "Каждый, кто её надевал, становился чуть менее живым.", Slot = EquipSlot.Charm,
                Rarity = Rarity.Epic, Tier = 3, Hp = 24f, Crit = 0.04f, SkillPower = 0.05f, Weight = 10f
            }
        };
    }

    public static HeroClassDef Hero(HeroClass id)
    {
        return HeroClasses.Find(h => h.Id == id) ?? HeroClasses[0];
    }

    public static DungeonDef Dungeon(string id)
    {
        return Dungeons.Find(d => d.Id == id) ?? Dungeons[0];
    }

    public static DungeonDef Dungeon(int index)
    {
        if (index < 0 || index >= Dungeons.Count) return Dungeons[0];
        return Dungeons[index];
    }

    public static EquipmentDef Item(string id)
    {
        return Equipment.Find(e => e.Id == id);
    }

    public static string SlotName(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.Weapon: return "ОРУЖИЕ";
            case EquipSlot.Armor: return "БРОНЯ";
            default: return "АМУЛЕТ";
        }
    }

    public static string RarityName(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return "ОБЫЧНОЕ";
            case Rarity.Rare: return "РЕДКОЕ";
            case Rarity.Epic: return "ЭПИЧЕСКОЕ";
            default: return "ЛЕГЕНДАРНОЕ";
        }
    }

    public static Color RarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return new Color32(190, 184, 170, 255);
            case Rarity.Rare: return new Color32(95, 168, 232, 255);
            case Rarity.Epic: return new Color32(192, 123, 255, 255);
            default: return new Color32(242, 179, 76, 255);
        }
    }

    public static string DifficultyName(DungeonDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DungeonDifficulty.Easy: return "ЛЕГКО";
            case DungeonDifficulty.Medium: return "СРЕДНЕ";
            default: return "ТРУДНО";
        }
    }

    public static Color DifficultyColor(DungeonDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DungeonDifficulty.Easy: return new Color32(129, 190, 128, 255);
            case DungeonDifficulty.Medium: return new Color32(226, 176, 88, 255);
            default: return new Color32(214, 91, 78, 255);
        }
    }

    public static RoomTheme Theme(string id)
    {
        if (themeCache == null) themeCache = BuildThemes();
        RoomTheme theme;
        if (!string.IsNullOrEmpty(id) && themeCache.TryGetValue(id, out theme)) return theme;
        return themeCache["crypt_cellar"];
    }

    private static Dictionary<string, RoomTheme> BuildThemes()
    {
        var d = new Dictionary<string, RoomTheme>();

        void Add(string id, string floor, string wall, string floorTint, string wallTint, string fogA, string fogB,
            string torch, string motes, string props, string center, int count = 4)
        {
            d[id] = new RoomTheme(id, floor, wall, C(floorTint), C(wallTint), C(fogA), C(fogB), C(torch), C(motes),
                string.IsNullOrEmpty(props) ? new string[0] : props.Split(','), center, count);
        }

        // Костяной склеп — каждая комната имеет собственный свет, декор и центр.
        Add("crypt_cellar", "floor_crypt_a", "wall_crypt", "#B9A98B", "#7A6955", "#3A5A46", "#4A3028",
            "#FFB35C", "#C9B58A", "prop_barrel,prop_crate,prop_bones,prop_moss", "prop_sarcophagus", 5);
        Add("crypt_storage", "floor_crypt_b", "wall_crypt", "#C6B08A", "#8B7358", "#6B4B26", "#3A2A1F",
            "#FFC46B", "#E0C48A", "prop_crate,prop_barrel,prop_chest,prop_bones", "prop_rack", 5);
        Add("crypt_guard", "floor_crypt_a", "wall_crypt", "#9AA6B6", "#6E7C8E", "#2E4660", "#262E3E",
            "#FFD07A", "#B8C8E0", "prop_rack,prop_shield,prop_bones,prop_chain", "prop_cage", 4);
        Add("crypt_bones", "floor_crypt_b", "wall_crypt", "#D0C3A8", "#8E826E", "#3A5A46", "#4A3028",
            "#FFB35C", "#D8CCAA", "prop_bones,prop_skullpile,prop_ribs,prop_moss", "prop_bone_arch", 5);
        Add("crypt_treasure", "floor_crypt_a", "wall_crypt", "#D6B46A", "#A98A56", "#7A5A20", "#3A2A1F",
            "#FFD98A", "#FFE0A0", "prop_chest,prop_barrel,prop_candles,prop_bones", "prop_chest", 4);
        Add("crypt_web", "floor_crypt_b", "wall_crypt", "#7E6E84", "#5E5068", "#4A2E5A", "#2A2038",
            "#D9A0FF", "#C8A8E0", "prop_web,prop_cocoon,prop_bones,prop_moss", "prop_web", 5);
        Add("crypt_altar", "floor_crypt_a", "wall_crypt", "#A88C78", "#7A5F54", "#5A1E2A", "#2A1018",
            "#FF7A5A", "#E0A0A0", "prop_candles,prop_skullpile,prop_altar,prop_chain", "prop_altar", 4);
        Add("crypt_rest", "floor_crypt_b", "wall_crypt", "#C0A070", "#8A6A48", "#6A4A22", "#2A1E16",
            "#FFC06A", "#FFD8A0", "prop_campfire,prop_barrel,prop_bones,prop_moss", "prop_campfire", 3);
        Add("crypt_dark", "floor_crypt_a", "wall_crypt", "#5E5A66", "#3E3A48", "#1E2A44", "#121422",
            "#7A9CFF", "#8AA0C8", "prop_chain,prop_rock,prop_bones,prop_moss", "prop_chain", 4);
        Add("crypt_seals", "floor_crypt_b", "wall_crypt", "#7A8A86", "#566A68", "#1E4A4A", "#14282A",
            "#7AF0E0", "#8AD8D0", "prop_candles,prop_chain,prop_skullpile,prop_rock", "prop_altar", 4);
        Add("crypt_vestibule", "floor_crypt_a", "wall_crypt", "#8A6A5A", "#5E4438", "#4A1E22", "#241014",
            "#FF8A5A", "#D8A080", "prop_banner,prop_column,prop_chain,prop_candles", "prop_throne", 5);
        Add("crypt_boss", "floor_crypt_b", "wall_crypt", "#6A7A66", "#4A5A48", "#2A5A3A", "#14281E",
            "#9CFFB0", "#A0D8A8", "prop_ribs,prop_skullpile,prop_banner,prop_chain", "prop_bone_arch", 6);

        // Паучье гнездо — липкие туннели, коконы и кислотные лужи.
        Add("web_entry", "floor_web_a", "wall_web", "#7A8A70", "#5A6A54", "#3A5A3A", "#1E2A1E",
            "#A8FF9A", "#B0D8A0", "prop_web,prop_roots,prop_bones,prop_eggs", "prop_web", 5);
        Add("web_tunnels", "floor_web_b", "wall_web", "#6E5A7A", "#4E4058", "#3A1E4A", "#1A1022",
            "#C08AFF", "#B090D0", "prop_web,prop_cocoon,prop_bones,prop_roots", "prop_cocoon", 5);
        Add("web_brood", "floor_web_a", "wall_web", "#8A5A6A", "#5E3E4A", "#5A1E3A", "#2A0E1A",
            "#FF8AC0", "#E0A0C0", "prop_eggs,prop_cocoon,prop_web,prop_bones", "prop_eggs", 5);
        Add("web_cocoon", "floor_web_b", "wall_web", "#7A6A4A", "#5A4A32", "#4A3A1E", "#221A0E",
            "#FFD08A", "#D8C090", "prop_cocoon,prop_web,prop_bones,prop_roots", "prop_cocoon", 6);
        Add("web_treasure", "floor_web_a", "wall_web", "#8A7A4A", "#5E543A", "#5A4A1E", "#2A2010",
            "#FFE08A", "#F0D890", "prop_chest,prop_web,prop_candles,prop_bones", "prop_chest", 4);
        Add("web_pit", "floor_web_b", "wall_web", "#4A4A5A", "#2E2E3A", "#1E1E3A", "#0E0E1E",
            "#8AA0FF", "#8090B0", "prop_web,prop_rock,prop_bones,prop_eggs", "prop_web", 5);
        Add("web_broodmother", "floor_web_a", "wall_web", "#6A4A6A", "#4A2E4A", "#4A1E4A", "#1E0E1E",
            "#FF7AD0", "#D090D0", "prop_eggs,prop_cocoon,prop_web,prop_skullpile", "prop_eggs", 6);
        Add("web_rest", "floor_web_b", "wall_web", "#7A6A4A", "#5A4A32", "#4A341E", "#22180E",
            "#FFB86A", "#E0C090", "prop_campfire,prop_web,prop_bones,prop_moss", "prop_campfire", 3);
        Add("web_blind", "floor_web_a", "wall_web", "#5A5A60", "#3A3A40", "#1E2A2A", "#0E1414",
            "#7AD8D0", "#90B0B0", "prop_rock,prop_web,prop_chain,prop_bones", "prop_web", 5);
        Add("web_acid", "floor_web_b", "wall_web", "#6A7A4A", "#4A5A32", "#3A5A1E", "#1A2A0E",
            "#B0FF6A", "#C0E090", "prop_acid,prop_bones,prop_web,prop_roots", "prop_acid", 5);
        Add("web_queenlair", "floor_web_a", "wall_web", "#6A3A5A", "#4A2A3E", "#5A1040", "#280818",
            "#FF5AA0", "#E080B0", "prop_eggs,prop_cocoon,prop_web,prop_banner", "prop_web_throne", 6);
        Add("web_boss", "floor_web_b", "wall_web", "#5A2E4A", "#3A1E32", "#6A104A", "#2A0820",
            "#FF4A90", "#F080B0", "prop_eggs,prop_cocoon,prop_web,prop_skullpile", "prop_web_throne", 6);

        // Проклятый трон — двор мёртвого королевства.
        Add("throne_gate", "floor_throne_a", "wall_throne", "#6A6A7A", "#4A4A58", "#1E2A4A", "#0E1222",
            "#FFB86A", "#A0B0D0", "prop_chain,prop_column,prop_banner,prop_bones", "prop_gate", 5);
        Add("throne_executioner", "floor_throne_b", "wall_throne", "#5A3030", "#3A2020", "#3A0E0E", "#1A0606",
            "#FF6A4A", "#D08070", "prop_rack,prop_skullpile,prop_chain,prop_blood", "prop_rack", 6);
        Add("throne_royal", "floor_throne_a", "wall_throne", "#4A5A7A", "#2E3A52", "#1E2E5A", "#0E1428",
            "#FFD08A", "#B0C0E0", "prop_banner,prop_column,prop_candles,prop_shield", "prop_throne", 5);
        Add("throne_weeping", "floor_throne_b", "wall_throne", "#4A5A6A", "#2E3A44", "#1E3A5A", "#0E1A28",
            "#7AC8FF", "#90B8D8", "prop_mirror,prop_candles,prop_chain,prop_bones", "prop_mirror", 6);
        Add("throne_treasure", "floor_throne_a", "wall_throne", "#6A5A3A", "#4A3E28", "#4A3A1E", "#221A0E",
            "#FFD86A", "#F0D090", "prop_chest,prop_candles,prop_banner,prop_bones", "prop_chest", 4);
        Add("throne_plague", "floor_throne_b", "wall_throne", "#5A6A4A", "#3E4A32", "#3A5A1E", "#1A2A0E",
            "#B0FF7A", "#C0E090", "prop_cauldron,prop_bones,prop_chain,prop_roots", "prop_cauldron", 6);
        Add("throne_mirror", "floor_throne_a", "wall_throne", "#6A6A7A", "#4A4A58", "#2A2A4A", "#121222",
            "#C0D0FF", "#C0C8E0", "prop_mirror,prop_candles,prop_chain,prop_skullpile", "prop_mirror", 6);
        Add("throne_rest", "floor_throne_b", "wall_throne", "#6A5A4A", "#4A3E32", "#4A2E1E", "#22140E",
            "#FFB86A", "#D8B090", "prop_campfire,prop_banner,prop_bones,prop_candles", "prop_campfire", 3);
        Add("throne_bridge", "floor_throne_a", "wall_throne", "#4A4A54", "#2E2E36", "#1E2A3A", "#0E121A",
            "#8AB8FF", "#90A8C0", "prop_chain,prop_rock,prop_bones,prop_mirror", "prop_gate", 5);
        Add("throne_ghost", "floor_throne_b", "wall_throne", "#4A5A66", "#2E3A44", "#1E4A5A", "#0E1E28",
            "#7AFFE0", "#90D8D0", "prop_mirror,prop_candles,prop_chain,prop_bones", "prop_mirror", 6);
        Add("throne_vestibule", "floor_throne_a", "wall_throne", "#5A4A5A", "#3A2E3A", "#3A1E3A", "#180E18",
            "#FF8AC0", "#D0A0C0", "prop_banner,prop_column,prop_candles,prop_chain", "prop_throne", 6);
        Add("throne_boss", "floor_throne_b", "wall_throne", "#5A2A2A", "#3A1A1A", "#5A0E0E", "#200606",
            "#FF4A3A", "#E08070", "prop_throne,prop_banner,prop_candles,prop_chain", "prop_throne", 7);

        return d;
    }
}
