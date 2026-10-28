using System.Collections;
using System.IO;
using UnityEngine;

public enum GameState
{
    Menu,
    Intro,
    Combat,
    Clear,
    Transition,
    Perk,
    Pause,
    Dead,
    Victory
}

public class Game : MonoBehaviour
{
    public static Game I;

    public Art Art;
    public AudioManager Audio;
    public UIRoot UI;
    public CameraDirector Cam;
    public Fx Fx;
    public RunManager Run;
    public RoomManager Rooms;
    public Player Player;
    public SkillSystem Skills;

    public GameState State = GameState.Menu;
    public bool QA;
    public bool SkipRequested;
    public bool Transitioning;

    private Coroutine flow;
    private Coroutine hitstop;
    private int auditPassed;
    private int auditFailed;

    public bool InputLocked => State != GameState.Combat;
    public bool InRun => State == GameState.Intro || State == GameState.Combat || State == GameState.Clear ||
                         State == GameState.Transition || State == GameState.Perk;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
        QA = HasArg("-qa");
        if (!QA || HasArg("-qaSettings")) ScreenSettings.ApplySaved();
        else
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }
        Physics2D.queriesHitTriggers = false;

        Art = new Art();
        Art.Init();

        Audio = gameObject.AddComponent<AudioManager>();
        Audio.Init();
        Audio.ApplyVolumes();

        Cam = CameraDirector.Create();
        Cam.transform.SetParent(transform);
        Cam.Init();

        Fx = gameObject.AddComponent<Fx>();
        Fx.Init();

        Run = gameObject.AddComponent<RunManager>();
        Run.Init();

        Rooms = gameObject.AddComponent<RoomManager>();
        Rooms.Init();

        Player = Player.Create(transform);
        Skills = Player.gameObject.AddComponent<SkillSystem>();
        Skills.Init(Player);
        Player.Skills = Skills;

        UI = gameObject.AddComponent<UIRoot>();
        UI.Init();

        Debug.Log("[Cardboard] Boot complete");
        ToMenu();
        if (QA && HasArg("-qaEquip")) StartCoroutine(QaEquipShot());
        else if (QA && HasArg("-qaSettingsShot")) StartCoroutine(QaSettingsShot());
        else if (QA && HasArg("-qaBossAttacks")) StartCoroutine(QaBossAttacks());
        else if (QA && HasArg("-qaEndless")) StartCoroutine(QaEndless());
        else if (QA && HasArg("-qaAudit")) StartCoroutine(QaAudit());
        else if (QA && HasArg("-qaTour")) StartCoroutine(QaTour());
    }

    private static bool HasArg(string arg)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
        {
            if (a == arg) return true;
        }
        return false;
    }

    private static string ArgValue(string prefix, string fallback)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
        {
            if (a.StartsWith(prefix)) return a.Substring(prefix.Length);
        }
        return fallback;
    }

    private IEnumerator QaTour()
    {
        string dungeonId = ArgValue("-qaDungeon=", "crypt");
        string classArg = ArgValue("-qaClass=", "mage");
        string shotsDir = ArgValue("-qaShots=", Path.Combine(Application.dataPath, "qa_shots"));
        Directory.CreateDirectory(shotsDir);

        int dungeonIndex = Data.Dungeons.FindIndex(d => d.Id == dungeonId);
        if (dungeonIndex < 0) dungeonIndex = 0;
        dungeonId = Data.Dungeons[dungeonIndex].Id;
        UI.ClickDungeonCard(dungeonIndex);
        Debug.Log("[QA] dungeon card " + dungeonIndex + " -> " + MetaProgress.SelectedDungeonId);
        if (MetaProgress.SelectedDungeonId != dungeonId)
        {
            Debug.LogError("[QA] dungeon card selected " + MetaProgress.SelectedDungeonId +
                           " instead of " + dungeonId);
        }

        HeroClass hero = HeroClass.Mage;
        if (classArg == "swordsman") hero = HeroClass.Swordsman;
        else if (classArg == "ranger") hero = HeroClass.Ranger;
        MetaProgress.SelectedClass = hero;
        UI.SelectClass(hero);

        yield return new WaitForSecondsRealtime(1.0f);
        yield return QaShot(shotsDir, "menu_" + dungeonId);

        Player.QaInvincible = true;
        StartRun();
        for (int i = 0; i < Run.RoomCount; i++)
        {
            float wait = 12f;
            while (wait > 0f && (Run.RoomIndex != i ||
                   (State != GameState.Combat && State != GameState.Perk &&
                    State != GameState.Victory && State != GameState.Dead)))
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }
            Player.Heal(9999f);
            var room = Run.Dungeon.Rooms[i];
            float shotDelay = room.Type == RoomType.Boss ? 3.2f : 1.2f;
            yield return new WaitForSecondsRealtime(shotDelay);
            Player.Heal(9999f);
            if (State == GameState.Combat)
            {
                yield return QaCastStarters();
            }
            yield return QaShot(shotsDir, "room_" + (i + 1).ToString("00") + "_" + room.Theme);
            if (State == GameState.Dead)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                yield return QaShot(shotsDir, "defeat_" + dungeonId);
                break;
            }

            float killTimer = 20f;
            while (State == GameState.Combat && killTimer > 0f)
            {
                Rooms.KillAllEnemies();
                Player.Heal(9999f);
                killTimer -= 0.2f;
                yield return new WaitForSecondsRealtime(0.2f);
            }

            wait = 8f;
            while (wait > 0f && State != GameState.Perk && State != GameState.Victory && State != GameState.Dead)
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (State == GameState.Dead)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                yield return QaShot(shotsDir, "defeat_" + dungeonId);
                break;
            }
            if (State == GameState.Perk)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                AdvanceQaChoice();
                yield return new WaitForSecondsRealtime(0.3f);
                if (State == GameState.Perk) AdvanceQaChoice();
            }

            wait = 12f;
            while (wait > 0f && State != GameState.Victory && State != GameState.Dead && Run.RoomIndex == i)
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (State == GameState.Victory || State == GameState.Dead) break;
        }

        yield return new WaitForSecondsRealtime(3.0f);
        yield return QaShot(shotsDir, "victory_" + dungeonId);
        if (HasArg("-qaNext") && State == GameState.Victory && MetaProgress.HasNextDungeon(Run.Dungeon))
        {
            StartNextDungeon();
            yield return WaitReal(3.0f);
            yield return QaShot(shotsDir, "next_" + Run.Dungeon.Id);
        }
        yield return new WaitForSecondsRealtime(0.5f);
        Application.Quit();
    }

    private IEnumerator QaCastStarters()
    {
        if (Skills == null) yield break;
        for (int slot = 0; slot < 2; slot++)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var castSlot = Skills.Slots[slot];
                if (castSlot.Def != null && castSlot.Cd <= 0f && Player.StunTimer <= 0f && !Player.Dead)
                {
                    Skills.TryCast(slot);
                    break;
                }
                yield return WaitReal(0.2f);
            }
            yield return WaitReal(0.22f);
        }
        yield return WaitReal(0.2f);
    }

    private void AdvanceQaChoice()
    {
        if (UI == null) return;
        if (UI.RestScreenOpen)
        {
            ChooseRest(true);
            return;
        }
        if (UI.SkillScreenOpen && UI.CurrentSkills.Count > 0)
        {
            ChooseSkill(UI.CurrentSkills[0].Id);
            return;
        }
        if (UI.CurrentPerks.Count > 0)
        {
            int slotChoice = UI.CurrentPerks.FindIndex(p => p != null && p.Id == "slot");
            ChoosePerk(slotChoice >= 0 ? slotChoice : 0);
        }
    }

    private IEnumerator QaShot(string dir, string name)
    {
        string path = Path.Combine(dir, name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForSecondsRealtime(0.8f);
    }

    private IEnumerator QaEquipShot()
    {
        string shotsDir = ArgValue("-qaShots=", Path.Combine(Application.dataPath, "qa_shots"));
        Directory.CreateDirectory(shotsDir);
        string[] items =
        {
            "grave_cleaver", "web_carapace", "crown_of_ashes",
            "kingsbane", "royal_shroud", "lich_seal"
        };
        foreach (var id in items) MetaProgress.AddItem(id);
        MetaProgress.Equip(Data.Item("kingsbane"));
        MetaProgress.Equip(Data.Item("royal_shroud"));
        MetaProgress.Equip(Data.Item("lich_seal"));
        yield return new WaitForSecondsRealtime(0.8f);
        UI.ShowEquipment();
        yield return new WaitForSecondsRealtime(1.0f);
        yield return QaShot(shotsDir, "equipment");
        yield return new WaitForSecondsRealtime(0.5f);
        Application.Quit();
    }

    private IEnumerator QaSettingsShot()
    {
        string shotsDir = ArgValue("-qaShots=", Path.Combine(Application.dataPath, "qa_shots"));
        Directory.CreateDirectory(shotsDir);
        yield return new WaitForSecondsRealtime(0.8f);
        UI.ShowSettings();
        yield return new WaitForSecondsRealtime(0.8f);
        yield return QaShot(shotsDir, "settings");
        Application.Quit();
    }

    private IEnumerator QaBossAttacks()
    {
        Player.QaInvincible = true;
        string[] dungeons = { "crypt", "web", "throne" };
        int started = 0;
        foreach (string id in dungeons)
        {
            MetaProgress.SelectedDungeonId = id;
            MetaProgress.SelectedClass = HeroClass.Mage;
            StartRun();
            yield return QaWaitFor(() => State == GameState.Combat, 4f);
            yield return QaEnterBoss();
            Enemy boss = Rooms.Enemies.Find(e => e != null && e.Boss);
            if (boss == null)
            {
                Debug.LogError("[QaBossAttacks] boss missing: " + id);
                break;
            }
            boss.MaxHp = 999999f;
            boss.Hp = boss.MaxHp;
            for (int which = 0; which < 6; which++)
            {
                if (!boss.QaCastAttack(which))
                {
                    Debug.LogError("[QaBossAttacks] cast failed: " + id + " pattern=" + which);
                    continue;
                }
                started++;
                yield return QaWaitFor(() => !boss.QaCasting, 3f);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Debug.Log("[QaBossAttacks] " + id + " done");
        }
        Debug.Log("[QaBossAttacks] started=" + started + " expected=18");
        yield return new WaitForSecondsRealtime(0.3f);
        Application.Quit();
    }

    private IEnumerator QaEndless()
    {
        string shotsDir = ArgValue("-qaShots=", Path.Combine(Application.dataPath, "qa_shots"));
        Directory.CreateDirectory(shotsDir);
        MetaProgress.SelectedClass = HeroClass.Mage;
        Player.QaInvincible = true;
        yield return new WaitForSecondsRealtime(0.6f);
        yield return QaShot(shotsDir, "endless_menu");

        StartEndlessRun();
        yield return QaWaitFor(() => State == GameState.Combat && Run.RoomIndex == 0, 6f);
        EndlessCheck("endless run starts", Run.Endless && Run.Dungeon != null &&
                                            Run.Dungeon.Id == EndlessContent.DungeonId);
        EndlessCheck("endless starts with 25 coins", Run.Coins == EndlessContent.StartCoins);
        yield return QaShot(shotsDir, "endless_room_01");

        for (int expected = 0; expected < 5; expected++)
        {
            yield return QaWaitFor(() => State == GameState.Combat && Run.RoomIndex == expected, 12f);
            EndlessCheck("endless room " + (expected + 1) + " reached",
                State == GameState.Combat && Run.RoomIndex == expected);
            if (State != GameState.Combat || Run.RoomIndex != expected) break;

            if (expected == 4)
            {
                EndlessCheck("endless boss room", Run.IsBossRoom(Run.RoomIndex));
                EndlessCheck("endless boss kind", Run.BossKind == EndlessContent.BossFor(Run.RoomIndex));
                yield return QaWaitFor(() => Rooms.Enemies.Exists(e => e != null && e.Boss), 3f);
                EndlessCheck("endless boss spawned", Rooms.Enemies.Exists(e => e != null && e.Boss));
                EndlessCheck("endless scaling grows", Run.EnemyHpScale > EndlessContent.EnemyHpScale(0));
                yield return QaShot(shotsDir, "endless_boss");
                break;
            }

            yield return QaCastStarters();
            float killTimer = 20f;
            while (State == GameState.Combat && killTimer > 0f)
            {
                Rooms.KillAllEnemies();
                Player.Heal(9999f);
                killTimer -= 0.25f;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            yield return QaWaitFor(() => State == GameState.Perk, 8f);
            EndlessCheck("endless shop opens", State == GameState.Perk && UI.ShopOpen);
            if (State != GameState.Perk || !UI.ShopOpen) break;

            EndlessCheck("endless shop offers three", UI.CurrentShopOffers.Count == EndlessContent.ShopOfferCount);
            if (expected == 0)
            {
                EndlessCheck("endless kills drop coins", Run.Coins > EndlessContent.StartCoins);
            }
            yield return new WaitForSecondsRealtime(0.25f);
            yield return QaShot(shotsDir, "endless_shop_" + (expected + 1).ToString("00"));

            if (UI.CurrentShopOffers.Count > 0)
            {
                var offer = UI.CurrentShopOffers[0];
                int beforeBuy = Run.Coins;
                Run.AddCoins(10000);
                int expectedCoins = Run.Coins - offer.Cost;
                ChooseShop(0);
                yield return null;
                EndlessCheck("endless shop purchase works",
                    Run.Coins == expectedCoins && UI.ShopOpen);
            }

            // Проверяем, что баннер mastery не скрывается обновлением лавки.
            foreach (var slot in Skills.Slots)
            {
                if (slot.Def != null) Run.SetSkillLevel(slot.Def.Id, EndlessContent.MaxSkillLevel - 1);
            }
            Run.AddCoins(10000);
            UI.ShowShop(Run.BuildShopOffers(EndlessContent.ShopOfferCount), Run.Coins, Run.RoomIndex);
            int masteryOffer = -1;
            for (int i = 0; i < UI.CurrentShopOffers.Count; i++)
            {
                var candidate = UI.CurrentShopOffers[i];
                if (candidate != null && candidate.Kind == ShopOfferKind.Upgrade &&
                    candidate.TargetLevel == EndlessContent.MaxSkillLevel)
                {
                    masteryOffer = i;
                    break;
                }
            }
            EndlessCheck("endless mastery offer available", masteryOffer >= 0);
            if (masteryOffer >= 0)
            {
                ChooseShop(masteryOffer);
                yield return new WaitForSecondsRealtime(0.35f);
                EndlessCheck("endless mastery banner visible", UI.BannerVisible);
                yield return QaShot(shotsDir, "endless_mastery");
            }

            Run.SetSkillLevel("fireball", EndlessContent.MaxSkillLevel);
            bool mastery = false;
            foreach (var slot in Skills.Slots)
            {
                if (slot.Def != null && slot.Def.Id == "fireball" && slot.Mastery) mastery = true;
            }
            EndlessCheck("endless skill reaches mastery", mastery &&
                Run.SkillLevel("fireball") == EndlessContent.MaxSkillLevel);
            var offers = Run.BuildShopOffers(EndlessContent.ShopOfferCount);
            EndlessCheck("endless maxed skill is not sold",
                !offers.Exists(o => o != null && o.Kind == ShopOfferKind.Upgrade &&
                                    o.Skill != null && o.Skill.Id == "fireball"));

            ContinueShop();
            yield return QaWaitFor(() => State == GameState.Combat && Run.RoomIndex == expected + 1, 12f);
        }

        if (State == GameState.Combat && Run.RoomIndex == 4 && Skills.Slots[0].Def != null &&
            Skills.Slots[0].Def.Id == "fireball")
        {
            Skills.Slots[0].Cd = 0f;
            int before = FindObjectsOfType<Projectile>().Length;
            Skills.TryCast(0);
            yield return new WaitForSecondsRealtime(0.12f);
            int after = FindObjectsOfType<Projectile>().Length;
            EndlessCheck("fireball mastery fires a volley", after - before >= 3);
        }

        Debug.Log("[QaEndless] passed=" + auditPassed + " failed=" + auditFailed);
        yield return new WaitForSecondsRealtime(0.3f);
        Application.Quit();
    }

    private void EndlessCheck(string name, bool ok)
    {
        if (ok)
        {
            auditPassed++;
            Debug.Log("[QaEndless] PASS " + name);
        }
        else
        {
            auditFailed++;
            Debug.LogError("[QaEndless] FAIL " + name);
        }
    }

    private IEnumerator QaAudit()
    {
        Player.QaInvincible = false;
        MetaProgress.SelectedDungeonId = "crypt";
        MetaProgress.SelectedClass = HeroClass.Mage;
        yield return new WaitForSecondsRealtime(0.2f);

        // 1. Обычный старт и DoT: яд должен пробивать неуязвимость,
        // но не превращаться в обычный удар.
        StartRun();
        yield return QaWaitFor(() => State == GameState.Combat, 4f);
        AuditCheck("run starts in combat", State == GameState.Combat);
        float hpBeforeDot = Player.Stats.hp;
        Player.Invuln = 1f;
        Player.TakeDamage(5f, Player.transform.position, true, true);
        AuditCheck("poison tick ignores invulnerability", Player.Stats.hp < hpBeforeDot);
        Player.Invuln = 0f;
        Player.Heal(9999f);

        // 1b. Вражеские удары: снаряд и собака должны реально снимать HP.
        yield return QaEnemyHitChecks();

        // 2. Босс убит, но забег перезапускают до конца задержки победы.
        yield return QaEnterBoss();
        AuditCheck("boss spawned", Rooms.Enemies.Exists(e => e != null && e.Boss));
        Hazard.Poison(Player.transform.position, 1.5f, 5f, 5f, false);
        Projectile.Enemy((Vector2)Player.transform.position + Vector2.up * 2.5f, Vector2.down, 1f, 1f, "proj_arrow");
        Rooms.KillAllEnemies();
        yield return QaWaitFor(() => Rooms.BossDead, 1f);
        AudCheckBossPending();
        PauseGame();
        AuditCheck("pause blocked after boss death", State == GameState.Combat);
        float hpAfterLock = Player.Stats.hp;
        Player.TakeDamage(9999f, Player.transform.position, true, true);
        AuditCheck("boss victory lock blocks damage", Mathf.Approximately(hpAfterLock, Player.Stats.hp));
        yield return new WaitForSecondsRealtime(0.12f);
        AuditCheck("boss death clears projectiles and hazards",
            FindObjectsOfType<Projectile>().Length == 0 && FindObjectsOfType<Hazard>().Length == 0);
        RestartRun();
        yield return new WaitForSecondsRealtime(1.9f);
        AuditCheck("restart cancels pending boss victory",
            State != GameState.Victory && Run.RoomIndex == 0 && !Rooms.BossDead);

        // 3. Босс убит, но игрок уходит в меню до конца задержки.
        yield return QaWaitFor(() => State == GameState.Combat, 4f);
        yield return QaEnterBoss();
        Rooms.KillAllEnemies();
        yield return QaWaitFor(() => Rooms.BossDead, 1f);
        ToMenu();
        yield return new WaitForSecondsRealtime(1.9f);
        AuditCheck("menu cancels pending boss victory", State == GameState.Menu);

        // 4. Если игрок погиб раньше босса, победа не должна перебить поражение.
        StartRun();
        yield return QaWaitFor(() => State == GameState.Combat, 4f);
        yield return QaEnterBoss();
        Player.Invuln = 0f;
        Player.TakeDamage(99999f, Player.transform.position, true, true);
        Rooms.KillAllEnemies();
        yield return new WaitForSecondsRealtime(1.25f);
        AuditCheck("death before boss death stays defeat", State == GameState.Dead);

        // 5. Штатная победа всё ещё работает и не ломается новыми проверками.
        RestartRun();
        yield return QaWaitFor(() => State == GameState.Combat, 4f);
        yield return QaEnterBoss();
        Rooms.KillAllEnemies();
        yield return QaWaitFor(() => Rooms.BossDead, 1f);
        yield return new WaitForSecondsRealtime(2.0f);
        AuditCheck("normal boss victory still completes", State == GameState.Victory);

        Debug.Log("[QaAudit] passed=" + auditPassed + " failed=" + auditFailed);
        yield return new WaitForSecondsRealtime(0.3f);
        Application.Quit();
    }

    private void AudCheckBossPending()
    {
        AuditCheck("boss victory is pending", Rooms.BossDead && State == GameState.Combat && Player.VictoryLocked);
    }

    private void AuditCheck(string name, bool ok)
    {
        if (ok)
        {
            auditPassed++;
            Debug.Log("[QaAudit] PASS " + name);
        }
        else
        {
            auditFailed++;
            Debug.LogError("[QaAudit] FAIL " + name);
        }
    }

    private IEnumerator QaWaitFor(System.Func<bool> condition, float timeout)
    {
        float left = timeout;
        while (!condition() && left > 0f)
        {
            left -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator QaEnemyHitChecks()
    {
        // Отдельная пустая комната, чтобы чужие атаки не маскировали результат.
        StopFlow();
        Run.RoomIndex = 1;
        Rooms.BuildRoom(1, false);
        Player.RevivePosition(Rooms.PlayerSpawn);
        Player.SetVisible(true);
        Player.OnRoomStart();
        Player.QaInvincible = false;
        Player.Invuln = 0f;
        Player.Heal(9999f);
        State = GameState.Combat;
        Transitioning = false;
        UI.RefreshRoom();
        yield return new WaitForSecondsRealtime(0.3f);

        float hpBeforeProjectile = Player.Stats.hp;
        Player.Invuln = 0f;
        Projectile.Enemy((Vector2)Player.transform.position + Vector2.up * 1.4f, Vector2.down,
            6f, 6f, "proj_arrow");
        yield return QaWaitFor(() => Player.Stats.hp < hpBeforeProjectile, 1.5f);
        AuditCheck("enemy projectile hits player", Player.Stats.hp < hpBeforeProjectile);
        Player.Heal(9999f);
        Player.Invuln = 0f;

        Vector2 houndPos = (Vector2)Player.transform.position + Vector2.right * 2.8f;
        var hound = Enemy.Spawn(EnemyKind.Hound, houndPos, 1f, 1f);
        hound.MaxHp = 99999f;
        hound.Hp = hound.MaxHp;
        Rooms.Enemies.Add(hound);
        float startDistance = Vector2.Distance(Player.transform.position, hound.transform.position);
        yield return new WaitForSecondsRealtime(0.75f);
        float closedDistance = Vector2.Distance(Player.transform.position, hound.transform.position);
        AuditCheck("hound closes distance to attack", closedDistance < startDistance - 0.25f);

        float hpBeforeBite = Player.Stats.hp;
        Player.Invuln = 0f;
        yield return QaWaitFor(() => Player.Stats.hp < hpBeforeBite, 3f);
        AuditCheck("hound bite damages player", Player.Stats.hp < hpBeforeBite);

        if (hound != null)
        {
            Rooms.Enemies.Remove(hound);
            Destroy(hound.gameObject);
        }
        Player.Heal(9999f);
        Player.Invuln = 0f;

        // Переход в следующую комнату должен вернуть 20% максимального HP.
        Player.QaInvincible = true;
        Player.Stats.hp = Player.Stats.maxHp * 0.5f;
        float expectedAfterTransition = Player.Stats.hp + Player.Stats.maxHp * Data.RoomTransitionHeal;
        ContinueAfterChoice();
        yield return QaWaitFor(() => State == GameState.Combat && Run.RoomIndex == 2, 6f);
        AuditCheck("room transition heals 20% max hp",
            Player.Stats.hp >= expectedAfterTransition - 1f);
        Player.QaInvincible = false;
        Player.Heal(9999f);
        yield return new WaitForSecondsRealtime(0.2f);
    }

    private IEnumerator QaEnterBoss()
    {
        StopFlow();
        int index = Run.Endless ? 4 : Run.RoomCount - 1;
        Run.RoomIndex = index;
        Rooms.BuildRoom(index, false);
        Player.RevivePosition(Rooms.PlayerSpawn);
        Player.SetVisible(true);
        Player.OnRoomStart();
        State = GameState.Combat;
        Transitioning = false;
        UI.RefreshRoom();
        Rooms.SpawnWave();
        yield return new WaitForSecondsRealtime(0.5f);
        yield return QaWaitFor(() => Rooms.Enemies.Count > 0, 1f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UI.SettingsOpen)
            {
                UI.HideSettings();
                return;
            }
            if (State == GameState.Combat) PauseGame();
            else if (State == GameState.Pause) ResumeGame();
        }

        if (State == GameState.Menu && !UI.MenuOverlayOpen &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            StartRun();
        }

        if ((State == GameState.Dead || State == GameState.Victory) && Input.GetKeyDown(KeyCode.R))
        {
            RestartRun();
        }

        if (State == GameState.Victory && Input.GetKeyDown(KeyCode.N))
        {
            StartNextDungeon();
        }

        if (QA)
        {
            if (State == GameState.Combat && Input.GetKeyDown(KeyCode.F1))
            {
                Rooms.KillAllEnemies();
            }
            if (State == GameState.Combat && Input.GetKeyDown(KeyCode.F2))
            {
                Rooms.KillAllEnemies();
            }
            if (State == GameState.Combat && Input.GetKeyDown(KeyCode.F3))
            {
                Player.Heal(9999);
            }
            if (State == GameState.Combat && Input.GetKeyDown(KeyCode.F4))
            {
                Time.timeScale = 1f;
                Run.AddPerk(Data.Perk("damage"));
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                ScreenCapture.CaptureScreenshot("screenshot_" + Time.frameCount + ".png");
            }
            if (State == GameState.Menu && Input.GetKeyDown(KeyCode.F6))
            {
                int next = (Data.Dungeons.IndexOf(MetaProgress.SelectedDungeon) + 1) % Data.Dungeons.Count;
                MetaProgress.SelectedDungeonId = Data.Dungeons[next].Id;
                UI.SelectDungeon(next);
            }
            if (State == GameState.Menu && Input.GetKeyDown(KeyCode.F7))
            {
                int next = ((int)MetaProgress.SelectedClass + 1) % 3;
                UI.SelectClass((HeroClass)next);
            }
            if (State == GameState.Combat && Input.GetKeyDown(KeyCode.F8))
            {
                Run.RoomIndex = Run.RoomCount - 1;
                Rooms.KillAllEnemies();
            }
        }
    }

    public void ToMenu()
    {
        StopFlow();
        if (Rooms != null) Rooms.CancelPendingVictory();
        Time.timeScale = 1f;
        State = GameState.Menu;
        Transitioning = false;
        if (!QA && !MetaProgress.IsDungeonUnlocked(MetaProgress.SelectedDungeonId))
        {
            MetaProgress.SelectedDungeonId = "crypt";
        }
        Rooms.BuildMenu();
        Player.ShowInMenu();
        UI.ShowMenu();
        UI.HideHud();
        Audio.Music("music_dungeon", 0.8f);
    }

    public void StartRun()
    {
        if (!QA && !MetaProgress.IsDungeonUnlocked(MetaProgress.SelectedDungeonId))
        {
            string locked = MetaProgress.SelectedDungeon != null
                ? MetaProgress.SelectedDungeon.Name
                : "ПОДЗЕМЕЛЬЕ";
            UI.ShowBanner("ПУТЬ ЗАКРЫТ", locked + " откроется после победы в предыдущем подземелье",
                1.8f, Pal.Red);
            Audio.Play("ui_click", 0.5f, 0.75f);
            return;
        }
        StopFlow();
        if (Rooms != null) Rooms.CancelPendingVictory();
        Time.timeScale = 1f;
        UI.HideMenu();
        UI.HideEnd();
        UI.HidePause();
        UI.ShowHud();
        Run.StartRun(false);
        Audio.Music("music_dungeon", 0.8f);
        flow = StartCoroutine(BeginRoom(0, true));
    }

    public void StartEndlessRun()
    {
        StopFlow();
        if (Rooms != null) Rooms.CancelPendingVictory();
        Time.timeScale = 1f;
        UI.HideMenu();
        UI.HideEnd();
        UI.HidePause();
        UI.ShowHud();
        Run.StartRun(true);
        Audio.Music("music_dungeon", 0.8f);
        flow = StartCoroutine(BeginRoom(0, true));
    }

    public void StartNextDungeon()
    {
        if (Run == null || Run.Dungeon == null || Run.Endless) return;
        var next = MetaProgress.NextDungeon(Run.Dungeon);
        if (next == null) return;
        int index = Data.Dungeons.FindIndex(d => d.Id == next.Id);
        if (index < 0) return;
        MetaProgress.SelectedDungeonId = next.Id;
        Time.timeScale = 1f;
        UI.HideEnd();
        UI.HidePause();
        UI.HidePerks();
        UI.HideSkills();
        StartRun();
    }

    public void RestartRun()
    {
        StopFlow();
        if (Rooms != null) Rooms.CancelPendingVictory();
        Time.timeScale = 1f;
        UI.HideEnd();
        UI.HidePause();
        UI.HidePerks();
        UI.HideSkills();
        UI.HideRest();
        UI.ShowHud();
        bool endless = Run != null && Run.Endless;
        Run.StartRun(endless);
        Audio.Music("music_dungeon", 0.6f);
        flow = StartCoroutine(BeginRoom(0, true));
    }

    private IEnumerator BeginRoom(int index, bool firstRoom)
    {
        State = GameState.Intro;
        Transitioning = true;
        SkipRequested = false;
        Run.RoomIndex = index;
        Rooms.BuildRoom(index, firstRoom);
        var room = Run.RoomAt(index);
        if (room == null)
        {
            ToMenu();
            yield break;
        }
        bool bossRoom = Run.IsBossRoom(index);
        Debug.Log("[Cardboard] Enter room " + (index + 1) + " / " + Run.RoomCount +
                  " type=" + room.Type + (Run.Endless ? " endless" : ""));
        Player.RevivePosition(Rooms.PlayerSpawn);
        Player.SetVisible(true);
        Player.OnRoomStart();
        UI.RefreshRoom();
        UI.ShowBanner(room.Name, room.Subtitle, 1.8f, Data.RoomAccent(room.Type));
        if (bossRoom) Audio.Music("music_boss", 1.2f);
        yield return WaitReal(1.3f);
        Transitioning = false;
        State = GameState.Combat;
        if (room.Type == RoomType.Treasure)
        {
            ShowTreasure();
            yield break;
        }
        if (room.Type == RoomType.Rest)
        {
            ShowRest();
            yield break;
        }
        Rooms.SpawnWave();
        if (bossRoom)
        {
            UI.ShowBossBar(true, Run.BossName);
            Audio.Play("sfx_boss_roar", 0.9f);
            Cam.Shake(0.25f, 0.7f);
        }
    }

    public void OnRoomCleared()
    {
        if (State != GameState.Combat) return;
        State = GameState.Clear;
        Rooms.CloseCombat();
        Run.OnRoomCleared();
        UI.ShowBanner("КОМНАТА ЗАЧИЩЕНА", Player.Stats.roomHeal > 0 ? "Передышка восстановила силы" : "Пора идти дальше", 1.2f, Pal.Gold);
        flow = StartCoroutine(ClearRoutine());
    }

    private IEnumerator ClearRoutine()
    {
        yield return WaitReal(1.1f);
        if (State != GameState.Clear || Player == null || Player.Dead)
        {
            yield break;
        }
        if (Run.Endless)
        {
            OfferEndlessShop();
            yield break;
        }
        OfferPerks("ВЫБЕРИ ПЕРК");
    }

    public void OfferEndlessShop()
    {
        if (Player == null || Player.Dead || State == GameState.Dead) return;
        if (!Run.Endless) return;
        State = GameState.Perk;
        Time.timeScale = 0f;
        UI.HideBanner();
        var offers = Run.BuildShopOffers(EndlessContent.ShopOfferCount);
        if (offers.Count == 0)
        {
            ContinueAfterChoice();
            return;
        }
        UI.ShowShop(offers, Run.Coins, Run.RoomIndex);
        Audio.Play("sfx_pickup", 0.7f);
    }

    public void ChooseShop(int index)
    {
        if (State != GameState.Perk || UI == null || !UI.ShopOpen) return;
        var offer = UI.CurrentShopOffers != null && index >= 0 && index < UI.CurrentShopOffers.Count
            ? UI.CurrentShopOffers[index]
            : null;
        if (offer == null) return;
        string error;
        if (!Run.TryBuy(offer, out error))
        {
            UI.ShowBanner("ЛАВКА", error, 1.6f, Pal.Red);
            Audio.Play("ui_click", 0.5f, 0.7f);
            return;
        }
        UI.RefreshCoins();
        UI.RefreshSkills();
        Audio.Play("sfx_pickup", 0.85f);
        if (offer.Kind == ShopOfferKind.Upgrade && offer.TargetLevel >= EndlessContent.MaxSkillLevel)
        {
            UI.ShowBanner("МАСТЕРИЯ", offer.Skill != null
                ? offer.Skill.Name + ": " + EndlessContent.MasteryDescription(offer.Skill.Id)
                : "Навык достиг предела", 2.4f, Pal.Purple);
        }
        var offers = Run.BuildShopOffers(EndlessContent.ShopOfferCount);
        UI.ShowShop(offers, Run.Coins, Run.RoomIndex);
    }

    public void ContinueShop()
    {
        if (State != GameState.Perk || UI == null || !UI.ShopOpen) return;
        ContinueAfterChoice();
    }

    public void OfferPerks(string title)
    {
        if (Player == null || Player.Dead || State == GameState.Dead) return;
        State = GameState.Perk;
        Time.timeScale = 0f;
        var perks = Run.RollPerks(3);
        if (perks.Count == 0)
        {
            ContinueAfterChoice();
            return;
        }
        UI.ShowPerks(perks, title);
        Audio.Play("sfx_pickup", 0.7f);
    }

    public void ChoosePerk(int index)
    {
        if (State != GameState.Perk) return;
        var def = UI.CurrentPerks != null && index >= 0 && index < UI.CurrentPerks.Count
            ? UI.CurrentPerks[index]
            : null;
        if (def == null) return;
        if (!Run.AddPerk(def))
        {
            UI.ShowBanner("НЕДОСТУПНО", "Этот перк больше нельзя взять", 1.4f, Pal.Red);
            return;
        }
        UI.HidePerks();
        if (def.Id == "slot")
        {
            var skills = Run.RollSkills(3);
            if (skills.Count > 0)
            {
                UI.ShowSkills(skills, "ВЫБЕРИ НОВЫЙ НАВЫК");
                State = GameState.Perk;
                Time.timeScale = 0f;
                return;
            }
        }
        ContinueAfterChoice();
    }

    public void ChooseSkill(string id)
    {
        if (State != GameState.Perk) return;
        if (!Skills.AddSkill(id))
        {
            UI.ShowBanner("НЕДОСТУПНО", "Этот навык уже изучен или нет свободного слота", 1.4f, Pal.Red);
            return;
        }
        UI.HideSkills();
        ContinueAfterChoice();
    }

    public void ChooseRest(bool heal)
    {
        if (State != GameState.Perk) return;
        UI.HideRest();
        Time.timeScale = 1f;
        if (heal)
        {
            Player.Heal(Player.Stats.maxHp * 0.28f);
            Fx.Heal(Player.transform.position);
            Audio.Play("sfx_heal", 0.8f);
            ContinueAfterChoice();
        }
        else
        {
            OfferPerks("ПРИВАЛ");
        }
    }

    public void ShowTreasure()
    {
        var perks = Run.RollPerks(3);
        if (perks.Count == 0)
        {
            // Пустая сокровищница не должна оставлять игру в состоянии Perk.
            ContinueAfterChoice();
            return;
        }
        State = GameState.Perk;
        Time.timeScale = 0f;
        UI.ShowTreasure(perks);
        Audio.Play("sfx_pickup", 0.8f);
    }

    public void ShowRest()
    {
        State = GameState.Perk;
        Time.timeScale = 0f;
        UI.ShowRest();
    }

    public void ContinueAfterChoice()
    {
        Time.timeScale = 1f;
        UI.HidePerks();
        UI.HideSkills();
        UI.HideRest();
        UI.HideShop();
        flow = StartCoroutine(TransitionToNextRoom());
    }

    private IEnumerator TransitionToNextRoom()
    {
        State = GameState.Transition;
        Transitioning = true;
        SkipRequested = false;
        UI.SetSkipHint(true);
        Player.SetControl(false);
        Rooms.CloseCombat();
        yield return WaitReal(0.25f);
        if (State == GameState.Dead) yield break;

        // маг идёт к двери
        yield return Player.WalkTo(Rooms.RightDoorPosition, 0.9f);
        if (State == GameState.Dead) yield break;
        Rooms.OpenDoor();
        Audio.Play("sfx_door", 0.8f);
        Vector2 doorFx = Rooms.RightDoorPosition + Vector2.up * 0.55f;
        Fx.Ring(doorFx, new Color(1f, 0.78f, 0.45f, 0.8f), 1.0f, 0.5f);
        Fx.Burst("fx_dust", doorFx, 12, 2.2f, 0.7f, 0.9f,
            new Color(0.95f, 0.8f, 0.55f, 0.7f), 1600);
        UI.Flash(new Color(1f, 0.82f, 0.5f, 0.16f), 0.32f);
        Cam.Shake(0.12f, 0.35f);
        yield return WaitReal(0.35f);
        if (State == GameState.Dead) yield break;

        // бумажный занавес
        yield return UI.Wipe(true, 0.5f);
        if (State == GameState.Dead) yield break;
        Run.RoomIndex++;
        Rooms.BuildRoom(Run.RoomIndex, false);
        var room = Run.RoomAt(Run.RoomIndex);
        if (room == null)
        {
            if (Run.Endless) CompleteEndlessRun();
            else ToMenu();
            yield break;
        }
        bool bossRoom = Run.IsBossRoom(Run.RoomIndex);
        Debug.Log("[Cardboard] Enter room " + (Run.RoomIndex + 1) + " / " + Run.RoomCount +
                  " type=" + room.Type + (Run.Endless ? " endless" : ""));
        Player.RevivePosition(Rooms.PlayerSpawn);
        Player.SetVisible(true);
        Player.OnRoomStart();
        UI.RefreshRoom();
        yield return UI.Wipe(false, 0.55f);

        // После перехода герой получает передышку в 20% максимального запаса HP.
        float beforeHeal = Player.Stats.hp;
        Player.Heal(Player.Stats.maxHp * Data.RoomTransitionHeal);
        bool healed = Player.Stats.hp > beforeHeal + 0.5f;
        if (healed) Fx.Heal(Player.transform.position + Vector3.up * 0.5f);
        Fx.Ring(Player.transform.position + Vector3.up * 0.5f,
            new Color(0.55f, 0.85f, 1f, 0.75f), 1.4f, 0.55f);
        Fx.Burst("fx_dust", Player.transform.position, 8, 1.8f, 0.55f, 0.75f,
            new Color(0.7f, 0.9f, 1f, 0.6f), 1600);
        UI.Flash(new Color(0.9f, 0.95f, 1f, 0.1f), 0.32f);

        UI.SetSkipHint(false);
        UI.ShowBanner(room.Name, room.Subtitle, 1.4f, Data.RoomAccent(room.Type));
        Transitioning = false;
        State = GameState.Combat;
        Player.SetControl(true);
        if (bossRoom)
        {
            Audio.Music("music_boss", 1.0f);
            Audio.Play("sfx_boss_roar", 0.9f);
            Cam.Shake(0.3f, 0.8f);
            yield return WaitReal(0.9f);
        }
        else if (Run.Endless)
        {
            Audio.Music("music_dungeon", 0.8f);
        }
        Rooms.SpawnWave();
        if (bossRoom)
        {
            UI.ShowBossBar(true, Run.BossName);
        }
        if (room.Type == RoomType.Treasure)
        {
            ShowTreasure();
        }
        else if (room.Type == RoomType.Rest)
        {
            ShowRest();
        }
    }

    public void PauseGame()
    {
        if (State != GameState.Combat) return;
        if (Rooms != null && Rooms.BossDead) return;
        State = GameState.Pause;
        Time.timeScale = 0f;
        UI.ShowPause();
        Audio.Play("ui_click", 0.6f);
    }

    public void ResumeGame()
    {
        if (State != GameState.Pause) return;
        UI.HidePause();
        Time.timeScale = 1f;
        State = GameState.Combat;
        Audio.Play("ui_click", 0.5f);
    }

    public void CompleteEndlessRun()
    {
        if (Run == null || !Run.Endless || Player == null || Player.Dead) return;
        State = GameState.Victory;
        Transitioning = false;
        Time.timeScale = 0f;
        UI.SetSkipHint(false);
        UI.ShowBossBar(false);
        UI.HideShop();
        Run.SaveRecord(true);
        UI.ShowEnd(true);
        Audio.StopMusic(0.8f);
        Audio.Play("sfx_victory", 0.9f);
    }

    public void Victory()
    {
        if (Run != null && Run.Endless)
        {
            OnRoomCleared();
            return;
        }
        if (State != GameState.Combat || Rooms == null || !Rooms.BossDead || !Rooms.Cleared) return;
        if (Player == null || Player.Dead) return;
        StopFlow();
        State = GameState.Victory;
        Time.timeScale = 0f;
        UI.ShowBossBar(false);
        var loot = Run.RollVictoryLoot();
        Run.SaveRecord(true);
        UI.ShowEnd(true, loot);
        Audio.StopMusic(0.8f);
        Audio.Play("sfx_victory", 0.9f);
    }

    public void PlayerDied()
    {
        if (State == GameState.Dead || State == GameState.Victory) return;
        StopFlow();
        if (Rooms != null)
        {
            Rooms.CancelPendingVictory();
            Rooms.CloseCombat();
        }
        State = GameState.Dead;
        Time.timeScale = 0f;
        UI.ShowBossBar(false);
        Run.SaveRecord(false);
        UI.ShowEnd(false);
        Audio.StopMusic(0.8f);
        Audio.Play("sfx_defeat", 0.9f);
    }

    public void Shake(float amount, float duration)
    {
        if (Cam != null) Cam.Shake(amount, duration);
    }

    public void Hitstop(float duration)
    {
        if (hitstop != null) StopCoroutine(hitstop);
        hitstop = StartCoroutine(HitstopRoutine(duration));
    }

    private IEnumerator HitstopRoutine(float duration)
    {
        if (State == GameState.Pause || State == GameState.Perk || State == GameState.Dead ||
            State == GameState.Victory) yield break;
        Time.timeScale = 0.04f;
        yield return new WaitForSecondsRealtime(duration);
        if (State != GameState.Pause && State != GameState.Perk && State != GameState.Dead &&
            State != GameState.Victory)
        {
            Time.timeScale = 1f;
        }
    }

    public IEnumerator WaitReal(float seconds)
    {
        float t = 0f;
        while (t < seconds && !SkipRequested)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public void RequestSkip()
    {
        if (State == GameState.Transition) SkipRequested = true;
    }

    private void StopFlow()
    {
        if (flow != null)
        {
            StopCoroutine(flow);
            flow = null;
        }
        Transitioning = false;
        SkipRequested = false;
    }

    public void SetState(GameState s)
    {
        State = s;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
