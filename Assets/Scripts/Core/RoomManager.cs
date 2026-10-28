using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle
{
    public Vector2 Pos;
    public float Radius;
}

public class RoomManager : MonoBehaviour
{
    public Rect Arena = new Rect(-8.55f, -4.45f, 17.1f, 8.9f);
    public Vector2 PlayerSpawn = new Vector2(-7.1f, -0.3f);
    public Vector2 RightDoorPosition = new Vector2(7.3f, -0.35f);
    public List<Enemy> Enemies = new List<Enemy>();
    public List<Obstacle> Obstacles = new List<Obstacle>();
    public RoomDef CurrentRoom;
    public DungeonDef CurrentDungeon;
    public RoomTheme CurrentTheme;
    public Transform Root;
    public int WaveIndex;
    public bool WaveSpawning;
    public bool Cleared;
    public bool BossDead;

    /// <summary>Меняется при сносе комнаты, чтобы старые корутины не трогали новый бой.</summary>
    public int CombatToken { get; private set; }

    private Coroutine waveRoutine;
    private Coroutine nextWaveRoutine;
    private Coroutine victoryRoutine;
    private Transform doorLeaf;
    private SpriteRenderer doorLight;
    private bool openingDoor;
    private Coroutine doorRoutine;

    public void Init()
    {
    }

    public void BuildMenu()
    {
        ClearRoom();
        CurrentRoom = null;
        CurrentDungeon = MetaProgress.SelectedDungeon;
        CurrentTheme = Data.Theme(CurrentDungeon.Rooms[0].Theme);
        BossDead = false;
        Cleared = true;
        BuildBase(-1, false, CurrentTheme);
        AddCampfire(new Vector2(2.1f, -1.35f));
        AddProp("prop_rock", new Vector2(-3.6f, -2.4f), 1.1f, 0.4f);
        AddProp("prop_bones", new Vector2(4.6f, 1.4f), 1.0f, 0f);
        AddProp("prop_column", new Vector2(-5.3f, 2.6f), 1.0f, 0.45f);
        AddThemeDecor(-1, false, false);
        AddTorches(CurrentTheme);
    }

    public void RebuildMenuForSelection()
    {
        if (Game.I == null || Game.I.State != GameState.Menu) return;
        BuildMenu();
        if (Game.I.Player != null) Game.I.Player.ShowInMenu();
    }

    public void BuildRoom(int index, bool firstRoom)
    {
        ClearRoom();
        CurrentDungeon = Game.I.Run.Dungeon ?? MetaProgress.SelectedDungeon;
        if (CurrentDungeon == null) CurrentDungeon = Data.Dungeons[0];
        CurrentRoom = Game.I.Run.RoomAt(index);
        if (CurrentRoom == null) CurrentRoom = Data.Dungeons[0].Rooms[0];
        CurrentTheme = Data.Theme(CurrentRoom.Theme);
        BossDead = false;
        Cleared = false;
        WaveIndex = 0;
        bool boss = CurrentRoom.Type == RoomType.Boss;
        BuildBase(index, boss, CurrentTheme);
        AddTorches(CurrentTheme);
        AddDoor();
        AddThemeDecor(index, boss, true);

        if (CurrentRoom.Type == RoomType.Treasure)
        {
            AddChest(new Vector2(2.0f, -0.5f));
        }
        else if (CurrentRoom.Type == RoomType.Rest)
        {
            AddCampfire(new Vector2(1.4f, -0.6f));
        }
    }

