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

    [MenuItem("Window/toshi/VLiveKit/Create Package Setup")]
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

        EditorGUILayout.HelpBox(
            "Target Folder に package root にしたいフォルダをドラッグ&ドロップしてください。\n" +
            "例: Assets/toshi.VLiveKit/ArtNetLink",
            MessageType.Info
        );
    }

    private void CreateSetup()
    {
        string rootPath = GetTargetFolderPath();

        if (string.IsNullOrEmpty(rootPath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Target Folder に有効なフォルダを指定してください。",
                "OK"
            );
            return;
        }

        CreatePackageJson(rootPath);

        string runtimePath = createRuntimeFolder
            ? Path.Combine(rootPath, "Runtime")
            : rootPath;

        string editorPath = Path.Combine(rootPath, "Editor");

        if (createRuntimeFolder)
            Directory.CreateDirectory(runtimePath);

        if (createEditorFolder)
            Directory.CreateDirectory(editorPath);

        CreateRuntimeAsmdef(runtimePath);

        if (createEditorFolder)
            CreateEditorAsmdef(editorPath);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Done",
            $"Created package setup in:\n{rootPath}",
            "OK"
        );
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
            Debug.LogWarning($"[VLiveKit] Skipped existing file: {path}");
            return;
        }

        File.WriteAllText(path, content);
        Debug.Log($"[VLiveKit] Created: {path}");
    }
}
#endif
