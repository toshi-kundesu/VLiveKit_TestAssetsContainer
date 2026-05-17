#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class VLiveKitSceneDescriptionSceneWatcher
{
    private const string MenuPath = "toshi/VLiveKit/Test Assets/Scene Description/Show Current Scene Description";

    static VLiveKitSceneDescriptionSceneWatcher()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    [MenuItem(MenuPath)]
    private static void ShowCurrentSceneDescription()
    {
        var scene = SceneManager.GetActiveScene();
        var descriptions = CollectDescriptions(scene, requireAutoShow: false);

        if (descriptions.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Scene Description",
                "No VLiveKitSceneDescription components were found in the active scene.",
                "OK");
            return;
        }

        VLiveKitSceneDescriptionWindow.ShowForScene(scene, descriptions);
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.delayCall += () => TryShowSceneDescription(scene);
    }

    private static void TryShowSceneDescription(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded)
            return;

        var descriptions = CollectDescriptions(scene, requireAutoShow: true);
        for (var i = descriptions.Count - 1; i >= 0; i--)
        {
            var description = descriptions[i];
            if (!ShouldShowInSession(description))
                descriptions.RemoveAt(i);
        }

        if (descriptions.Count == 0)
            return;

        foreach (var description in descriptions)
            MarkShownInSession(description);

        VLiveKitSceneDescriptionWindow.ShowForScene(scene, descriptions);
    }

    internal static List<VLiveKitSceneDescription> CollectDescriptions(Scene scene, bool requireAutoShow)
    {
        var results = new List<VLiveKitSceneDescription>();
        if (!scene.IsValid() || !scene.isLoaded)
            return results;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var descriptions = root.GetComponentsInChildren<VLiveKitSceneDescription>(true);
            foreach (var description in descriptions)
            {
                if (!description || !description.enabled || !description.HasContent)
                    continue;

                if (requireAutoShow && !description.ShowOnSceneOpen)
                    continue;

                results.Add(description);
            }
        }

        return results;
    }

    private static bool ShouldShowInSession(VLiveKitSceneDescription description)
    {
        if (!description.ShowOnlyOncePerEditorSession)
            return true;

        return !SessionState.GetBool(GetSessionKey(description), false);
    }

    private static void MarkShownInSession(VLiveKitSceneDescription description)
    {
        if (description.ShowOnlyOncePerEditorSession)
            SessionState.SetBool(GetSessionKey(description), true);
    }

    private static string GetSessionKey(VLiveKitSceneDescription description)
    {
        var objectId = GlobalObjectId.GetGlobalObjectIdSlow(description);
        return "VLiveKit.SceneDescription.Seen." + objectId;
    }
}

[CustomEditor(typeof(VLiveKitSceneDescription))]
internal sealed class VLiveKitSceneDescriptionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(!((VLiveKitSceneDescription)target).HasContent))
        {
            if (GUILayout.Button("Preview Popup"))
                VLiveKitSceneDescriptionWindow.ShowForDescriptions("Preview", new[] { (VLiveKitSceneDescription)target });
        }
    }
}

internal sealed class VLiveKitSceneDescriptionWindow : EditorWindow
{
    private List<Entry> entries = new List<Entry>();
    private string sceneName = "Scene Description";
    private Vector2 scroll;
    private GUIStyle sceneNameStyle;
    private GUIStyle bodyStyle;
    private GUIStyle metaStyle;

    private struct Entry
    {
        public string Title;
        public string Description;
        public Object Source;
    }

    internal static void ShowForScene(Scene scene, IReadOnlyList<VLiveKitSceneDescription> descriptions)
    {
        var title = string.IsNullOrWhiteSpace(scene.name) ? "Untitled Scene" : scene.name;
        ShowForDescriptions(title, descriptions);
    }

    internal static void ShowForDescriptions(string title, IReadOnlyList<VLiveKitSceneDescription> descriptions)
    {
        var window = CreateInstance<VLiveKitSceneDescriptionWindow>();
        window.titleContent = new GUIContent("Scene Description");
        window.sceneName = title;
        window.SetDescriptions(title, descriptions);
        window.minSize = new Vector2(420f, 240f);
        window.position = GetCenteredPosition(560f, 420f);
        window.ShowUtility();
        window.Focus();
    }

    private static Rect GetCenteredPosition(float width, float height)
    {
        var mainWindow = EditorGUIUtility.GetMainWindowPosition();
        var x = mainWindow.x + Mathf.Max(0f, (mainWindow.width - width) * 0.5f);
        var y = mainWindow.y + Mathf.Max(0f, (mainWindow.height - height) * 0.5f);
        return new Rect(x, y, width, height);
    }

    private void SetDescriptions(string fallbackTitle, IReadOnlyList<VLiveKitSceneDescription> descriptions)
    {
        if (entries == null)
            entries = new List<Entry>();

        entries.Clear();

        foreach (var description in descriptions)
        {
            if (!description || !description.HasContent)
                continue;

            entries.Add(new Entry
            {
                Title = description.GetDisplayTitle(fallbackTitle),
                Description = string.IsNullOrWhiteSpace(description.Description)
                    ? "(No description text.)"
                    : description.Description.Trim(),
                Source = description
            });
        }
    }

    private void OnGUI()
    {
        EnsureStyles();
        if (entries == null)
            entries = new List<Entry>();

        EditorGUILayout.LabelField(sceneName, sceneNameStyle);
        EditorGUILayout.LabelField("Scene notes", metaStyle);
        EditorGUILayout.Space(6);

        using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = scrollView.scrollPosition;

            foreach (var entry in entries)
                DrawEntry(entry);
        }

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(96)))
                Close();
        }
    }

    private void DrawEntry(Entry entry)
    {
        EditorGUILayout.LabelField(entry.Title, EditorStyles.boldLabel);

        var content = new GUIContent(entry.Description);
        var width = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 42f);
        var height = Mathf.Max(EditorGUIUtility.singleLineHeight * 2f, bodyStyle.CalcHeight(content, width));
        EditorGUILayout.SelectableLabel(entry.Description, bodyStyle, GUILayout.Height(height + 4f));

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!entry.Source))
            {
                if (GUILayout.Button("Select Object", GUILayout.Width(112)))
                    Selection.activeObject = entry.Source;
            }
        }

        EditorGUILayout.Space(10);
    }

    private void EnsureStyles()
    {
        if (sceneNameStyle != null && bodyStyle != null && metaStyle != null)
            return;

        sceneNameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            wordWrap = true
        };

        bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            richText = false
        };

        metaStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true
        };
    }
}
#endif