    private void BuildBase(int index, bool boss, RoomTheme theme)
    {
        var art = Game.I.Art;
        var dungeon = CurrentDungeon ?? MetaProgress.SelectedDungeon;
        if (theme == null) theme = Data.Theme("crypt_cellar");
        var rootGo = new GameObject("Room");
        Root = rootGo.transform;

        var table = new GameObject("Table");
        table.transform.SetParent(Root, false);
        var tableSr = table.AddComponent<SpriteRenderer>();
        Color floorTint = VisibleTint(theme.FloorTint, 0.24f);
        Color wallTint = VisibleTint(theme.WallTint, 0.32f);
        tableSr.sprite = art.World("table_bg");
        tableSr.color = Color.Lerp(new Color(0.72f, 0.68f, 0.78f, 1f), floorTint, 0.32f);
        tableSr.sortingOrder = -3000;
        tableSr.drawMode = SpriteDrawMode.Simple;
        float tw = tableSr.sprite.bounds.size.x;
        float th = tableSr.sprite.bounds.size.y;
        table.transform.localScale = new Vector3(31f / tw, 17.5f / th, 1f);

        string floorName = index % 2 == 0 ? dungeon.FloorA : dungeon.FloorB;
        if (theme != null && !string.IsNullOrEmpty(theme.Floor)) floorName = theme.Floor;
        var floorSr = AddTile(floorName, Vector3.zero, new Vector2(17.9f, 9.1f), -2000);
        floorSr.color = floorTint;
        AddQuad(new Vector3(0f, 0f, 0f), new Vector2(17.9f, 9.1f), A(theme.FogA, 0.065f), -1995, 0f);
        AddQuad(new Vector3(0f, 2.9f, 0f), new Vector2(17.9f, 3.4f), A(theme.Torch, 0.022f), -1994, 0f);

        // Мягкая тень по краям арены, но без полного затемнения текстур.
        AddQuad(new Vector3(0f, 3.9f, 0f), new Vector2(18.4f, 1.35f), new Color(0.01f, 0.02f, 0.05f, 0.18f), -1988, 0f);
        AddQuad(new Vector3(0f, -3.9f, 0f), new Vector2(18.4f, 1.35f), new Color(0.01f, 0.02f, 0.05f, 0.16f), -1988, 0f);

        // тёмная кромка листа картона
        AddQuad(new Vector3(0f, -4.62f, 0f), new Vector2(18.4f, 0.18f), new Color(0.08f, 0.05f, 0.06f, 0.34f), -1990, 0f);
        AddQuad(new Vector3(0f, 4.62f, 0f), new Vector2(18.4f, 0.18f), new Color(0.08f, 0.05f, 0.06f, 0.28f), -1990, 0f);

        string wallName = !string.IsNullOrEmpty(theme.Wall) ? theme.Wall : "wall_h";
        var wallTop = AddTile(wallName, new Vector3(0f, 5.05f, 0f), new Vector2(19.6f, 1.25f), -1500);
        var wallBottom = AddTile(wallName, new Vector3(0f, -5.05f, 0f), new Vector2(19.6f, 1.25f), -1490);
        var wallLeft = AddTile(wallName, new Vector3(-9.3f, 0f, 0f), new Vector2(1.25f, 10.4f), -1480, 90f);
        var wallRight = AddTile(wallName, new Vector3(9.3f, 0f, 0f), new Vector2(1.25f, 10.4f), -1470, -90f);
        wallTop.color = wallTint;
        wallBottom.color = wallTint;
        wallLeft.color = wallTint;
        wallRight.color = wallTint;

        // бумажный мусор
        var rng = new System.Random(index * 13 + 71 + (dungeon.Id.GetHashCode() & 0xFF));
        for (int i = 0; i < 14; i++)
        {
            var p = new Vector3((float)rng.NextDouble() * 16f - 8f, (float)rng.NextDouble() * 8f - 4f, 0f);
            if (Mathf.Abs(p.x) < 2f && Mathf.Abs(p.y) < 1.6f) continue;
            var q = AddQuad(p, new Vector2(0.35f + (float)rng.NextDouble() * 0.7f,
                0.2f + (float)rng.NextDouble() * 0.4f), A(theme.Motes, 0.11f), -1950,
                (float)rng.NextDouble() * 180f);
            q.transform.localScale = new Vector3(q.transform.localScale.x, q.transform.localScale.y, 1f);
        }

        AddFog(new Vector3(-3.8f, 1.2f, 0f), 7.5f, A(theme.FogA, 0.04f), 0.17f);
        AddFog(new Vector3(3.4f, -1.6f, 0f), 6.2f, A(theme.FogB, 0.03f), 0.11f);
        AddFog(new Vector3(0.4f, 3.2f, 0f), 5.2f, A(theme.FogA, 0.025f), 0.08f);
        for (int i = 0; i < 26; i++)
        {
            var mote = new GameObject("mote");
            mote.transform.SetParent(Root, false);
            mote.transform.position = new Vector3(Random.Range(-8.2f, 8.2f), Random.Range(-4.2f, 4.2f), 0f);
            var msr = mote.AddComponent<SpriteRenderer>();
            msr.sprite = art.World("fx_mote");
            msr.color = i % 3 == 0 ? A(theme.Torch, 0.34f) : A(theme.Motes, 0.2f);
            msr.sortingOrder = -450;
            float s = Random.Range(0.05f, 0.17f);
            mote.transform.localScale = new Vector3(s, s, 1f);
            var drift = mote.AddComponent<MoteDrift>();
            drift.Init(Random.Range(0.15f, 0.55f), Random.Range(0.1f, 0.5f), Random.Range(0f, 6.28f));
        }
    }

    private static Color A(Color c, float alpha)
    {
        return new Color(c.r, c.g, c.b, alpha);
    }

    /// <summary>Поднимает тёмные палитры комнат, чтобы текстуры пола и стен читались.</summary>
    public static Color VisibleTint(Color c, float lift)
    {
        Color lifted = Color.Lerp(c, Color.white, Mathf.Clamp01(lift));
        return new Color(Mathf.Min(1f, lifted.r * 1.05f), Mathf.Min(1f, lifted.g * 1.05f),
            Mathf.Min(1f, lifted.b * 1.05f), c.a);
    }

