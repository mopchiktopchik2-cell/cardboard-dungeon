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
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
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
        MetaProgress.SelectedDungeonId = dungeonId;
        UI.SelectDungeon(dungeonIndex);

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
        Time.timeScale = 1f;
        UI.HideMenu();
        UI.HideEnd();
        UI.HidePause();
        UI.ShowHud();
        Run.StartRun();
        Audio.Music("music_dungeon", 0.8f);
        flow = StartCoroutine(BeginRoom(0, true));
    }

    public void StartNextDungeon()
    {
        if (Run == null || Run.Dungeon == null) return;
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
        Time.timeScale = 1f;
        UI.HideEnd();
        UI.HidePause();
        UI.HidePerks();
        UI.HideSkills();
        UI.ShowHud();
        Run.StartRun();
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
        var rooms = Run.Dungeon.Rooms;
        Debug.Log("[Cardboard] Enter room " + (index + 1) + " / " + Run.RoomCount + " type=" + rooms[index].Type);
        Player.RevivePosition(Rooms.PlayerSpawn);
        Player.SetVisible(true);
        Player.OnRoomStart();
        UI.RefreshRoom();
        var room = rooms[index];
        UI.ShowBanner(room.Name, room.Subtitle, 1.8f, Data.RoomAccent(room.Type));
        if (index == Run.RoomCount - 1)
        {
            Audio.Music("music_boss", 1.2f);
        }
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
        if (index == Run.RoomCount - 1)
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
        if (Run.RoomIndex >= Run.RoomCount - 1)
        {
            Victory();
            yield break;
        }
        OfferPerks("ВЫБЕРИ ПЕРК");
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
        State = GameState.Perk;
        Time.timeScale = 0f;
        UI.ShowTreasure(Run.RollPerks(3));
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
        Cam.Shake(0.08f, 0.3f);
        yield return WaitReal(0.35f);
        if (State == GameState.Dead) yield break;

        // бумажный занавес
        yield return UI.Wipe(true, 0.45f);
        if (State == GameState.Dead) yield break;
        Run.RoomIndex++;
        Rooms.BuildRoom(Run.RoomIndex, false);
        var rooms = Run.Dungeon.Rooms;
        Debug.Log("[Cardboard] Enter room " + (Run.RoomIndex + 1) + " / " + Run.RoomCount +
                  " type=" + rooms[Run.RoomIndex].Type);
        Player.RevivePosition(Rooms.PlayerSpawn);
        Player.SetVisible(true);
        Player.OnRoomStart();
        UI.RefreshRoom();
        yield return UI.Wipe(false, 0.5f);

        UI.SetSkipHint(false);
        var room = rooms[Run.RoomIndex];
        UI.ShowBanner(room.Name, room.Subtitle, 1.4f, Data.RoomAccent(room.Type));
        Transitioning = false;
        State = GameState.Combat;
        Player.SetControl(true);
        if (Run.RoomIndex == Run.RoomCount - 1)
        {
            Audio.Music("music_boss", 1.0f);
            Audio.Play("sfx_boss_roar", 0.9f);
            Cam.Shake(0.3f, 0.8f);
            yield return WaitReal(0.9f);
        }
        Rooms.SpawnWave();
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

    public void Victory()
    {
        if (State == GameState.Dead || State == GameState.Victory) return;
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
