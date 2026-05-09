#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(FpsDisplay))]
[CanEditMultipleObjects]
public class FpsDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        bool changed = EditorGUI.EndChangeCheck();

        serializedObject.ApplyModifiedProperties();

        if (changed)
            RefreshTargets();

        EditorGUILayout.Space(10);
        DrawUtilityButtons();
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create TMP Label"))
                CreateAndBindLabel(true);

            if (GUILayout.Button("Create Text Label"))
                CreateAndBindLabel(false);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto Assign References"))
                ForEachTarget("Auto Assign FPS Display References", display => display.AutoAssignReferences());

            if (GUILayout.Button("Refresh Layout"))
                RefreshTargets();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset Stats"))
                ForEachTarget("Reset FPS Display Stats", display => display.ResetStats());

            if (GUILayout.Button("Reset Leak Watch"))
                ForEachTarget("Reset FPS Display Leak Watch", display => display.ResetLeakWatch());
        }

        if (GUILayout.Button("Run GC.Collect"))
            ForEachTarget("Force GC Collect For FPS Display Debug", display => display.ForceGarbageCollectionForDebug(), refresh: false);

        EditorGUILayout.HelpBox(
            "GC.Collect is intended for leak-watch checks. Use it only when you want to sample memory behavior.",
            MessageType.None);
    }

    private void ForEachTarget(string undoName, System.Action<FpsDisplay> action, bool refresh = true)
    {
        foreach (UnityEngine.Object obj in targets)
        {
            FpsDisplay display = (FpsDisplay)obj;
            if (!display)
                continue;

            Undo.RecordObject(display, undoName);
            action(display);

            if (refresh)
                display.ForceRefresh();

            EditorUtility.SetDirty(display);
        }
    }

    private void RefreshTargets()
    {
        foreach (UnityEngine.Object obj in targets)
        {
            FpsDisplay display = (FpsDisplay)obj;
            if (!display)
                continue;

            display.ForceRefresh();
            EditorUtility.SetDirty(display);
        }
    }

    private void CreateAndBindLabel(bool useTmp)
    {
        serializedObject.ApplyModifiedProperties();

        foreach (UnityEngine.Object obj in targets)
        {
            FpsDisplay display = (FpsDisplay)obj;
            if (!display)
                continue;

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
        string objectName = useTmp ? "PC Status HUD TMP" : "PC Status HUD Text";

        GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(labelObject, "Create Performance HUD Label");

        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(860f, 320f);
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(10f, -10f);

        if (useTmp)
        {
            TextMeshProUGUI tmp = Undo.AddComponent<TextMeshProUGUI>(labelObject);
            tmp.text = "FPS: --";
            tmp.fontSize = 28f;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
        }
        else
        {
            Text text = Undo.AddComponent<Text>(labelObject);
            text.text = "FPS: --";
            text.fontSize = 28;
            text.color = Color.white;
            text.raycastTarget = false;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Font builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (builtinFont == null)
                builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (builtinFont != null)
                text.font = builtinFont;
        }

        return labelObject;
    }

    private static Canvas FindOrCreateCanvas(Transform context)
    {
        if (context)
        {
            Canvas parentCanvas = context.GetComponentInParent<Canvas>();
            if (parentCanvas)
                return parentCanvas;
        }

#if UNITY_2023_1_OR_NEWER
        Canvas existingCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
#else
        Canvas existingCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
#endif

        if (existingCanvas)
            return existingCanvas;

        GameObject canvasObject = new GameObject(
            "VLiveKit HUD Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Undo.RegisterCreatedObjectUndo(canvasObject, "Create VLiveKit HUD Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    [MenuItem("GameObject/UI/VLiveKit/Performance HUD/TextMeshPro", false, 10)]
    private static void CreateTmpDisplayFromMenu()
    {
        CreateDisplayFromMenu(true);
    }

    [MenuItem("GameObject/UI/VLiveKit/Performance HUD/Legacy Text", false, 11)]
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
