using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 画面上下にレターボックス（黒帯）を描画するユーティリティ。<br/>
/// DisplayMethod : OnGUI / UI_Image / UI_TMP<br/>
/// ─ UI_Image … Canvas に Image 2 枚を生成して塗りつぶし<br/>
/// ─ UI_TMP  … TMP_Text 2 つを生成して「█」で塗りつぶし（環境依存対策用）
[ExecuteAlways, DefaultExecutionOrder(100)]
public sealed class LetterBoxDisplay : MonoBehaviour
{
    public enum DisplayMethod { OnGUI, UI_Image, UI_TMP }

    [Header("表示方式")]
    [SerializeField] DisplayMethod displayMethod = DisplayMethod.UI_Image;

    [Header("ON / OFF")]
    [SerializeField] bool enableLetterBox = true;

    [Header("上下帯の高さ (画面高さ比率)")]
    [Range(0f, 0.5f)] [SerializeField] float barPercent = 0.1f;

    [Header("帯の色")]
    [SerializeField] Color barColor = Color.black;

    [Header("OnGUI 描画順 (小さいほど前面)")]
    [SerializeField] int guiDepth = 0;

    [Header("UI 親 (自動生成)")]
    [SerializeField] RectTransform uiRoot;   // 共通の親

    [Header("TMP 用フォント (█を含む)")]
    [SerializeField] TMP_FontAsset blockFont;

    // 内部保持 ------------------------------------------------------------
    Image           topImg, botImg;
    TextMeshProUGUI topTmp, botTmp;
    DisplayMethod   prevMethod;
    bool            prevEnable;
    float           prevPercent;
    Color           prevColor;

    //======================================================================
    // Unity Events
    //======================================================================
    void Awake()      { Setup(); }
    void OnValidate() { Setup(); }
    void Update()     { Refresh(); }

    //======================================================================
    // 初期セットアップ
    //======================================================================
    void Setup()
    {
        // 変更検知
        bool needRebuild =
               prevMethod  != displayMethod
            || prevEnable  != enableLetterBox
            || prevPercent != barPercent
            || prevColor   != barColor;

        prevMethod  = displayMethod;
        prevEnable  = enableLetterBox;
        prevPercent = barPercent;
        prevColor   = barColor;

        if (!needRebuild) return;

        if (!enableLetterBox) { DestroyUI(); return; }

        if (displayMethod == DisplayMethod.OnGUI)
        {
            DestroyUI();        // UI 要素不要
        }
        else if (displayMethod == DisplayMethod.UI_Image)
        {
            BuildImageBars();   // 必要なら生成
            UpdateImageBars();  // サイズ/色 更新
        }
        else // UI_TMP
        {
            BuildTmpBars();
            UpdateTmpBars();
        }
    }

    //======================================================================
    //  毎フレーム更新
    //======================================================================
    void Refresh()
    {
        if (!enableLetterBox) return;

        float barH = Screen.height * barPercent;

        if (displayMethod == DisplayMethod.UI_Image && topImg)
            UpdateImageBars(barH);
        else if (displayMethod == DisplayMethod.UI_TMP && topTmp)
            UpdateTmpBars(barH);
    }

    //======================================================================
    // OnGUI 描画
    //======================================================================
    void OnGUI()
    {
        if (!enableLetterBox || displayMethod != DisplayMethod.OnGUI) return;

        float h = Screen.height * barPercent;
        GUI.depth = guiDepth;

        Color prev = GUI.color;
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, h),               Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - h, Screen.width, h), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    //======================================================================
    // UI_Image
    //======================================================================
    void BuildImageBars()
    {
        EnsureRoot();
        topImg ??= CreateImage("TopBar");
        botImg ??= CreateImage("BottomBar");
    }
    void UpdateImageBars(float h = -1f)
    {
        if (h < 0) h = Screen.height * barPercent;
        SetRect(topImg.rectTransform, h, true);  topImg.color = barColor;
        SetRect(botImg.rectTransform, h, false); botImg.color = barColor;
    }

    //======================================================================
    // UI_TMP
    //======================================================================
    void BuildTmpBars()
    {
        EnsureRoot();
        topTmp ??= CreateTMP("TopTMP");
        botTmp ??= CreateTMP("BotTMP");
    }
    void UpdateTmpBars(float h = -1f)
    {
        if (h < 0) h = Screen.height * barPercent;
        FillTMP(topTmp, h, true);
        FillTMP(botTmp, h, false);
    }

    //======================================================================
    // Utility
    //======================================================================
    void EnsureRoot()
    {
        if (uiRoot) return;
        var go = new GameObject("LetterboxRoot", typeof(RectTransform));
        uiRoot = go.GetComponent<RectTransform>();
        uiRoot.SetParent(transform, false);
        uiRoot.localRotation = Quaternion.identity;
    }

    Image CreateImage(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(uiRoot, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI CreateTMP(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(uiRoot, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font     = blockFont ? blockFont : tmp.font;
        tmp.raycastTarget = false;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    void SetRect(RectTransform rt, float h, bool top)
    {
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot     = new Vector2(0.5f, top ? 1f : 0f);
        rt.sizeDelta = new Vector2(0, h);
        rt.anchoredPosition = Vector2.zero;
    }

    void FillTMP(TextMeshProUGUI tmp, float h, bool top)
    {
        tmp.fontSize = h;
        int count = Mathf.CeilToInt(Screen.width / (h * 0.5f)) + 2;
        tmp.text  = new string('█', count);
        tmp.color = barColor;
        SetRect(tmp.rectTransform, h, top);
    }

    void DestroyUI()
    {
        if (uiRoot) DestroyImmediate(uiRoot.gameObject);
        uiRoot = null; topImg = botImg = null; topTmp = botTmp = null;
    }
}