    private SpriteRenderer AddTile(string sprite, Vector3 pos, Vector2 size, int order, float rotation = 0f)
    {
        var go = new GameObject("tile_" + sprite);
        go.transform.SetParent(Root, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Game.I.Art.Tiled(sprite);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingOrder = order;
        return sr;
    }

    private SpriteRenderer AddQuad(Vector3 pos, Vector2 size, Color color, int order, float rotation)
    {
        var go = new GameObject("quad");
        go.transform.SetParent(Root, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Game.I.Art.World("white");
        sr.color = color;
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(size.x / 0.042f, size.y / 0.042f, 1f);
        return sr;
    }

    private void AddFog(Vector3 pos, float size, Color color, float speed)
    {
        var go = new GameObject("fog");
        go.transform.SetParent(Root, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Game.I.Art.World("fx_glow");
        sr.color = color;
        sr.sortingOrder = -700;
        go.transform.localScale = Vector3.one * size;
        go.AddComponent<FogDrift>().Init(sr, pos, size, color, speed);
    }

    private void AddTorches(RoomTheme theme)
    {
        Color flameColor = theme != null ? theme.Torch : new Color(1f, 0.72f, 0.38f);
        float[] xs = { -6.4f, -3.2f, 0f, 3.2f, 6.4f };
        foreach (float x in xs)
        {
            var go = new GameObject("torch");
            go.transform.SetParent(Root, false);
            go.transform.position = new Vector3(x, 4.42f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Game.I.Art.Prop("prop_torch");
            sr.color = Color.white;
            sr.sortingOrder = 620;
            go.transform.localScale = Vector3.one * 1.25f;

            var flameGo = new GameObject("flame");
            flameGo.transform.SetParent(go.transform, false);
            flameGo.transform.localPosition = new Vector3(0f, 0.83f, 0f);
            var fr = flameGo.AddComponent<SpriteRenderer>();
            fr.sprite = Game.I.Art.World("fx_flame");
            fr.color = A(flameColor, 0.95f);
            fr.sortingOrder = 625;
            flameGo.transform.localScale = Vector3.one * 0.65f;

            var glowGo = new GameObject("glow");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.85f, 0.1f);
            var gr = glowGo.AddComponent<SpriteRenderer>();
            gr.sprite = Game.I.Art.World("fx_glow");
            gr.color = A(flameColor, 0.22f);
            gr.sortingOrder = 610;
            glowGo.transform.localScale = Vector3.one * 1.6f;

            var flicker = flameGo.AddComponent<TorchFlicker>();
            flicker.Init(fr, gr, flameColor);
        }
    }

    private void AddThemeDecor(int index, bool boss, bool allowCenterpiece)
    {
        var theme = CurrentTheme ?? Data.Theme("crypt_cellar");
        var rng = new System.Random(index * 97 + 11 + (CurrentDungeon != null ? CurrentDungeon.Id.GetHashCode() & 0xFF : 0));
        bool skipCenter = !allowCenterpiece || CurrentRoom == null ||
                          CurrentRoom.Type == RoomType.Treasure || CurrentRoom.Type == RoomType.Rest;
        if (!skipCenter && !string.IsNullOrEmpty(theme.Centerpiece))
        {
            Vector2 centerPos = boss ? new Vector2(0f, 2.35f) : new Vector2(0f, 0.25f);
            float centerScale = boss ? 1.45f : 1.08f;
            float radius = CenterpieceRadius(theme.Centerpiece);
            if (boss) centerScale *= 1.12f;
            AddProp(theme.Centerpiece, centerPos, centerScale, radius);
            AddDecal(theme.Centerpiece, centerPos + Vector2.up * -0.15f, centerScale * 0.9f, A(theme.Torch, 0.12f), -1850);
        }

        if (boss)
        {
            AddProp("prop_column", new Vector2(-5.9f, 2.6f), 1.25f, 0.5f);
            AddProp("prop_column", new Vector2(5.9f, 2.6f), 1.25f, 0.5f);
            AddProp("prop_column", new Vector2(-5.9f, -2.7f), 1.25f, 0.5f);
            AddProp("prop_column", new Vector2(5.9f, -2.7f), 1.25f, 0.5f);
        }

        Vector2[] spots =
        {
            new Vector2(-6.7f, 3.0f), new Vector2(6.5f, 2.9f),
            new Vector2(-6.4f, -2.8f), new Vector2(6.6f, -2.7f),
            new Vector2(-2.4f, 3.3f), new Vector2(2.4f, -3.3f),
            new Vector2(-0.7f, 3.5f), new Vector2(0.9f, -3.5f)
        };
        var available = new List<int>();
        for (int i = 0; i < spots.Length; i++) available.Add(i);
        int count = Mathf.Clamp(theme.PropCount + (boss ? -1 : 0), 2, spots.Length);
        for (int i = 0; i < count && theme.Props.Length > 0 && available.Count > 0; i++)
        {
            int slot = rng.Next(0, available.Count);
            int si = available[slot];
            available.RemoveAt(slot);
            string name = theme.Props[i % theme.Props.Length];
            float scale = 0.85f + (float)rng.NextDouble() * 0.35f;
            float obstacle = PropRadius(name);
            AddProp(name, spots[si], scale, obstacle);
        }

        // Пара плоских пятен на полу: мох, кровь, кислотная лужа или паутина.
        string decal = null;
        if (theme.Props != null)
        {
            foreach (var p in theme.Props)
            {
                if (p == "prop_moss" || p == "prop_blood" || p == "prop_acid" || p == "prop_web")
                {
                    decal = p;
                    break;
                }
            }
        }
        if (!string.IsNullOrEmpty(decal))
        {
            AddDecal(decal, new Vector2(-3.4f + (float)rng.NextDouble() * 1.5f, -2.2f), 1.7f,
                new Color(1f, 1f, 1f, 0.72f), -1840);
            AddDecal(decal, new Vector2(2.8f + (float)rng.NextDouble() * 1.7f, 1.8f), 1.45f,
                new Color(1f, 1f, 1f, 0.6f), -1840);
        }
    }

    private static float CenterpieceRadius(string name)
    {
        switch (name)
        {
            case "prop_web":
            case "prop_moss":
            case "prop_blood":
            case "prop_acid":
                return 0f;
            case "prop_altar":
            case "prop_sarcophagus":
            case "prop_cauldron":
            case "prop_eggs":
                return 0.48f;
            case "prop_throne":
            case "prop_web_throne":
            case "prop_bone_arch":
            case "prop_gate":
                return 0.7f;
            default:
                return 0.4f;
        }
    }

    private static float PropRadius(string name)
    {
        switch (name)
        {
            case "prop_bones":
            case "prop_skullpile":
            case "prop_moss":
            case "prop_web":
            case "prop_blood":
            case "prop_acid":
            case "prop_roots":
                return 0f;
            case "prop_column":
            case "prop_bone_arch":
            case "prop_gate":
            case "prop_throne":
            case "prop_web_throne":
                return 0.45f;
            default:
                return 0.3f;
        }
    }

    private void AddDecal(string name, Vector2 pos, float scale, Color tint, int order)
    {
        var go = new GameObject("decal_" + name);
        go.transform.SetParent(Root, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Game.I.Art.Prop(name);
        sr.color = tint;
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void AddDoor()
    {
        var frameGo = new GameObject("door_frame");
        frameGo.transform.SetParent(Root, false);
        frameGo.transform.position = new Vector3(8.48f, -1.72f, 0f);
        frameGo.transform.localScale = Vector3.one * 1.85f;
        var fsr = frameGo.AddComponent<SpriteRenderer>();
        fsr.sprite = Game.I.Art.Prop("door_frame");
        fsr.sortingOrder = 810;

        var leafGo = new GameObject("door_leaf");
        leafGo.transform.SetParent(Root, false);
        leafGo.transform.position = new Vector3(7.92f, -1.66f, 0f);
        var lsr = leafGo.AddComponent<SpriteRenderer>();
        lsr.sprite = Game.I.Art.Pivot("door_leaf", 0.06f, 0.08f);
        lsr.sortingOrder = 820;
        leafGo.transform.localScale = Vector3.one * 1.8f;
        doorLeaf = leafGo.transform;

        var light = new GameObject("door_light");
        light.transform.SetParent(Root, false);
        light.transform.position = new Vector3(8.35f, -0.6f, 0f);
        doorLight = light.AddComponent<SpriteRenderer>();
        doorLight.sprite = Game.I.Art.World("fx_glow");
        doorLight.color = new Color(1f, 0.72f, 0.38f, 0.18f);
        doorLight.sortingOrder = 800;
        light.transform.localScale = Vector3.one * 2.3f;
    }

    private void AddProp(string name, Vector2 pos, float scale, float obstacleRadius)
    {
        var actor = PaperActor.Create(Root, Game.I.Art, name, scale, Color.white, true, true);
        actor.transform.position = pos;
        actor.Walking = false;
        if (obstacleRadius > 0.01f)
        {
            Obstacles.Add(new Obstacle { Pos = pos, Radius = obstacleRadius });
        }
    }

    private void AddChest(Vector2 pos)
    {
        var actor = PaperActor.Create(Root, Game.I.Art, "prop_chest", 1.2f, Color.white, true, true);
        actor.transform.position = pos;
        Obstacles.Add(new Obstacle { Pos = pos, Radius = 0.45f });
        var glow = new GameObject("chest_glow");
        glow.transform.SetParent(Root, false);
        glow.transform.position = pos + Vector2.up * 0.35f;
        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite = Game.I.Art.World("fx_glow");
        sr.color = new Color(1f, 0.8f, 0.35f, 0.35f);
        sr.sortingOrder = 100;
        glow.transform.localScale = Vector3.one * 1.4f;
        glow.AddComponent<GlowPulse>();
    }

    private void AddCampfire(Vector2 pos)
    {
        var actor = PaperActor.Create(Root, Game.I.Art, "prop_campfire", 1.15f, Color.white, true, true);
        actor.transform.position = pos;
        Obstacles.Add(new Obstacle { Pos = pos, Radius = 0.38f });
    }

    public void StartWaveIfNeeded()
    {
        if (CurrentRoom == null || Game.I == null || Game.I.State != GameState.Combat) return;
        if (BossDead) return;
        if (CurrentRoom.Type == RoomType.Boss)
        {
            if (WaveIndex == 0)
            {
                WaveIndex = 1;
                var bossKind = Game.I.Run.BossKind;
                SpawnOne(bossKind, false, new Vector2(3.2f, 0f));
            }
            return;
        }
        if (WaveIndex >= CurrentRoom.Waves) return;
        var comp = Composition(Game.I.Run.RoomIndex, WaveIndex);
        bool elite = CurrentRoom.Type == RoomType.Elite ||
                     (Game.I.Run.RoomIndex >= 7 && WaveIndex >= 1 &&
                      (Game.I.Run.RoomIndex + WaveIndex) % 2 == 0);
        WaveIndex++;
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = StartCoroutine(SpawnRoutine(comp, elite, CombatToken, Game.I.Run.RunId, Game.I.Run.RoomIndex));
    }

    public void SpawnWave()
    {
        if (WaveSpawning || BossDead) return;
        StartWaveIfNeeded();
    }

    private IEnumerator SpawnRoutine(List<EnemyKind> comp, bool eliteFirst, int token, int runId, int roomIndex)
    {
        WaveSpawning = true;
        for (int i = 0; i < comp.Count; i++)
        {
            if (!IsCombatValid(token, runId, roomIndex))
            {
                WaveSpawning = false;
                waveRoutine = null;
                yield break;
            }
            Vector2 pos = PickSpawn();
            SpawnOne(comp[i], eliteFirst && i == 0, pos);
            yield return new WaitForSeconds(0.11f);
        }
        WaveSpawning = false;
        waveRoutine = null;
        if (IsCombatValid(token, runId, roomIndex)) CheckClear();
    }

    private void SpawnOne(EnemyKind kind, bool elite, Vector2 pos)
    {
        if (BossDead || Game.I == null || Game.I.State != GameState.Combat) return;
        var e = Enemy.Spawn(kind, pos, Game.I.Run.EnemyHpScale, Game.I.Run.EnemyDamageScale, elite);
        Enemies.Add(e);
        if (e.Boss)
        {
            Game.I.UI.ShowBossBar(true, e.Def.Name);
        }
    }

    public void SpawnAdds(int count)
    {
        if (BossDead || Game.I == null || Game.I.State != GameState.Combat) return;
        var dungeon = CurrentDungeon ?? Game.I.Run.Dungeon;
        EnemyKind[] adds = dungeon != null && dungeon.Adds != null && dungeon.Adds.Length > 0
            ? dungeon.Adds
            : new[] { EnemyKind.Imp, EnemyKind.Skeleton };
        if (dungeon != null && Game.I.Run.BossKind == EnemyKind.SpiderQueen)
        {
            adds = new[] { EnemyKind.Spider };
        }
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = adds[i % adds.Length];
            SpawnOne(kind, false, PickSpawnNear(new Vector2(3.2f, 0f), 2.5f));
        }
    }

    private List<EnemyKind> Composition(int room, int wave)
    {
        var list = new List<EnemyKind>();
        var dungeon = CurrentDungeon ?? Game.I.Run.Dungeon;
        EnemyKind[] pool = dungeon != null && dungeon.Pool != null && dungeon.Pool.Length > 0
            ? dungeon.Pool
            : new[] { EnemyKind.Imp, EnemyKind.Skeleton, EnemyKind.Spider, EnemyKind.Boar };
        int difficulty = dungeon != null ? (int)dungeon.Difficulty : 0;
        int count = 2 + wave + difficulty;
        if (room > 7) count++;
        if (Game.I.Run.Endless) count += Mathf.Min(2, room / 15);
        if (CurrentRoom != null && CurrentRoom.Type == RoomType.Elite) count++;
        count = Mathf.Clamp(count, 2, 9);
        var rng = new System.Random((room + 1) * 977 + wave * 131 + (dungeon != null ? dungeon.Id.GetHashCode() : 0));
        for (int i = 0; i < count; i++)
        {
            int pick = rng.Next(0, pool.Length);
            list.Add(pool[pick]);
        }
        // Хотя бы один дальний или тяжёлый враг в поздних волнах.
        if (wave > 0 && pool.Length > 1)
        {
            list[list.Count - 1] = pool[(room + wave) % pool.Length];
        }
        return list;
    }

    private Vector2 PickSpawn()
    {
        for (int tries = 0; tries < 20; tries++)
        {
            float x = Random.Range(0.5f, 7.6f);
            float y = Random.Range(-3.6f, 3.6f);
            Vector2 p = new Vector2(x, y);
            if (IsSpawnPointSafe(p, 2.6f)) return p;
        }

        // Резервные точки у краёв арены. Если и они заняты, выбираем самую
        // далёкую от игрока безопасную точку, а не случайную внутри декора.
        Vector2[] fallback =
        {
            new Vector2(6.8f, 3.2f), new Vector2(6.8f, -3.2f),
            new Vector2(3.4f, 3.2f), new Vector2(3.4f, -3.2f),
            new Vector2(-2.6f, 3.2f), new Vector2(-2.6f, -3.2f)
        };
        Vector2 best = Clamp(fallback[0], 0.6f);
        float bestScore = float.MinValue;
        foreach (var raw in fallback)
        {
            Vector2 p = Clamp(raw, 0.6f);
            if (IsSpawnPointSafe(p, 0f)) return p;
            float score = PlayerDistance(p);
            if (score > bestScore)
            {
                bestScore = score;
                best = p;
            }
        }
        return best;
    }

    private Vector2 PickSpawnNear(Vector2 center, float range)
    {
        for (int tries = 0; tries < 16; tries++)
        {
            Vector2 p = center + Random.insideUnitCircle * range;
            p = Clamp(p, 0.6f);
            if (IsSpawnPointSafe(p, 2.2f)) return p;
        }
        return PickSpawn();
    }

    private bool IsSpawnPointSafe(Vector2 p, float minPlayerDistance)
    {
        p = Clamp(p, 0.6f);
        if (PlayerDistance(p) < minPlayerDistance) return false;
        for (int i = 0; i < Obstacles.Count; i++)
        {
            var o = Obstacles[i];
            if (Vector2.Distance(p, o.Pos) < o.Radius + 0.6f) return false;
        }
        return true;
    }

    private static float PlayerDistance(Vector2 p)
    {
        if (Game.I == null || Game.I.Player == null) return float.MaxValue;
        return Vector2.Distance(p, Game.I.Player.transform.position);
    }

    private bool IsCombatValid(int token, int runId, int roomIndex)
    {
        return Game.I != null && Game.I.State == GameState.Combat &&
               token == CombatToken && !BossDead &&
               Game.I.Run != null && Game.I.Run.RunId == runId && Game.I.Run.RoomIndex == roomIndex;
    }

    public void NotifyKilled(Enemy enemy)
    {
        Enemies.Remove(enemy);
        if (enemy != null && enemy.Boss)
        {
            BossDefeated();
            return;
        }
        CheckClear();
    }

    private void CheckClear()
    {
        if (Cleared || WaveSpawning || BossDead) return;
        if (Game.I == null || Game.I.State != GameState.Combat) return;
        if (Enemies.Count > 0) return;
        if (CurrentRoom == null) return;
        if (WaveIndex < CurrentRoom.Waves)
        {
            if (nextWaveRoutine == null)
            {
                nextWaveRoutine = StartCoroutine(NextWaveRoutine(CombatToken, Game.I.Run.RunId,
                    Game.I.Run.RoomIndex));
            }
            return;
        }
        Cleared = true;
        Game.I.OnRoomCleared();
    }

    private IEnumerator NextWaveRoutine(int token, int runId, int roomIndex)
    {
        Game.I.UI.ShowBanner("ВОЛНА " + (WaveIndex + 1), "Они вылезают из-под пола", 1.1f, Pal.Red);
        Game.I.Audio.Play("sfx_spawn", 0.6f);
        yield return new WaitForSeconds(1.0f);
        nextWaveRoutine = null;
        if (!IsCombatValid(token, runId, roomIndex)) yield break;
        SpawnWave();
    }

    public void BossDefeated()
    {
        if (BossDead) return;
        BossDead = true;
        Cleared = true;

        // Волны и старые отложенные победы больше не должны выполняться.
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
        if (nextWaveRoutine != null)
        {
            StopCoroutine(nextWaveRoutine);
            nextWaveRoutine = null;
        }
        WaveSpawning = false;
        ClearCombatDangers();

        // Аддов убираем без урона: иначе 9999 урона раздувает статистику и
        // полностью лечит игрока через вампиризм.
        var copy = new List<Enemy>(Enemies);
        foreach (var e in copy)
        {
            if (e != null && !e.Dead) e.DespawnSilently();
        }
        Enemies.Clear();

        Game.I.UI.ShowBossBar(false);

        // Если игрок уже погиб, победа не должна перебивать поражение.
        if (Game.I.Player == null || Game.I.Player.Dead || Game.I.State != GameState.Combat) return;

        Game.I.Player.LockVictory();
        victoryRoutine = StartCoroutine(BossVictoryRoutine(Game.I.Run.RunId, CombatToken,
            Game.I.Run.RoomIndex));
    }

    private IEnumerator BossVictoryRoutine(int runId, int token, int roomIndex)
    {
        yield return new WaitForSeconds(1.4f);

        bool valid = Game.I != null && Game.I.State == GameState.Combat &&
                     BossDead && Cleared && !Game.I.Player.Dead &&
                     Game.I.Run != null && Game.I.Run.RunId == runId &&
                     Game.I.Run.RoomIndex == roomIndex &&
                     CombatToken == token && CurrentRoom != null &&
                     CurrentRoom.Type == RoomType.Boss;
        victoryRoutine = null;
        if (!valid) yield break;
        if (Game.I.Run.Endless)
        {
            Game.I.Player.VictoryLocked = false;
            Game.I.OnRoomCleared();
            yield break;
        }
        Game.I.Victory();
    }

    public void CancelPendingVictory()
    {
        if (victoryRoutine != null)
        {
            StopCoroutine(victoryRoutine);
            victoryRoutine = null;
        }
    }

    public void CloseCombat()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
        if (nextWaveRoutine != null)
        {
            StopCoroutine(nextWaveRoutine);
            nextWaveRoutine = null;
        }
        CancelPendingVictory();
        WaveSpawning = false;
        ClearCombatDangers();
    }

    private void ClearCombatDangers()
    {
        Projectile.ClearAll();
        foreach (var h in FindObjectsOfType<Hazard>())
        {
            if (h != null) h.Defuse();
        }
        foreach (var t in FindObjectsOfType<TelegraphFx>())
        {
            if (t != null) Destroy(t.gameObject);
        }
    }

    public void OpenDoor()
    {
        if (doorLeaf == null || openingDoor) return;
        openingDoor = true;
        doorRoutine = StartCoroutine(DoorRoutine());
    }

    private IEnumerator DoorRoutine()
    {
        if (doorLight != null)
        {
            var lc = doorLight.color;
            doorLight.color = new Color(1f, 0.8f, 0.45f, 0.6f);
            doorLight.transform.localScale = Vector3.one * 3f;
        }
        var leaf = doorLeaf;
        if (leaf == null)
        {
            openingDoor = false;
            doorRoutine = null;
            yield break;
        }
        float t = 0f;
        Quaternion start = leaf.localRotation;
        Quaternion end = Quaternion.Euler(0f, 0f, -102f);
        while (t < 0.65f && leaf != null)
        {
            t += Time.deltaTime;
            if (leaf == null) break;
            leaf.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0f, 1f, t / 0.65f));
            yield return null;
        }
        if (leaf != null && Game.I != null && Game.I.Fx != null) Game.I.Fx.Dust(leaf.position, 5, 1.1f);
        doorRoutine = null;
    }

    public Vector2 Move(Vector2 pos, Vector2 delta, float radius)
    {
        Vector2 p = Clamp(pos + delta, radius);
        // Несколько проходов не дают вытолкнуть персонажа из одного препятствия прямо в другое.
        for (int pass = 0; pass < 3; pass++)
        {
            bool pushed = false;
            for (int i = 0; i < Obstacles.Count; i++)
            {
                var o = Obstacles[i];
                Vector2 d = p - o.Pos;
                float min = o.Radius + radius;
                if (d.sqrMagnitude < min * min)
                {
                    float dist = d.magnitude;
                    if (dist < 0.0001f) d = Vector2.up;
                    p = o.Pos + d.normalized * min;
                    pushed = true;
                }
            }
            p = Clamp(p, radius);
            if (!pushed) break;
        }
        return p;
    }

    /// <summary>
    /// Локальный обход препятствий: если прямой путь перекрыт, пробуем отвернуть.
    /// Это не полноценный A*, но мобы перестают бесконечно тереться о декор.
    /// </summary>
    public Vector2 Steer(Vector2 pos, Vector2 desired, float radius, float lookAhead = 1.7f, float side = 1f)
    {
        if (desired.sqrMagnitude < 0.001f) return desired;
        Vector2 d = desired.normalized;
        if (!PathBlocked(pos, d, radius, lookAhead)) return d;

        float[] degrees = { 28f, 52f, 78f, 105f, 132f, 158f };
        for (int i = 0; i < degrees.Length; i++)
        {
            for (int s = 0; s < 2; s++)
            {
                float sign = (s == 0 ? 1f : -1f) * (side >= 0f ? 1f : -1f);
                Vector2 candidate = Rotate(d, degrees[i] * sign);
                if (!PathBlocked(pos, candidate, radius, lookAhead)) return candidate;
            }
        }
        return d;
    }

    private bool PathBlocked(Vector2 pos, Vector2 dir, float radius, float lookAhead)
    {
        Vector2 end = pos + dir * lookAhead;
        if (end.x < Arena.xMin + radius + 0.08f || end.x > Arena.xMax - radius - 0.08f ||
            end.y < Arena.yMin + radius + 0.08f || end.y > Arena.yMax - radius - 0.08f)
        {
            return true;
        }

        for (int i = 0; i < Obstacles.Count; i++)
        {
            var o = Obstacles[i];
            float forward = Vector2.Dot(o.Pos - pos, dir);
            if (forward < -radius || forward > lookAhead + o.Radius) continue;
            float t = Mathf.Clamp(forward, 0f, lookAhead);
            Vector2 nearest = pos + dir * t;
            float clearance = o.Radius + radius + 0.12f;
            if ((nearest - o.Pos).sqrMagnitude < clearance * clearance) return true;
        }
        return false;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    /// <summary>Проверяет, пересекает ли отрезок какое-либо препятствие.</summary>
    public bool SegmentBlocked(Vector2 from, Vector2 to, float radius)
    {
        for (int i = 0; i < Obstacles.Count; i++)
        {
            var o = Obstacles[i];
            if (SegmentCircleDistance(from, to, o.Pos) < o.Radius + radius) return true;
        }
        return false;
    }

    /// <summary>Прямая видимость цели без учёта других врагов.</summary>
    public bool HasLineOfSight(Vector2 from, Vector2 to, float clearance = 0.05f)
    {
        return !SegmentBlocked(from, to, clearance);
    }

    private static float SegmentCircleDistance(Vector2 a, Vector2 b, Vector2 center)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.000001f) return Vector2.Distance(a, center);
        float t = Mathf.Clamp01(Vector2.Dot(center - a, ab) / lenSq);
        Vector2 nearest = a + ab * t;
        return Vector2.Distance(nearest, center);
    }

