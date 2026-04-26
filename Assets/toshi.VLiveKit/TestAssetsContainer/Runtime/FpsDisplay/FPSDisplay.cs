using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class FpsDisplay : MonoBehaviour
{
    public enum DisplayMethod { OnGUI, UI_Text, UI_TextMeshPro }

    [Header("表示方式")]
    [SerializeField] private DisplayMethod displayMethod = DisplayMethod.OnGUI;

    [Header("計測間隔 (秒)")]
    [SerializeField, Min(0.05f)] private float updateInterval = 0.5f;

    [Header("参照解像度 (OnGUI 用)")]
    [SerializeField] private int referenceWidth  = 1920;
    [SerializeField] private int referenceHeight = 1080;

    [Header("基準解像度での anchoredPosition (左上アンカー)")]
    [SerializeField] private Vector2 anchoredPosition = new(10f, -10f);

    [Header("フォントサイズ (共通)")]
    [SerializeField] private int fontSize = 32;
    [SerializeField] private Color fontColor = Color.white;
    [SerializeField] private string prefix   = "FPS: ";

    [Header("UI_Text / TMP 参照 (自動取得可)")]
    [SerializeField] private Text           uiText;
    [SerializeField] private TextMeshProUGUI tmpText;

    int   frameCount;
    float timeAcc;
    float currentFps;
    GUIStyle guiStyle;

    void Awake()
    {
        CacheUILabels();
        InitStylesAndLayout();
        UpdateText();
    }

    void OnEnable()  => InitStylesAndLayout();

    void OnValidate()
    {
        CacheUILabels();
        InitStylesAndLayout();
        UpdateText();
    }

    void Update()
    {
        frameCount++;
        timeAcc += Time.unscaledDeltaTime;

        if (timeAcc >= updateInterval)
        {
            currentFps = frameCount / timeAcc;
            frameCount = 0;
            timeAcc    = 0f;
            UpdateText();
        }
    }

    void OnGUI()
    {
        if (displayMethod != DisplayMethod.OnGUI) return;
        if (guiStyle == null) InitGuiStyle();

        float scale = Mathf.Min((float)Screen.width / referenceWidth,
                                (float)Screen.height / referenceHeight);

        Matrix4x4 prev = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                                   new Vector3(scale, scale, 1));

        float y = (anchoredPosition.y >= 0)
                  ? anchoredPosition.y
                  : referenceHeight + anchoredPosition.y - fontSize;

        Rect r = new Rect(anchoredPosition.x, y, 250, fontSize + 10);
        GUI.Label(r, $"{prefix}{currentFps:F2}", guiStyle);

        GUI.matrix = prev;
    }
    void CacheUILabels()
    {
        if (!uiText && displayMethod == DisplayMethod.UI_Text)
            uiText = GetComponent<Text>();

        if (!tmpText && displayMethod == DisplayMethod.UI_TextMeshPro)
            tmpText = GetComponent<TextMeshProUGUI>();
    }

    void InitStylesAndLayout()
    {
        if (displayMethod == DisplayMethod.OnGUI)        InitGuiStyle();
        if (displayMethod == DisplayMethod.UI_Text)      ApplyRect(uiText?.rectTransform);
        if (displayMethod == DisplayMethod.UI_TextMeshPro) ApplyRect(tmpText?.rectTransform);

        ApplyFontSizeAndColor();
    }

    void InitGuiStyle()
    {
        guiStyle = new GUIStyle
        {
            fontSize = fontSize,
            normal   = { textColor = fontColor }
        };
    }

    void ApplyFontSizeAndColor()
    {
        if (uiText)
        {
            uiText.fontSize = fontSize;
            uiText.color    = fontColor;
        }
        if (tmpText)
        {
            tmpText.fontSize = fontSize;
            tmpText.color    = fontColor;
        }
        if (guiStyle != null)
        {
            guiStyle.fontSize         = fontSize;
            guiStyle.normal.textColor = fontColor;
        }
    }

    void UpdateText()
    {
        string txt = $"{prefix}{currentFps:F2}";
        if (displayMethod == DisplayMethod.UI_Text && uiText)            uiText.text = txt;
        if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpText)    tmpText.text = txt;
    }

    void ApplyRect(RectTransform rt)
    {
        if (!rt) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 左上固定
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
    }
}
