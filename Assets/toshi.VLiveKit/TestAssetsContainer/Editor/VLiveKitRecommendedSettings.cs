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

        private string statusMessage = "Ready.";

        [MenuItem(MenuRoot + "/Open")]
        public static void Open()
        {
            GetWindow<VLiveKitRecommendedSettings>("VLiveKit Settings");
        }

        public static void ApplyRecommendedSettings()
        {
            string result = ApplyRecommendedSettingsCore();
            Debug.Log($"[VLiveKit] {result}");
            ShowWindowStatus(result, "Recommended settings applied");
        }

        public static void ApplyRecommendedCameraLayersMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Apply Camera Layers",
                    "Add cam01-cam08 to empty User Layer slots. Existing layer names stay unchanged.",
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            string result = ApplyRecommendedCameraLayers();
            Debug.Log($"[VLiveKit] {result}");
            ShowWindowStatus(result, "Camera layers checked");
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
            DrawCameraLayerPlan();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Run In Background", PlayerSettings.runInBackground);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Recommended Settings", GUILayout.Height(36)))
            {
                statusMessage = ApplyRecommendedSettingsCore();
                Debug.Log($"[VLiveKit] {statusMessage}");
                ShowNotification(new GUIContent("Recommended settings applied"));
            }

            if (GUILayout.Button("Apply Camera Layers cam01-cam08", GUILayout.Height(28)))
            {
                ApplyRecommendedCameraLayersMenu();
            }

            if (GUILayout.Button("Enable Run In Background", GUILayout.Height(28)))
            {
                ApplyRunInBackground();
                statusMessage = "Run In Background is enabled.";
                ShowNotification(new GUIContent("Run In Background enabled"));
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(statusMessage, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.HelpBox(
                "Applies Core Render Pipeline additional property visibility, Run In Background, and camera layers without replacing existing custom layer names.",
                MessageType.None
            );
        }

        private static string ApplyRecommendedSettingsCore()
        {
            ApplyCoreRenderPipelineAdditionalProperties();
            ApplyRunInBackground();
            string cameraLayerResult = ApplyRecommendedCameraLayers();

            return "Recommended settings applied. Core Render Pipeline additional properties are visible, Run In Background is enabled. "
                   + cameraLayerResult;
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

        private static string ApplyRecommendedCameraLayers()
        {
            SerializedObject tagManager = LoadTagManager();
            if (tagManager == null)
            {
                return "Camera layers were not changed because TagManager.asset could not be loaded.";
            }

            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                return "Camera layers were not changed because TagManager.asset did not expose a layer list.";
            }

            int addedCount = 0;
            string blockedLayerName = null;

            foreach (string layerName in CameraLayerNames)
            {
                if (HasLayer(layers, layerName))
                {
                    continue;
                }

                int emptyIndex = FindEmptyUserLayerIndex(layers);
                if (emptyIndex < 0)
                {
                    blockedLayerName = layerName;
                    break;
                }

                layers.GetArrayElementAtIndex(emptyIndex).stringValue = layerName;
                addedCount++;
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(blockedLayerName))
            {
                return $"Camera layers checked. Added {addedCount} layer(s). No empty User Layer slot remained for '{blockedLayerName}'. Existing layers were preserved.";
            }

            return $"Camera layers checked. Added {addedCount} layer(s). Existing layers were preserved.";
        }

        private static void DrawCameraLayerPlan()
        {
            SerializedObject tagManager = LoadTagManager();
            SerializedProperty layers = tagManager?.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                EditorGUILayout.HelpBox("Camera layer plan is unavailable because TagManager.asset could not be read.", MessageType.None);
                return;
            }

            int emptySlots = CountEmptyUserLayerSlots(layers);
            EditorGUILayout.LabelField("After Apply", $"{emptySlots} empty User Layer slot(s) available", EditorStyles.miniLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string[] plannedLayers = CopyLayerValues(layers);
                foreach (string layerName in CameraLayerNames)
                {
                    int existingIndex = FindLayerIndex(plannedLayers, layerName);
                    if (existingIndex >= 0)
                    {
                        EditorGUILayout.LabelField(layerName, $"Keep existing Layer {existingIndex}", EditorStyles.miniLabel);
                        continue;
                    }

                    int emptyIndex = FindEmptyUserLayerIndex(plannedLayers);
                    if (emptyIndex >= 0)
                    {
                        plannedLayers[emptyIndex] = layerName;
                        EditorGUILayout.LabelField(layerName, $"Add to empty Layer {emptyIndex}", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(layerName, "Not added; no empty User Layer slot", EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.HelpBox("Existing layer names are preserved. Missing cam layers are assigned to empty User Layer slots only.", MessageType.None);
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

        private static string[] CopyLayerValues(SerializedProperty layers)
        {
            var values = new string[layers.arraySize];
            for (int i = 0; i < layers.arraySize; i++)
            {
                values[i] = layers.GetArrayElementAtIndex(i).stringValue;
            }

            return values;
        }

        private static int FindLayerIndex(string[] layers, string layerName)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == layerName)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindEmptyUserLayerIndex(string[] layers)
        {
            for (int i = 8; i < layers.Length; i++)
            {
                if (string.IsNullOrEmpty(layers[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int CountEmptyUserLayerSlots(SerializedProperty layers)
        {
            int count = 0;
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    count++;
                }
            }

            return count;
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

        private static void ShowWindowStatus(string status, string notification)
        {
            var window = GetWindow<VLiveKitRecommendedSettings>("VLiveKit Settings");
            window.statusMessage = status;
            window.ShowNotification(new GUIContent(notification));
            window.Repaint();
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
                Debug.Log($"[VLiveKit] Core RP preference setter was unavailable. Falling back to editor preferences. {exception.Message}");
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
                    Debug.Log($"[VLiveKit] Additional properties callback was skipped: {method.DeclaringType?.FullName}.{method.Name}. {exception.Message}");
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
