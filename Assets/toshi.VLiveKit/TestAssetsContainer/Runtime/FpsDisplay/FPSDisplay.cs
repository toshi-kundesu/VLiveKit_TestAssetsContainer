using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class FpsDisplay : MonoBehaviour
{
    public enum DisplayMethod
    {
        OnGUI,
        UI_Text,
        UI_TextMeshPro
    }

    public enum ScreenAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [Header("表示方式")]
    [SerializeField] private DisplayMethod displayMethod = DisplayMethod.OnGUI;

    [Header("計測")]
    [SerializeField, Min(0.05f)] private float updateInterval = 0.5f;
    [SerializeField] private bool measureInEditMode = false;
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("表示内容")]
    [SerializeField] private string prefix = "FPS: ";
    [SerializeField] private string separator = "  ";
    [SerializeField, Range(0, 3)] private int decimalPlaces = 1;
    [SerializeField] private bool showMilliseconds = true;
    [SerializeField] private bool showAverage = false;
    [SerializeField] private bool showMinMax = false;

    [Header("レイアウト")]
    [SerializeField] private ScreenAnchor anchor = ScreenAnchor.TopLeft;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(10f, -10f);
    [SerializeField] private Vector2 uiSize = new Vector2(420f, 80f);

    [Header("OnGUI 用")]
    [SerializeField] private int referenceWidth = 1920;
    [SerializeField] private int referenceHeight = 1080;
    [SerializeField] private Vector2 onGuiSize = new Vector2(420f, 90f);
    [SerializeField] private bool drawBackground = true;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private bool drawShadow = true;
    [SerializeField] private Vector2 shadowOffset = new Vector2(2f, 2f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.8f);

    [Header("フォント")]
    [SerializeField, Min(1)] private int fontSize = 32;
    [SerializeField] private Color fontColor = Color.white;

    [Header("FPS 色しきい値")]
    [SerializeField] private bool useColorThresholds = true;
    [SerializeField] private float warningFps = 45f;
    [SerializeField] private float criticalFps = 30f;
    [SerializeField] private Color goodColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.75f, 0.1f, 1f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("UI_Text / TMP 参照")]
    [SerializeField] private Text uiText;
    [SerializeField] private TextMeshProUGUI tmpText;

    private int frameCount;
    private float timeAcc;
    private float lastTimestamp;

    private float minFrameTime;
    private float maxFrameTime;

    private long totalFrameCount;
    private double totalTime;

    private float currentFps;
    private float currentMs;
    private float averageFps;
    private float windowMinFps;
    private float windowMaxFps;

    private string cachedText;
    private GUIStyle guiStyle;
    private readonly StringBuilder builder = new StringBuilder(128);

    private void Reset()
    {
        AutoAssignReferences();
        ResetStats();
        ForceRefresh();
    }

    private void Awake()
    {
        AutoAssignReferences();

        if (Application.isPlaying && dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        ResetStats();
        ForceRefresh();
    }

    private void OnEnable()
    {
        AutoAssignReferences();
        ResetStats();
        ForceRefresh();
    }

    private void OnValidate()
    {
        updateInterval = Mathf.Max(0.05f, updateInterval);
        referenceWidth = Mathf.Max(1, referenceWidth);
        referenceHeight = Mathf.Max(1, referenceHeight);
        fontSize = Mathf.Max(1, fontSize);

        AutoAssignReferences();
        ForceRefresh();
    }

    private void Update()
    {
        if (!Application.isPlaying && !measureInEditMode)
        {
            SetPreviewText();
            return;
        }

        float now = Time.realtimeSinceStartup;

        if (lastTimestamp <= 0f)
        {
            lastTimestamp = now;
            return;
        }

        float deltaTime = now - lastTimestamp;
        lastTimestamp = now;

        if (deltaTime <= 0f)
        {
            return;
        }

        // Editor復帰時などの極端な外れ値を避ける
        if (deltaTime > 5f)
        {
            ResetSamplingWindow();
            return;
        }

        frameCount++;
        timeAcc += deltaTime;

        minFrameTime = Mathf.Min(minFrameTime, deltaTime);
        maxFrameTime = Mathf.Max(maxFrameTime, deltaTime);

        if (timeAcc >= updateInterval)
        {
            currentFps = frameCount / timeAcc;
            currentMs = frameCount > 0 ? (timeAcc / frameCount) * 1000f : 0f;

            windowMinFps = maxFrameTime > 0f ? 1f / maxFrameTime : 0f;
            windowMaxFps = minFrameTime < float.MaxValue ? 1f / minFrameTime : 0f;

            totalFrameCount += frameCount;
            totalTime += timeAcc;
            averageFps = totalTime > 0.0 ? (float)(totalFrameCount / totalTime) : 0f;

            ResetSamplingWindow();
            UpdateDisplayText();
        }
    }

    private void OnGUI()
    {
        if (displayMethod != DisplayMethod.OnGUI)
        {
            return;
        }

        if (guiStyle == null)
        {
            InitGuiStyle();
        }

        string text = string.IsNullOrEmpty(cachedText) ? BuildInactiveText() : cachedText;

        float scale = Mathf.Min(
            (float)Screen.width / Mathf.Max(1, referenceWidth),
            (float)Screen.height / Mathf.Max(1, referenceHeight)
        );

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;

        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(scale, scale, 1f)
        );

        Rect rect = GetOnGuiRect();

        if (drawBackground)
        {
            GUI.color = backgroundColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        Color textColor = GetActiveColor();

        if (drawShadow)
        {
            Color previousTextColor = guiStyle.normal.textColor;

            guiStyle.normal.textColor = shadowColor;
            Rect shadowRect = rect;
            shadowRect.position += shadowOffset;
            GUI.Label(shadowRect, text, guiStyle);

            guiStyle.normal.textColor = previousTextColor;
        }

        guiStyle.fontSize = fontSize;
        guiStyle.alignment = GetTextAnchor();
        guiStyle.normal.textColor = textColor;

        GUI.Label(rect, text, guiStyle);

        GUI.color = previousColor;
        GUI.matrix = previousMatrix;
    }

    public void AutoAssignReferences()
    {
        if (!uiText)
        {
            uiText = GetComponent<Text>();
        }

        if (!tmpText)
        {
            tmpText = GetComponent<TextMeshProUGUI>();
        }
    }

    public void ForceRefresh()
    {
        AutoAssignReferences();
        InitGuiStyle();
        ApplyRectTransform();
        UpdateDisplayText();
    }

    public void ResetStats()
    {
        frameCount = 0;
        timeAcc = 0f;
        lastTimestamp = 0f;

        currentFps = 0f;
        currentMs = 0f;
        averageFps = 0f;
        windowMinFps = 0f;
        windowMaxFps = 0f;

        totalFrameCount = 0;
        totalTime = 0.0;

        ResetSamplingWindow();

        cachedText = BuildInactiveText();
        PushTextToUI();
    }

    private void ResetSamplingWindow()
    {
        frameCount = 0;
        timeAcc = 0f;
        minFrameTime = float.MaxValue;
        maxFrameTime = 0f;
    }

    private void SetPreviewText()
    {
        string preview = BuildInactiveText();

        if (cachedText == preview)
        {
            return;
        }

        cachedText = preview;
        ApplyFontSizeAndColor();
        PushTextToUI();
    }

    private string BuildInactiveText()
    {
        return prefix + "--";
    }

    private void UpdateDisplayText()
    {
        cachedText = BuildText();
        ApplyFontSizeAndColor();
        PushTextToUI();
    }

    private string BuildText()
    {
        int decimals = Mathf.Clamp(decimalPlaces, 0, 3);
        string fpsFormat = "F" + decimals;

        builder.Length = 0;

        builder.Append(prefix);
        builder.Append(currentFps > 0f ? currentFps.ToString(fpsFormat) : "--");

        if (showMilliseconds && currentMs > 0f)
        {
            builder.Append(separator);
            builder.Append(currentMs.ToString("F2"));
            builder.Append(" ms");
        }

        if (showAverage && averageFps > 0f)
        {
            builder.Append(separator);
            builder.Append("Avg ");
            builder.Append(averageFps.ToString(fpsFormat));
        }

        if (showMinMax && windowMinFps > 0f && windowMaxFps > 0f)
        {
            builder.Append(separator);
            builder.Append("Min ");
            builder.Append(windowMinFps.ToString(fpsFormat));
            builder.Append(" / Max ");
            builder.Append(windowMaxFps.ToString(fpsFormat));
        }

        return builder.ToString();
    }

    private void PushTextToUI()
    {
        if (displayMethod == DisplayMethod.UI_Text && uiText)
        {
            uiText.text = cachedText;
        }

        if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpText)
        {
            tmpText.text = cachedText;
        }
    }

    private void InitGuiStyle()
    {
        if (guiStyle == null)
        {
            guiStyle = new GUIStyle();
        }

        guiStyle.fontSize = fontSize;
        guiStyle.normal.textColor = GetActiveColor();
        guiStyle.alignment = GetTextAnchor();
        guiStyle.clipping = TextClipping.Overflow;
        guiStyle.wordWrap = false;
        guiStyle.richText = false;
    }

    private void ApplyFontSizeAndColor()
    {
        Color activeColor = GetActiveColor();

        if (uiText)
        {
            uiText.fontSize = fontSize;
            uiText.color = activeColor;
            uiText.alignment = GetTextAnchor();
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.raycastTarget = false;
        }

        if (tmpText)
        {
            tmpText.fontSize = fontSize;
            tmpText.color = activeColor;
            tmpText.alignment = GetTmpAlignment();
            tmpText.enableWordWrapping = false;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            tmpText.raycastTarget = false;
        }

        if (guiStyle != null)
        {
            guiStyle.fontSize = fontSize;
            guiStyle.normal.textColor = activeColor;
            guiStyle.alignment = GetTextAnchor();
        }
    }

    private Color GetActiveColor()
    {
        if (!useColorThresholds || currentFps <= 0f)
        {
            return fontColor;
        }

        if (currentFps < criticalFps)
        {
            return criticalColor;
        }

        if (currentFps < warningFps)
        {
            return warningColor;
        }

        return goodColor;
    }

    private void ApplyRectTransform()
    {
        if (displayMethod == DisplayMethod.UI_Text && uiText)
        {
            ApplyRect(uiText.rectTransform);
        }

        if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpText)
        {
            ApplyRect(tmpText.rectTransform);
        }

        ApplyFontSizeAndColor();
    }

    private void ApplyRect(RectTransform rectTransform)
    {
        if (!rectTransform)
        {
            return;
        }

        Vector2 anchorVector = GetAnchorVector();

        rectTransform.anchorMin = anchorVector;
        rectTransform.anchorMax = anchorVector;
        rectTransform.pivot = anchorVector;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = uiSize;
    }

    private Rect GetOnGuiRect()
    {
        float x;
        float y;

        switch (anchor)
        {
            case ScreenAnchor.TopRight:
                x = referenceWidth + anchoredPosition.x - onGuiSize.x;
                y = -anchoredPosition.y;
                break;

            case ScreenAnchor.BottomLeft:
                x = anchoredPosition.x;
                y = referenceHeight - anchoredPosition.y - onGuiSize.y;
                break;

            case ScreenAnchor.BottomRight:
                x = referenceWidth + anchoredPosition.x - onGuiSize.x;
                y = referenceHeight - anchoredPosition.y - onGuiSize.y;
                break;

            case ScreenAnchor.TopLeft:
            default:
                x = anchoredPosition.x;
                y = -anchoredPosition.y;
                break;
        }

        return new Rect(x, y, onGuiSize.x, onGuiSize.y);
    }

    private Vector2 GetAnchorVector()
    {
        switch (anchor)
        {
            case ScreenAnchor.TopRight:
                return new Vector2(1f, 1f);

            case ScreenAnchor.BottomLeft:
                return new Vector2(0f, 0f);

            case ScreenAnchor.BottomRight:
                return new Vector2(1f, 0f);

            case ScreenAnchor.TopLeft:
            default:
                return new Vector2(0f, 1f);
        }
    }

    private TextAnchor GetTextAnchor()
    {
        switch (anchor)
        {
            case ScreenAnchor.TopRight:
                return TextAnchor.UpperRight;

            case ScreenAnchor.BottomLeft:
                return TextAnchor.LowerLeft;

            case ScreenAnchor.BottomRight:
                return TextAnchor.LowerRight;

            case ScreenAnchor.TopLeft:
            default:
                return TextAnchor.UpperLeft;
        }
    }

    private TextAlignmentOptions GetTmpAlignment()
    {
        switch (anchor)
        {
            case ScreenAnchor.TopRight:
                return TextAlignmentOptions.TopRight;

            case ScreenAnchor.BottomLeft:
                return TextAlignmentOptions.BottomLeft;

            case ScreenAnchor.BottomRight:
                return TextAlignmentOptions.BottomRight;

            case ScreenAnchor.TopLeft:
            default:
                return TextAlignmentOptions.TopLeft;
        }
    }
}