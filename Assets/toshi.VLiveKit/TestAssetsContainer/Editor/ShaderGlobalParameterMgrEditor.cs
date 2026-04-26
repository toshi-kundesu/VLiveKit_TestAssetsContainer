// // VLiveKit is all Unlicense.
// // unlicense: https://unlicense.org/
// // this comment & namespace can be removed. you can use this code freely.
// // last update: 2024/11/26

// using UnityEngine;
// using UnityEditor;

// namespace toshi.VLiveKit.Utility
// {
//     [CustomEditor(typeof(ShaderGlobalParameterMgr))]
//     public class ShaderGlobalParameterMgrEditor : Editor
//     {
//         public override void OnInspectorGUI()
//         {
//             var mgr = (ShaderGlobalParameterMgr)target;
            
//             EditorGUI.BeginChangeCheck();
            
//             SerializedProperty paramList = serializedObject.FindProperty("parameters");
//             EditorGUILayout.PropertyField(paramList.FindPropertyRelative("Array.size"));
            
//             for (int i = 0; i < paramList.arraySize; i++)
//             {
//                 SerializedProperty param = paramList.GetArrayElementAtIndex(i);
                
//                 EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
//                 // パラメータ名
//                 EditorGUILayout.PropertyField(param.FindPropertyRelative("parameterName"));
//                 // 最小値
//                 EditorGUILayout.PropertyField(param.FindPropertyRelative("minValue"));
//                 // 最大値
//                 EditorGUILayout.PropertyField(param.FindPropertyRelative("maxValue"));
                
//                 // 現在値のスライダー
//                 SerializedProperty valueProp = param.FindPropertyRelative("value");
//                 SerializedProperty minProp = param.FindPropertyRelative("minValue");
//                 SerializedProperty maxProp = param.FindPropertyRelative("maxValue");
//                 valueProp.floatValue = EditorGUILayout.Slider("Value", valueProp.floatValue, minProp.floatValue, maxProp.floatValue);
                
//                 EditorGUILayout.EndVertical();
//             }
            
//             if (EditorGUI.EndChangeCheck())
//             {
//                 serializedObject.ApplyModifiedProperties();
//             }
//         }
//     }
// }