// VLiveKit is all Unlicense.
// unlicense: https://unlicense.org/
// this comment & namespace can be removed.
// last update: 2025/05/31

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[ExecuteAlways]
public class SceneNameDisplay : MonoBehaviour
{
    public enum DisplayMethod { OnGUI, TextMeshPro }
    [SerializeField] private DisplayMethod displayMethod = DisplayMethod.TextMeshPro;

    [SerializeField] private string prefix = "SCENE: ";

    [SerializeField] private int referenceWidth  = 1920;
    [SerializeField] private int referenceHeight = 1080;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, 60f);

    [SerializeField] private int   fontSize  = 48;
    [SerializeField] private Color fontColor = Color.white;

    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private RectTransform   rect;

    private GUIStyle guiStyle;

    void Awake()
    {
        if (displayMethod == DisplayMethod.TextMeshPro)
        {
            if (!label) label = GetComponent<TextMeshProUGUI>();
            if (label)  rect = label.rectTransform;
            ApplyLayoutToTMPro();
        }
        else
        {
            InitGUIStyle();
        }

        UpdateSceneName();
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnValidate()
    {
        if (displayMethod == DisplayMethod.TextMeshPro && rect) ApplyLayoutToTMPro();
        if (displayMethod == DisplayMethod.OnGUI && guiStyle != null)
        {
            guiStyle.fontSize         = fontSize;
            guiStyle.normal.textColor = fontColor;
        }
        UpdateSceneName();
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __) => UpdateSceneName();

    void ApplyLayoutToTMPro()
    {
        if (!rect) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot     = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, referenceWidth);
        label.fontSize = fontSize;
        label.color    = fontColor;
    }

    void InitGUIStyle()
    {
        guiStyle = new GUIStyle
        {
            alignment = TextAnchor.LowerCenter,
            fontSize  = fontSize,
            normal    = { textColor = fontColor }
        };
    }

    void UpdateSceneName()
    {
        string text = prefix + SceneManager.GetActiveScene().name;
        if (displayMethod == DisplayMethod.TextMeshPro && label) label.text = text;
    }

    void OnGUI()
    {
        if (displayMethod != DisplayMethod.OnGUI) return;
        if (guiStyle == null) InitGUIStyle();

        float scale = Mathf.Min((float)Screen.width / referenceWidth,
                                (float)Screen.height / referenceHeight);

        Matrix4x4 prev = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                                   new Vector3(scale, scale, 1));

        string text = prefix + SceneManager.GetActiveScene().name;
        float yPos = referenceHeight - fontSize - anchoredPosition.y;
        Rect  r    = new Rect(anchoredPosition.x, yPos, referenceWidth, fontSize);

        GUI.Label(r, text, guiStyle);
        GUI.matrix = prev;
    }
}
