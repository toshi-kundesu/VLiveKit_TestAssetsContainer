#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[CustomEditor(typeof(FpsDisplay))]
[CanEditMultipleObjects]
public class FpsDisplayEditor : Editor
{
    private SerializedProperty displayMethod;

    private SerializedProperty updateInterval;
    private SerializedProperty measureInEditMode;
    private SerializedProperty dontDestroyOnLoad;

    private SerializedProperty prefix;
    private SerializedProperty separator;
    private SerializedProperty decimalPlaces;
    private SerializedProperty showMilliseconds;
    private SerializedProperty showAverage;
    private SerializedProperty showMinMax;

    private SerializedProperty anchor;
    private SerializedProperty anchoredPosition;
    private SerializedProperty uiSize;

    private SerializedProperty referenceWidth;
    private SerializedProperty referenceHeight;
    private SerializedProperty onGuiSize;
    private SerializedProperty drawBackground;
    private SerializedProperty backgroundColor;
    private SerializedProperty drawShadow;
    private SerializedProperty shadowOffset;
    private SerializedProperty shadowColor;

    private SerializedProperty fontSize;
    private SerializedProperty fontColor;

    private SerializedProperty useColorThresholds;
    private SerializedProperty warningFps;
    private SerializedProperty criticalFps;
    private SerializedProperty goodColor;
    private SerializedProperty warningColor;
    private SerializedProperty criticalColor;

    private SerializedProperty uiText;
    private SerializedProperty tmpText;

    private void OnEnable()
    {
        displayMethod = serializedObject.FindProperty("displayMethod");

        updateInterval = serializedObject.FindProperty("updateInterval");
        measureInEditMode = serializedObject.FindProperty("measureInEditMode");
        dontDestroyOnLoad = serializedObject.FindProperty("dontDestroyOnLoad");

        prefix = serializedObject.FindProperty("prefix");
        separator = serializedObject.FindProperty("separator");
        decimalPlaces = serializedObject.FindProperty("decimalPlaces");
        showMilliseconds = serializedObject.FindProperty("showMilliseconds");
        showAverage = serializedObject.FindProperty("showAverage");
        showMinMax = serializedObject.FindProperty("showMinMax");

        anchor = serializedObject.FindProperty("anchor");
        anchoredPosition = serializedObject.FindProperty("anchoredPosition");
        uiSize = serializedObject.FindProperty("uiSize");

        referenceWidth = serializedObject.FindProperty("referenceWidth");
        referenceHeight = serializedObject.FindProperty("referenceHeight");
        onGuiSize = serializedObject.FindProperty("onGuiSize");
        drawBackground = serializedObject.FindProperty("drawBackground");
        backgroundColor = serializedObject.FindProperty("backgroundColor");
        drawShadow = serializedObject.FindProperty("drawShadow");
        shadowOffset = serializedObject.FindProperty("shadowOffset");
        shadowColor = serializedObject.FindProperty("shadowColor");

        fontSize = serializedObject.FindProperty("fontSize");
        fontColor = serializedObject.FindProperty("fontColor");

        useColorThresholds = serializedObject.FindProperty("useColorThresholds");
        warningFps = serializedObject.FindProperty("warningFps");
        criticalFps = serializedObject.FindProperty("criticalFps");
        goodColor = serializedObject.FindProperty("goodColor");
        warningColor = serializedObject.FindProperty("warningColor");
        criticalColor = serializedObject.FindProperty("criticalColor");

        uiText = serializedObject.FindProperty("uiText");
        tmpText = serializedObject.FindProperty("tmpText");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        DrawDisplaySection();
        DrawMeasureSection();
        DrawContentSection();
        DrawLayoutSection();
        DrawStyleSection();
        DrawReferenceSection();
        DrawUtilityButtons();

        bool changed = EditorGUI.EndChangeCheck();

        serializedObject.ApplyModifiedProperties();

        if (changed)
        {
            RefreshTargets();
        }
    }

