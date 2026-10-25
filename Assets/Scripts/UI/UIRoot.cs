using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotView
{
    public RectTransform Root;
    public Image Frame;
    public Image Icon;
    public Image Cooldown;
    public Text Key;
    public Text CooldownText;
}

public class UIRoot : MonoBehaviour
{
    public Canvas Canvas;
    public List<PerkDef> CurrentPerks = new List<PerkDef>();
    public List<SkillDef> CurrentSkills = new List<SkillDef>();

    private Art art;
    private CanvasGroup menuGroup;
    private CanvasGroup hudGroup;
    private CanvasGroup perkGroup;
    private CanvasGroup pauseGroup;
    private CanvasGroup endGroup;
    private CanvasGroup bannerGroup;
    private CanvasGroup equipGroup;
    private CanvasGroup settingsGroup;

    private RectTransform perkCardsRoot;
    private RectTransform menuRoot;
    private RectTransform helpPanel;
    private RectTransform restRoot;
    private RectTransform pausePanel;
    private RectTransform endPanel;
    private RectTransform equipItemsRoot;

    private Text menuBest;
    private Text menuClassTitle;
    private Text menuClassStats;
    private Text menuClassDesc;
    private Text menuDungeonTitle;
    private Text menuDungeonStats;
    private Text equipHint;
    private Text endLoot;
    private RectTransform endLootCard;
    private Image endLootIcon;
    private Image endLootGlow;
    private Image endLootFrame;
    private Text endLootBadge;
    private Text endLootName;
    private Text endLootMeta;
    private Text endLootStats;
    private Image menuHero;
    private Image menuDungeonTint;
    private readonly List<Image> classFrames = new List<Image>();
    private readonly List<Image> classIcons = new List<Image>();
    private readonly List<Image> classAccents = new List<Image>();
    private readonly List<Image> dungeonFrames = new List<Image>();
    private readonly List<Image> dungeonIcons = new List<Image>();
    private readonly List<Image> dungeonAccents = new List<Image>();
    private readonly List<GameObject> dungeonLocks = new List<GameObject>();
    private readonly List<Button> dungeonButtons = new List<Button>();
    private readonly Text[] equipSlotNames = new Text[3];
    private readonly Text[] equipSlotStats = new Text[3];
    private readonly Image[] equipSlotIcons = new Image[3];

    private string currentBossName = "ЛИЧ-ПЕРЕПЛЁТ";
    private Text roomText;
    private Text roomSub;
    private Text hpText;
    private Text shieldText;
    private Text statsText;
    private Text dashText;
    private Text bossName;
    private Text bannerTitle;
    private Text bannerSub;
    private Text perkTitle;
    private Text perkHint;
    private Text endTitle;
    private Text endStats;
    private Text pauseTitle;
    private Text fullscreenText;
    private Text settingsFullscreenText;
    private Text settingsResolutionText;
    private Text settingsFpsText;
    private Text settingsResetText;
    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Slider settingsMasterSlider;
    private Slider settingsMusicSlider;
    private Slider settingsSfxSlider;
    private bool resetConfirm;

    private Image hpFill;
    private Image shieldFill;
    private Image bossFill;
    private Image dashIcon;
    private Image dashCooldown;
    private Image flashImage;
    private Image fadeImage;
    private Image wipeImage;
    private Image bossGroup;
    private Image bannerBg;
    private Image grainImage;
    private Image lowHpImage;
    private RectTransform skipRoot;
    private Text skipText;
    private Button nextDungeonButton;
    private Button endRestartButton;
    private Button endMenuButton;

    private SlotView[] slots;
    private Coroutine bannerRoutine;
    private Coroutine flashRoutine;
    private Coroutine wipeRoutine;
    private Coroutine fadeRoutine;

    private bool perkMode;
    private bool skillMode;
    private bool restMode;
    private float readyPulse;

    public void Init()
    {
        art = Game.I.Art;
        BuildCanvas();
        BuildHud();
        BuildMenu();
        BuildPerkScreen();
        BuildPause();
        BuildEnd();
        BuildBanner();
        BuildOverlays();
        BuildEquipment();
        BuildSettings();
        Debug.Log("[Cardboard] UI ready");
    }

    private void BuildCanvas()
    {
        var go = new GameObject("UI");
        go.transform.SetParent(transform, false);
        Canvas = go.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.sortingOrder = 10;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.transform.SetParent(transform, false);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private RectTransform MakeRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot,
        Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private RectTransform FullRect(string name, Transform parent)
    {
        return MakeRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private CanvasGroup NewGroup(string name)
    {
        var rt = FullRect(name, Canvas.transform);
        var cg = rt.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        return cg;
    }

    private void SetGroup(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.gameObject.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    private Image Img(Transform parent, Sprite sprite, Color color, Vector2 aMin, Vector2 aMax, Vector2 pivot,
        Vector2 pos, Vector2 size, bool sliced = true, bool raycast = false)
    {
        var rt = MakeRect("img", parent, aMin, aMax, pivot, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        img.preserveAspect = false;
        if (sliced && sprite != null && sprite.border != Vector4.zero)
        {
            img.type = Image.Type.Sliced;
        }
        return img;
    }

    private Image FullImage(string name, Sprite sprite, Color color, bool raycast = false)
    {
        var rt = FullRect(name, Canvas.transform);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    private Text Label(RectTransform rt, string text, int size, Color color, TextAnchor anchor, Font font,
        bool shadow = true)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text = text;
        t.font = font != null ? font : art.Sans;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.lineSpacing = 1.05f;
        if (shadow)
        {
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.05f, 0.02f, 0.04f, 0.85f);
            sh.effectDistance = new Vector2(1.6f, -1.8f);
        }
        return t;
    }

    private Text TextAt(Transform parent, string text, int fontSize, Color color, TextAnchor anchor,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, Font font = null, bool shadow = true)
    {
        var rt = MakeRect("text", parent, aMin, aMax, pivot, pos, size);
        return Label(rt, text, fontSize, color, anchor, font, shadow);
    }

    private Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityAction action,
        int fontSize = 26, bool primary = true)
    {
        var rt = MakeRect("button_" + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = art.UI(primary ? "ui_button" : "ui_button_hover",
            primary ? new Vector4(24f, 24f, 24f, 24f) : new Vector4(24f, 24f, 24f, 24f));
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.raycastTarget = true;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        btn.onClick.AddListener(() =>
        {
            if (Game.I != null && Game.I.Audio != null) Game.I.Audio.Play("ui_click", 0.7f);
            action?.Invoke();
        });
        var t = TextAt(rt, label, fontSize, Pal.Cream, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, art.Serif, true);
        t.rectTransform.offsetMin = new Vector2(8f, 4f);
        t.rectTransform.offsetMax = new Vector2(-8f, -4f);
        var fx = rt.gameObject.AddComponent<UIButtonFx>();
        fx.Init(rt, img, primary ? art.UI("ui_button", new Vector4(24f, 24f, 24f, 24f))
            : art.UI("ui_button_hover", new Vector4(24f, 24f, 24f, 24f)));
        return btn;
    }

    // ------------------------------------------------------------------ меню
    private void BuildMenu()
    {
        menuGroup = NewGroup("Menu");
        menuRoot = menuGroup.GetComponent<RectTransform>();

        var backdrop = Img(menuRoot, art.UI("ui_menu_bg"), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
        backdrop.preserveAspect = false;
        backdrop.raycastTarget = true;

        menuDungeonTint = Img(menuRoot, art.UI("white"), new Color(0.2f, 0.2f, 0.3f, 0.08f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);

        var frame = Img(menuRoot, art.UI("ui_menu_frame"), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
        frame.raycastTarget = false;

        for (int i = 0; i < 5; i++)
        {
            var fog = Img(menuRoot, art.UI("fx_glow"), new Color(0.42f, 0.48f, 0.72f, 0.05f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-540f + i * 260f, -230f + (i % 2) * 80f), new Vector2(760f, 400f), false);
            var drift = fog.gameObject.AddComponent<UIDrift>();
            drift.Init(new Vector2(80f, 34f), 0.1f + i * 0.018f, i * 1.7f, 0f);
        }

        var logoGlow = Img(menuRoot, art.UI("fx_glow"), new Color(0.55f, 0.27f, 0.65f, 0.16f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -38f), new Vector2(980f, 430f), false);
        logoGlow.gameObject.AddComponent<UIPulseAlpha>().Init(0.12f, 0.24f, 0.42f);

        var rune = Img(menuRoot, art.UI("ui_rune"), new Color(0.82f, 0.65f, 0.34f, 0.12f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 30f), new Vector2(670f, 670f), false);
        rune.gameObject.AddComponent<UIRotate>().Speed = -3.8f;
        rune.gameObject.AddComponent<UIPulseAlpha>().Init(0.08f, 0.16f, 0.26f);

        menuHero = Img(menuRoot, art.UI("wizard"), new Color(1f, 1f, 1f, 0.16f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 8f), new Vector2(255f, 360f), false);
        menuHero.preserveAspect = true;
        menuHero.gameObject.AddComponent<UIFloat>().Init(new Vector2(0f, 5f), 0.38f, 1.2f);

        var logo = Img(menuRoot, art.UI("ui_logo"), Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f),
            new Vector2(780f, 189f), false);
        logo.preserveAspect = true;
        logo.gameObject.AddComponent<UIFloat>().Init(new Vector2(0f, 4.5f), 0.55f, 0f);
        logo.gameObject.AddComponent<UIPulseAlpha>().Init(0.9f, 1f, 0.5f);

        TextAt(menuRoot, "МРАЧНОЕ ПЕРЕИЗДАНИЕ  ·  v1.3.1", 20, new Color(0.84f, 0.74f, 0.58f),
            TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(760f, 30f), art.Sans, true);

        Img(menuRoot, art.UI("ui_divider"), new Color(1f, 1f, 1f, 0.85f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -272f), new Vector2(680f, 42f), false);

        TextAt(menuRoot, "ГЕРОЙ", 19, new Color(0.86f, 0.75f, 0.58f), TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-415f, 185f), new Vector2(380f, 28f), art.Serif, true);
        TextAt(menuRoot, "ПОДЗЕМЕЛЬЕ", 19, new Color(0.86f, 0.75f, 0.58f), TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(415f, 185f), new Vector2(380f, 28f), art.Serif, true);

        for (int i = 0; i < Data.HeroClasses.Count; i++)
        {
            var hero = Data.HeroClasses[i];
            var frameImg = MakeMenuCard(menuRoot, new Vector2(-415f, 120f - i * 104f), hero.Sprite,
                hero.Name, hero.Title, ClassFoot(hero), hero.Accent, new Color(0.82f, 0.78f, 0.7f),
                () => SelectClass(hero.Id), out var icon, out var accent);
            classFrames.Add(frameImg);
            classIcons.Add(icon);
            classAccents.Add(accent);
        }

        for (int i = 0; i < Data.Dungeons.Count; i++)
        {
            var dungeon = Data.Dungeons[i];
            string foot = "СЛОЖНОСТЬ: " + Data.DifficultyName(dungeon.Difficulty) +
                          " · ДОБЫЧА " + Mathf.RoundToInt(dungeon.DropChance * 100f) + "%";
            int dungeonIndex = i;
            var frameImg = MakeMenuCard(menuRoot, new Vector2(415f, 120f - i * 104f), "ui_dungeon_" + dungeon.Id,
                dungeon.Name, dungeon.Tagline, foot, dungeon.Accent, Data.DifficultyColor(dungeon.Difficulty),
                () => SelectDungeon(dungeonIndex), out var icon, out var accent);
            dungeonFrames.Add(frameImg);
            dungeonIcons.Add(icon);
            dungeonAccents.Add(accent);
            dungeonButtons.Add(frameImg.GetComponent<Button>());

            var lockBg = Img(frameImg.rectTransform, art.UI("white"), new Color(0.035f, 0.02f, 0.045f, 0.78f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
            TextAt(lockBg.rectTransform, "ЗАКРЫТО\n<size=11>победи в предыдущем</size>", 16,
                new Color(0.95f, 0.58f, 0.48f), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                art.Serif, true);
            lockBg.gameObject.SetActive(false);
            dungeonLocks.Add(lockBg.gameObject);
        }

        var summary = Img(menuRoot, art.UI("ui_card_dark", new Vector4(22f, 22f, 22f, 22f)),
            new Color(1f, 1f, 1f, 0.88f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(430f, 252f), true);
        summary.raycastTarget = false;

        menuClassTitle = TextAt(summary.rectTransform, "МАГ", 26, Pal.Gold, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f),
            new Vector2(390f, 34f), art.Serif, true);
        menuClassStats = TextAt(summary.rectTransform, "", 14, new Color(0.86f, 0.8f, 0.66f),
            TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -48f), new Vector2(390f, 22f), art.Sans, true);
        menuClassDesc = TextAt(summary.rectTransform, "", 13, new Color(0.78f, 0.74f, 0.7f),
            TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -72f), new Vector2(380f, 42f), art.Sans, true);
        Img(summary.rectTransform, art.UI("ui_divider"), new Color(1f, 1f, 1f, 0.55f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -122f),
            new Vector2(340f, 24f), false);
        menuDungeonTitle = TextAt(summary.rectTransform, "КОСТЯНОЙ СКЛЕП", 20, new Color(0.86f, 0.9f, 0.75f),
            TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -132f), new Vector2(390f, 26f), art.Serif, true);
        menuDungeonStats = TextAt(summary.rectTransform, "", 13, new Color(0.82f, 0.78f, 0.7f),
            TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -160f), new Vector2(390f, 22f), art.Sans, true);
        menuBest = TextAt(summary.rectTransform, "", 13, new Color(0.72f, 0.68f, 0.62f),
            TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -188f), new Vector2(390f, 36f), art.Sans, true);

        MakeButton(menuRoot, "СПУСТИТЬСЯ", new Vector2(0f, -232f), new Vector2(430f, 64f),
            () => Game.I.StartRun(), 29);

        MakeButton(menuRoot, "СНАРЯЖЕНИЕ", new Vector2(-255f, -314f), new Vector2(160f, 46f),
            ShowEquipment, 16, false);
        MakeButton(menuRoot, "КАК ИГРАТЬ", new Vector2(-85f, -314f), new Vector2(160f, 46f),
            ToggleHelp, 16, false);
        MakeButton(menuRoot, "НАСТРОЙКИ", new Vector2(85f, -314f), new Vector2(160f, 46f),
            ShowSettings, 16, false);
        MakeButton(menuRoot, "ВЫЙТИ", new Vector2(255f, -314f), new Vector2(160f, 46f),
            () => Game.I.QuitGame(), 16, false);

        for (int i = 0; i < 24; i++)
        {
            var ember = Img(menuRoot, art.UI("fx_ember"),
                i % 3 == 0 ? new Color(1f, 0.55f, 0.22f, 0.55f) : new Color(1f, 0.72f, 0.36f, 0.4f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-710f + (i * 83) % 1420, -430f + (i * 47) % 860),
                new Vector2(10f + (i % 4) * 3f, 10f + (i % 4) * 3f), false);
            ember.raycastTarget = false;
            ember.gameObject.AddComponent<UIMote>().Init(12f + (i % 5) * 5f, 0.8f + (i % 3) * 0.4f, i * 1.9f);
        }

        var controlsBg = Img(menuRoot, art.UI("ui_panel_dark", new Vector4(14f, 14f, 14f, 14f)),
            new Color(1f, 1f, 1f, 0.72f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1120f, 38f), true);
        controlsBg.raycastTarget = false;

        TextAt(menuRoot, "WASD / стрелки — движение   ·   автоатака   ·   1–5 — навыки   ·   SPACE — рывок   ·   ESC — пауза",
            18, new Color(0.8f, 0.74f, 0.64f, 0.95f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 33f),
            new Vector2(1120f, 28f), art.Sans, true);

        BuildHelp();
        RefreshMenuSelection();
    }

    private static string ClassFoot(HeroClassDef hero)
    {
        return "HP " + hero.Hp.ToString("0") + "  ·  УРОН " + hero.Damage.ToString("0.#") +
               "  ·  " + (hero.Style == HeroAttackStyle.Slash ? "БЛИЖНИЙ БОЙ" : "ДАЛЬНИЙ БОЙ");
    }

    private Image MakeMenuCard(Transform parent, Vector2 pos, string icon, string title, string sub, string foot,
        Color accent, Color footColor, UnityAction action, out Image iconImage, out Image accentImage)
    {
        var rt = MakeRect("menu_card_" + title, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), pos, new Vector2(380f, 92f));
        var bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = art.UI("ui_card_dark", new Vector4(18f, 18f, 18f, 18f));
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;
        bg.raycastTarget = true;

        accentImage = Img(rt, art.UI("white"), new Color(accent.r, accent.g, accent.b, 0.9f),
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f),
            new Vector2(5f, -14f), false);
        accentImage.rectTransform.offsetMin = new Vector2(2f, 14f);
        accentImage.rectTransform.offsetMax = new Vector2(7f, -14f);

