#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace toshi.VLiveKit.TestAssetsContainer.Editor
{
    public sealed class VLiveKitHDRPSceneColorRefractionShaderGUI : ShaderGUI
    {
        private enum RefractionPreset
        {
            Acrylic,
            Ice,
            Water,
            Glass,
            Diamond
        }

        private RefractionPreset preset;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            MaterialProperty relativeIndex = FindProperty("_RelativeRefractionIndex", properties, false);
            if (relativeIndex == null)
                return;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                preset = (RefractionPreset)EditorGUILayout.EnumPopup("From Material", preset);
                if (GUILayout.Button("Apply", GUILayout.Width(72)))
                {
                    materialEditor.RegisterPropertyChangeUndo("Apply Refraction Preset");
                    relativeIndex.floatValue = 1.0f / GetAbsoluteRefractionIndex(preset);
                }
            }
        }

        private static float GetAbsoluteRefractionIndex(RefractionPreset preset)
        {
            switch (preset)
            {
                case RefractionPreset.Acrylic:
                    return 1.49f;
                case RefractionPreset.Ice:
                    return 1.309f;
                case RefractionPreset.Water:
                    return 1.3334f;
                case RefractionPreset.Glass:
                    return 1.5f;
                case RefractionPreset.Diamond:
                    return 2.417f;
                default:
                    return 1.0f;
            }
        }
    }
}
#endif