    private void DrawDisplaySection()
    {
        EditorGUILayout.LabelField("表示方式", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(displayMethod);

        FpsDisplay.DisplayMethod method =
            (FpsDisplay.DisplayMethod)displayMethod.enumValueIndex;

        if (method == FpsDisplay.DisplayMethod.OnGUI)
        {
            EditorGUILayout.HelpBox(
                "OnGUI は即席デバッグには便利ですが、常時表示や本番寄りの検証では TextMeshPro 表示を推奨します。",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(8);
    }

    private void DrawMeasureSection()
    {
        EditorGUILayout.LabelField("計測", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(updateInterval);
        EditorGUILayout.PropertyField(measureInEditMode);
        EditorGUILayout.PropertyField(dontDestroyOnLoad);

        EditorGUILayout.Space(8);
    }

    private void DrawContentSection()
    {
        EditorGUILayout.LabelField("表示内容", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(prefix);
        EditorGUILayout.PropertyField(separator);
        EditorGUILayout.PropertyField(decimalPlaces);
        EditorGUILayout.PropertyField(showMilliseconds);
        EditorGUILayout.PropertyField(showAverage);
        EditorGUILayout.PropertyField(showMinMax);

        EditorGUILayout.Space(8);
    }

    private void DrawLayoutSection()
    {
        EditorGUILayout.LabelField("レイアウト", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(anchor);
        EditorGUILayout.PropertyField(anchoredPosition);
        EditorGUILayout.PropertyField(uiSize);

        FpsDisplay.DisplayMethod method =
            (FpsDisplay.DisplayMethod)displayMethod.enumValueIndex;

        if (method == FpsDisplay.DisplayMethod.OnGUI)
        {
            EditorGUILayout.PropertyField(referenceWidth);
            EditorGUILayout.PropertyField(referenceHeight);
            EditorGUILayout.PropertyField(onGuiSize);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(drawBackground);

            if (drawBackground.boolValue)
            {
                EditorGUILayout.PropertyField(backgroundColor);
            }

            EditorGUILayout.PropertyField(drawShadow);

            if (drawShadow.boolValue)
            {
                EditorGUILayout.PropertyField(shadowOffset);
                EditorGUILayout.PropertyField(shadowColor);
            }
        }

        EditorGUILayout.Space(8);
    }

    private void DrawStyleSection()
    {
        EditorGUILayout.LabelField("スタイル", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fontSize);
        EditorGUILayout.PropertyField(fontColor);

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(useColorThresholds);

        if (useColorThresholds.boolValue)
        {
            EditorGUILayout.PropertyField(warningFps);
            EditorGUILayout.PropertyField(criticalFps);
            EditorGUILayout.PropertyField(goodColor);
            EditorGUILayout.PropertyField(warningColor);
            EditorGUILayout.PropertyField(criticalColor);
        }

        EditorGUILayout.Space(8);
    }

    private void DrawReferenceSection()
    {
        EditorGUILayout.LabelField("参照", EditorStyles.boldLabel);

        FpsDisplay.DisplayMethod method =
            (FpsDisplay.DisplayMethod)displayMethod.enumValueIndex;

        if (method == FpsDisplay.DisplayMethod.UI_Text)
        {
            EditorGUILayout.PropertyField(uiText);

            if (uiText.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "UI Text の参照がありません。下のボタンで作成・割り当てできます。",
                    MessageType.Warning
                );
            }
        }
        else if (method == FpsDisplay.DisplayMethod.UI_TextMeshPro)
        {
            EditorGUILayout.PropertyField(tmpText);

            if (tmpText.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "TextMeshProUGUI の参照がありません。下のボタンで作成・割り当てできます。",
                    MessageType.Warning
                );
            }
        }
        else
        {
            EditorGUILayout.PropertyField(uiText);
            EditorGUILayout.PropertyField(tmpText);
        }

        EditorGUILayout.Space(8);
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.LabelField("ツール", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Text を作成/割り当て"))
            {
                CreateAndBindLabel(false);
            }

            if (GUILayout.Button("TMP を作成/割り当て"))
            {
                CreateAndBindLabel(true);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("参照を自動取得"))
            {
                foreach (Object obj in targets)
                {
                    FpsDisplay display = (FpsDisplay)obj;
                    Undo.RecordObject(display, "Auto Assign FPS Display References");
                    display.AutoAssignReferences();
                    display.ForceRefresh();
                    EditorUtility.SetDirty(display);
                }
            }

            if (GUILayout.Button("レイアウト反映"))
            {
                RefreshTargets();
            }
        }

        if (GUILayout.Button("統計をリセット"))
        {
            foreach (Object obj in targets)
            {
                FpsDisplay display = (FpsDisplay)obj;
                Undo.RecordObject(display, "Reset FPS Display Stats");
                display.ResetStats();
                display.ForceRefresh();
                EditorUtility.SetDirty(display);
            }
        }

        EditorGUILayout.Space(4);
    }

    private void RefreshTargets()
    {
        foreach (Object obj in targets)
        {
            FpsDisplay display = (FpsDisplay)obj;
            if (!display)
            {
                continue;
            }

            display.ForceRefresh();
            EditorUtility.SetDirty(display);
        }
    }

    private void CreateAndBindLabel(bool useTmp)
    {
        serializedObject.ApplyModifiedProperties();

        foreach (Object obj in targets)
        {
            FpsDisplay display = (FpsDisplay)obj;
            if (!display)
            {
                continue;
            }

            Canvas canvas = FindOrCreateCanvas(display.transform);
            GameObject labelObject = CreateLabelObject(canvas.transform, useTmp);

            SerializedObject so = new SerializedObject(display);
            so.Update();

            if (useTmp)
            {
                TextMeshProUGUI tmp = labelObject.GetComponent<TextMeshProUGUI>();
                so.FindProperty("displayMethod").enumValueIndex =
                    (int)FpsDisplay.DisplayMethod.UI_TextMeshPro;
                so.FindProperty("tmpText").objectReferenceValue = tmp;
            }
            else
            {
                Text text = labelObject.GetComponent<Text>();
                so.FindProperty("displayMethod").enumValueIndex =
                    (int)FpsDisplay.DisplayMethod.UI_Text;
                so.FindProperty("uiText").objectReferenceValue = text;
            }

            so.ApplyModifiedProperties();

            display.ForceRefresh();
            EditorUtility.SetDirty(display);
            Selection.activeGameObject = labelObject;
        }

        serializedObject.Update();
    }

    private static GameObject CreateLabelObject(Transform parent, bool useTmp)
    {
        string objectName = useTmp ? "FPS Display TMP" : "FPS Display Text";

        GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(labelObject, "Create FPS Display Label");

        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(420f, 80f);

        if (useTmp)
        {
            TextMeshProUGUI tmp = Undo.AddComponent<TextMeshProUGUI>(labelObject);
            tmp.text = "FPS: --";
            tmp.fontSize = 32f;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }
        }
        else
        {
            Text text = Undo.AddComponent<Text>(labelObject);
            text.text = "FPS: --";
            text.fontSize = 32;
            text.color = Color.white;
            text.raycastTarget = false;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Font builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (builtinFont != null)
            {
                text.font = builtinFont;
            }
        }

        return labelObject;
    }

    private static Canvas FindOrCreateCanvas(Transform context)
    {
        if (context)
        {
            Canvas parentCanvas = context.GetComponentInParent<Canvas>();
            if (parentCanvas)
            {
                return parentCanvas;
            }
        }

        Canvas existingCanvas = Object.FindObjectOfType<Canvas>();
        if (existingCanvas)
        {
            return existingCanvas;
        }

        GameObject canvasObject = new GameObject(
            "Debug Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Debug Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    [MenuItem("GameObject/UI/FPS Display/TextMeshPro", false, 10)]
    private static void CreateTmpDisplayFromMenu()
    {
        CreateDisplayFromMenu(true);
    }

    [MenuItem("GameObject/UI/FPS Display/Legacy Text", false, 11)]
    private static void CreateTextDisplayFromMenu()
    {
        CreateDisplayFromMenu(false);
    }

    private static void CreateDisplayFromMenu(bool useTmp)
    {
        Canvas canvas = FindOrCreateCanvas(Selection.activeTransform);
        GameObject labelObject = CreateLabelObject(canvas.transform, useTmp);

        FpsDisplay display = Undo.AddComponent<FpsDisplay>(labelObject);

        SerializedObject so = new SerializedObject(display);
        so.Update();

        if (useTmp)
        {
            TextMeshProUGUI tmp = labelObject.GetComponent<TextMeshProUGUI>();
            so.FindProperty("displayMethod").enumValueIndex =
                (int)FpsDisplay.DisplayMethod.UI_TextMeshPro;
            so.FindProperty("tmpText").objectReferenceValue = tmp;
        }
        else
        {
            Text text = labelObject.GetComponent<Text>();
            so.FindProperty("displayMethod").enumValueIndex =
                (int)FpsDisplay.DisplayMethod.UI_Text;
            so.FindProperty("uiText").objectReferenceValue = text;
        }

        so.ApplyModifiedProperties();

        display.ForceRefresh();
        EditorUtility.SetDirty(display);

        Selection.activeGameObject = labelObject;
    }
}

#endif