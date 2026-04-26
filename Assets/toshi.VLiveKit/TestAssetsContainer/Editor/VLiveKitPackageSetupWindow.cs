#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class VLiveKitPackageSetupWindow : EditorWindow
{
    private string packageName = "com.toshi.vlivekit.newpackage";
    private string displayName = "VLiveKit NewPackage";
    private string version = "0.0.1";
    private string description = "VLiveKit package.";

    private string asmdefName = "toshi.VLiveKit.NewPackage";
    private string rootNamespace = "toshi.VLiveKit.NewPackage";

    private bool createRuntimeFolder = true;
    private bool createEditorFolder = true;
    private bool overwrite = false;

    [MenuItem("Tools/VLiveKit/Create Package Setup")]
    public static void Open()
    {
        GetWindow<VLiveKitPackageSetupWindow>("VLiveKit Package Setup");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Package Setup Generator", EditorStyles.boldLabel);
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

        if (GUILayout.Button("Create In Selected Folder", GUILayout.Height(36)))
        {
            CreateSetup();
        }

        EditorGUILayout.HelpBox(
            "Projectビューでフォルダを選択 → 実行\n" +
            "そのフォルダ直下が package root になります。",
            MessageType.Info
        );
    }

    private void CreateSetup()
    {
        string rootPath = GetSelectedFolderPath();

        if (string.IsNullOrEmpty(rootPath))
        {
            EditorUtility.DisplayDialog("Error", "フォルダを選択してください", "OK");
            return;
        }

        Debug.Log($"[VLiveKit] Create package at: {rootPath}");

        CreatePackageJson(rootPath);

        string runtimePath = createRuntimeFolder ? Path.Combine(rootPath, "Runtime") : rootPath;
        string editorPath = Path.Combine(rootPath, "Editor");

        if (createRuntimeFolder) Directory.CreateDirectory(runtimePath);
        if (createEditorFolder) Directory.CreateDirectory(editorPath);

        CreateRuntimeAsmdef(runtimePath);

        if (createEditorFolder)
            CreateEditorAsmdef(editorPath);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done", "Package setup created!", "OK");
    }

    // ⭐ここが今回の本質
    private string GetSelectedFolderPath()
    {
        Object selected = Selection.activeObject;

        if (selected == null)
            return null;

        string path = AssetDatabase.GetAssetPath(selected);

        if (string.IsNullOrEmpty(path))
            return null;

        // フォルダならそのまま使う
        if (AssetDatabase.IsValidFolder(path))
            return path.Replace("\\", "/");

        // ファイルなら親フォルダ
        string parent = Path.GetDirectoryName(path);
        return parent?.Replace("\\", "/");
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
            Debug.LogWarning($"[VLiveKit] Skip existing: {path}");
            return;
        }

        File.WriteAllText(path, content);
        Debug.Log($"[VLiveKit] Created: {path}");
    }
}
#endif