        var iconRt = MakeRect("icon", rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(64f, 78f));
        iconImage = iconRt.gameObject.AddComponent<Image>();
        iconImage.sprite = art.UI(icon);
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var titleText = TextAt(rt, title, 20, new Color(0.94f, 0.87f, 0.74f), TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(94f, -10f),
            new Vector2(270f, 26f), art.Serif, true);
        titleText.color = accent;
        TextAt(rt, sub, 13, new Color(0.8f, 0.76f, 0.7f), TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(94f, -38f),
            new Vector2(272f, 20f), art.Sans, true);
        TextAt(rt, foot, 12, footColor, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(94f, -62f),
            new Vector2(272f, 20f), art.Sans, true);

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.None;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        btn.onClick.AddListener(() =>
        {
            if (Game.I != null && Game.I.Audio != null) Game.I.Audio.Play("ui_click", 0.7f);
            action?.Invoke();
        });
        var fx = rt.gameObject.AddComponent<UIButtonFx>();
        fx.Init(rt, bg, bg.sprite);
        return bg;
    }

    public void SelectClass(HeroClass hero)
    {
        MetaProgress.SelectedClass = hero;
        if (Game.I.Player != null)
        {
            Game.I.Player.SetClassVisual(hero);
            Game.I.Player.ShowInMenu();
        }
        RefreshMenuSelection();
        if (Game.I.Audio != null) Game.I.Audio.Play("ui_click", 0.55f);
    }

    public void ClickDungeonCard(int index)
    {
        if (index < 0 || index >= dungeonButtons.Count) return;
        dungeonButtons[index].onClick.Invoke();
    }

    public void SelectDungeon(int index)
    {
        if (index < 0 || index >= Data.Dungeons.Count) return;
        var dungeon = Data.Dungeons[index];
        if (dungeon == null) return;
        if ((Game.I == null || !Game.I.QA) && !MetaProgress.IsDungeonUnlocked(index))
        {
            string previous = index > 0 ? Data.Dungeons[index - 1].Name : "предыдущем подземелье";
            ShowBanner("ПУТЬ ЗАКРЫТ", "Сначала победи в: " + previous, 1.7f, Pal.Red);
            if (Game.I != null && Game.I.Audio != null) Game.I.Audio.Play("ui_click", 0.5f, 0.7f);
            return;
        }
        MetaProgress.SelectedDungeonId = dungeon.Id;
        if (Game.I.Rooms != null) Game.I.Rooms.RebuildMenuForSelection();
        RefreshMenuSelection();
        if (Game.I.Audio != null) Game.I.Audio.Play("ui_click", 0.55f);
    }

    private void RefreshMenuSelection()
    {
        if (menuRoot == null) return;
        var hero = MetaProgress.SelectedHero;
        var dungeon = MetaProgress.SelectedDungeon;

        for (int i = 0; i < classFrames.Count; i++)
        {
            bool selected = i == (int)MetaProgress.SelectedClass;
            classFrames[i].color = selected ? Color.white : new Color(0.58f, 0.56f, 0.62f, 0.92f);
            classIcons[i].color = selected ? Color.white : new Color(0.7f, 0.68f, 0.7f, 0.8f);
            classAccents[i].color = new Color(Data.HeroClasses[i].Accent.r, Data.HeroClasses[i].Accent.g,
                Data.HeroClasses[i].Accent.b, selected ? 1f : 0.25f);
        }
        for (int i = 0; i < dungeonFrames.Count; i++)
        {
            bool selected = Data.Dungeons[i].Id == dungeon.Id;
            bool unlocked = Game.I != null && Game.I.QA || MetaProgress.IsDungeonUnlocked(i);
            if (dungeonLocks.Count > i) dungeonLocks[i].SetActive(!unlocked);
            if (!unlocked)
            {
                dungeonFrames[i].color = new Color(0.42f, 0.4f, 0.45f, 0.9f);
                dungeonIcons[i].color = new Color(0.55f, 0.53f, 0.56f, 0.6f);
                dungeonAccents[i].color = new Color(Data.Dungeons[i].Accent.r, Data.Dungeons[i].Accent.g,
                    Data.Dungeons[i].Accent.b, 0.1f);
                continue;
            }
            dungeonFrames[i].color = selected ? Color.white : new Color(0.58f, 0.56f, 0.62f, 0.92f);
            dungeonIcons[i].color = selected ? Color.white : new Color(0.7f, 0.68f, 0.7f, 0.8f);
            dungeonAccents[i].color = new Color(Data.Dungeons[i].Accent.r, Data.Dungeons[i].Accent.g,
                Data.Dungeons[i].Accent.b, selected ? 1f : 0.25f);
        }

        if (menuHero != null)
        {
            menuHero.sprite = art.UI(hero.Sprite);
            menuHero.color = new Color(Data.Hero(hero.Id).Accent.r, Data.Hero(hero.Id).Accent.g,
                Data.Hero(hero.Id).Accent.b, 0.24f);
        }
        if (menuDungeonTint != null)
        {
            menuDungeonTint.color = new Color(dungeon.Accent.r, dungeon.Accent.g, dungeon.Accent.b, 0.11f);
        }
        if (menuClassTitle != null) menuClassTitle.text = hero.Name;
        if (menuClassStats != null)
        {
            menuClassStats.text = "HP " + hero.Hp.ToString("0") + "  ·  УРОН " + hero.Damage.ToString("0.#") +
                                  "  ·  " + (hero.Style == HeroAttackStyle.Slash ? "БЛИЖНИЙ БОЙ" : "ДАЛЬНИЙ БОЙ");
        }
        if (menuClassDesc != null) menuClassDesc.text = hero.Desc;
        if (menuDungeonTitle != null)
        {
            menuDungeonTitle.text = dungeon.Name;
            menuDungeonTitle.color = dungeon.Accent;
        }
        if (menuDungeonStats != null)
        {
            menuDungeonStats.text = Data.DifficultyName(dungeon.Difficulty) + "  ·  ШАНС ДОБЫЧИ " +
                                    Mathf.RoundToInt(dungeon.DropChance * 100f) + "%";
        }
        if (menuBest != null)
        {
            int best = MetaProgress.BestRoom(dungeon.Id);
            int kills = MetaProgress.BestKills(dungeon.Id);
            int clears = MetaProgress.Clears(dungeon.Id);
            menuBest.text = best > 0
                ? "ЛУЧШИЙ: КОМНАТА " + best + "  ·  УБИТО " + kills + "  ·  ПОБЕД " + clears
                : "ЭТО ПОДЗЕМЕЛЬЕ ЕЩЁ НЕ ПРОЙДЕНО";
        }
    }

    private void BuildHelp()
    {
        helpPanel = FullRect("Help", menuRoot);
        var blocker = helpPanel.gameObject.AddComponent<Image>();
        blocker.sprite = art.UI("white");
        blocker.color = new Color(0.02f, 0.01f, 0.03f, 0.72f);
        blocker.raycastTarget = true;

        var panelRt = MakeRect("HelpPanel", helpPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 600f));
        var bg = panelRt.gameObject.AddComponent<Image>();
        bg.sprite = art.UI("white");
        bg.type = Image.Type.Simple;
        bg.color = new Color(0.055f, 0.035f, 0.065f, 1f);
        bg.raycastTarget = true;
        Img(panelRt, art.UI("ui_panel", new Vector4(28f, 28f, 28f, 28f)), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true, false);

        TextAt(panelRt, "КАК ИГРАТЬ", 42, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f),
            new Vector2(600f, 60f), art.Serif, true);

        string body =
            "<color=#E8C16D>WASD / стрелки</color> — движение. Атака происходит сама.\n" +
            "Маг бьёт снарядами, мечник — широким размахом, рейнджер — быстрыми стрелами.\n\n" +
            "<color=#E8C16D>1 2 3 4 5</color> — активные навыки. Перезарядка видна на слотах.\n\n" +
            "<color=#E8C16D>SPACE</color> — рывок. Короткая неуязвимость.\n\n" +
            "<color=#E8C16D>ESC</color> — пауза, настройки громкости и выход в меню.\n\n" +
            "В меню выбери героя и одно из трёх подземелий. У каждого свой декор, враги и босс. " +
            "После победы снаряжение может выпасть — чем выше сложность, тем лучше награда. " +
            "Надетые предметы сохраняются между забегами.";

        TextAt(panelRt, body, 21, Pal.Cream, TextAnchor.UpperLeft,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f),
            new Vector2(-80f, -160f), art.Sans, true).rectTransform.anchoredPosition = new Vector2(0f, -18f);

        var back = MakeButton(panelRt, "НАЗАД", new Vector2(0f, 54f), new Vector2(240f, 56f),
            ToggleHelp, 24, false);
        back.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        back.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        back.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        back.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 28f);
        helpPanel.gameObject.SetActive(false);
    }

    private void ToggleHelp()
    {
        if (helpPanel == null) return;
        helpPanel.gameObject.SetActive(!helpPanel.gameObject.activeSelf);
    }

    public bool HelpOpen => helpPanel != null && helpPanel.gameObject.activeSelf;
    public bool EquipmentOpen => equipGroup != null && equipGroup.gameObject.activeSelf;
    public bool SettingsOpen => settingsGroup != null && settingsGroup.gameObject.activeSelf;
    public bool MenuOverlayOpen => HelpOpen || EquipmentOpen || SettingsOpen;
    public bool RestScreenOpen => restMode;
    public bool SkillScreenOpen => skillMode;

    public void ShowMenu()
    {
        if (helpPanel != null) helpPanel.gameObject.SetActive(false);
        SetGroup(equipGroup, false);
        SetGroup(settingsGroup, false);
        ResetLowHp();
        SetGroup(menuGroup, true);
        SetGroup(hudGroup, false);
        SetGroup(perkGroup, false);
        SetGroup(pauseGroup, false);
        SetGroup(endGroup, false);
        SetGroup(bannerGroup, false);
        RefreshMenuSelection();
        if (skipRoot != null) skipRoot.gameObject.SetActive(false);
    }

    public void HideMenu()
    {
        SetGroup(equipGroup, false);
        SetGroup(settingsGroup, false);
        SetGroup(menuGroup, false);
    }

    public void ShowHud()
    {
        SetGroup(hudGroup, true);
        hudGroup.blocksRaycasts = false;
    }

    public void HideHud()
    {
        SetGroup(hudGroup, false);
    }

    // ------------------------------------------------------------------ HUD
    private void BuildHud()
    {
        hudGroup = NewGroup("HUD");
        hudGroup.blocksRaycasts = false;
        var root = hudGroup.GetComponent<RectTransform>();

        var heart = Img(root, art.UI("ui_heart"), Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(30f, -28f), new Vector2(44f, 44f), false);
        heart.preserveAspect = true;

        var barBg = Img(root, art.UI("ui_bar_bg", new Vector4(10f, 10f, 10f, 10f)), Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -32f),
            new Vector2(300f, 26f), true);
        barBg.raycastTarget = false;

        var fillRt = MakeRect("fill", barBg.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero);
        fillRt.offsetMin = new Vector2(3f, 3f);
        fillRt.offsetMax = new Vector2(-3f, -3f);
        hpFill = fillRt.gameObject.AddComponent<Image>();
        hpFill.sprite = art.UI("ui_bar_fill", new Vector4(10f, 10f, 10f, 10f));
        hpFill.type = Image.Type.Sliced;
        hpFill.color = new Color(0.83f, 0.35f, 0.3f);
        hpFill.raycastTarget = false;

        var shieldRt = MakeRect("shield", barBg.transform, Vector2.zero, Vector2.one,
            new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        shieldRt.offsetMin = new Vector2(4f, 4f);
        shieldRt.offsetMax = new Vector2(-4f, -4f);
        shieldFill = shieldRt.gameObject.AddComponent<Image>();
        shieldFill.sprite = art.UI("white");
        shieldFill.color = new Color(0.48f, 0.78f, 1f, 0.9f);
        shieldFill.raycastTarget = false;
        shieldFill.enabled = false;

        hpText = TextAt(root, "120 / 120", 18, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(232f, -45f),
            new Vector2(300f, 24f), art.Sans, true);

        shieldText = TextAt(root, "", 16, new Color(0.55f, 0.8f, 1f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -60f),
            new Vector2(300f, 22f), art.Sans, true);

        roomText = TextAt(root, "КОМНАТА 1 / 12", 25, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f),
            new Vector2(800f, 34f), art.Serif, true);
        roomSub = TextAt(root, "", 17, new Color(0.84f, 0.78f, 0.68f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f),
            new Vector2(900f, 26f), art.Sans, true);

        var statsBg = Img(root, art.UI("ui_panel_dark", new Vector4(16f, 16f, 16f, 16f)),
            new Color(1f, 1f, 1f, 0.72f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-22f, -20f), new Vector2(248f, 64f), true);
        statsBg.raycastTarget = false;
        statsText = TextAt(root, "", 16, new Color(0.86f, 0.8f, 0.68f), TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -28f),
            new Vector2(220f, 52f), art.Sans, true);

        BuildBossBar(root);
        BuildSkillBar(root);
        BuildDash(root);

        skipRoot = MakeRect("skip", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-28f, 118f), new Vector2(360f, 30f));
        skipText = Label(skipRoot, "SPACE — ПРОПУСТИТЬ", 16, new Color(0.88f, 0.8f, 0.68f),
            TextAnchor.MiddleRight, art.Sans, true);
        skipRoot.gameObject.SetActive(false);
    }

    private void BuildBossBar(Transform root)
    {
        var bgRoot = MakeRect("boss", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(760f, 56f));
        bossGroup = bgRoot.gameObject.AddComponent<Image>();
        bossGroup.sprite = art.UI("ui_panel_dark", new Vector4(24f, 24f, 24f, 24f));
        bossGroup.type = Image.Type.Sliced;
        bossGroup.color = new Color(1f, 1f, 1f, 0.92f);
        bossGroup.raycastTarget = false;

        bossName = TextAt(bgRoot, "ЛИЧ-ПЕРЕПЛЁТ", 18, new Color(0.9f, 0.75f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f),
            new Vector2(680f, 24f), art.Serif, true);

        var bg = Img(bgRoot, art.UI("ui_bar_bg", new Vector4(10f, 10f, 10f, 10f)), Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f),
            new Vector2(720f, 20f), true);
        bg.raycastTarget = false;
        var fr = MakeRect("fill", bg.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero);
        fr.offsetMin = new Vector2(3f, 3f);
        fr.offsetMax = new Vector2(-3f, -3f);
        bossFill = fr.gameObject.AddComponent<Image>();
        bossFill.sprite = art.UI("ui_bar_fill", new Vector4(10f, 10f, 10f, 10f));
        bossFill.type = Image.Type.Sliced;
        bossFill.color = new Color(0.65f, 0.35f, 0.8f);
        bossFill.raycastTarget = false;
        bgRoot.gameObject.SetActive(false);
    }

    private void BuildSkillBar(Transform root)
    {
        var bar = MakeRect("skills", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(500f, 94f));
        slots = new SlotView[Data.MaxSkillSlots];
        float total = Data.MaxSkillSlots * 84f + (Data.MaxSkillSlots - 1) * 12f;
        for (int i = 0; i < Data.MaxSkillSlots; i++)
        {
            var sv = new SlotView();
            float x = -total * 0.5f + 42f + i * 96f;
            var slotRt = MakeRect("slot" + i, bar, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(x, 0f), new Vector2(84f, 84f));
            var bg = slotRt.gameObject.AddComponent<Image>();
            bg.sprite = art.UI("ui_slot");
            bg.color = Color.white;
            bg.raycastTarget = false;
            Img(slotRt, art.UI("white"), new Color(0.06f, 0.04f, 0.07f, 0.62f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -18f), false);
            sv.Root = slotRt;
            sv.Frame = bg;

            var iconRt = MakeRect("icon", slotRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(62f, 62f));
            sv.Icon = iconRt.gameObject.AddComponent<Image>();
            sv.Icon.raycastTarget = false;
            sv.Icon.preserveAspect = true;

            var cdRt = MakeRect("cd", slotRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            cdRt.offsetMin = new Vector2(5f, 5f);
            cdRt.offsetMax = new Vector2(-5f, -5f);
            sv.Cooldown = cdRt.gameObject.AddComponent<Image>();
            sv.Cooldown.sprite = art.UI("white");
            sv.Cooldown.color = new Color(0.02f, 0.01f, 0.03f, 0.68f);
            sv.Cooldown.type = Image.Type.Filled;
            sv.Cooldown.fillMethod = Image.FillMethod.Radial360;
            sv.Cooldown.fillOrigin = 2;
            sv.Cooldown.fillClockwise = true;
            sv.Cooldown.raycastTarget = false;
            sv.Cooldown.enabled = false;

            sv.Key = TextAt(slotRt, (i + 1).ToString(), 18, Pal.Gold, TextAnchor.LowerLeft,
                Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(7f, 4f), new Vector2(30f, 24f),
                art.Sans, true);
            sv.CooldownText = TextAt(slotRt, "", 22, Pal.Cream, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                art.Serif, true);
            sv.CooldownText.rectTransform.offsetMin = Vector2.zero;
            sv.CooldownText.rectTransform.offsetMax = Vector2.zero;
            slots[i] = sv;
        }
    }

    private void BuildDash(Transform root)
    {
        var dashRoot = MakeRect("dash", root, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 0f), new Vector2(30f, 34f), new Vector2(150f, 60f));
        var iconRt = MakeRect("icon", dashRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(48f, 48f));
        dashIcon = iconRt.gameObject.AddComponent<Image>();
        dashIcon.sprite = art.UI("icon_blink");
        dashIcon.raycastTarget = false;
        dashIcon.preserveAspect = true;

        dashCooldown = Img(iconRt, art.UI("white"), new Color(0.02f, 0.01f, 0.03f, 0.68f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
        dashCooldown.type = Image.Type.Filled;
        dashCooldown.fillMethod = Image.FillMethod.Radial360;
        dashCooldown.fillOrigin = 2;
        dashCooldown.fillClockwise = true;
        dashCooldown.rectTransform.offsetMin = new Vector2(4f, 4f);
        dashCooldown.rectTransform.offsetMax = new Vector2(-4f, -4f);

        dashText = TextAt(dashRoot, "SPACE\nРЫВОК", 15, Pal.Cream, TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(54f, 0f),
            new Vector2(100f, 50f), art.Sans, true);
    }

    public void RefreshRoom()
    {
        if (Game.I == null || Game.I.Run == null || Game.I.Rooms == null) return;
        var run = Game.I.Run;
        var room = Game.I.Rooms.CurrentRoom;
        if (roomText != null)
        {
            roomText.text = room == null ? "" : "КОМНАТА " + (run.RoomIndex + 1) + " / " + run.RoomCount;
            roomText.color = room == null ? Pal.Gold : Data.RoomAccent(room.Type);
        }
        if (roomSub != null)
        {
            roomSub.text = room == null ? "" : room.Name + "  ·  " + run.Dungeon.Name;
        }
    }

    public void RefreshSkills()
    {
        if (slots == null || Game.I == null || Game.I.Skills == null) return;
        var skills = Game.I.Skills;
        for (int i = 0; i < slots.Length; i++)
        {
            var sv = slots[i];
            var slot = skills.Slots[i];
            bool unlocked = i < skills.MaxSlots;
            if (slot.Def != null)
            {
                sv.Icon.sprite = art.UI(slot.Def.Icon);
                sv.Icon.color = Color.white;
                sv.Icon.enabled = true;
                sv.Key.color = Pal.Gold;
                sv.Frame.color = Color.white;
            }
            else if (unlocked)
            {
                sv.Icon.sprite = art.UI("icon_perk_slot");
                sv.Icon.color = new Color(1f, 1f, 1f, 0.3f);
                sv.Icon.enabled = true;
                sv.Key.color = new Color(1f, 1f, 1f, 0.35f);
                sv.Frame.color = new Color(1f, 1f, 1f, 0.55f);
            }
            else
            {
                sv.Icon.enabled = false;
                sv.Key.color = new Color(1f, 1f, 1f, 0.12f);
                sv.Frame.color = new Color(1f, 1f, 1f, 0.18f);
            }
            if (slot.Def == null)
            {
                sv.Cooldown.enabled = false;
                sv.CooldownText.text = "";
            }
        }
    }

    public void ShowBossBar(bool show, string name = null)
    {
        if (!string.IsNullOrEmpty(name)) currentBossName = name;
        if (bossGroup != null) bossGroup.gameObject.SetActive(show);
        if (show && bossName != null) bossName.text = currentBossName;
    }

    public void BossPhase()
    {
        if (bossName != null) bossName.text = currentBossName + "  ·  ФАЗА II";
        Flash(new Color(0.5f, 0.2f, 0.7f, 0.3f), 0.5f);
    }

    public void SetSkipHint(bool show)
    {
        if (skipRoot != null) skipRoot.gameObject.SetActive(show);
    }

    // ------------------------------------------------------------------ выбор перков и навыков
    private void BuildPerkScreen()
    {
        perkGroup = NewGroup("PerkScreen");
        var root = perkGroup.GetComponent<RectTransform>();
        Img(root, art.UI("white"), new Color(0.03f, 0.015f, 0.04f, 0.72f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false,
            true);

        perkTitle = TextAt(root, "ВЫБЕРИ ПЕРК", 48, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f),
            new Vector2(900f, 66f), art.Serif, true);
        perkHint = TextAt(root, "1, 2, 3 — выбрать карту", 20, new Color(0.85f, 0.78f, 0.66f),
            TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(700f, 28f), art.Sans, true);

        perkCardsRoot = MakeRect("Cards", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(1200f, 520f));
        restRoot = MakeRect("RestCards", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1000f, 500f));
    }

    public void ShowPerks(List<PerkDef> perks, string title)
    {
        SetGroup(perkGroup, true);
        perkMode = true;
        skillMode = false;
        restMode = false;
        CurrentPerks = perks ?? new List<PerkDef>();
        CurrentSkills.Clear();
        perkTitle.text = title;
        perkTitle.color = Pal.Gold;
        ClearChildren(perkCardsRoot);
        ClearChildren(restRoot);
        perkCardsRoot.gameObject.SetActive(true);
        restRoot.gameObject.SetActive(false);
        SetChoiceHint(CurrentPerks.Count, "выбрать карту");
        for (int i = 0; i < CurrentPerks.Count; i++)
        {
            int idx = i;
            var def = CurrentPerks[i];
            float x = (i - (CurrentPerks.Count - 1) * 0.5f) * 350f;
            BuildCard(perkCardsRoot, new Vector2(x, 0f), def.Icon, def.Name, def.Desc,
                Pal.Gold, (i + 1).ToString(), def.Unique ? "УНИКАЛЬНЫЙ" : "ПЕРК",
                () => ChoosePerkCard(idx));
        }
    }

    public void ShowSkills(List<SkillDef> skills, string title)
    {
        SetGroup(perkGroup, true);
        perkMode = false;
        skillMode = true;
        restMode = false;
        CurrentSkills = skills ?? new List<SkillDef>();
        CurrentPerks.Clear();
        perkTitle.text = title;
        perkTitle.color = Pal.Teal;
        ClearChildren(perkCardsRoot);
        ClearChildren(restRoot);
        perkCardsRoot.gameObject.SetActive(true);
        restRoot.gameObject.SetActive(false);
        SetChoiceHint(CurrentSkills.Count, "выбрать навык");
        for (int i = 0; i < CurrentSkills.Count; i++)
        {
            int idx = i;
            var def = CurrentSkills[i];
            float x = (i - (CurrentSkills.Count - 1) * 0.5f) * 350f;
            float cooldown = Data.EffectiveCooldown(def, MetaProgress.SelectedClass);
            BuildCard(perkCardsRoot, new Vector2(x, 0f), def.Icon, def.Name,
                def.Desc + "\n<color=#BFAF96>Перезарядка: " + cooldown.ToString("0") + " сек.</color>",
                def.Color, (i + 1).ToString(), "НАВЫК",
                () => ChooseSkillCard(idx));
        }
    }

    public void ShowTreasure(List<PerkDef> perks)
    {
        ShowPerks(perks, "СОКРОВИЩНИЦА");
    }

    public void ShowRest()
    {
        SetGroup(perkGroup, true);
        perkMode = false;
        skillMode = false;
        restMode = true;
        perkTitle.text = "ПРИВАЛ";
        perkTitle.color = Pal.Green;
        ClearChildren(perkCardsRoot);
        ClearChildren(restRoot);
        perkCardsRoot.gameObject.SetActive(false);
        restRoot.gameObject.SetActive(true);
        SetChoiceHint(2, "выбрать карту");

        BuildCard(restRoot, new Vector2(-220f, 0f), "icon_heal", "ОТДОХНУТЬ",
            "Восстановить 28% максимального здоровья.\n\nБезопасно и просто.",
            Pal.Green, "1", "ЛЕЧЕНИЕ", () => Game.I.ChooseRest(true));
        BuildCard(restRoot, new Vector2(220f, 0f), "icon_perk_slot", "ПЕРК НА ВЫБОР",
            "Выбрать один из трёх перков и стать сильнее.\n\nРискованно, но полезно.",
            Pal.Gold, "2", "УСИЛЕНИЕ", () => Game.I.ChooseRest(false));
    }


    private void SetChoiceHint(int count, string noun)
    {
        if (perkHint == null) return;
        if (count <= 1)
        {
            perkHint.text = "1 — " + noun;
            return;
        }
        string keys = "";
        for (int i = 0; i < count; i++)
        {
            if (i > 0) keys += ", ";
            keys += (i + 1).ToString();
        }
        perkHint.text = keys + " — " + noun;
    }

    private void BuildCard(Transform parent, Vector2 pos, string iconName, string title, string desc,
        Color accent, string key, string badge, Action action)
    {
        var glowColor = new Color(accent.r, accent.g, accent.b, 0.24f);
        var glow = Img(parent, art.UI("fx_glow"), glowColor, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(360f, 440f), false);
        glow.preserveAspect = true;

        var cardRt = MakeRect("card", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), pos, new Vector2(300f, 400f));
        var bg = cardRt.gameObject.AddComponent<Image>();
        bg.sprite = art.UI("white");
        bg.type = Image.Type.Simple;
        bg.color = new Color(0.075f, 0.05f, 0.07f, 0.97f);
        bg.raycastTarget = true;
        Img(cardRt, art.UI("ui_card"), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true, false);

        var iconRt = MakeRect("icon", cardRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -86f), new Vector2(92f, 92f));
        var icon = iconRt.gameObject.AddComponent<Image>();
        icon.sprite = art.UI(iconName);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var badgeText = TextAt(cardRt, badge, 13, accent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f),
            new Vector2(200f, 22f), art.Sans, true);
        badgeText.fontStyle = FontStyle.Bold;

        var titleText = TextAt(cardRt, title, 21, Pal.Cream, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -154f),
            new Vector2(276f, 66f), art.Serif, true);
        titleText.fontStyle = FontStyle.Bold;
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 14;
        titleText.resizeTextMaxSize = 21;

        var descText = TextAt(cardRt, desc, 18, new Color(0.86f, 0.8f, 0.7f), TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f),
            new Vector2(252f, 120f), art.Sans, true);
        descText.lineSpacing = 1.08f;

        var keyText = TextAt(cardRt, key, 27, accent, TextAnchor.MiddleCenter,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 22f),
            new Vector2(48f, 48f), art.Serif, true);
        keyText.fontStyle = FontStyle.Bold;

        var view = cardRt.gameObject.AddComponent<CardView>();
        view.Init(cardRt, glow, pos, action);
    }

    private void ClearChildren(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var go = root.GetChild(i).gameObject;
            go.SetActive(false);
            Destroy(go);
        }
    }

    private void ChoosePerkCard(int index)
    {
        if (CurrentPerks == null || index < 0 || index >= CurrentPerks.Count) return;
        Game.I.ChoosePerk(index);
    }

    private void ChooseSkillCard(int index)
    {
        if (CurrentSkills == null || index < 0 || index >= CurrentSkills.Count) return;
        Game.I.ChooseSkill(CurrentSkills[index].Id);
    }

    public void HidePerks()
    {
        SetGroup(perkGroup, false);
        perkMode = false;
        ClearChildren(perkCardsRoot);
        ClearChildren(restRoot);
    }

    public void HideSkills()
    {
        SetGroup(perkGroup, false);
        skillMode = false;
        ClearChildren(perkCardsRoot);
        ClearChildren(restRoot);
    }

    public void HideRest()
    {
        SetGroup(perkGroup, false);
        restMode = false;
        ClearChildren(perkCardsRoot);
        ClearChildren(restRoot);
    }

    // ------------------------------------------------------------------ пауза
    private void BuildPause()
    {
        pauseGroup = NewGroup("Pause");
        var root = pauseGroup.GetComponent<RectTransform>();
        Img(root, art.UI("white"), new Color(0.03f, 0.015f, 0.04f, 0.7f), Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false, true);

        pausePanel = MakeRect("PausePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580f, 700f));
        var panel = pausePanel.gameObject.AddComponent<Image>();
        panel.sprite = art.UI("white");
        panel.type = Image.Type.Simple;
        panel.color = new Color(0.06f, 0.04f, 0.07f, 1f);
        panel.raycastTarget = true;
        Img(pausePanel, art.UI("ui_panel", new Vector4(28f, 28f, 28f, 28f)), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true, false);

        pauseTitle = TextAt(pausePanel, "ПАУЗА", 48, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f),
            new Vector2(400f, 60f), art.Serif, true);

        MakeButton(pausePanel, "ПРОДОЛЖИТЬ", new Vector2(0f, 120f), new Vector2(360f, 60f),
            () => Game.I.ResumeGame(), 25);
        MakeButton(pausePanel, "ЗАНОВО", new Vector2(0f, 48f), new Vector2(360f, 56f),
            () => { Game.I.ResumeGame(); Game.I.RestartRun(); }, 24, false);
        MakeButton(pausePanel, "В МЕНЮ", new Vector2(0f, -22f), new Vector2(360f, 56f),
            () => { Time.timeScale = 1f; Game.I.ToMenu(); }, 24, false);

        masterSlider = AddSlider(pausePanel, "ОБЩАЯ ГРОМКОСТЬ", -110f, 0.8f);
        musicSlider = AddSlider(pausePanel, "МУЗЫКА", -178f, 0.6f);
        sfxSlider = AddSlider(pausePanel, "ЗВУКИ", -246f, 0.85f);
        masterSlider.onValueChanged.AddListener(_ => ApplyVolumes());
        musicSlider.onValueChanged.AddListener(_ => ApplyVolumes());
        sfxSlider.onValueChanged.AddListener(_ => ApplyVolumes());

        var fsBtn = MakeButton(pausePanel, "ЭКРАН: ОКНО", new Vector2(-88f, -304f), new Vector2(172f, 46f),
            ToggleFullscreen, 17, false);
        fullscreenText = fsBtn.GetComponentInChildren<Text>();
        MakeButton(pausePanel, "НАСТРОЙКИ", new Vector2(88f, -304f), new Vector2(172f, 46f),
            ShowSettings, 17, false);
    }

    private Slider AddSlider(Transform parent, string label, float y, float value)
    {
        TextAt(parent, label, 17, new Color(0.86f, 0.79f, 0.68f), TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(-200f, y + 31f), new Vector2(400f, 22f), art.Sans, true);

        var root = MakeRect("slider", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(400f, 26f));
        var bg = root.gameObject.AddComponent<Image>();
        bg.sprite = art.UI("ui_bar_bg", new Vector4(10f, 10f, 10f, 10f));
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;
        bg.raycastTarget = true;

        var fillArea = MakeRect("Fill Area", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 12f));
        fillArea.offsetMin = new Vector2(8f, -6f);
        fillArea.offsetMax = new Vector2(-8f, 6f);
        var fill = Img(fillArea, art.UI("ui_bar_fill", new Vector4(10f, 10f, 10f, 10f)),
            new Color(0.91f, 0.72f, 0.42f), Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero, true);

        var handleArea = MakeRect("Handle Slide Area", root, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f));
        var handle = Img(handleArea, art.UI("white"), Pal.Cream, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 30f), false);
        handle.raycastTarget = true;

        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        return slider;
    }

    private void ApplyVolumes()
    {
        Game.I.Audio.SetVolumes(masterSlider.value, musicSlider.value, sfxSlider.value);
    }

    private void ToggleFullscreen()
    {
        ScreenSettings.SetFullscreen(!ScreenSettings.Fullscreen);
        UpdateScreenLabels();
    }

    public void ShowPause()
    {
        ResetLowHp();
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(Game.I.Audio.Master);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(Game.I.Audio.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(Game.I.Audio.SfxVolume);
        UpdateScreenLabels();
        SetGroup(pauseGroup, true);
    }

    public void HidePause()
    {
        SetGroup(settingsGroup, false);
        SetGroup(pauseGroup, false);
    }

    // ------------------------------------------------------------------ конец забега
    private void BuildEnd()
    {
        endGroup = NewGroup("End");
        var root = endGroup.GetComponent<RectTransform>();
        Img(root, art.UI("white"), new Color(0.03f, 0.015f, 0.04f, 0.72f), Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false, true);

        endPanel = MakeRect("EndPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 840f));
        var panel = endPanel.gameObject.AddComponent<Image>();
        panel.sprite = art.UI("white");
        panel.type = Image.Type.Simple;
        panel.color = new Color(0.06f, 0.04f, 0.07f, 1f);
        panel.raycastTarget = true;
        Img(endPanel, art.UI("ui_panel", new Vector4(28f, 28f, 28f, 28f)), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true, false);

        endTitle = TextAt(endPanel, "ПОБЕДА", 58, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f),
            new Vector2(660f, 70f), art.Serif, true);

        endStats = TextAt(endPanel, "", 19, Pal.Cream, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f),
            new Vector2(680f, 180f), art.Sans, true);

        endLoot = TextAt(endPanel, "", 16, Pal.Cream, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -288f),
            new Vector2(700f, 66f), art.Sans, true);

        endLootCard = MakeRect("LootCard", endPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(560f, 300f));
        var cardBg = endLootCard.gameObject.AddComponent<Image>();
        cardBg.sprite = art.UI("white");
        cardBg.type = Image.Type.Simple;
        cardBg.color = new Color(0.05f, 0.035f, 0.055f, 0.98f);
        cardBg.raycastTarget = false;
        endLootFrame = Img(endLootCard, art.UI("ui_panel", new Vector4(24f, 24f, 24f, 24f)),
            new Color(0.55f, 0.45f, 0.34f, 0.9f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, true, false);
        endLootGlow = Img(endLootCard, art.UI("fx_glow"), new Color(0.85f, 0.65f, 0.3f, 0.16f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-185f, 10f), new Vector2(250f, 250f), false, false);
        endLootGlow.preserveAspect = true;
        Img(endLootCard, art.UI("ui_panel", new Vector4(18f, 18f, 18f, 18f)),
            new Color(0.02f, 0.012f, 0.03f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-185f, 10f), new Vector2(150f, 150f), true, false);
        var lootIconRt = MakeRect("icon", endLootCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-185f, 10f), new Vector2(116f, 116f));
        endLootIcon = lootIconRt.gameObject.AddComponent<Image>();
        endLootIcon.preserveAspect = true;
        endLootIcon.raycastTarget = false;

        endLootBadge = TextAt(endLootCard, "НОВОЕ СНАРЯЖЕНИЕ", 13, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(65f, 116f), new Vector2(340f, 24f), art.Sans, true);
        endLootBadge.fontStyle = FontStyle.Bold;

        endLootName = TextAt(endLootCard, "", 24, Pal.Cream, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(65f, 76f), new Vector2(340f, 46f), art.Serif, true);
        endLootName.fontStyle = FontStyle.Bold;
        endLootName.resizeTextForBestFit = true;
        endLootName.resizeTextMinSize = 16;
        endLootName.resizeTextMaxSize = 24;

        endLootMeta = TextAt(endLootCard, "", 15, new Color(0.78f, 0.72f, 0.63f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(65f, 42f), new Vector2(340f, 26f), art.Sans, true);

        endLootStats = TextAt(endLootCard, "", 17, Pal.Cream, TextAnchor.UpperCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(65f, -32f), new Vector2(340f, 94f), art.Sans, true);
        endLootStats.lineSpacing = 1.1f;

        nextDungeonButton = MakeButton(endPanel, "СЛЕДУЮЩЕЕ", new Vector2(-260f, -378f),
            new Vector2(240f, 48f), () => Game.I.StartNextDungeon(), 21);
        endRestartButton = MakeButton(endPanel, "ЕЩЁ РАЗ", new Vector2(0f, -378f),
            new Vector2(200f, 48f), () => Game.I.RestartRun(), 21, false);
        endMenuButton = MakeButton(endPanel, "В МЕНЮ", new Vector2(260f, -378f),
            new Vector2(200f, 48f), () => { Time.timeScale = 1f; Game.I.ToMenu(); }, 20, false);
    }

    public void ShowEnd(bool victory, LootRoll loot = null)
    {
        ResetLowHp();
        SetGroup(hudGroup, false);
        SetGroup(endGroup, true);
        endTitle.text = victory ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
        endTitle.color = victory ? Pal.Gold : new Color(0.85f, 0.3f, 0.28f);
        var run = Game.I.Run;
        string time = FormatTime(run.RunTime);
        var dungeon = run.Dungeon;
        var next = victory ? MetaProgress.NextDungeon(dungeon) : null;
        bool hasNext = next != null;
        endStats.text =
            (victory
                ? "БОСС ПОВЕРЖЕН. " + (dungeon != null ? dungeon.Name : "ПОДЗЕМЕЛЬЕ") + " затихло."
                : "ГЕРОЙ ПАЛ. Но лист можно перевернуть.") +
            "\n<color=#E8C16D>Комнат пройдено:</color> " + (run.RoomIndex + (victory ? 1 : 0)) +
            " / " + run.RoomCount +
            "\n<color=#E8C16D>Убито врагов:</color> " + run.Kills +
            "\n<color=#E8C16D>Нанесено урона:</color> " + Mathf.RoundToInt(run.DamageDealt) +
            "\n<color=#E8C16D>Время:</color> " + time +
            "\n<color=#E8C16D>Лучший забег:</color> комната " + Mathf.Max(1, run.BestRoom) +
            " · убито " + run.BestKills +
            (victory
                ? hasNext
                    ? "\n<color=#8FD3C7>ОТКРЫТО:</color> " + next.Name + "  ·  N — идти дальше"
                    : "\n<color=#8FD3C7>ВСЕ ПОДЗЕМЕЛЬЯ ПРОЙДЕНЫ</color>"
                : "");

        if (nextDungeonButton != null) nextDungeonButton.gameObject.SetActive(hasNext);
        if (endRestartButton != null)
        {
            SetEndButtonX(endRestartButton, hasNext ? 0f : -110f);
        }
        if (endMenuButton != null)
        {
            SetEndButtonX(endMenuButton, hasNext ? 260f : 110f);
        }

        if (endLoot != null) endLoot.text = "";
        ShowEndLoot(victory, loot, dungeon);
    }

    private static void SetEndButtonX(Button button, float x)
    {
        if (button == null) return;
        var rt = button.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
    }

    private void ShowEndLoot(bool victory, LootRoll loot, DungeonDef dungeon)
    {
        if (endLootCard == null) return;
        endLootCard.gameObject.SetActive(true);
        endLootIcon.color = Color.white;
        endLootName.color = Pal.Cream;
        endLootBadge.color = Pal.Gold;
        endLootGlow.color = new Color(0.85f, 0.65f, 0.3f, 0.16f);
        endLootFrame.color = new Color(0.55f, 0.45f, 0.34f, 0.9f);
        endLootMeta.color = new Color(0.78f, 0.72f, 0.63f);
        endLootStats.color = Pal.Cream;

        if (loot != null && loot.Item != null)
        {
            var item = loot.Item;
            var col = Data.RarityColor(item.Rarity);
            endLootBadge.text = "НОВОЕ СНАРЯЖЕНИЕ";
            endLootBadge.color = col;
            endLootName.text = item.Name;
            endLootName.color = col;
            endLootMeta.text = Data.RarityName(item.Rarity) + " · " + Data.SlotName(item.Slot);
            endLootStats.text = item.StatLine();
            endLootIcon.sprite = art.UI(item.Icon);
            endLootGlow.color = new Color(col.r, col.g, col.b, 0.24f);
            endLootFrame.color = new Color(col.r, col.g, col.b, 0.85f);
            return;
        }

        endLootIcon.sprite = art.UI("prop_chest");
        if (victory && loot != null && loot.AllOwned)
        {
            endLootBadge.text = "АРСЕНАЛ СОБРАН";
            endLootBadge.color = Pal.Gold;
            endLootName.text = "ВСЁ УЖЕ ТВОЁ";
            endLootName.color = Pal.Gold;
            endLootMeta.text = "ВСЁ СНАРЯЖЕНИЕ ЭТОГО УРОВНЯ";
            endLootStats.text = "Повышай сложность, чтобы найти более редкие вещи.";
            endLootIcon.color = new Color(0.9f, 0.8f, 0.55f, 0.85f);
            return;
        }

        if (victory)
        {
            int chance = dungeon != null ? Mathf.RoundToInt(dungeon.DropChance * 100f) : 0;
            endLootBadge.text = "БЕЗ ДОБЫЧИ";
            endLootBadge.color = new Color(0.7f, 0.64f, 0.55f);
            endLootName.text = "СУНДУК ПУСТ";
            endLootName.color = new Color(0.82f, 0.76f, 0.66f);
            endLootMeta.text = "ШАНС ДОБЫЧИ БЫЛ " + chance + "%";
            endLootStats.text = "Соберись с силами и попробуй ещё.\nДобыча не гарантирована.";
            endLootIcon.color = new Color(0.5f, 0.46f, 0.42f, 0.65f);
            endLootGlow.color = new Color(0.4f, 0.35f, 0.3f, 0.08f);
            return;
        }

        endLootBadge.text = "ТОЛЬКО ЗА ПОБЕДУ";
        endLootBadge.color = new Color(0.85f, 0.3f, 0.28f);
        endLootName.text = "СНАРЯЖЕНИЕ ЖДЁТ";
        endLootName.color = new Color(0.85f, 0.3f, 0.28f);
        endLootMeta.text = "ПОБЕДИ БОССА";
        endLootStats.text = "Добыча достаётся только тем, кто доходит до конца подземелья.";
        endLootIcon.color = new Color(0.45f, 0.4f, 0.38f, 0.55f);
        endLootGlow.color = new Color(0.4f, 0.2f, 0.2f, 0.07f);
    }

    public void HideEnd()
    {
        SetGroup(endGroup, false);
    }

    // ------------------------------------------------------------------ настройки
    private void BuildSettings()
    {
        settingsGroup = NewGroup("Settings");
        var root = settingsGroup.GetComponent<RectTransform>();
        Img(root, art.UI("white"), new Color(0.02f, 0.01f, 0.03f, 0.78f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false, true);

        var panel = MakeRect("SettingsPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 780f));
        var bg = panel.gameObject.AddComponent<Image>();
        bg.sprite = art.UI("white");
        bg.type = Image.Type.Simple;
        bg.color = new Color(0.055f, 0.035f, 0.065f, 1f);
        bg.raycastTarget = true;
        Img(panel, art.UI("ui_panel", new Vector4(28f, 28f, 28f, 28f)), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true, false);

        TextAt(panel, "НАСТРОЙКИ", 44, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f),
            new Vector2(600f, 56f), art.Serif, true);
        settingsMasterSlider = AddSlider(panel, "ОБЩАЯ ГРОМКОСТЬ", 235f, 0.8f);
        settingsMusicSlider = AddSlider(panel, "МУЗЫКА", 165f, 0.6f);
        settingsSfxSlider = AddSlider(panel, "ЗВУКИ", 95f, 0.85f);
        settingsMasterSlider.onValueChanged.AddListener(_ => ApplySettingsVolumes());
        settingsMusicSlider.onValueChanged.AddListener(_ => ApplySettingsVolumes());
        settingsSfxSlider.onValueChanged.AddListener(_ => ApplySettingsVolumes());

        TextAt(panel, "ЭКРАН", 18, new Color(0.86f, 0.75f, 0.58f), TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(-220f, 22f), new Vector2(430f, 24f), art.Sans, true);

        var fullBtn = MakeButton(panel, "ЭКРАН: ОКНО", new Vector2(0f, -20f), new Vector2(440f, 44f),
            ToggleFullscreen, 18, false);
        settingsFullscreenText = fullBtn.GetComponentInChildren<Text>();

        var resBtn = MakeButton(panel, "РАЗРЕШЕНИЕ: 1600×900", new Vector2(0f, -76f), new Vector2(440f, 44f),
            CycleResolution, 18, false);
        settingsResolutionText = resBtn.GetComponentInChildren<Text>();

        var fpsBtn = MakeButton(panel, "ЧАСТОТА: 60 FPS", new Vector2(0f, -132f), new Vector2(440f, 44f),
            CycleFps, 18, false);
        settingsFpsText = fpsBtn.GetComponentInChildren<Text>();

        var resetBtn = MakeButton(panel, "СБРОСИТЬ ПРОГРЕСС", new Vector2(0f, -222f), new Vector2(440f, 44f),
            ResetProgressClicked, 17, false);
        settingsResetText = resetBtn.GetComponentInChildren<Text>();

        TextAt(panel, "Настройки сохраняются автоматически. Сброс прогресса удалит лут и рекорды.",
            14, new Color(0.7f, 0.66f, 0.6f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 82f),
            new Vector2(600f, 32f), art.Sans, true);

        MakeButton(panel, "ЗАКРЫТЬ", new Vector2(0f, -330f), new Vector2(240f, 46f),
            HideSettings, 21, false);

        SetGroup(settingsGroup, false);
        UpdateScreenLabels();
    }

    public void ShowSettings()
    {
        if (helpPanel != null) helpPanel.gameObject.SetActive(false);
        SetGroup(equipGroup, false);
        resetConfirm = false;
        UpdateResetLabel();
        if (settingsMasterSlider != null) settingsMasterSlider.SetValueWithoutNotify(Game.I.Audio.Master);
        if (settingsMusicSlider != null) settingsMusicSlider.SetValueWithoutNotify(Game.I.Audio.MusicVolume);
        if (settingsSfxSlider != null) settingsSfxSlider.SetValueWithoutNotify(Game.I.Audio.SfxVolume);
        UpdateScreenLabels();
        SetGroup(settingsGroup, true);
    }

    public void HideSettings()
    {
        resetConfirm = false;
        UpdateResetLabel();
        SetGroup(settingsGroup, false);
    }

    private void ApplySettingsVolumes()
    {
        if (settingsMasterSlider == null || settingsMusicSlider == null || settingsSfxSlider == null) return;
        Game.I.Audio.SetVolumes(settingsMasterSlider.value, settingsMusicSlider.value, settingsSfxSlider.value);
    }

    private void CycleResolution()
    {
        int next = (ScreenSettings.ResolutionIndex + 1) % ScreenSettings.Resolutions.Length;
        ScreenSettings.SetResolution(next);
        UpdateScreenLabels();
    }

    private void CycleFps()
    {
        int next = (ScreenSettings.FpsMode + 1) % ScreenSettings.FpsNames.Length;
        ScreenSettings.SetFps(next);
        UpdateScreenLabels();
    }

    private void ResetProgressClicked()
    {
        if (!resetConfirm)
        {
            resetConfirm = true;
            UpdateResetLabel();
            return;
        }
        resetConfirm = false;
        MetaProgress.ResetProgress();
        Game.I.ToMenu();
        ShowSettings();
    }

    private void UpdateResetLabel()
    {
        if (settingsResetText != null)
        {
            settingsResetText.text = resetConfirm ? "ТОЧНО СБРОСИТЬ?" : "СБРОСИТЬ ПРОГРЕСС";
        }
    }

    private void UpdateScreenLabels()
    {
        if (fullscreenText != null)
        {
            fullscreenText.text = ScreenSettings.Fullscreen ? "ЭКРАН: ПОЛНЫЙ" : "ЭКРАН: ОКНО";
        }
        if (settingsFullscreenText != null)
        {
            settingsFullscreenText.text = ScreenSettings.Fullscreen ? "ЭКРАН: ПОЛНЫЙ" : "ЭКРАН: ОКНО";
        }
        if (settingsResolutionText != null)
        {
            settingsResolutionText.text = "РАЗРЕШЕНИЕ: " + ScreenSettings.ResolutionName(ScreenSettings.ResolutionIndex);
        }
        if (settingsFpsText != null)
        {
            settingsFpsText.text = "ЧАСТОТА: " + ScreenSettings.FpsNames[ScreenSettings.FpsMode];
        }
    }

    private void BuildEquipment()
    {
        equipGroup = NewGroup("Equipment");
        var root = equipGroup.GetComponent<RectTransform>();
        Img(root, art.UI("white"), new Color(0.02f, 0.01f, 0.03f, 0.8f), Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false, true);

        var panel = MakeRect("EquipPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 780f));
        var bg = panel.gameObject.AddComponent<Image>();
        bg.sprite = art.UI("white");
        bg.type = Image.Type.Simple;
        bg.color = new Color(0.055f, 0.035f, 0.065f, 1f);
        bg.raycastTarget = true;
        Img(panel, art.UI("ui_panel", new Vector4(28f, 28f, 28f, 28f)), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true, false);

        TextAt(panel, "СНАРЯЖЕНИЕ", 44, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f),
            new Vector2(700f, 54f), art.Serif, true);
        TextAt(panel, "НАДЕТЫЕ ПРЕДМЕТЫ СОХРАНЯЮТСЯ МЕЖДУ ЗАБЕГАМИ", 14,
            new Color(0.75f, 0.7f, 0.62f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f),
            new Vector2(700f, 24f), art.Sans, true);

        for (int i = 0; i < 3; i++)
        {
            var slot = (EquipSlot)i;
            float x = -360f + i * 360f;
            var slotRt = MakeRect("equip_slot_" + slot, panel, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 185f),
                new Vector2(320f, 146f));
            var slotBg = slotRt.gameObject.AddComponent<Image>();
            slotBg.sprite = art.UI("ui_card_dark", new Vector4(18f, 18f, 18f, 18f));
            slotBg.type = Image.Type.Sliced;
            slotBg.color = new Color(1f, 1f, 1f, 0.9f);
            slotBg.raycastTarget = false;

            TextAt(slotRt, Data.SlotName(slot), 14, new Color(0.8f, 0.7f, 0.55f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -12f),
                new Vector2(180f, 22f), art.Sans, true);

            var iconRt = MakeRect("icon", slotRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(18f, -10f), new Vector2(64f, 64f));
            equipSlotIcons[i] = iconRt.gameObject.AddComponent<Image>();
            equipSlotIcons[i].preserveAspect = true;
            equipSlotIcons[i].raycastTarget = false;

            equipSlotNames[i] = TextAt(slotRt, "ПУСТО", 16, new Color(0.7f, 0.66f, 0.6f),
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(92f, -38f), new Vector2(210f, 24f), art.Serif, true);
            equipSlotStats[i] = TextAt(slotRt, "", 11, new Color(0.75f, 0.7f, 0.64f),
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(92f, -64f), new Vector2(212f, 40f), art.Sans, true);

            var unequip = MakeButton(slotRt, "СНЯТЬ", new Vector2(112f, -50f), new Vector2(88f, 30f),
                () =>
                {
                    MetaProgress.Unequip(slot);
                    RefreshEquipment();
                }, 13, false);
            unequip.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0f);
            unequip.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0f);
            unequip.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);
            unequip.GetComponent<RectTransform>().anchoredPosition = new Vector2(-14f, 12f);
        }

        TextAt(panel, "НАЙДЕННЫЕ ПРЕДМЕТЫ", 20, Pal.Gold, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-340f, 82f), new Vector2(360f, 28f), art.Serif, true);
        equipHint = TextAt(panel, "", 13, new Color(0.72f, 0.68f, 0.62f), TextAnchor.MiddleRight,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(340f, 82f), new Vector2(380f, 24f), art.Sans, true);

        equipItemsRoot = MakeRect("Items", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -108f), new Vector2(1020f, 364f));

        MakeButton(panel, "ЗАКРЫТЬ", new Vector2(0f, -342f), new Vector2(220f, 46f),
            HideEquipment, 21, false);

        SetGroup(equipGroup, false);
    }

    public void ShowEquipment()
    {
        RefreshEquipment();
        SetGroup(equipGroup, true);
    }

    public void HideEquipment()
    {
        SetGroup(equipGroup, false);
    }

    private void RefreshEquipment()
    {
        if (equipItemsRoot == null) return;
        for (int i = 0; i < 3; i++)
        {
            var item = MetaProgress.Equipped((EquipSlot)i);
            equipSlotIcons[i].sprite = item != null ? art.UI(item.Icon) : null;
            equipSlotIcons[i].enabled = item != null;
            equipSlotNames[i].text = item != null ? item.Name : "ПУСТО";
            equipSlotNames[i].color = item != null ? Data.RarityColor(item.Rarity) : new Color(0.7f, 0.66f, 0.6f);
            equipSlotStats[i].text = item != null
                ? Data.RarityName(item.Rarity) + "\n" + item.StatLine()
                : "Ничего не надето";
        }

        for (int i = equipItemsRoot.childCount - 1; i >= 0; i--)
        {
            var child = equipItemsRoot.GetChild(i).gameObject;
            // Destroy отложен до конца кадра; выключаем карточку сразу,
            // чтобы она не перехватывала клики у нового списка.
            child.SetActive(false);
            Destroy(child);
        }

        var items = MetaProgress.OwnedItems();
        if (items.Count == 0)
        {
            TextAt(equipItemsRoot, "Пока пусто. Спустись в подземелье — снаряжение выпадает не всегда.",
                18, new Color(0.72f, 0.68f, 0.62f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 80f), new Vector2(820f, 60f), art.Sans, true);
        }
        else
        {
            for (int i = 0; i < items.Count; i++)
            {
                int col = i % 3;
                int row = i / 3;
                MakeItemCard(equipItemsRoot, new Vector2(-340f + col * 340f, 132f - row * 90f), items[i]);
            }
        }
        if (equipHint != null)
        {
            equipHint.text = "СОБРАНО " + items.Count + " / " + Data.Equipment.Count;
        }
    }

    private void MakeItemCard(Transform parent, Vector2 pos, EquipmentDef item)
    {
        bool equipped = MetaProgress.Equipped(item.Slot) != null &&
                        MetaProgress.Equipped(item.Slot).Id == item.Id;
        var rt = MakeRect("item_" + item.Id, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), pos, new Vector2(330f, 78f));
        var bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = art.UI("ui_card_dark", new Vector4(16f, 16f, 16f, 16f));
        bg.type = Image.Type.Sliced;
        bg.color = equipped ? new Color(0.82f, 1f, 0.86f, 1f) : new Color(1f, 1f, 1f, 0.92f);
        bg.raycastTarget = true;

        var accent = Img(rt, art.UI("white"), new Color(Data.RarityColor(item.Rarity).r,
                Data.RarityColor(item.Rarity).g, Data.RarityColor(item.Rarity).b, 0.9f),
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f),
            new Vector2(4f, -12f), false);
        accent.rectTransform.offsetMin = new Vector2(2f, 12f);
        accent.rectTransform.offsetMax = new Vector2(6f, -12f);

        var iconRt = MakeRect("icon", rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(52f, 52f));
        var icon = iconRt.gameObject.AddComponent<Image>();
        icon.sprite = art.UI(item.Icon);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TextAt(rt, item.Name, 14, Data.RarityColor(item.Rarity), TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(74f, -8f),
            new Vector2(240f, 20f), art.Serif, true);
        TextAt(rt, Data.RarityName(item.Rarity) + " · " + Data.SlotName(item.Slot), 10,
            new Color(0.72f, 0.68f, 0.62f), TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(74f, -28f),
            new Vector2(240f, 16f), art.Sans, true);
        TextAt(rt, item.StatLine(), 10, new Color(0.82f, 0.78f, 0.7f), TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(74f, -44f),
            new Vector2(244f, 30f), art.Sans, true);
        if (equipped)
        {
            // equipped items are highlighted by the green frame tint above
        }

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.None;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        btn.onClick.AddListener(() =>
        {
            MetaProgress.Equip(item);
            RefreshEquipment();
            if (Game.I.Audio != null) Game.I.Audio.Play("ui_click", 0.55f);
        });
        var fx = rt.gameObject.AddComponent<UIButtonFx>();
        fx.Init(rt, bg, bg.sprite);
    }

    // ------------------------------------------------------------------ баннер и оверлеи
    private void BuildBanner()
    {
        bannerGroup = NewGroup("Banner");
        bannerGroup.blocksRaycasts = false;
        var root = bannerGroup.GetComponent<RectTransform>();
        bannerBg = Img(root, art.UI("ui_panel_dark", new Vector4(24f, 24f, 24f, 24f)),
            new Color(1f, 1f, 1f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 92f), new Vector2(760f, 156f), true);
        bannerBg.raycastTarget = false;

        bannerTitle = TextAt(root, "", 62, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 120f), new Vector2(1000f, 76f), art.Serif, true);
        bannerSub = TextAt(root, "", 22, new Color(0.88f, 0.81f, 0.7f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 70f), new Vector2(1000f, 30f), art.Sans, true);
    }

    public void ShowBanner(string title, string subtitle, float duration, Color accent)
    {
        if (bannerGroup == null) return;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerGroup.gameObject.SetActive(true);
        bannerGroup.blocksRaycasts = false;
        bannerTitle.text = title;
        bannerTitle.color = accent;
        bannerSub.text = subtitle ?? "";
        bannerRoutine = StartCoroutine(BannerRoutine(duration));
    }

    private IEnumerator BannerRoutine(float hold)
    {
        bannerGroup.alpha = 0f;
        var rt = bannerGroup.GetComponent<RectTransform>();
        rt.localScale = Vector3.one * 0.92f;
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.18f);
            bannerGroup.alpha = k;
            rt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        bannerGroup.alpha = 1f;
        rt.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(hold);
        t = 0f;
        while (t < 0.32f)
        {
            t += Time.unscaledDeltaTime;
            bannerGroup.alpha = 1f - Mathf.Clamp01(t / 0.32f);
            yield return null;
        }
        bannerGroup.alpha = 0f;
        bannerGroup.gameObject.SetActive(false);
        bannerRoutine = null;
    }

    private void BuildOverlays()
    {
        // A cold cinematic grade, heavier vignette and living film grain.
        var cool = FullImage("CoolGrade", art.UI("white"), new Color(0.035f, 0.07f, 0.15f, 0.10f));
        cool.raycastTarget = false;

        var vignette = FullImage("Vignette", art.UI("ui_vignette"), new Color(1f, 1f, 1f, 0.92f));
        vignette.raycastTarget = false;

        lowHpImage = FullImage("LowHpVignette", art.UI("ui_vignette"), new Color(0.8f, 0.05f, 0.04f, 0f));
        lowHpImage.raycastTarget = false;

        grainImage = FullImage("Grain", art.UI("ui_grain"), new Color(1f, 1f, 1f, 0.11f));
        grainImage.type = Image.Type.Tiled;
        grainImage.preserveAspect = false;
        grainImage.raycastTarget = false;
        grainImage.rectTransform.offsetMin = new Vector2(-24f, -24f);
        grainImage.rectTransform.offsetMax = new Vector2(24f, 24f);
        grainImage.gameObject.AddComponent<UIGrainDrift>().Init(0.09f, 0.13f);

        flashImage = FullImage("Flash", art.UI("white"), new Color(1f, 1f, 1f, 0f));
        flashImage.raycastTarget = false;

        fadeImage = FullImage("Fade", art.UI("white"), new Color(0f, 0f, 0f, 0f));
        fadeImage.raycastTarget = false;

        var wipeRt = MakeRect("Wipe", Canvas.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(3000f, 0f),
            new Vector2(2600f, 1500f));
        wipeRt.localRotation = Quaternion.Euler(0f, 0f, -7f);
        wipeImage = wipeRt.gameObject.AddComponent<Image>();
        wipeImage.sprite = art.UI("white");
        wipeImage.color = new Color(0.91f, 0.85f, 0.72f);
        wipeImage.raycastTarget = false;
        wipeRt.gameObject.SetActive(false);
    }

    public void Flash(Color color, float duration = 0.25f)
    {
        if (flashImage == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        float t = 0f;
        flashImage.color = color;
        flashImage.enabled = true;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = color.a * (1f - Mathf.Clamp01(t / duration));
            flashImage.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        flashImage.color = new Color(color.r, color.g, color.b, 0f);
        flashRoutine = null;
    }

    public void FadeTo(float alpha, float duration)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(alpha, duration));
    }

    private IEnumerator FadeRoutine(float target, float duration)
    {
        float start = fadeImage.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            fadeImage.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, target);
    }

    public IEnumerator Wipe(bool covered, float duration)
    {
        var rt = wipeImage.rectTransform;
        wipeImage.gameObject.SetActive(true);
        float start = covered ? 2900f : 0f;
        float end = covered ? 0f : -2900f;
        rt.anchoredPosition = new Vector2(start, 0f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            rt.anchoredPosition = new Vector2(Mathf.Lerp(start, end, k), 0f);
            yield return null;
        }
        rt.anchoredPosition = new Vector2(end, 0f);
        if (!covered) wipeImage.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ обновление
    private void Update()
    {
        readyPulse += Time.unscaledDeltaTime;
        if (helpPanel != null && helpPanel.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            helpPanel.gameObject.SetActive(false);
        }
        if (hudGroup != null && hudGroup.gameObject.activeSelf)
        {
            UpdateHud();
        }

        if (Game.I == null) return;
        if (Game.I.State == GameState.Transition && Input.GetKeyDown(KeyCode.Space))
        {
            Game.I.RequestSkip();
        }

        if (perkMode && Game.I.State == GameState.Perk)
        {
            for (int i = 0; i < 3; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    ChoosePerkCard(i);
                }
            }
        }
        else if (skillMode && Game.I.State == GameState.Perk)
        {
            for (int i = 0; i < 3; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    ChooseSkillCard(i);
                }
            }
        }
        else if (restMode && Game.I.State == GameState.Perk)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                Game.I.ChooseRest(true);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                Game.I.ChooseRest(false);
            }
        }
    }

    private void ResetLowHp()
    {
        if (lowHpImage != null) lowHpImage.color = new Color(0.8f, 0.05f, 0.04f, 0f);
    }

    private void UpdateHud()
    {
        if (Game.I == null) return;
        var p = Game.I.Player;
        if (p == null) return;
        float pct = Mathf.Clamp01(p.Stats.hp / Mathf.Max(1f, p.Stats.maxHp));
        if (hpFill != null)
        {
            hpFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, pct), 1f);
            hpFill.color = Color.Lerp(new Color(0.75f, 0.2f, 0.18f), new Color(0.86f, 0.42f, 0.3f), pct);
        }
        if (shieldFill != null)
        {
            float shieldPct = Mathf.Clamp01(p.Shield / Mathf.Max(1f, p.Stats.maxHp));
            bool showShield = shieldPct > 0.002f && pct > 0.002f;
            shieldFill.enabled = showShield;
            if (showShield)
            {
                // Щит рисуется поверх правой части текущего HP, не выходя за его границу.
                float start = Mathf.Max(0f, pct - shieldPct);
                shieldFill.rectTransform.anchorMin = new Vector2(start, 0f);
                shieldFill.rectTransform.anchorMax = new Vector2(pct, 1f);
                shieldFill.color = new Color(0.48f, 0.78f, 1f, 0.88f);
            }
        }
        if (hpText != null) hpText.text = Mathf.CeilToInt(p.Stats.hp) + " / " + Mathf.CeilToInt(p.Stats.maxHp);
        if (shieldText != null) shieldText.text = p.Shield > 0.5f ? "ЩИТ " + Mathf.CeilToInt(p.Shield) : "";
        if (lowHpImage != null)
        {
            float danger = Game.I.State == GameState.Combat && pct < 0.34f
                ? Mathf.Clamp01((0.34f - pct) / 0.34f)
                : 0f;
            float pulse = 0.72f + Mathf.Sin(readyPulse * 5.2f) * 0.28f;
            lowHpImage.color = new Color(0.8f, 0.05f, 0.04f, danger * 0.34f * pulse);
        }

        var run = Game.I.Run;
        if (statsText != null)
        {
            statsText.text = "УБИТО  " + run.Kills + "\nВРЕМЯ  " + FormatTime(run.RunTime);
        }

        if (dashCooldown != null && dashIcon != null && p != null)
        {
            float cd = Mathf.Clamp01(p.DashCd / Mathf.Max(0.01f, p.DashCooldown));
            dashCooldown.enabled = cd > 0.001f;
            dashCooldown.fillAmount = cd;
            dashIcon.color = cd > 0.001f ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
        }

        if (dashText != null)
        {
            dashText.color = p.DashCd > 0f ? new Color(0.75f, 0.7f, 0.62f, 0.6f) : Pal.Cream;
        }

        var skills = Game.I.Skills;
        if (skills != null && slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var sv = slots[i];
                var slot = skills.Slots[i];
                if (slot.Def == null) continue;
                float cd = slot.Cd;
                bool ready = cd <= 0.001f;
                sv.Cooldown.enabled = !ready;
                sv.Cooldown.fillAmount = slot.MaxCd > 0.001f ? Mathf.Clamp01(cd / slot.MaxCd) : 0f;
                sv.CooldownText.text = ready ? "" : Mathf.CeilToInt(cd).ToString();
                sv.Frame.color = ready
                    ? new Color(1f, 1f, 1f, 0.8f + Mathf.Sin(readyPulse * 3.2f) * 0.2f)
                    : Color.white;
            }
        }

        if (bossGroup != null && bossGroup.gameObject.activeSelf)
        {
            Enemy boss = null;
            foreach (var e in Game.I.Rooms.Enemies)
            {
                if (e != null && e.Boss && !e.Dead) { boss = e; break; }
            }
            if (boss != null && bossFill != null)
            {
                bossFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, Mathf.Clamp01(boss.Hp / boss.MaxHp)), 1f);
            }
        }
    }

    private static string FormatTime(float time)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(time));
        int m = total / 60;
        int s = total % 60;
        return m.ToString("00") + ":" + s.ToString("00");
    }
}

