#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class VLiveKitPackageSetupWindow : EditorWindow
{
    [SerializeField] private DefaultAsset targetFolder;

    private string packageName = "com.toshi.vlivekit.artnetlink";
    private string displayName = "VLiveKit ArtNetLink";
    private string version = "0.0.1";
    private string description = "VLiveKit package.";

    private string asmdefName = "toshi.VLiveKit.ArtNetLink";
    private string rootNamespace = "toshi.VLiveKit.ArtNetLink";

    private bool createRuntimeFolder = true;
    private bool createEditorFolder = true;
    private bool overwrite = false;

    private string statusMessage = "Ready.";
    private int createdCount;
    private int skippedCount;

    public static void Open()
    {
        GetWindow<VLiveKitPackageSetupWindow>("VLiveKit Package Setup");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("VLiveKit Package Setup", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Target Folder",
            targetFolder,
            typeof(DefaultAsset),
            false
        );

        EditorGUILayout.Space();

        packageName = EditorGUILayout.TextField("Package Name", packageName);
        displayName = EditorGUILayout.TextField("Display Name", displayName);
        version = EditorGUILayout.TextField("Version", version);
        description = EditorGUILayout.TextField("Description", description);

        EditorGUILayout.Space();

        asmdefName = EditorGUILayout.TextField("Runtime asmdef", asmdefName);
        rootNamespace = EditorGUILayout.TextField("Root Namespace", rootNamespace);

        EditorGUILayout.Space();

        createRuntimeFolder = EditorGUILayout.Toggle("Create Runtime folder", createRuntimeFolder);
        createEditorFolder = EditorGUILayout.Toggle("Create Editor folder", createEditorFolder);
        overwrite = EditorGUILayout.Toggle("Overwrite existing files", overwrite);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Package Setup", GUILayout.Height(36)))
        {
            CreateSetup();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(statusMessage, EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.HelpBox(
            "Select the package root folder, such as Assets/toshi.VLiveKit/ArtNetLink. Existing files are skipped unless overwrite is enabled.",
            MessageType.None
        );
    }

    private void CreateSetup()
    {
        string rootPath = GetTargetFolderPath();

        if (string.IsNullOrEmpty(rootPath))
        {
            statusMessage = "Select a valid package root folder.";
            ShowNotification(new GUIContent("Select a valid target folder"));
            return;
        }

        string runtimePath = createRuntimeFolder
            ? Path.Combine(rootPath, "Runtime")
            : rootPath;

        string editorPath = Path.Combine(rootPath, "Editor");

        if (overwrite && WillOverwriteAnyFile(rootPath, runtimePath, editorPath))
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Overwrite package setup files?",
                "Existing package.json or asmdef files in the target folder may be replaced.",
                "Create",
                "Cancel"
            );

            if (!confirmed)
            {
                statusMessage = "Package setup unchanged.";
                return;
            }
        }

        createdCount = 0;
        skippedCount = 0;

        CreatePackageJson(rootPath);

        if (createRuntimeFolder)
            Directory.CreateDirectory(runtimePath);

        if (createEditorFolder)
            Directory.CreateDirectory(editorPath);

        CreateRuntimeAsmdef(runtimePath);

        if (createEditorFolder)
            CreateEditorAsmdef(editorPath);

        AssetDatabase.Refresh();

        statusMessage = $"Package setup created in {rootPath}. Wrote {createdCount} file(s), skipped {skippedCount}.";
        ShowNotification(new GUIContent("Package setup created"));
    }

    private string GetTargetFolderPath()
    {
        if (targetFolder == null)
            return null;

        string path = AssetDatabase.GetAssetPath(targetFolder);

        if (string.IsNullOrEmpty(path))
            return null;

        if (!AssetDatabase.IsValidFolder(path))
            return null;

        return path.Replace("\\", "/");
    }

    private bool WillOverwriteAnyFile(string rootPath, string runtimePath, string editorPath)
    {
        if (File.Exists(Path.Combine(rootPath, "package.json")))
            return true;

        if (File.Exists(Path.Combine(runtimePath, asmdefName + ".asmdef")))
            return true;

        if (createEditorFolder && File.Exists(Path.Combine(editorPath, asmdefName + ".Editor.asmdef")))
            return true;

        return false;
    }

    private void CreatePackageJson(string rootPath)
    {
        string path = Path.Combine(rootPath, "package.json");

        string json =
$@"{{
  ""name"": ""{packageName}"",
  ""displayName"": ""{displayName}"",
  ""version"": ""{version}"",
  ""unity"": ""2022.3"",
  ""description"": ""{description}"",
  ""author"": {{
    ""name"": ""toshi""
  }},
  ""dependencies"": {{
    ""com.unity.cinemachine"": ""2.9.7""
  }}
}}";

        WriteFile(path, json);
    }

    private void CreateRuntimeAsmdef(string folderPath)
    {
        string path = Path.Combine(folderPath, asmdefName + ".asmdef");

        string json =
$@"{{
  ""name"": ""{asmdefName}"",
  ""rootNamespace"": ""{rootNamespace}"",
  ""references"": [
    ""Unity.Cinemachine""
  ],
  ""includePlatforms"": [],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": false,
  ""precompiledReferences"": [],
  ""autoReferenced"": true,
  ""defineConstraints"": [],
  ""versionDefines"": [],
  ""noEngineReferences"": false
}}";

        WriteFile(path, json);
    }

    private void CreateEditorAsmdef(string folderPath)
    {
        string editorAsmdefName = asmdefName + ".Editor";
        string path = Path.Combine(folderPath, editorAsmdefName + ".asmdef");

        string json =
$@"{{
  ""name"": ""{editorAsmdefName}"",
  ""rootNamespace"": ""{rootNamespace}.Editor"",
  ""references"": [
    ""{asmdefName}""
  ],
  ""includePlatforms"": [
    ""Editor""
  ],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": false,
  ""precompiledReferences"": [],
  ""autoReferenced"": true,
  ""defineConstraints"": [],
  ""versionDefines"": [],
  ""noEngineReferences"": false
}}";

        WriteFile(path, json);
    }

    private void WriteFile(string path, string content)
    {
        path = path.Replace("\\", "/");

        if (File.Exists(path) && !overwrite)
        {
            skippedCount++;
            Debug.Log($"[VLiveKit] Skipped existing file: {path}");
            return;
        }

        File.WriteAllText(path, content);
        createdCount++;
        Debug.Log($"[VLiveKit] Created: {path}");
    }
}
#endif
