using System.Collections.Generic;
using UnityEngine;
using TMPro;
using toshi.VLiveKit.Photography;

[DefaultExecutionOrder(100)]
// [ExecuteAlways]
public class ColorPaletteDisplay : MonoBehaviour
{
    public enum DisplayMethod { OnGUI, TextMeshPro }

    [Header("表示方式")]
    [SerializeField] DisplayMethod displayMethod = DisplayMethod.OnGUI;

    [Header("カラーパレット")]
    [SerializeField] ColorPalette colorPalette;

    [Header("レイアウト")]
    [SerializeField] float circleSize = 20f;
    [SerializeField] float circleSpacing = 5f;
    [SerializeField] float offsetX = 10f;
    [SerializeField] float offsetY = 10f;

    [Header("TMP 親コンテナ (任意)")]
    [SerializeField] RectTransform tmpParent;

    readonly List<Texture2D> texCircles = new();
    readonly List<TextMeshProUGUI> tmpCircles = new();

    int lastColorCount = -1;
    DisplayMethod lastDisplayMethod = DisplayMethod.OnGUI;

    // void Start()      => Rebuild();
    // void OnValidate() => Rebuild();

    // void Update()
    // {
    //     if (colorPalette == null) return;

    //     bool needsRebuild = false;

    //     if (colorPalette.colors.Count != lastColorCount)
    //     {
    //         lastColorCount = colorPalette.colors.Count;
    //         needsRebuild = true;
    //     }

    //     if (displayMethod != lastDisplayMethod)
    //     {
    //         lastDisplayMethod = displayMethod;
    //         needsRebuild = true;
    //     }

    //     if (needsRebuild) Rebuild();
    // }

    // void OnGUI()
    // {
    //     if (displayMethod != DisplayMethod.OnGUI || colorPalette == null) return;
    //     if (texCircles.Count == 0) BuildTextures();

    //     float x = offsetX;
    //     float y = Screen.height - circleSize - offsetY;

    //     GUI.color = Color.white;
    //     foreach (var tex in texCircles)
    //     {
    //         GUI.DrawTexture(new Rect(x, y, circleSize, circleSize), tex);
    //         x += circleSize + circleSpacing;
    //     }
    // }

    // void Rebuild()
    // {
    //     if (displayMethod == DisplayMethod.OnGUI)
    //         BuildTextures();
    //     else
    //         BuildTmp();
    // }

    // //――――― OnGUI 用円テクスチャ生成 ―――――――――――――――――――
    // void BuildTextures()
    // {
    //     texCircles.Clear();
    //     if (colorPalette == null) return;

    //     foreach (var col in colorPalette.colors)
    //         texCircles.Add(MakeCircleTex((int)circleSize, col));
    // }

    // //――――― TMP 用“●”生成 ―――――――――――――――――――――――――
    // void BuildTmp()
    // {
    //     if (colorPalette == null) return;

    //     if (!tmpParent)
    //     {
    //         if (transform is RectTransform rt) tmpParent = rt;
    //         else
    //         {
    //             tmpParent = new GameObject("PaletteTMP", typeof(RectTransform))
    //                            .GetComponent<RectTransform>();
    //             tmpParent.SetParent(transform, false);
    //         }
    //     }

    //     // 必要数を確保
    //     while (tmpCircles.Count < colorPalette.colors.Count)
    //         tmpCircles.Add(CreateTmpCircle());

    //     // 余分を削除
    //     while (tmpCircles.Count > colorPalette.colors.Count)
    //     {
    //         DestroyImmediate(tmpCircles[^1].gameObject);
    //         tmpCircles.RemoveAt(tmpCircles.Count - 1);
    //     }

    //     // 配置 & 色
    //     float x = offsetX;
    //     float y = offsetY;

    //     for (int i = 0; i < colorPalette.colors.Count; i++)
    //     {
    //         var t = tmpCircles[i];
    //         t.text = "●";
    //         t.fontSize = circleSize;
    //         t.color = colorPalette.colors[i];

    //         var rt = t.rectTransform;
    //         rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); // 左下アンカー
    //         rt.pivot = new Vector2(0f, 0f);
    //         rt.anchoredPosition = new Vector2(x, y);

    //         x += circleSize + circleSpacing;
    //     }
    // }

    // TextMeshProUGUI CreateTmpCircle()
    // {
    //     var go = new GameObject("CircleTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
    //     go.transform.SetParent(tmpParent, false);
    //     var tmp = go.GetComponent<TextMeshProUGUI>();
    //     tmp.alignment = TextAlignmentOptions.Center;
    //     tmp.raycastTarget = false;
    //     return tmp;
    // }

    // //――――― 円テクスチャ作成 ―――――――――――――――――――――――――
    // static Texture2D MakeCircleTex(int d, Color col)
    // {
    //     var tex = new Texture2D(d, d, TextureFormat.ARGB32, false);
    //     float r = d / 2f; Vector2 c = new(r, r);
    //     var pix = new Color[d * d];

    //     for (int y = 0; y < d; y++)
    //     for (int x = 0; x < d; x++)
    //         pix[y * d + x] = Vector2.Distance(new(x, y), c) <= r ? col : Color.clear;

    //     tex.SetPixels(pix);
    //     tex.Apply();
    //     return tex;
    // }
}
