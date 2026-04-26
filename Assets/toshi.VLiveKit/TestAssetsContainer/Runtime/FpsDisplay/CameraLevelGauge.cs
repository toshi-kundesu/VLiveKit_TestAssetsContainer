using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// カメラのロール角を可視化する円形水平器。<br/>
/// ● OnGUI  : 中央円＋回転する水平線を Immediate GUI で描画<br/>
/// ● UI_Image: Canvas に Image を 2 枚生成（円は固定・線のみ回転）
/// </summary>
[ExecuteAlways]
public class CameraLevelGauge : MonoBehaviour
{
    public enum DisplayMethod { OnGUI, UI_Image }

    [Header("表示方式")]
    [SerializeField] DisplayMethod displayMethod = DisplayMethod.UI_Image;

    [Header("ターゲットカメラ (空なら Main)")]
    [SerializeField] Camera targetCamera;

    [Header("円の見た目")]
    [SerializeField] float circleDiameter = 100f;       // px
    [SerializeField] Color circleColor    = Color.white;
    [SerializeField] float circleAlpha    = 0.3f;       // 0=透過,1=塗り潰し

    [Header("水平線の見た目")]
    [SerializeField] float lineWidth       = 4f;
    [SerializeField] float lineLengthRatio = 0.8f;      // 円直径に対する割合
    [SerializeField] Color lineColor       = Color.white;

    [Header("座標オフセット (px, 画面中心基準)")]
    [SerializeField] Vector2 offset = Vector2.zero;

    //―― UI_Image 用 ------------------------------------------------------
    [Header("UI_Image 親 (自動生成可)")]
    [SerializeField] RectTransform root;
    Image circleImg, lineImg;

    // OnGUI 用テクスチャ
    Texture2D circleTex;

    //======================================================================
    // 初期化
    //======================================================================
    void Awake()      { Init(); }
    void OnValidate() { Init(); UpdateUI(0f); }

    void Init()
    {
        if (!targetCamera) targetCamera = Camera.main;

        if (displayMethod == DisplayMethod.UI_Image) CreateUI();
        else BuildGuiTex();
    }

    //======================================================================
    // 更新
    //======================================================================
    void Update()
    {
        if (!targetCamera) return;

        float roll = targetCamera.transform.eulerAngles.z;
        roll = (roll > 180f) ? roll - 360f : roll;   // → -180〜180°

        if (displayMethod == DisplayMethod.UI_Image)
            UpdateUI(roll);
    }

    //======================================================================
    // UI_Image モード
    //======================================================================
    void CreateUI()
    {
        if (!root)
        {
            var go = new GameObject("LevelGaugeRoot", typeof(RectTransform));
            root = go.GetComponent<RectTransform>();
            root.SetParent(transform, false);
        }

        circleImg = EnsureImage(circleImg, "GaugeCircle");
        lineImg   = EnsureImage(lineImg,   "GaugeLine");

        // スプライト生成
        Texture2D tex = MakeCircleTex((int)circleDiameter, circleColor, circleAlpha);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f), 100f);

        circleImg.sprite = sprite;
        circleImg.raycastTarget = false;

        lineImg.color = lineColor;
        lineImg.raycastTarget = false;

        UpdateUI(0f);
    }

    Image EnsureImage(Image current, string name)
    {
        if (current) return current;

        var g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(root, false);
        return g.GetComponent<Image>();
    }

    void UpdateUI(float roll)
    {
        if (!circleImg || !lineImg) return;

        // ルート位置（中央固定）
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot     = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = offset;

        // 円
        var rtC = circleImg.rectTransform;
        rtC.sizeDelta = new Vector2(circleDiameter, circleDiameter);

        // 水平線
        float len = circleDiameter * lineLengthRatio;
        var rtL = lineImg.rectTransform;
        rtL.sizeDelta = new Vector2(len, lineWidth);
        rtL.localEulerAngles = new Vector3(0f, 0f, -roll); // 逆回転で“水平”を保つ
    }

    //======================================================================
    // OnGUI モード
    //======================================================================
    void OnGUI()
    {
        if (displayMethod != DisplayMethod.OnGUI || !targetCamera) return;
        if (!circleTex) BuildGuiTex();

        float roll = targetCamera.transform.eulerAngles.z;
        roll = (roll > 180f) ? roll - 360f : roll;

        float cx = Screen.width  * 0.5f + offset.x;
        float cy = Screen.height * 0.5f - offset.y;

        // 円
        float r = circleDiameter / 2f;
        GUI.color = new Color(circleColor.r, circleColor.g, circleColor.b, circleAlpha);
        GUI.DrawTexture(new Rect(cx - r, cy - r, circleDiameter, circleDiameter), circleTex);

        // 水平線
        float len = circleDiameter * lineLengthRatio;
        GUI.color = lineColor;
        Matrix4x4 prev = GUI.matrix;
        GUIUtility.RotateAroundPivot(-roll, new Vector2(cx, cy));
        GUI.DrawTexture(new Rect(cx - len / 2f, cy - lineWidth / 2f,
                                 len, lineWidth), Texture2D.whiteTexture);
        GUI.matrix = prev;
    }

    void BuildGuiTex()
    {
        circleTex = MakeCircleTex((int)circleDiameter, circleColor, 1f);
    }

    //======================================================================
    // 共通ユーティリティ
    //======================================================================
    static Texture2D MakeCircleTex(int d, Color col, float alpha = 1f)
    {
        var tex = new Texture2D(d, d, TextureFormat.ARGB32, false);
        float r  = d / 2f;
        float r2 = r * r;
        var pix  = new Color[d * d];

        for (int y = 0; y < d; y++)
        for (int x = 0; x < d; x++)
        {
            float dx = x - r + 0.5f;
            float dy = y - r + 0.5f;
            bool inside = dx * dx + dy * dy <= r2;
            pix[y * d + x] = inside ? new Color(col.r, col.g, col.b, alpha) : Color.clear;
        }
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
