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
    public Transform Root;
    public int WaveIndex;
    public bool WaveSpawning;
    public bool Cleared;
    public bool BossDead;

    private Coroutine waveRoutine;
    private Transform doorLeaf;
    private SpriteRenderer doorLight;
    private bool openingDoor;

    public void Init()
    {
    }

    public void BuildMenu()
    {
        ClearRoom();
        CurrentRoom = null;
        BossDead = false;
        Cleared = true;
        BuildBase(-1, false);
        AddCampfire(new Vector2(2.1f, -1.35f));
        AddProp("prop_rock", new Vector2(-3.6f, -2.4f), 1.1f, 0.4f);
        AddProp("prop_bones", new Vector2(4.6f, 1.4f), 1.0f, 0f);
        AddProp("prop_column", new Vector2(-5.3f, 2.6f), 1.0f, 0.45f);
        AddTorches();
    }

    public void BuildRoom(int index, bool firstRoom)
    {
        ClearRoom();
        CurrentRoom = Data.Rooms[index];
        BossDead = false;
        Cleared = false;
        WaveIndex = 0;
        bool boss = CurrentRoom.Type == RoomType.Boss;
        BuildBase(index, boss);
        AddTorches();
        AddDoor();
        var rng = new System.Random(index * 97 + 11);
        if (boss)
        {
            AddProp("prop_column", new Vector2(-5.9f, 2.6f), 1.25f, 0.5f);
            AddProp("prop_column", new Vector2(5.9f, 2.6f), 1.25f, 0.5f);
            AddProp("prop_column", new Vector2(-5.9f, -2.7f), 1.25f, 0.5f);
            AddProp("prop_column", new Vector2(5.9f, -2.7f), 1.25f, 0.5f);
            AddProp("prop_bones", new Vector2(-2.5f, -2.9f), 1.1f, 0f);
            AddProp("prop_bones", new Vector2(2.8f, 2.7f), 0.9f, 0f);
        }
        else
        {
            Vector2[] spots =
            {
                new Vector2(-5.7f, 2.6f), new Vector2(5.4f, 2.5f),
                new Vector2(-4.8f, -2.8f), new Vector2(5.2f, -2.7f),
                new Vector2(-1.8f, 3.0f), new Vector2(1.4f, -3.1f)
            };
            int count = 2 + rng.Next(0, 3);
            var used = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int si = rng.Next(0, spots.Length);
                if (used.Contains(si)) continue;
                used.Add(si);
                string[] props = { "prop_column", "prop_rock", "prop_barrel", "prop_bones" };
                string name = props[rng.Next(0, props.Length)];
                float scale = 0.85f + (float)rng.NextDouble() * 0.3f;
                float obstacle = name == "prop_bones" ? 0f : (name == "prop_column" ? 0.45f : 0.35f);
                AddProp(name, spots[si], scale, obstacle);
            }
        }

        if (CurrentRoom.Type == RoomType.Treasure)
        {
            AddChest(new Vector2(2.0f, -0.5f));
        }
        else if (CurrentRoom.Type == RoomType.Rest)
        {
            AddCampfire(new Vector2(1.4f, -0.6f));
        }
    }

    private void BuildBase(int index, bool boss)
    {
        var art = Game.I.Art;
        var rootGo = new GameObject("Room");
        Root = rootGo.transform;

        var table = new GameObject("Table");
        table.transform.SetParent(Root, false);
        var tableSr = table.AddComponent<SpriteRenderer>();
        tableSr.sprite = art.World("table_bg");
        tableSr.sortingOrder = -3000;
        tableSr.drawMode = SpriteDrawMode.Simple;
        float tw = tableSr.sprite.bounds.size.x;
        float th = tableSr.sprite.bounds.size.y;
        table.transform.localScale = new Vector3(31f / tw, 17.5f / th, 1f);

        string floorName = boss ? "floor_boss" : "floor";
        AddTile(floorName, Vector3.zero, new Vector2(17.9f, 9.1f), -2000);

        // тёмная кромка листа картона
        AddQuad(new Vector3(0f, -4.62f, 0f), new Vector2(18.4f, 0.18f), new Color(0.08f, 0.05f, 0.06f, 0.55f), -1990, 0f);
        AddQuad(new Vector3(0f, 4.62f, 0f), new Vector2(18.4f, 0.18f), new Color(0.08f, 0.05f, 0.06f, 0.45f), -1990, 0f);

        AddTile("wall_h", new Vector3(0f, 5.05f, 0f), new Vector2(19.6f, 1.25f), -1500);
        AddTile("wall_h", new Vector3(0f, -5.05f, 0f), new Vector2(19.6f, 1.25f), -1490);
        AddTile("wall_v", new Vector3(-9.3f, 0f, 0f), new Vector2(1.25f, 10.4f), -1480);
        AddTile("wall_v", new Vector3(9.3f, 0f, 0f), new Vector2(1.25f, 10.4f), -1470);

        // бумажный мусор
        var rng = new System.Random(index * 13 + 71);
        for (int i = 0; i < 12; i++)
        {
            var p = new Vector3((float)rng.NextDouble() * 16f - 8f, (float)rng.NextDouble() * 8f - 4f, 0f);
            if (Mathf.Abs(p.x) < 2f && Mathf.Abs(p.y) < 1.6f) continue;
            var q = AddQuad(p, new Vector2(0.35f + (float)rng.NextDouble() * 0.7f,
                0.2f + (float)rng.NextDouble() * 0.4f), new Color(0.9f, 0.82f, 0.65f, 0.12f), -1950,
                (float)rng.NextDouble() * 180f);
            q.transform.localScale = new Vector3(q.transform.localScale.x, q.transform.localScale.y, 1f);
        }
    }

    private SpriteRenderer AddTile(string sprite, Vector3 pos, Vector2 size, int order)
    {
        var go = new GameObject("tile_" + sprite);
        go.transform.SetParent(Root, false);
        go.transform.position = pos;
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

    private void AddTorches()
    {
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
            fr.color = new Color(1f, 0.72f, 0.38f, 0.95f);
            fr.sortingOrder = 625;
            flameGo.transform.localScale = Vector3.one * 0.65f;

            var glowGo = new GameObject("glow");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.85f, 0.1f);
            var gr = glowGo.AddComponent<SpriteRenderer>();
            gr.sprite = Game.I.Art.World("fx_glow");
            gr.color = new Color(1f, 0.55f, 0.2f, 0.22f);
            gr.sortingOrder = 610;
            glowGo.transform.localScale = Vector3.one * 1.6f;

            var flicker = flameGo.AddComponent<TorchFlicker>();
            flicker.Init(fr, gr);
        }
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
        if (CurrentRoom == null) return;
        if (CurrentRoom.Type == RoomType.Boss)
        {
            if (WaveIndex == 0)
            {
                WaveIndex = 1;
                SpawnOne(EnemyKind.Lich, false, new Vector2(3.2f, 0f));
            }
            return;
        }
        if (WaveIndex >= CurrentRoom.Waves) return;
        var comp = Composition(Game.I.Run.RoomIndex, WaveIndex);
        bool elite = CurrentRoom.Type == RoomType.Elite && WaveIndex == 0;
        WaveIndex++;
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = StartCoroutine(SpawnRoutine(comp, elite));
    }

    public void SpawnWave()
    {
        if (WaveSpawning) return;
        StartWaveIfNeeded();
    }

    private IEnumerator SpawnRoutine(List<EnemyKind> comp, bool eliteFirst)
    {
        WaveSpawning = true;
        for (int i = 0; i < comp.Count; i++)
        {
            Vector2 pos = PickSpawn();
            SpawnOne(comp[i], eliteFirst && i == 0, pos);
            yield return new WaitForSeconds(0.15f);
        }
        WaveSpawning = false;
        CheckClear();
    }

    private void SpawnOne(EnemyKind kind, bool elite, Vector2 pos)
    {
        var e = Enemy.Spawn(kind, pos, Game.I.Run.EnemyHpScale, Game.I.Run.EnemyDamageScale, elite);
        Enemies.Add(e);
        if (kind == EnemyKind.Lich)
        {
            Game.I.UI.ShowBossBar(true);
        }
    }

    public void SpawnAdds(int count)
    {
        for (int i = 0; i < count; i++)
        {
            EnemyKind kind = i % 2 == 0 ? EnemyKind.Imp : EnemyKind.Skeleton;
            Vector2 pos = new Vector2(3.2f + Random.Range(-2.5f, 2.5f), Random.Range(-2.5f, 2.5f));
            SpawnOne(kind, false, Clamp(pos, 0.6f));
        }
    }

    private List<EnemyKind> Composition(int room, int wave)
    {
        var list = new List<EnemyKind>();
        switch (room)
        {
            case 0:
                if (wave == 0) { list.Add(EnemyKind.Imp); list.Add(EnemyKind.Imp); list.Add(EnemyKind.Imp); }
                else { list.Add(EnemyKind.Imp); list.Add(EnemyKind.Imp); list.Add(EnemyKind.Skeleton); }
                break;
            case 1:
                if (wave == 0) { list.Add(EnemyKind.Imp); list.Add(EnemyKind.Imp); list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Spider); }
                else { list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Boar); }
                break;
            case 2:
                list.Add(EnemyKind.Guard); list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Skeleton);
                break;
            case 3:
                if (wave == 0) { list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Spider); list.Add(EnemyKind.Spider); }
                else if (wave == 1) { list.Add(EnemyKind.Imp); list.Add(EnemyKind.Imp); list.Add(EnemyKind.Bomber); }
                else { list.Add(EnemyKind.Boar); list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Spider); }
                break;
            case 5:
                list.Add(EnemyKind.Spider); list.Add(EnemyKind.Spider); list.Add(EnemyKind.Spider);
                list.Add(EnemyKind.Shaman); list.Add(EnemyKind.Skeleton);
                break;
            case 6:
                if (wave == 0) { list.Add(EnemyKind.Shaman); list.Add(EnemyKind.Boar); list.Add(EnemyKind.Boar); }
                else { list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Spider); }
                break;
            case 8:
                if (wave == 0) { list.Add(EnemyKind.Bomber); list.Add(EnemyKind.Bomber); list.Add(EnemyKind.Guard); }
                else if (wave == 1) { list.Add(EnemyKind.Imp); list.Add(EnemyKind.Imp); list.Add(EnemyKind.Shaman); }
                else { list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Boar); list.Add(EnemyKind.Spider); }
                break;
            case 9:
                if (wave == 0) { list.Add(EnemyKind.Guard); list.Add(EnemyKind.Skeleton); list.Add(EnemyKind.Shaman); }
                else if (wave == 1) { list.Add(EnemyKind.Bomber); list.Add(EnemyKind.Spider); list.Add(EnemyKind.Spider); }
                else { list.Add(EnemyKind.Boar); list.Add(EnemyKind.Guard); }
                break;
            case 10:
                if (wave == 0) { list.Add(EnemyKind.Boar); list.Add(EnemyKind.Bomber); list.Add(EnemyKind.Bomber); }
                else { list.Add(EnemyKind.Guard); list.Add(EnemyKind.Shaman); list.Add(EnemyKind.Skeleton); }
                break;
            default:
                int count = 3 + wave;
                EnemyKind[] pool = { EnemyKind.Imp, EnemyKind.Skeleton, EnemyKind.Spider, EnemyKind.Boar };
                for (int i = 0; i < count; i++) list.Add(pool[Random.Range(0, pool.Length)]);
                break;
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
            if (Vector2.Distance(p, Game.I.Player.transform.position) < 2.6f) continue;
            bool bad = false;
            foreach (var o in Obstacles)
            {
                if (Vector2.Distance(p, o.Pos) < o.Radius + 0.6f) { bad = true; break; }
            }
            if (!bad) return p;
        }
        return new Vector2(Random.Range(2f, 6f), Random.Range(-2f, 2f));
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
        if (Enemies.Count > 0) return;
        if (CurrentRoom == null) return;
        if (WaveIndex < CurrentRoom.Waves)
        {
            StartCoroutine(NextWaveRoutine());
            return;
        }
        Cleared = true;
        Game.I.OnRoomCleared();
    }

    private IEnumerator NextWaveRoutine()
    {
        Game.I.UI.ShowBanner("ВОЛНА " + (WaveIndex + 1), "Они вылезают из-под пола", 1.1f, Pal.Red);
        Game.I.Audio.Play("sfx_spawn", 0.6f);
        yield return new WaitForSeconds(1.0f);
        SpawnWave();
    }

    public void BossDefeated()
    {
        if (BossDead) return;
        BossDead = true;
        Cleared = true;
        Projectile.ClearAll();
        var copy = new List<Enemy>(Enemies);
        foreach (var e in copy)
        {
            if (e != null && !e.Dead) e.TakeDamage(9999f, e.transform.position, true, false);
        }
        Game.I.UI.ShowBossBar(false);
        StartCoroutine(BossVictoryRoutine());
    }

    private IEnumerator BossVictoryRoutine()
    {
        yield return new WaitForSeconds(1.4f);
        Game.I.Victory();
    }

    public void CloseCombat()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
        WaveSpawning = false;
    }

    public void OpenDoor()
    {
        if (doorLeaf == null || openingDoor) return;
        openingDoor = true;
        StartCoroutine(DoorRoutine());
    }

    private IEnumerator DoorRoutine()
    {
        if (doorLight != null)
        {
            var lc = doorLight.color;
            doorLight.color = new Color(1f, 0.8f, 0.45f, 0.6f);
            doorLight.transform.localScale = Vector3.one * 3f;
        }
        float t = 0f;
        Quaternion start = doorLeaf.localRotation;
        Quaternion end = Quaternion.Euler(0f, 0f, -102f);
        while (t < 0.65f)
        {
            t += Time.deltaTime;
            doorLeaf.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0f, 1f, t / 0.65f));
            yield return null;
        }
        Game.I.Fx.Dust(doorLeaf.position, 5, 1.1f);
    }

    public Vector2 Move(Vector2 pos, Vector2 delta, float radius)
    {
        Vector2 p = pos + delta;
        p = Clamp(p, radius);
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
            }
        }
        return Clamp(p, radius);
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
            if (e != null && !e.Dead) e.TakeDamage(99999f, e.transform.position, true, false);
        }
    }

    private void ClearRoom()
    {
        CloseCombat();
        Projectile.ClearAll();
        foreach (var a in FindObjectsOfType<Ally>()) Destroy(a.gameObject);
        foreach (var h in FindObjectsOfType<Hazard>()) Destroy(h.gameObject);
        foreach (var f in FindObjectsOfType<FxSprite>()) Destroy(f.gameObject);
        foreach (var f in FindObjectsOfType<TelegraphFx>()) Destroy(f.gameObject);
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

    public void Init(SpriteRenderer f, SpriteRenderer g)
    {
        flame = f;
        glow = g;
        seed = Random.value * 10f;
    }

    private void Update()
    {
        float n = Mathf.PerlinNoise(seed + Time.time * 6f, 0.5f);
        float n2 = Mathf.PerlinNoise(0.3f, seed + Time.time * 9f);
        if (flame != null)
        {
            flame.transform.localScale = Vector3.one * (0.58f + n * 0.18f);
            flame.color = new Color(1f, 0.66f + n2 * 0.16f, 0.32f, 0.9f);
            flame.transform.localRotation = Quaternion.Euler(0f, 0f, (n - 0.5f) * 5f);
        }
        if (glow != null)
        {
            glow.color = new Color(1f, 0.52f, 0.2f, 0.16f + n * 0.1f);
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
