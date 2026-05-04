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
        private const string MenuRoot = "Tools/VLiveKit/Recommended Settings";

        [MenuItem(MenuRoot + "/Open")]
        public static void Open()
        {
            GetWindow<VLiveKitRecommendedSettings>("VLiveKit Settings");
        }

        [MenuItem(MenuRoot + "/Apply Recommended Settings")]
        public static void ApplyRecommendedSettings()
        {
            ApplyCoreRenderPipelineAdditionalProperties();

            Debug.Log("[VLiveKit] Applied recommended editor settings.");
            EditorUtility.DisplayDialog(
                "VLiveKit Recommended Settings",
                "Recommended editor settings have been applied.\n\nCore Render Pipeline > Additional Properties: All Visible",
                "OK"
            );
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

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Recommended Settings", GUILayout.Height(36)))
            {
                ApplyRecommendedSettings();
            }

            EditorGUILayout.HelpBox(
                "Sets Core Render Pipeline > Additional Properties > Visibility to All Visible, so HDRP advanced properties are visible from the start.",
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
