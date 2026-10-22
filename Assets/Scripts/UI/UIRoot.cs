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

    private RectTransform perkCardsRoot;
    private RectTransform menuRoot;
    private RectTransform helpPanel;
    private RectTransform restRoot;
    private RectTransform pausePanel;
    private RectTransform endPanel;

    private Text menuBest;
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

    private Image hpFill;
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

        // Layered dungeon backdrop: a cold arch, candlelight, drifting fog and embers.
        var backdrop = Img(menuRoot, art.UI("ui_menu_bg"), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
        backdrop.preserveAspect = false;
        backdrop.raycastTarget = true;

        var frame = Img(menuRoot, art.UI("ui_menu_frame"), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
        frame.raycastTarget = false;

        for (int i = 0; i < 4; i++)
        {
            var fog = Img(menuRoot, art.UI("fx_glow"), new Color(0.42f, 0.48f, 0.72f, 0.055f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-380f + i * 250f, -220f + (i % 2) * 70f), new Vector2(760f, 400f), false);
            var drift = fog.gameObject.AddComponent<UIDrift>();
            drift.Init(new Vector2(80f, 34f), 0.12f + i * 0.02f, i * 1.7f, 0f);
        }

        var logoGlow = Img(menuRoot, art.UI("fx_glow"), new Color(0.55f, 0.27f, 0.65f, 0.16f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -40f), new Vector2(980f, 430f), false);
        logoGlow.gameObject.AddComponent<UIPulseAlpha>().Init(0.12f, 0.24f, 0.42f);

        var rune = Img(menuRoot, art.UI("ui_rune"), new Color(0.82f, 0.65f, 0.34f, 0.16f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 58f), new Vector2(650f, 650f), false);
        rune.gameObject.AddComponent<UIRotate>().Speed = -4.5f;
        rune.gameObject.AddComponent<UIPulseAlpha>().Init(0.10f, 0.19f, 0.26f);

        var logo = Img(menuRoot, art.UI("ui_logo"), Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f),
            new Vector2(780f, 189f), false);
        logo.preserveAspect = true;
        logo.gameObject.AddComponent<UIFloat>().Init(new Vector2(0f, 4.5f), 0.55f, 0f);
        logo.gameObject.AddComponent<UIPulseAlpha>().Init(0.9f, 1f, 0.5f);

        TextAt(menuRoot, "МРАЧНОЕ ПЕРЕИЗДАНИЕ  ·  v1.1", 21, new Color(0.84f, 0.74f, 0.58f),
            TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -247f), new Vector2(760f, 32f), art.Sans, true);

        Img(menuRoot, art.UI("ui_divider"), new Color(1f, 1f, 1f, 0.9f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -278f), new Vector2(620f, 42f), false);

        for (int side = -1; side <= 1; side += 2)
        {
            Img(menuRoot, art.UI("white"), new Color(0.72f, 0.55f, 0.32f, 0.2f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(side * 506f, 36f), new Vector2(2f, 530f), false);
            var tick = Img(menuRoot, art.UI("white"), new Color(0.9f, 0.72f, 0.42f, 0.35f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(side * 506f, 36f), new Vector2(6f, 6f), false);
            tick.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        var bestPanel = Img(menuRoot, art.UI("ui_panel_dark", new Vector4(18f, 18f, 18f, 18f)),
            new Color(1f, 1f, 1f, 0.86f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(38f, 20f), new Vector2(456f, 84f), true);
        bestPanel.raycastTarget = false;
        var bestLabel = TextAt(bestPanel.rectTransform, "ЛУЧШИЙ ЗАБЕГ", 15,
            new Color(0.76f, 0.65f, 0.48f), TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -12f),
            new Vector2(380f, 22f), art.Sans, false);
        bestLabel.fontStyle = FontStyle.Bold;
        menuBest = TextAt(bestPanel.rectTransform, "", 20, new Color(0.9f, 0.83f, 0.68f),
            TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(22f, 14f), new Vector2(410f, 36f), art.Sans, true);

        MakeButton(menuRoot, "НАЧАТЬ ЗАБЕГ", new Vector2(0f, 82f), new Vector2(390f, 70f),
            () => Game.I.StartRun(), 30);
        MakeButton(menuRoot, "КАК ИГРАТЬ", new Vector2(0f, 2f), new Vector2(390f, 60f),
            ToggleHelp, 25, false);
        MakeButton(menuRoot, "ВЫЙТИ", new Vector2(0f, -72f), new Vector2(320f, 52f),
            () => Game.I.QuitGame(), 23, false);

        for (int side = -1; side <= 1; side += 2)
        {
            Img(menuRoot, art.UI("white"), new Color(0.8f, 0.62f, 0.36f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(side * 235f, 82f), new Vector2(44f, 2f), false);
            var diamond = Img(menuRoot, art.UI("white"), new Color(0.95f, 0.78f, 0.46f, 0.8f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(side * 258f, 82f), new Vector2(7f, 7f), false);
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        for (int i = 0; i < 18; i++)
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

        TextAt(menuRoot, "v1.1", 16, new Color(0.7f, 0.65f, 0.58f, 0.8f), TextAnchor.MiddleRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 22f),
            new Vector2(120f, 24f), art.Sans, false);

        BuildHelp();
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
            "<color=#E8C16D>WASD / стрелки</color> — движение\n" +
            "Маг стреляет сам в ближайшего врага.\n\n" +
            "<color=#E8C16D>1 2 3 4 5</color> — активные навыки. Перезарядка видна на слотах.\n\n" +
            "<color=#E8C16D>SPACE</color> — рывок. Короткая неуязвимость.\n\n" +
            "<color=#E8C16D>ESC</color> — пауза, настройки громкости и выход в меню.\n\n" +
            "Зачищай комнаты, выбирай перки и новые навыки. " +
            "После каждой комнаты маг идёт дальше — впереди 12 залов и Лич-Переплёт.";

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

    public void ShowMenu()
    {
        if (helpPanel != null) helpPanel.gameObject.SetActive(false);
        ResetLowHp();
        SetGroup(menuGroup, true);
        SetGroup(hudGroup, false);
        SetGroup(perkGroup, false);
        SetGroup(pauseGroup, false);
        SetGroup(endGroup, false);
        SetGroup(bannerGroup, false);
        if (menuBest != null)
        {
            var run = Game.I.Run;
            string best = run.BestRoom > 0
                ? "КОМНАТА " + run.BestRoom + "  ·  УБИТО " + run.BestKills
                : "—";
            menuBest.text = best;
        }
        if (skipRoot != null) skipRoot.gameObject.SetActive(false);
    }

    public void HideMenu()
    {
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
            roomText.text = room == null ? "" : "КОМНАТА " + (run.RoomIndex + 1) + " / " + Data.RoomCount;
            roomText.color = room == null ? Pal.Gold : Data.RoomAccent(room.Type);
        }
        if (roomSub != null) roomSub.text = room == null ? "" : room.Name;
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

    public void ShowBossBar(bool show)
    {
        if (bossGroup != null) bossGroup.gameObject.SetActive(show);
        if (show && bossName != null) bossName.text = "ЛИЧ-ПЕРЕПЛЁТ";
    }

    public void BossPhase()
    {
        if (bossName != null) bossName.text = "ЛИЧ-ПЕРЕПЛЁТ  ·  ФАЗА II";
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
            BuildCard(perkCardsRoot, new Vector2(x, 0f), def.Icon, def.Name,
                def.Desc + "\n<color=#BFAF96>Перезарядка: " + def.Cooldown.ToString("0") + " сек.</color>",
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
    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Text fullscreenText;

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

        var fsBtn = MakeButton(pausePanel, "ЭКРАН: ОКНО", new Vector2(0f, -304f), new Vector2(300f, 46f),
            ToggleFullscreen, 19, false);
        fullscreenText = fsBtn.GetComponentInChildren<Text>();
    }

    private Slider AddSlider(Transform parent, string label, float y, float value)
    {
        TextAt(parent, label, 17, new Color(0.86f, 0.79f, 0.68f), TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(-200f, y + 23f), new Vector2(400f, 22f), art.Sans, true);

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
        Screen.fullScreenMode = Screen.fullScreenMode == FullScreenMode.Windowed
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
        if (fullscreenText != null)
        {
            fullscreenText.text = Screen.fullScreenMode == FullScreenMode.Windowed
                ? "ЭКРАН: ОКНО"
                : "ЭКРАН: ПОЛНЫЙ";
        }
    }

    public void ShowPause()
    {
        ResetLowHp();
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(Game.I.Audio.Master);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(Game.I.Audio.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(Game.I.Audio.SfxVolume);
        if (fullscreenText != null)
        {
            fullscreenText.text = Screen.fullScreenMode == FullScreenMode.Windowed
                ? "ЭКРАН: ОКНО"
                : "ЭКРАН: ПОЛНЫЙ";
        }
        SetGroup(pauseGroup, true);
    }

    public void HidePause()
    {
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
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 640f));
        var panel = endPanel.gameObject.AddComponent<Image>();
        panel.sprite = art.UI("white");
        panel.type = Image.Type.Simple;
        panel.color = new Color(0.06f, 0.04f, 0.07f, 1f);
        panel.raycastTarget = true;
        Img(endPanel, art.UI("ui_panel", new Vector4(28f, 28f, 28f, 28f)), Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true, false);

        endTitle = TextAt(endPanel, "ПОБЕДА", 62, Pal.Gold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f),
            new Vector2(620f, 76f), art.Serif, true);

        endStats = TextAt(endPanel, "", 22, Pal.Cream, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -124f),
            new Vector2(600f, 250f), art.Sans, true);

        MakeButton(endPanel, "ЕЩЁ РАЗ", new Vector2(0f, -64f), new Vector2(340f, 60f),
            () => Game.I.RestartRun(), 26);
        MakeButton(endPanel, "В МЕНЮ", new Vector2(0f, -138f), new Vector2(340f, 50f),
            () => { Time.timeScale = 1f; Game.I.ToMenu(); }, 23, false);

        TextAt(endPanel, "R — быстрый перезапуск", 16, new Color(0.78f, 0.72f, 0.62f),
            TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(320f, 24f), art.Sans, true);
    }

    public void ShowEnd(bool victory)
    {
        ResetLowHp();
        SetGroup(hudGroup, false);
        SetGroup(endGroup, true);
        endTitle.text = victory ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
        endTitle.color = victory ? Pal.Gold : new Color(0.85f, 0.3f, 0.28f);
        var run = Game.I.Run;
        string time = FormatTime(run.RunTime);
        endStats.text =
            (victory
                ? "Лич-Переплёт разорван на клочки.\nПодземелье затихло."
                : "Картонный маг пал.\nНо лист можно перевернуть.") +
            "\n\n<color=#E8C16D>Комнат пройдено:</color> " + (run.RoomIndex + (victory ? 1 : 0)) +
            " / " + Data.RoomCount +
            "\n<color=#E8C16D>Убито врагов:</color> " + run.Kills +
            "\n<color=#E8C16D>Нанесено урона:</color> " + Mathf.RoundToInt(run.DamageDealt) +
            "\n<color=#E8C16D>Время:</color> " + time +
            "\n<color=#E8C16D>Лучший забег:</color> комната " + Mathf.Max(1, run.BestRoom) +
            " · убито " + run.BestKills;
    }

    public void HideEnd()
    {
        SetGroup(endGroup, false);
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
