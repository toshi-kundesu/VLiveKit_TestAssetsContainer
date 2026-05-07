#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace toshi.VLiveKit.TestAssetsContainer.Editor
{
    public sealed class VLiveKitRecommendedSettings : EditorWindow
    {
        private const string AdditionalPropertiesPreferenceKey = "General.ShowAllAdditionalProperties";
        private const string MenuRoot = "toshi/VLiveKit/Project/Recommended Settings";
        private const string TagManagerAssetPath = "ProjectSettings/TagManager.asset";

        private static readonly string[] CameraLayerNames =
        {
            "cam01",
            "cam02",
            "cam03",
            "cam04",
            "cam05",
            "cam06",
            "cam07",
            "cam08"
        };

        [MenuItem(MenuRoot + "/Open")]
        public static void Open()
        {
            GetWindow<VLiveKitRecommendedSettings>("VLiveKit Settings");
        }

        [MenuItem(MenuRoot + "/Apply Recommended Settings")]
        public static void ApplyRecommendedSettings()
        {
            ApplyCoreRenderPipelineAdditionalProperties();
            ApplyRunInBackground();
            ApplyRecommendedCameraLayers(false);

            Debug.Log("[VLiveKit] Applied recommended editor settings.");
            EditorUtility.DisplayDialog(
                "VLiveKit Recommended Settings",
                "Recommended editor settings have been applied.\n\nCore Render Pipeline > Additional Properties: All Visible\nPlayer > Run In Background: Enabled\nCamera layers: cam01-cam08",
                "OK"
            );
        }

        [MenuItem(MenuRoot + "/Apply Camera Layers cam01-cam08")]
        public static void ApplyRecommendedCameraLayersMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Apply VLiveKit Camera Layers",
                    "This will add cam01-cam08 to empty User Layer slots only.\n\nExisting layer names are not overwritten. If your project already uses many custom layers, review Project Settings > Tags and Layers before applying.",
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            ApplyRecommendedCameraLayers(true);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VLiveKit Recommended Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "Core RP Additional Properties",
                    EditorPrefs.GetBool(AdditionalPropertiesPreferenceKey, false)
                );
            }

            EditorGUILayout.LabelField("Camera Layers", BuildCameraLayerStatusText());
            EditorGUILayout.Toggle("Run In Background", PlayerSettings.runInBackground);

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Recommended Settings", GUILayout.Height(36)))
            {
                ApplyRecommendedSettings();
            }

            if (GUILayout.Button("Apply Camera Layers cam01-cam08", GUILayout.Height(28)))
            {
                ApplyRecommendedCameraLayersMenu();
            }

            if (GUILayout.Button("Enable Run In Background", GUILayout.Height(28)))
            {
                ApplyRunInBackground();
            }

            EditorGUILayout.HelpBox(
                "Sets Core Render Pipeline > Additional Properties > Visibility to All Visible, enables Player > Run In Background, and adds camera layers only to empty User Layer slots. Existing custom layers are preserved.",
                MessageType.Info
            );
        }

        private static void ApplyCoreRenderPipelineAdditionalProperties()
        {
            if (TrySetCoreRenderPipelinePreference(true))
            {
                return;
            }

            EditorPrefs.SetBool(AdditionalPropertiesPreferenceKey, true);
            SetVolumeComponentAdditionalPropertiesPreferences(true);
            InvokeAdditionalPropertiesVisibilityCallbacks(true);
            InternalEditorUtility.RepaintAllViews();
        }

        private static void ApplyRunInBackground()
        {
            PlayerSettings.runInBackground = true;
            Debug.Log("[VLiveKit] Enabled Player > Resolution and Presentation > Run In Background.");
        }

        private static void ApplyRecommendedCameraLayers(bool showDialog)
        {
            SerializedObject tagManager = LoadTagManager();
            if (tagManager == null)
            {
                Debug.LogWarning("[VLiveKit] Could not load ProjectSettings/TagManager.asset.");
                return;
            }

            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogWarning("[VLiveKit] Could not find layer settings in TagManager.asset.");
                return;
            }

            int addedCount = 0;
            foreach (string layerName in CameraLayerNames)
            {
                if (HasLayer(layers, layerName))
                {
                    continue;
                }

                int emptyIndex = FindEmptyUserLayerIndex(layers);
                if (emptyIndex < 0)
                {
                    string message = $"No empty User Layer slot remains. Could not add '{layerName}'. Existing layers were not overwritten.";
                    Debug.LogWarning($"[VLiveKit] {message}");
                    if (showDialog)
                    {
                        EditorUtility.DisplayDialog("VLiveKit Camera Layers", message, "OK");
                    }

                    break;
                }

                layers.GetArrayElementAtIndex(emptyIndex).stringValue = layerName;
                addedCount++;
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            string result = $"Camera layers checked. Added {addedCount} layer(s). Existing layers were preserved.";
            Debug.Log($"[VLiveKit] {result}");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("VLiveKit Camera Layers", result, "OK");
            }
        }

        private static SerializedObject LoadTagManager()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerAssetPath);
            return assets != null && assets.Length > 0 ? new SerializedObject(assets[0]) : null;
        }

        private static bool HasLayer(SerializedProperty layers, string layerName)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindEmptyUserLayerIndex(SerializedProperty layers)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string BuildCameraLayerStatusText()
        {
            SerializedObject tagManager = LoadTagManager();
            SerializedProperty layers = tagManager?.FindProperty("layers");
            if (layers == null)
            {
                return "Unavailable";
            }

            int existingCount = CameraLayerNames.Count(layerName => HasLayer(layers, layerName));
            return $"{existingCount}/{CameraLayerNames.Length} configured";
        }

        private static bool TrySetCoreRenderPipelinePreference(bool visible)
        {
            Type preferencesType = Type.GetType(
                "UnityEditor.Rendering.AdditionalPropertiesPreferences, Unity.RenderPipelines.Core.Editor"
            );

            PropertyInfo showAllProperty = preferencesType?.GetProperty(
                "showAllAdditionalProperties",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            if (showAllProperty == null)
            {
                return false;
            }

            try
            {
                showAllProperty.SetValue(null, visible);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[VLiveKit] Failed to apply Core RP preference via setter. Falling back. {exception.Message}");
                return false;
            }
        }

        private static void InvokeAdditionalPropertiesVisibilityCallbacks(bool visible)
        {
            foreach (MethodInfo method in AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(GetLoadableTypes)
                         .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                         .Where(HasSetAdditionalPropertiesVisibilityAttribute))
            {
                try
                {
                    method.Invoke(null, new object[] { visible });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[VLiveKit] Failed to invoke additional properties callback: {method.DeclaringType?.FullName}.{method.Name}. {exception.Message}");
                }
            }
        }

        private static void SetVolumeComponentAdditionalPropertiesPreferences(bool visible)
        {
            Type editorType = Type.GetType(
                "UnityEditor.Rendering.VolumeComponentEditor, Unity.RenderPipelines.Core.Editor"
            );

            MethodInfo getKeyMethod = editorType?.GetMethod(
                "GetAdditionalPropertiesPreferenceKey",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            if (editorType == null || getKeyMethod == null)
            {
                return;
            }

            foreach (Type derivedType in TypeCache.GetTypesDerivedFrom(editorType).Where(type => !type.IsAbstract))
            {
                string key = getKeyMethod.Invoke(null, new object[] { derivedType }) as string;
                if (!string.IsNullOrEmpty(key))
                {
                    EditorPrefs.SetBool(key, visible);
                }
            }
        }

        private static bool HasSetAdditionalPropertiesVisibilityAttribute(MethodInfo method)
        {
            return method.GetParameters().Length == 1
                   && method.GetParameters()[0].ParameterType == typeof(bool)
                   && method.GetCustomAttributes(false).Any(attribute =>
                       attribute.GetType().FullName == "UnityEditor.Rendering.SetAdditionalPropertiesVisibilityAttribute");
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }
    }
}
#endif
