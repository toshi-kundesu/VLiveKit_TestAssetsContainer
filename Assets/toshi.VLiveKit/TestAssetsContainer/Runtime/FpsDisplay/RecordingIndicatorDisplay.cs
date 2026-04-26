using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 録画インジケータ + モードテキスト。<br/>
/// ● 実行中: 赤点滅 + PLAY ● 停止中: 緑固定 + EDITOR<br/>
/// 「マスターオフセット」を加えて、両パーツをまとめて位置調整可。
/// </summary>
[ExecuteAlways]
public class RecordingIndicatorDisplay : MonoBehaviour
{
    public enum DisplayMethod { OnGUI, UI_Image, UI_TextMeshPro }

    //────────────────────────────────────
    // 追加: マスターオフセット
    //────────────────────────────────────
    [Header("★ マスターオフセット (右上基準)")]
    [SerializeField] Vector2 masterOffset = Vector2.zero;   // これを動かすと両方一括で動く

    //──────── インジケータ ───────────────
    [Header("点滅インジケータ")]
    [SerializeField] Vector2 circleAnchoredPos = new(-30f, -30f);
    [SerializeField] float   circleSize        = 20f;
    [SerializeField] float   blinkHz           = 1f;
    [SerializeField] Color   playCircleColor   = Color.red;
    [SerializeField] Color   editorCircleColor = Color.green;

    //──────── モードテキスト ──────────────
    [Header("モードテキスト")]
    [SerializeField] Vector2 textAnchoredPos = new(-70f, -35f);
    [SerializeField] int     textFontSize    = 48;
    [SerializeField] Color   textColor       = Color.white;
    [SerializeField] string  playModeText    = "PLAY";
    [SerializeField] string  editorModeText  = "EDITOR";

    //──────── OnGUI 用参照解像度 ──────────
    [Header("参照解像度 (OnGUI)")]
    [SerializeField] int referenceWidth  = 1920;
    [SerializeField] int referenceHeight = 1080;

    //──────── 表示方式 & UI 参照 ──────────
    [Header("表示方式")]
    [SerializeField] DisplayMethod displayMethod = DisplayMethod.OnGUI;

    [Header("UI 参照 (自動セット)")]
    [SerializeField] Image           uiCircleImage;
    [SerializeField] Text            uiModeText;
    [SerializeField] TextMeshProUGUI tmpCircle;
    [SerializeField] TextMeshProUGUI tmpModeText;

    //──────── 内部 ────────────────────────
    Texture2D circleTex;
    GUIStyle  textStyle;

    //======================================================================
    // 初期化
    //======================================================================
    void Awake()      { CacheRefs(); InitLayout(); }
    void OnValidate() { CacheRefs(); InitLayout(); }

    void CacheRefs()
    {
        if (displayMethod == DisplayMethod.UI_Image)
        {
            uiCircleImage ??= GetComponent<Image>();
            if (!uiModeText || uiModeText == uiCircleImage?.GetComponent<Text>())
                uiModeText = GetComponentInChildren<Text>(true);
        }
        else if (displayMethod == DisplayMethod.UI_TextMeshPro)
        {
            tmpCircle   ??= GetComponent<TextMeshProUGUI>();
            if (!tmpModeText || tmpModeText == tmpCircle)
                tmpModeText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    void InitLayout()
    {
        if (displayMethod == DisplayMethod.OnGUI)
        {
            textStyle = new GUIStyle { fontSize = textFontSize, normal = { textColor = textColor } };
            return;
        }

        if (displayMethod == DisplayMethod.UI_Image && uiCircleImage && uiModeText)
        {
            ApplyRect(uiCircleImage.rectTransform, circleAnchoredPos);
            ApplyRect(uiModeText.rectTransform,    textAnchoredPos);
            uiModeText.fontSize = textFontSize;
            uiModeText.color    = textColor;
        }

        if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpCircle && tmpModeText)
        {
            tmpCircle.text     = "●";
            tmpCircle.fontSize = circleSize;
            ApplyRect(tmpCircle.rectTransform, circleAnchoredPos);

            tmpModeText.fontSize = textFontSize;
            tmpModeText.color    = textColor;
            ApplyRect(tmpModeText.rectTransform, textAnchoredPos);
        }
    }

    // masterOffset を加味して配置
    void ApplyRect(RectTransform rt, Vector2 localPos)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = masterOffset + localPos;
    }

