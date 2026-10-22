using UnityEngine;

public class WorldBar : MonoBehaviour
{
    private Transform target;
    private SpriteRenderer bg;
    private SpriteRenderer fill;
    private float width;
    private float height;
    private Color fillColor;
    private float shown;

    public static WorldBar Create(Transform parent, Art art, float width, float height, Color color, bool always)
    {
        var go = new GameObject("hpbar");
        go.transform.SetParent(parent, false);
        var bar = go.AddComponent<WorldBar>();
        bar.target = parent;
        bar.width = width;
        bar.height = height;
        bar.fillColor = color;

        var bgGo = new GameObject("bg");
        bgGo.transform.SetParent(go.transform, false);
        bar.bg = bgGo.AddComponent<SpriteRenderer>();
        bar.bg.sprite = art.World("white");
        bar.bg.color = new Color(0.08f, 0.05f, 0.08f, 0.8f);
        bar.bg.sortingOrder = 1500;
        bgGo.transform.localScale = new Vector3(width / 0.04f, height / 0.04f, 1f);

        var fGo = new GameObject("fill");
        fGo.transform.SetParent(go.transform, false);
        bar.fill = fGo.AddComponent<SpriteRenderer>();
        bar.fill.sprite = art.World("white");
        bar.fill.color = color;
        bar.fill.sortingOrder = 1501;
        fGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        fGo.transform.localScale = new Vector3(width / 0.04f, height / 0.04f, 1f);

        go.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        bar.SetVisible(always);
        return bar;
    }

    public void SetVisible(bool visible)
    {
        if (bg != null) bg.enabled = visible;
        if (fill != null) fill.enabled = visible;
    }

    public void Set(float pct, float y)
    {
        pct = Mathf.Clamp01(pct);
        if (Mathf.Abs(pct - shown) > 0.001f)
        {
            shown = pct;
            float w = width * pct;
            fill.transform.localScale = new Vector3(w / 0.04f, height / 0.04f, 1f);
            fill.transform.localPosition = new Vector3(-(width - w) * 0.5f, 0f, -0.01f);
            fill.color = Color.Lerp(new Color(0.75f, 0.2f, 0.18f), fillColor, pct);
        }
        transform.localPosition = new Vector3(0f, y, 0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        int order = 1500;
        var actor = target.GetComponent<PaperActor>();
        if (actor != null && actor.Main != null) order = actor.Main.sortingOrder + 6;
        if (bg != null) bg.sortingOrder = order;
        if (fill != null) fill.sortingOrder = order + 1;
    }
}