    public Vector2 Clamp(Vector2 p, float radius)
    {
        p.x = Mathf.Clamp(p.x, Arena.xMin + radius, Arena.xMax - radius);
        p.y = Mathf.Clamp(p.y, Arena.yMin + radius, Arena.yMax - radius);
        return p;
    }

    public Vector2 RandomArenaPoint(float margin)
    {
        return new Vector2(Random.Range(Arena.xMin + margin, Arena.xMax - margin),
            Random.Range(Arena.yMin + margin, Arena.yMax - margin));
    }

    public Enemy NearestEnemy(Vector3 pos, float maxRange)
    {
        Enemy best = null;
        float bestD = maxRange;
        for (int i = 0; i < Enemies.Count; i++)
        {
            var e = Enemies[i];
            if (e == null || e.Dead) continue;
            float d = Vector2.Distance(pos, e.transform.position);
            if (d < bestD)
            {
                bestD = d;
                best = e;
            }
        }
        return best;
    }

    public List<Enemy> NearestEnemies(Vector3 pos, float maxRange, int count)
    {
        var list = new List<Enemy>();
        foreach (var e in Enemies)
        {
            if (e == null || e.Dead) continue;
            if (Vector2.Distance(pos, e.transform.position) <= maxRange) list.Add(e);
        }
        list.Sort((a, b) => Vector2.Distance(pos, a.transform.position)
            .CompareTo(Vector2.Distance(pos, b.transform.position)));
        if (list.Count > count) list.RemoveRange(count, list.Count - count);
        return list;
    }