    //======================================================================
    // 更新
    //======================================================================
    void Update()
    {
        bool isPlay   = Application.isPlaying;
        float alpha   = isPlay ? Mathf.PingPong(Time.time * blinkHz, 1f) : 1f;
        Color cCol    = isPlay ? playCircleColor   : editorCircleColor;
        string mode   = isPlay ? playModeText      : editorModeText;

        if (displayMethod == DisplayMethod.UI_Image && uiCircleImage && uiModeText)
        {
            uiCircleImage.color = new Color(cCol.r, cCol.g, cCol.b, alpha);
            uiModeText.text     = mode;
        }
        else if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpCircle && tmpModeText)
        {
            tmpCircle.color   = new Color(cCol.r, cCol.g, cCol.b, alpha);
            tmpModeText.text  = mode; // 色は固定
        }
    }

    //======================================================================
    // OnGUI
    //======================================================================
    void OnGUI()
    {
        if (displayMethod != DisplayMethod.OnGUI) return;

        bool isPlay   = Application.isPlaying;
        float alpha   = isPlay ? Mathf.PingPong(Time.time * blinkHz, 1f) : 1f;
        Color cCol    = isPlay ? playCircleColor : editorCircleColor;
        string mode   = isPlay ? playModeText    : editorModeText;

        if (circleTex == null || circleTex.width != (int)circleSize || circleTex.GetPixel(0,0) != cCol)
            circleTex = MakeCircleTexture((int)circleSize, cCol);

        float scale = Mathf.Min((float)Screen.width / referenceWidth,
                                (float)Screen.height / referenceHeight);
        Matrix4x4 prev = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                                   new Vector3(scale, scale, 1));

        // Circle
        float cx = referenceWidth  + masterOffset.x + circleAnchoredPos.x - circleSize;
        float cy = masterOffset.y + circleAnchoredPos.y >= 0
                 ? masterOffset.y + circleAnchoredPos.y
                 : referenceHeight + masterOffset.y + circleAnchoredPos.y - circleSize;

        Color prevCol = GUI.color;
        GUI.color = new Color(cCol.r, cCol.g, cCol.b, alpha);
        GUI.DrawTexture(new Rect(cx, cy, circleSize, circleSize), circleTex);
        GUI.color = prevCol;

        // Text
        textStyle ??= new GUIStyle { fontSize = textFontSize };
        textStyle.normal.textColor = textColor;

        float tx = referenceWidth + masterOffset.x + textAnchoredPos.x - 300f;
        float ty = masterOffset.y + textAnchoredPos.y >= 0
                 ? masterOffset.y + textAnchoredPos.y
                 : referenceHeight + masterOffset.y + textAnchoredPos.y - textFontSize;

        GUI.Label(new Rect(tx, ty, 300, textFontSize + 4), mode, textStyle);
        GUI.matrix = prev;
    }

    //======================================================================
    // Utility
    //======================================================================
    static Texture2D MakeCircleTexture(int d, Color col)
    {
        var tex = new Texture2D(d, d, TextureFormat.ARGB32, false);
        float r = d / 2f; Vector2 c = new(r, r);
        var pix = new Color[d * d];
        for (int y=0; y<d; y++)
        for (int x=0; x<d; x++)
            pix[y*d+x] = Vector2.Distance(new(x,y), c) <= r ? col : Color.clear;
        tex.SetPixels(pix); tex.Apply(); return tex;
    }
}
