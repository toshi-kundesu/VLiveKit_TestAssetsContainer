using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Timeline;
using TMPro;

/// <summary>
/// LTC（hh:mm:ss:ff）を表示するユーティリティ。<br/>
/// ● <b>TimeSource</b> で「タイムラインの現在時刻」か「ゲーム時間（Time.time）」を選択可。<br/>
/// ● OnGUI／uGUI Text／TMP Text に出力方式を切替え可能。<br/>
/// </summary>
[ExecuteAlways]
public class LtcDisplay : MonoBehaviour
{
    public enum DisplayMethod { OnGUI, UI_Text, UI_TextMeshPro }
    public enum FrameType     { FPS24 = 24, FPS30 = 30, FPS60 = 60 }
    public enum TimeSource    { Sequence, GameTime }   // ★ 追加

    //────────────────────────────────────
    // 基本設定
    //────────────────────────────────────
    [Header("表示方式")]
    [SerializeField] DisplayMethod displayMethod = DisplayMethod.OnGUI;

    [Header("タイムソース")]
    [SerializeField] TimeSource    timeSource = TimeSource.Sequence;   // ★ 追加
    [SerializeField] PlayableDirector timelineDirector;               // Sequence 用

    // フレーム設定
    [Header("フレームレート")]
    [SerializeField] bool      copyFrameTypeFromSequence = true;
    [SerializeField] FrameType frameType = FrameType.FPS24;

    // 表示スタイル
    [Header("表示スタイル")]
    [SerializeField] string prefix   = "";
    [SerializeField] Color  playColor  = Color.white;
    [SerializeField] Color  pauseColor = Color.gray;
    [SerializeField] int    fontSize   = 48;
    [SerializeField] Vector2 anchoredPosition = new(10f, -60f);

    // OnGUI 用参照解像度
    [Header("参照解像度 (OnGUI)")]
    [SerializeField] int referenceWidth  = 1920;
    [SerializeField] int referenceHeight = 1080;

    // UI 参照
    [Header("UI_Text / TMP 参照 (自動取得可)")]
    [SerializeField] Text           uiText;
    [SerializeField] TextMeshProUGUI tmpText;

    //────────────────────────────────────
    // 内部
    //────────────────────────────────────
    GUIStyle guiStyle;

    //======================================================================
    // Unity イベント
    //======================================================================
    void Awake()      { CacheLabels(); InitLayout(); UpdateText(); }
    void OnValidate() { CacheLabels(); InitLayout(); UpdateText(); }
    void Update()     { UpdateText(); }

    //======================================================================
    // OnGUI
    //======================================================================
    void OnGUI()
    {
        if (displayMethod != DisplayMethod.OnGUI) return;
        if (guiStyle == null) InitGuiStyle();

        float scale = Mathf.Min((float)Screen.width / referenceWidth,
                                (float)Screen.height / referenceHeight);

        Matrix4x4 prev = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                                   new Vector3(scale, scale, 1));

        float y = anchoredPosition.y >= 0
                  ? anchoredPosition.y
                  : referenceHeight + anchoredPosition.y - fontSize;

        GUI.Label(new Rect(anchoredPosition.x, y, 500, fontSize + 10),
                  BuildTimeString(), guiStyle);

        GUI.matrix = prev;
    }

    //======================================================================
    // テキスト生成
    //======================================================================
    string BuildTimeString()
    {
        double t = 0;

        if (timeSource == TimeSource.Sequence)
        {
            if (timelineDirector == null)
                return prefix + "--:--:--:--";
            t = timelineDirector.time;
        }
        else // GameTime
        {
            t = Time.time; // ゲーム起動からの経過時間
        }

        int fps = Mathf.Max(1, GetCurrentFps());
        int h = (int)(t / 3600);
        int m = (int)((t % 3600) / 60);
        int s = (int)(t % 60);
        int f = (int)((t * fps) % fps);

        return $"{prefix}{h:00}:{m:00}:{s:00}:{f:00}";
    }

    int GetCurrentFps()
    {
        if (timeSource == TimeSource.GameTime)      // GameTime は指定フレームで処理
            return (int)frameType;

        if (!copyFrameTypeFromSequence || timelineDirector == null)
            return (int)frameType;

        if (timelineDirector.playableAsset is TimelineAsset tl)
            return Mathf.RoundToInt((float)tl.editorSettings.frameRate);

        return (int)frameType;
    }

    //======================================================================
    // レイアウト & スタイル
    //======================================================================
    void CacheLabels()
    {
        if (!uiText  && displayMethod == DisplayMethod.UI_Text)
            uiText = GetComponent<Text>();
        if (!tmpText && displayMethod == DisplayMethod.UI_TextMeshPro)
            tmpText = GetComponent<TextMeshProUGUI>();
    }

    void InitLayout()
    {
        if (displayMethod == DisplayMethod.OnGUI) InitGuiStyle();

        if (displayMethod == DisplayMethod.UI_Text)        ApplyRect(uiText?.rectTransform);
        if (displayMethod == DisplayMethod.UI_TextMeshPro) ApplyRect(tmpText?.rectTransform);

        ApplyFontAndColor();
    }

    void InitGuiStyle()
    {
        guiStyle = new GUIStyle { fontSize = fontSize, normal = { textColor = playColor } };
    }

    void ApplyFontAndColor()
    {
        Color col = IsPaused() ? pauseColor : playColor;

        if (uiText)  { uiText.fontSize = fontSize;  uiText.color  = col; }
        if (tmpText) { tmpText.fontSize = fontSize; tmpText.color = col; }
        if (guiStyle != null) { guiStyle.fontSize = fontSize; guiStyle.normal.textColor = col; }
    }

    void ApplyRect(RectTransform rt)
    {
        if (!rt) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 左上
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
    }

    bool IsPaused()
    {
        if (timeSource == TimeSource.GameTime) return false;
        return timelineDirector && timelineDirector.state == PlayState.Paused;
    }

    void UpdateText()
    {
        ApplyFontAndColor();
        string txt = BuildTimeString();

        if (displayMethod == DisplayMethod.UI_Text       && uiText)  uiText.text = txt;
        if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpText) tmpText.text = txt;
    }
}