public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    IPointerUpHandler
{
    private RectTransform rt;
    private Image img;
    private Sprite normal;
    private Sprite hover;
    private float hovered;
    private float pressed;
    private Vector3 baseScale;
    private float baseRot;
    private bool ready;

    public void Init(RectTransform rect, Image image, Sprite normalSprite)
    {
        rt = rect;
        img = image;
        normal = normalSprite;
        hover = normalSprite;
        baseScale = rect.localScale;
        baseRot = rect.localRotation.eulerAngles.z;
        ready = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = 1f;
        if (img != null && hover != null) img.sprite = hover;
        if (Game.I != null && Game.I.Audio != null) Game.I.Audio.Play("ui_hover", 0.35f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = 0f;
        pressed = 0f;
        if (img != null && normal != null) img.sprite = normal;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = 1f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = 0f;
    }

    private void Update()
    {
        if (!ready || rt == null) return;
        float target = 1f + hovered * 0.035f - pressed * 0.03f;
        rt.localScale = Vector3.Lerp(rt.localScale, baseScale * target, Time.unscaledDeltaTime * 14f);
        float rot = baseRot + hovered * -1.1f;
        rt.localRotation = Quaternion.Lerp(rt.localRotation, Quaternion.Euler(0f, 0f, rot),
            Time.unscaledDeltaTime * 14f);
    }
}

public class UIRotate : MonoBehaviour
{
    public float Speed = 5f;

    private void Update()
    {
        transform.Rotate(0f, 0f, Speed * Time.unscaledDeltaTime);
    }
}

public class UIFloat : MonoBehaviour
{
    private RectTransform rt;
    private Vector2 origin;
    private Vector2 amplitude;
    private float speed;
    private float phase;

    public void Init(Vector2 amp, float frequency, float startPhase)
    {
        rt = (RectTransform)transform;
        origin = rt.anchoredPosition;
        amplitude = amp;
        speed = frequency;
        phase = startPhase;
    }

    private void Update()
    {
        if (rt == null) return;
        float t = Time.unscaledTime * speed + phase;
        rt.anchoredPosition = origin + new Vector2(Mathf.Sin(t) * amplitude.x, Mathf.Cos(t * 0.83f) * amplitude.y);
    }
}

public class UIPulseAlpha : MonoBehaviour
{
    private Image image;
    private float min;
    private float max;
    private float speed;
    private float phase;

    public void Init(float minimum, float maximum, float frequency)
    {
        image = GetComponent<Image>();
        min = minimum;
        max = maximum;
        speed = frequency;
        phase = UnityEngine.Random.value * 6.28f;
    }

    private void Update()
    {
        if (image == null) return;
        Color c = image.color;
        c.a = Mathf.Lerp(min, max, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed + phase));
        image.color = c;
    }
}

