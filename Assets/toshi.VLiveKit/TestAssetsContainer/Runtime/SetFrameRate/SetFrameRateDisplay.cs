using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 起動時に targetFrameRate を固定し、現在設定値を OnGUI／UI.Text／TMP いずれかで表示。<br/>
/// **フォントサイズ・色・位置は共通パラメータで一元管理** できるようにしました。<br/>
/// </summary>
[ExecuteAlways]
public sealed class SetFrameRateDisplay : MonoBehaviour
{
    public enum FrameRate
    {
        Film_24          = 24,
        PAL_25           = 25,
        HD_30            = 30,
        InterlacedPAL_50 = 50,
        Game_60          = 60,
        Show_72          = 72,
        Show_96          = 96,
        Show_100         = 100,
        Show_120         = 120
    }

    public enum DisplayMethod { OnGUI, UI_Text, UI_TextMeshPro }

    //────────────────────────────────────
    // 固定 FPS
    //────────────────────────────────────
    [Header("固定フレームレート")]
    [SerializeField] FrameRate frameRate = FrameRate.HD_30;

    //────────────────────────────────────
    // 表示方式
    //────────────────────────────────────
    [Header("表示方式")]
    [SerializeField] DisplayMethod displayMethod = DisplayMethod.OnGUI;

    [SerializeField] bool isShowSetFrameRate = true;

    //────────────────────────────────────
    // 共通レイアウト & スタイル
    //────────────────────────────────────
    [Header("レイアウト / スタイル (共通)")]
    [SerializeField] int   fontSize  = 48;
    [SerializeField] Color fontColor = Color.white;

    [Tooltip("画面左上基準 +X → 右, +Y → 下 (ピクセル)")]
    [SerializeField] Vector2 offset = new(10f, 10f);

    [Tooltip("OnGUI のラベル幅 / 高さ (ピクセル)")]
    [SerializeField] Vector2 labelSize = new(500f, 100f);

    //────────────────────────────────────
    // UI 参照 (自動取得可)
    //────────────────────────────────────
    [Header("uGUI / TMP 参照 (自動取得可)")]
    [SerializeField] Text           uiText;
    [SerializeField] TextMeshProUGUI tmpText;

    //────────────────────────────────────
    // 内部
    //────────────────────────────────────
    GUIStyle guiStyle;

    //======================================================================
    // 初期化
    //======================================================================
    void Awake()
    {
        QualitySettings.vSyncCount  = 0;
        Application.targetFrameRate = (int)frameRate;

        CacheUiRefs();
        ApplyLayoutAndStyle();
        UpdateLabel();
    }

    void OnValidate()
    {
        Application.targetFrameRate = (int)frameRate;

        CacheUiRefs();
        ApplyLayoutAndStyle();
        UpdateLabel();
    }

    //======================================================================
    // 更新
    //======================================================================
    void Update() => UpdateLabel();

    //======================================================================
    // OnGUI
    //======================================================================
    void OnGUI()
    {
        if (!isShowSetFrameRate || displayMethod != DisplayMethod.OnGUI) return;

        if (guiStyle == null) InitGuiStyle();

        Rect r = new(offset.x, offset.y, labelSize.x, labelSize.y);
        GUI.Label(r, $"SetFPS: {Application.targetFrameRate}", guiStyle);
    }

    void InitGuiStyle()
    {
        guiStyle = new GUIStyle
        {
            fontSize = fontSize,
            normal   = { textColor = fontColor }
        };
    }

    //======================================================================
    // UI.Text / TMP
    //======================================================================
    void CacheUiRefs()
    {
        if (displayMethod == DisplayMethod.UI_Text && !uiText)
            uiText = GetComponent<Text>();

        if (displayMethod == DisplayMethod.UI_TextMeshPro && !tmpText)
            tmpText = GetComponent<TextMeshProUGUI>();
    }

    void ApplyLayoutAndStyle()
    {
        // 共通スタイル
        if (uiText)
        {
            uiText.fontSize = fontSize;
            uiText.color    = fontColor;
            ApplyRect(uiText.rectTransform);
        }

        if (tmpText)
        {
            tmpText.fontSize = fontSize;
            tmpText.color    = fontColor;
            ApplyRect(tmpText.rectTransform);
        }

        if (guiStyle != null)
        {
            guiStyle.fontSize         = fontSize;
            guiStyle.normal.textColor = fontColor;
        }
    }

    void ApplyRect(RectTransform rt)
    {
        // 画面左上アンカーに固定
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 左上
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(offset.x, -offset.y);
    }

    void UpdateLabel()
    {
        if (!isShowSetFrameRate) return;

        string txt = $"SetFPS: {Application.targetFrameRate}";

        if (displayMethod == DisplayMethod.UI_Text && uiText)
            uiText.text = txt;
        else if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpText)
            tmpText.text = txt;
    }
}
