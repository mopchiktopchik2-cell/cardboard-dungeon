using System.Collections.Generic;
using UnityEngine;

public class LootRoll
{
    public bool Rolled;
    public bool ChanceFailed;
    public bool AllOwned;
    public EquipmentDef Item;

    public string Message
    {
        get
        {
            if (Item != null) return "СНАРЯЖЕНИЕ ВЫПАЛО";
            if (AllOwned) return "ВСЁ СНАРЯЖЕНИЕ ЭТОГО УРОВНЯ УЖЕ СОБРАНО";
            return "СНАРЯЖЕНИЕ НЕ ВЫПАЛО";
        }
    }
}

public static class MetaProgress
{
    private const string ClassKey = "v12_class";
    private const string DungeonKey = "v12_dungeon";
    private const string OwnedKey = "v12_owned";
    private const string WeaponKey = "v12_weapon";
    private const string ArmorKey = "v12_armor";
    private const string CharmKey = "v12_charm";
    private const string SaveVersionKey = "v12_save_version";
    private const int SaveVersion = 1;

    private static bool loaded;
    private static HashSet<string> owned;
    private static Dictionary<EquipSlot, string> equipped;

    public static HeroClass SelectedClass
    {
        get
        {
            Load();
            int raw = PlayerPrefs.GetInt(ClassKey, (int)HeroClass.Mage);
            if (raw < 0 || raw > 2) raw = 0;
            return (HeroClass)raw;
        }
        set
        {
            Load();
            PlayerPrefs.SetInt(ClassKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static string SelectedDungeonId
    {
        get
        {
            Load();
            string id = PlayerPrefs.GetString(DungeonKey, "crypt");
            if (Data.FindDungeon(id) == null) id = "crypt";
            return id;
        }
        set
        {
            Load();
            string id = Data.FindDungeon(value) != null ? value : "crypt";
            PlayerPrefs.SetString(DungeonKey, id);
            PlayerPrefs.Save();
        }
    }

    public static DungeonDef SelectedDungeon => Data.Dungeon(SelectedDungeonId);
    public static HeroClassDef SelectedHero => Data.Hero(SelectedClass);

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        if (!PlayerPrefs.HasKey(SaveVersionKey)) PlayerPrefs.SetInt(SaveVersionKey, SaveVersion);
        owned = new HashSet<string>();
        equipped = new Dictionary<EquipSlot, string>();
        string raw = PlayerPrefs.GetString(OwnedKey, "");
        if (!string.IsNullOrEmpty(raw))
        {
            foreach (var id in raw.Split(','))
            {
                if (!string.IsNullOrEmpty(id) && Data.Item(id) != null) owned.Add(id);
            }
        }
        ReadEquipped(EquipSlot.Weapon, WeaponKey);
        ReadEquipped(EquipSlot.Armor, ArmorKey);
        ReadEquipped(EquipSlot.Charm, CharmKey);
    }

    private static void ReadEquipped(EquipSlot slot, string key)
    {
        string id = PlayerPrefs.GetString(key, "");
        if (!string.IsNullOrEmpty(id) && owned.Contains(id)) equipped[slot] = id;
    }

    public static bool IsOwned(string id)
    {
        Load();
        return !string.IsNullOrEmpty(id) && owned.Contains(id);
    }

    public static int OwnedCount
    {
        get
        {
            Load();
            return owned.Count;
        }
    }

    public static List<EquipmentDef> OwnedItems()
    {
        Load();
        var list = new List<EquipmentDef>();
        foreach (var item in Data.Equipment)
        {
            if (owned.Contains(item.Id)) list.Add(item);
        }
        list.Sort((a, b) =>
        {
            int tier = a.Tier.CompareTo(b.Tier);
            if (tier != 0) return tier;
            int slot = a.Slot.CompareTo(b.Slot);
            if (slot != 0) return slot;
            return a.Rarity.CompareTo(b.Rarity);
        });
        return list;
    }

    public static EquipmentDef Equipped(EquipSlot slot)
    {
        Load();
        string id;
        return equipped.TryGetValue(slot, out id) ? Data.Item(id) : null;
    }

    public static bool Equip(EquipmentDef item)
    {
        Load();
        if (item == null || !owned.Contains(item.Id)) return false;
        equipped[item.Slot] = item.Id;
        SaveEquipped();
        return true;
    }

    public static void Unequip(EquipSlot slot)
    {
        Load();
        equipped.Remove(slot);
        SaveEquipped();
    }

    private static void SaveEquipped()
    {
        PlayerPrefs.SetString(WeaponKey, equipped.TryGetValue(EquipSlot.Weapon, out var w) ? w : "");
        PlayerPrefs.SetString(ArmorKey, equipped.TryGetValue(EquipSlot.Armor, out var a) ? a : "");
        PlayerPrefs.SetString(CharmKey, equipped.TryGetValue(EquipSlot.Charm, out var c) ? c : "");
        PlayerPrefs.Save();
    }

    public static void AddItem(string id)
    {
        Load();
        if (string.IsNullOrEmpty(id) || Data.Item(id) == null) return;
        if (!owned.Add(id)) return;
        PlayerPrefs.SetString(OwnedKey, string.Join(",", new List<string>(owned).ToArray()));
        PlayerPrefs.Save();
    }

    public static void ApplyLoadout(PlayerStats stats)
    {
        if (stats == null) return;
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
        {
            var item = Equipped(slot);
            if (item == null) continue;
            stats.maxHp += item.Hp;
            stats.damage += item.Damage;
            stats.speed *= 1f + item.Speed;
            stats.critChance = Mathf.Min(0.75f, stats.critChance + item.Crit);
            stats.damageReduction = Mathf.Min(0.45f, stats.damageReduction + item.Armor);
            stats.lifesteal += item.Lifesteal;
            stats.skillPower += item.SkillPower;
        }
    }

    public static LootRoll RollDungeonDrop(DungeonDef dungeon)
    {
        Load();
        var result = new LootRoll();
        if (dungeon == null) return result;
        result.Rolled = Random.value < dungeon.DropChance;
        if (!result.Rolled)
        {
            result.ChanceFailed = true;
            return result;
        }

        var candidates = new List<EquipmentDef>();
        var fallback = new List<EquipmentDef>();
        foreach (var item in Data.Equipment)
        {
            if (item.Tier != dungeon.LootTier) continue;
            fallback.Add(item);
            if (!owned.Contains(item.Id)) candidates.Add(item);
        }
        if (candidates.Count == 0)
        {
            result.AllOwned = fallback.Count > 0;
            return result;
        }

        float total = 0f;
        foreach (var item in candidates) total += Mathf.Max(0.01f, item.Weight);
        float roll = Random.value * total;
        EquipmentDef picked = candidates[candidates.Count - 1];
        foreach (var item in candidates)
        {
            roll -= Mathf.Max(0.01f, item.Weight);
            if (roll <= 0f)
            {
                picked = item;
                break;
            }
        }
        AddItem(picked.Id);
        result.Item = picked;
        return result;
    }

    public static int BestRoom(string dungeonId)
    {
        return PlayerPrefs.GetInt("best_room_" + dungeonId, PlayerPrefs.GetInt("best_room", 0));
    }

    public static int BestKills(string dungeonId)
    {
        return PlayerPrefs.GetInt("best_kills_" + dungeonId, PlayerPrefs.GetInt("best_kills", 0));
    }

    public static int Clears(string dungeonId)
    {
        return PlayerPrefs.GetInt("clears_" + dungeonId, 0);
    }

    public static bool IsDungeonUnlocked(int index)
    {
        if (index <= 0) return true;
        if (index >= Data.Dungeons.Count) return false;
        var dungeon = Data.Dungeons[index];
        if (Clears(dungeon.Id) > 0) return true;
        var previous = Data.Dungeons[index - 1];
        return Clears(previous.Id) > 0;
    }

    public static bool IsDungeonUnlocked(string dungeonId)
    {
        int index = Data.Dungeons.FindIndex(d => d.Id == dungeonId);
        return index >= 0 && IsDungeonUnlocked(index);
    }

    public static DungeonDef NextDungeon(DungeonDef current)
    {
        if (current == null) return null;
        int index = Data.Dungeons.FindIndex(d => d.Id == current.Id);
        if (index < 0 || index + 1 >= Data.Dungeons.Count) return null;
        return Data.Dungeons[index + 1];
    }

    public static bool HasNextDungeon(DungeonDef current)
    {
        return NextDungeon(current) != null;
    }

    public static void RecordRun(string dungeonId, int reached, int kills, bool victory)
    {
        if (Data.FindDungeon(dungeonId) == null) return;
        int bestRoom = PlayerPrefs.GetInt("best_room_" + dungeonId, 0);
        int bestKills = PlayerPrefs.GetInt("best_kills_" + dungeonId, 0);
        if (reached > bestRoom) PlayerPrefs.SetInt("best_room_" + dungeonId, reached);
        if (kills > bestKills) PlayerPrefs.SetInt("best_kills_" + dungeonId, kills);
        if (victory) PlayerPrefs.SetInt("clears_" + dungeonId, Clears(dungeonId) + 1);
        if (reached > PlayerPrefs.GetInt("best_room", 0)) PlayerPrefs.SetInt("best_room", reached);
        if (kills > PlayerPrefs.GetInt("best_kills", 0)) PlayerPrefs.SetInt("best_kills", kills);
        PlayerPrefs.Save();
    }

    /// <summary>Сбрасывает только прогресс: класс, подземелье, лут и рекорды.</summary>
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(ClassKey);
        PlayerPrefs.DeleteKey(DungeonKey);
        PlayerPrefs.DeleteKey(OwnedKey);
        PlayerPrefs.DeleteKey(WeaponKey);
        PlayerPrefs.DeleteKey(ArmorKey);
        PlayerPrefs.DeleteKey(CharmKey);
        PlayerPrefs.DeleteKey("best_room");
        PlayerPrefs.DeleteKey("best_kills");
        foreach (var dungeon in Data.Dungeons)
        {
            PlayerPrefs.DeleteKey("best_room_" + dungeon.Id);
            PlayerPrefs.DeleteKey("best_kills_" + dungeon.Id);
            PlayerPrefs.DeleteKey("clears_" + dungeon.Id);
        }
        PlayerPrefs.SetInt(SaveVersionKey, SaveVersion);
        PlayerPrefs.Save();
        loaded = false;
        owned = null;
        equipped = null;
        Load();
    }
}