public class UIDrift : MonoBehaviour
{
    private RectTransform rt;
    private Vector2 origin;
    private Vector2 amplitude;
    private float speed;
    private float phase;
    private float spin;

    public void Init(Vector2 amp, float frequency, float startPhase, float rotationSpeed)
    {
        rt = (RectTransform)transform;
        origin = rt.anchoredPosition;
        amplitude = amp;
        speed = frequency;
        phase = startPhase;
        spin = rotationSpeed;
    }

    private void Update()
    {
        if (rt == null) return;
        float t = Time.unscaledTime * speed + phase;
        rt.anchoredPosition = origin + new Vector2(Mathf.Sin(t) * amplitude.x, Mathf.Cos(t * 0.8f) * amplitude.y);
        if (Mathf.Abs(spin) > 0.001f) rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * spin);
    }
}

public class UIMote : MonoBehaviour
{
    private RectTransform rt;
    private Image image;
    private Color color;
    private float rise;
    private float wobble;
    private float phase;

    public void Init(float riseSpeed, float wobbleSpeed, float startPhase)
    {
        rt = (RectTransform)transform;
        image = GetComponent<Image>();
        color = image != null ? image.color : Color.white;
        rise = riseSpeed;
        wobble = wobbleSpeed;
        phase = startPhase;
    }