    public List<Enemy> EnemiesInRadius(Vector3 pos, float radius)
    {
        var list = new List<Enemy>();
        foreach (var e in Enemies)
        {
            if (e == null || e.Dead) continue;
            if (Vector2.Distance(pos, e.transform.position) <= radius) list.Add(e);
        }
        return list;
    }

    public void DamageEnemies(Vector3 pos, float radius, float damage, bool crit = false)
    {
        var copy = new List<Enemy>(Enemies);
        foreach (var e in copy)
        {
            if (e == null || e.Dead) continue;
            if (Vector2.Distance(pos, e.transform.position) <= radius + 0.35f)
            {
                e.TakeDamage(damage, pos, true, crit);
            }
        }
    }

    public void KillAllEnemies()
    {
        var copy = new List<Enemy>(Enemies);
        foreach (var e in copy)
        {
            if (e != null && !e.Dead) e.TakeDamage(Mathf.Max(1f, e.Hp * 2f), e.transform.position, true, false);
        }
    }

    private void ClearRoom()
    {
        CombatToken++;
        BossDead = false;
        Cleared = false;
        WaveIndex = 0;
        CloseCombat();
        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
            doorRoutine = null;
        }
        Projectile.ClearAll();
        foreach (var a in FindObjectsOfType<Ally>()) Destroy(a.gameObject);
        foreach (var h in FindObjectsOfType<Hazard>()) Destroy(h.gameObject);
        foreach (var f in FindObjectsOfType<FxSprite>()) Destroy(f.gameObject);
        foreach (var f in FindObjectsOfType<TelegraphFx>()) Destroy(f.gameObject);
        foreach (var c in FindObjectsOfType<CoinPickup>()) Destroy(c.gameObject);
        foreach (var e in Enemies)
        {
            if (e != null) Destroy(e.gameObject);
        }
        Enemies.Clear();
        Obstacles.Clear();
        if (Root != null) Destroy(Root.gameObject);
        Root = null;
        doorLeaf = null;
        doorLight = null;
        openingDoor = false;
    }
}

