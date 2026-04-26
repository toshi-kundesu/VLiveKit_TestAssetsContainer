using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, DefaultExecutionOrder(100)]
public sealed class ViewfinderOverlay : MonoBehaviour
{
    public enum DisplayMethod { OnGUI, UI_Image }

    [Header("外周Ｌ字")]
    [SerializeField] Color edgeColor = Color.white;
    [SerializeField] float edgeWidth = 4f;
    [SerializeField] float edgeLength = 80f;
    [SerializeField] float edgeMargin = 20f;

    [Header("中央スクエアＬ字")]
    [SerializeField] bool showInnerSquare = true;
    [SerializeField] Color innerColor = Color.white;
    [SerializeField] float innerWidth = 3f;
    [SerializeField] float innerLength = 40f;
    [SerializeField] float innerSize = 220f;

    [Header("中央クロスヘア")]
    [SerializeField] bool showCross = true;
    [SerializeField] Color crossColor = Color.white;
    [SerializeField] float crossWidth = 2f;
    [SerializeField] float crossLength = 25f;

    [Header("描画方式")]
    [SerializeField] DisplayMethod displayMethod = DisplayMethod.UI_Image;

    [Header("UI_Image 親 (自動生成)")]
    [SerializeField] RectTransform root;

    readonly List<Image> lines = new();
    bool inited;

    void Awake() { }
    void OnValidate() { }
    void Update()
    {
        if (displayMethod == DisplayMethod.UI_Image && root != null)
            LayoutImages();
    }

    [ContextMenu("UI_Image を初期化する")]
    public void ForceCreateUIImages()
    {
        Init();
        LayoutImages();
    }

    void Init()
    {
        if (inited) return;
        inited = true;
        if (displayMethod == DisplayMethod.UI_Image)
            EnsureImages();
    }

    void EnsureImages()
    {
        if (!root)
        {
            var go = new GameObject("ViewfinderRoot", typeof(RectTransform));
            root = go.GetComponent<RectTransform>();
            root.SetParent(transform, false);
        }

        int need = 8 + (showInnerSquare ? 8 : 0) + (showCross ? 2 : 0);

        while (lines.Count < need)
        {
            var g = new GameObject("Line", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(root, false);
            lines.Add(g.GetComponent<Image>());
        }
        while (lines.Count > need)
        {
            DestroyImmediate(lines[^1].gameObject);
            lines.RemoveAt(lines.Count - 1);
        }
    }

    void LayoutImages()
    {
        if (displayMethod != DisplayMethod.UI_Image) return;
        if (lines.Count == 0) return;

        int idx = 0;

        idx = LayoutCornerSet(idx, edgeColor, edgeWidth, edgeLength, edgeMargin, Screen.width, Screen.height);

        if (showInnerSquare)
        {
            float half = innerSize / 2f;
            float ox = Screen.width * 0.5f - half;
            float oy = Screen.height * 0.5f - half;

            idx = LayoutCornerSet(idx, innerColor, innerWidth, innerLength, 0f, innerSize, innerSize, ox, oy);
        }

        if (showCross)
        {
            Vector2 c = new(Screen.width * 0.5f, Screen.height * 0.5f);
            SetLine(lines[idx++], c + new Vector2(-crossLength, -crossWidth * 0.5f),
                new Vector2(crossLength * 2f, crossWidth), crossColor);
            SetLine(lines[idx++], c + new Vector2(-crossWidth * 0.5f, -crossLength),
                new Vector2(crossWidth, crossLength * 2f), crossColor);
        }
    }

    int LayoutCornerSet(int start, Color col, float w, float len, float margin, float areaW, float areaH, float ox = 0f, float oy = 0f)
    {
        start = LayoutL(start, col, w, len, ox + margin, oy + margin, true, true);
        start = LayoutL(start, col, w, len, ox + areaW - margin, oy + margin, false, true);
        start = LayoutL(start, col, w, len, ox + margin, oy + areaH - margin, true, false);
        start = LayoutL(start, col, w, len, ox + areaW - margin, oy + areaH - margin, false, false);
        return start;
    }

    int LayoutL(int idx, Color col, float w, float len, float cx, float cy, bool left, bool top)
    {
        float hx = left ? cx : cx - len;
        SetLine(lines[idx++], new Vector2(hx, cy - w * 0.5f), new Vector2(len, w), col);
        float vy = top ? cy : cy - len;
        SetLine(lines[idx++], new Vector2(cx - w * 0.5f, vy), new Vector2(w, len), col);
        return idx;
    }

    void SetLine(Image img, Vector2 pos, Vector2 size, Color col)
    {
        img.color = col;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(pos.x, -pos.y);
    }

    void OnGUI()
    {
        if (displayMethod != DisplayMethod.OnGUI) return;

        DrawCornerSet(edgeColor, edgeWidth, edgeLength, edgeMargin, Screen.width, Screen.height);

        if (showInnerSquare)
        {
            float half = innerSize / 2f;
            float ox = Screen.width * 0.5f - half;
            float oy = Screen.height * 0.5f - half;
            DrawCornerSet(innerColor, innerWidth, innerLength, 0f, innerSize, innerSize, ox, oy);
        }

        if (showCross)
        {
            Vector2 c = new(Screen.width * 0.5f, Screen.height * 0.5f);
            GUI.color = crossColor;
            GUI.DrawTexture(new Rect(c.x - crossLength, c.y - crossWidth * 0.5f, crossLength * 2f, crossWidth), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(c.x - crossWidth * 0.5f, c.y - crossLength, crossWidth, crossLength * 2f), Texture2D.whiteTexture);
        }
    }

    void DrawCornerSet(Color col, float w, float len, float margin, float areaW, float areaH, float ox = 0f, float oy = 0f)
    {
        GUI.color = col;
        DrawL(ox + margin, oy + margin, w, len, true, true);
        DrawL(ox + areaW - margin, oy + margin, w, len, false, true);
        DrawL(ox + margin, oy + areaH - margin, w, len, true, false);
        DrawL(ox + areaW - margin, oy + areaH - margin, w, len, false, false);
    }

    void DrawL(float cx, float cy, float w, float len, bool left, bool top)
    {
        float hx = left ? cx : cx - len;
        GUI.DrawTexture(new Rect(hx, cy - w * 0.5f, len, w), Texture2D.whiteTexture);
        float vy = top ? cy : cy - len;
        GUI.DrawTexture(new Rect(cx - w * 0.5f, vy, w, len), Texture2D.whiteTexture);
    }
}
