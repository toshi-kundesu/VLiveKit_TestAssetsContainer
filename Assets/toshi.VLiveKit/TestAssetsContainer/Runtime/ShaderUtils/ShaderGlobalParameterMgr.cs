// VLiveKit is all Unlicense.
// unlicense: https://unlicense.org/
// this comment & namespace can be removed. you can use this code freely.
// last update: 2024/11/26

using UnityEngine;
using System;
using System.Collections.Generic;

namespace toshi.VLiveKit.Utility
{
    [Serializable]
    public class FloatShaderParameter
    {
        public string parameterName;
        public float value = 1f;
    }

    [Serializable]
    public class ColorShaderParameter
    {
        public string parameterName;
        public Color value = Color.white;
    }

    [Serializable]
    public class VectorShaderParameter
    {
        public string parameterName;
        public Vector4 value = Vector4.zero;
    }

    [ExecuteInEditMode]
    public class ShaderGlobalParameterMgr : MonoBehaviour
    {
        public List<FloatShaderParameter> floatParameters = new List<FloatShaderParameter>();
        public List<ColorShaderParameter> colorParameters = new List<ColorShaderParameter>();
        public List<VectorShaderParameter> vectorParameters = new List<VectorShaderParameter>();

        void Update()
        {
            foreach (var param in floatParameters)
            {
                Shader.SetGlobalFloat(param.parameterName, param.value);
            }

            foreach (var param in colorParameters)
            {
                Shader.SetGlobalColor(param.parameterName, param.value);
            }

            foreach (var param in vectorParameters)
            {
                Shader.SetGlobalVector(param.parameterName, param.value);
            }
        }

        // エディタで使いやすいように、パラメータを追加するヘルパーメソッド
        public void AddFloatParameter(string name, float defaultValue = 1f)
        {
            var param = new FloatShaderParameter
            {
                parameterName = name,
                value = defaultValue
            };
            floatParameters.Add(param);
        }

        public void AddColorParameter(string name, Color defaultValue = default)
        {
            var param = new ColorShaderParameter
            {
                parameterName = name,
                value = defaultValue
            };
            colorParameters.Add(param);
        }

        public void AddVectorParameter(string name, Vector4 defaultValue = default)
        {
            var param = new VectorShaderParameter
            {
                parameterName = name,
                value = defaultValue
            };
            vectorParameters.Add(param);
        }

        // カスタムエディタを使用するために追加
        void OnValidate()
        {
            foreach (var param in floatParameters)
            {
                param.value = param.value; // これでClampが実行されます
            }
            // Add similar validation if needed for colorParameters and vectorParameters
        }
    }

}