public class TorchFlicker : MonoBehaviour
{
    private SpriteRenderer flame;
    private SpriteRenderer glow;
    private float seed;
    private Color baseColor = new Color(1f, 0.72f, 0.38f);

    public void Init(SpriteRenderer f, SpriteRenderer g)
    {
        Init(f, g, new Color(1f, 0.72f, 0.38f));
    }

    public void Init(SpriteRenderer f, SpriteRenderer g, Color color)
    {
        flame = f;
        glow = g;
        baseColor = color;
        seed = Random.value * 10f;
    }

    private void Update()
    {
        float n = Mathf.PerlinNoise(seed + Time.time * 6f, 0.5f);
        float n2 = Mathf.PerlinNoise(0.3f, seed + Time.time * 9f);
        if (flame != null)
        {
            flame.transform.localScale = Vector3.one * (0.58f + n * 0.18f);
            flame.color = new Color(baseColor.r, baseColor.g * (0.82f + n2 * 0.18f), baseColor.b, 0.9f);
            flame.transform.localRotation = Quaternion.Euler(0f, 0f, (n - 0.5f) * 5f);
        }
        if (glow != null)
        {
            glow.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.16f + n * 0.1f);
        }
    }
}

public class GlowPulse : MonoBehaviour
{
    private SpriteRenderer sr;
    private float seed;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 4f;
    }

    private void Update()
    {
        if (sr == null) return;
        float a = 0.22f + Mathf.Sin(Time.time * 1.7f + seed) * 0.08f;
        sr.color = new Color(1f, 0.8f, 0.35f, a);
        transform.localScale = Vector3.one * (1.3f + Mathf.Sin(Time.time * 1.3f + seed) * 0.08f);
    }
}

