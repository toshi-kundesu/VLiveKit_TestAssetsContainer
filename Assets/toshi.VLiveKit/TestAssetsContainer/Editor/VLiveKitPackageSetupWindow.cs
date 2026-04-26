#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class VLiveKitPackageSetupWindow : EditorWindow
{
    private string packageName = "com.toshi.vlivekit.cameraunit";
    private string displayName = "VLiveKit Camera Unit";
    private string version = "0.0.1";
    private string description = "VLiveKit package.";
    private string asmdefName = "toshi.VLiveKit.CameraUnit";
    private string rootNamespace = "toshi.VLiveKit.Photography";

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
        EditorGUILayout.LabelField("Create package.json / asmdef", EditorStyles.boldLabel);

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

        if (GUILayout.Button("Create In Selected Folder", GUILayout.Height(32)))
        {
            CreateSetup();
        }

        EditorGUILayout.HelpBox(
            "Projectビューで package root にしたいフォルダを選択してから実行。\n" +
            "例: Assets/toshi.VLiveKit/VLiveCameraUnit",
            MessageType.Info
        );
    }

    private void CreateSetup()
    {
        string rootPath = GetSelectedFolderPath();

        if (string.IsNullOrEmpty(rootPath))
        {
            EditorUtility.DisplayDialog("Error", "Projectビューでフォルダを選択してください。", "OK");
            return;
        }

        CreatePackageJson(rootPath);

        string runtimePath = createRuntimeFolder ? Path.Combine(rootPath, "Runtime") : rootPath;
        string editorPath = createEditorFolder ? Path.Combine(rootPath, "Editor") : Path.Combine(rootPath, "Editor");

        Directory.CreateDirectory(runtimePath);
        Directory.CreateDirectory(editorPath);

        CreateRuntimeAsmdef(runtimePath);
        CreateEditorAsmdef(editorPath);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Done",
            $"Created package setup in:\n{rootPath}",
            "OK"
        );
    }

    private string GetSelectedFolderPath()
    {
        Object selected = Selection.activeObject;

        if (selected == null)
            return null;

        string path = AssetDatabase.GetAssetPath(selected);

        if (string.IsNullOrEmpty(path))
            return null;

        if (File.Exists(path))
            path = Path.GetDirectoryName(path);

        return path?.Replace("\\", "/");
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
            Debug.LogWarning($"Skipped existing file: {path}");
            return;
        }

        File.WriteAllText(path, content);
        Debug.Log($"Created: {path}");
    }
}
#endif