    private void Update()
    {
        if (rt == null) return;
        float dt = Time.unscaledDeltaTime;
        Vector2 p = rt.anchoredPosition;
        p.y += rise * dt;
        p.x += Mathf.Sin(Time.unscaledTime * wobble + phase) * 7f * dt;
        if (p.y > 470f)
        {
            p.y = -470f;
            p.x = UnityEngine.Random.Range(-710f, 710f);
        }
        rt.anchoredPosition = p;
        if (image != null)
        {
            float a = color.a * (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2.4f + phase));
            image.color = new Color(color.r, color.g, color.b, a);
        }
    }
}

public class UIGrainDrift : MonoBehaviour
{
    private RectTransform rt;
    private Image image;
    private Color color;
    private float baseAlpha;
    private float phase;

    public void Init(float alpha, float speed)
    {
        rt = (RectTransform)transform;
        image = GetComponent<Image>();
        color = image != null ? image.color : Color.white;
        baseAlpha = alpha;
        phase = UnityEngine.Random.value * 6.28f;
    }

    private void Update()
    {
        if (rt == null || image == null) return;
        float n = Mathf.PerlinNoise(phase + Time.unscaledTime * 13f, 0.37f);
        rt.anchoredPosition = new Vector2(n * 8f - 4f, Mathf.PerlinNoise(0.22f, phase + Time.unscaledTime * 11f) * 8f - 4f);
        color.a = baseAlpha * (0.78f + n * 0.22f);
        image.color = color;
    }
}

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private RectTransform rt;
    private Image glow;
    private Vector2 basePos;
    private Action action;
    private float hovered;
    private float seed;
    private Color glowColor;

    public void Init(RectTransform rect, Image glowImage, Vector2 pos, Action onClick)
    {
        rt = rect;
        glow = glowImage;
        basePos = pos;
        action = onClick;
        seed = UnityEngine.Random.value * 6f;
        if (glow != null) glowColor = glow.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = 1f;
        if (Game.I != null && Game.I.Audio != null) Game.I.Audio.Play("ui_hover", 0.4f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = 0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (action == null) return;
        if (Game.I != null && Game.I.Audio != null) Game.I.Audio.Play("ui_click", 0.8f);
        action();
    }

    private void Update()
    {
        if (rt == null) return;
        float speed = Time.unscaledDeltaTime * 13f;
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, basePos + Vector2.up * hovered * 16f, speed);
        rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * (1f + hovered * 0.045f), speed);
        rt.localRotation = Quaternion.Lerp(rt.localRotation,
            Quaternion.Euler(0f, 0f, hovered * 1.4f), speed);
        if (glow != null)
        {
            float pulse = 0.75f + Mathf.Sin(Time.unscaledTime * 2.2f + seed) * 0.25f;
            var c = glowColor;
            c.a = (0.13f + hovered * 0.3f) * pulse;
            glow.color = c;
            glow.rectTransform.anchoredPosition = basePos;
            glow.rectTransform.localScale = Vector3.one * (1f + hovered * 0.06f);
        }
    }
}