public class FogDrift : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector3 origin;
    private float size;
    private Color color;
    private float speed;
    private float seed;

    public void Init(SpriteRenderer renderer, Vector3 start, float scale, Color tint, float driftSpeed)
    {
        sr = renderer;
        origin = start;
        size = scale;
        color = tint;
        speed = driftSpeed;
        seed = Random.value * 6.28f;
    }

    private void Update()
    {
        if (sr == null) return;
        float t = Time.time * speed + seed;
        transform.position = origin + new Vector3(Mathf.Sin(t * 1.7f) * 3.2f + Mathf.Cos(t * 0.7f) * 1.1f,
            Mathf.Cos(t * 1.3f) * 0.55f, 0f);
        float pulse = 0.7f + Mathf.Sin(t * 1.9f) * 0.3f;
        sr.color = new Color(color.r, color.g, color.b, color.a * pulse);
        float s = size * (1f + Mathf.Sin(t * 1.1f) * 0.06f);
        transform.localScale = new Vector3(s, s * 0.72f, 1f);
    }
}

public class MoteDrift : MonoBehaviour
{
    private Vector3 origin;
    private float speed;
    private float rise;
    private float seed;
    private SpriteRenderer sr;
    private Color color;

    public void Init(float driftSpeed, float riseSpeed, float phase)
    {
        origin = transform.position;
        speed = driftSpeed;
        rise = riseSpeed;
        seed = phase;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) color = sr.color;
    }

    private void Update()
    {
        float t = Time.time * speed + seed;
        transform.position = origin + new Vector3(Mathf.Sin(t * 1.3f) * 1.1f, Mathf.Sin(t * 0.8f) * 0.55f, 0f);
        if (sr != null)
        {
            float a = color.a * (0.55f + Mathf.Sin(t * 2.7f) * 0.45f);
            sr.color = new Color(color.r, color.g, color.b, a);
        }
        transform.position += Vector3.up * (Mathf.Sin(t * 0.45f) * rise * Time.deltaTime);
    }
}
