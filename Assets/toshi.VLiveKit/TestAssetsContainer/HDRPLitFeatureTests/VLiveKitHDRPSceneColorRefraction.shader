Shader "toshi/VLiveKit/Test/HDRP Lit Feature/Scene Color Refraction"
{
    Properties
    {
        [MainColor] _Tint("Tint", Color) = (1, 1, 1, 1)
        _RelativeRefractionIndex("Relative Refraction Index", Range(0.0, 1.0)) = 0.75
        [PowerSlider(5)] _Distance("Distance", Range(0.0, 100.0)) = 10.0
        _SceneColorLod("Scene Color Mip", Range(0.0, 8.0)) = 0.0
        _Opacity("Opacity", Range(0.0, 1.0)) = 1.0
        _NormalMap("Normal Map", 2D) = "bump" {}
        _UseNormalMap("Use Normal Map", Range(0.0, 1.0)) = 0.0
        _NormalScale("Normal Scale", Range(0.0, 4.0)) = 1.0
        [ToggleUI] _ZWrite("ZWrite", Float) = 0.0
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 2.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #define SUPPORT_GLOBAL_MIP_BIAS
    #define PREFER_HALF 0

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    TEXTURE2D(_NormalMap);
    SAMPLER(sampler_NormalMap);

    CBUFFER_START(UnityPerMaterial)
    float4 _Tint;
    float4 _NormalMap_ST;
    float _RelativeRefractionIndex;
    float _Distance;
    float _SceneColorLod;
    float _Opacity;
    float _UseNormalMap;
    float _NormalScale;
    CBUFFER_END

    float3 VLiveKitGetNormalWS(FragInputs input)
    {
        float3 normalWS = SafeNormalize(input.tangentToWorld[2]);

        if (_UseNormalMap > 0.0)
        {
            float2 uv = input.texCoord0.xy * _NormalMap_ST.xy + _NormalMap_ST.zw;
            float4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv);
            float3 normalTS = UnpackNormalMapRGorAG(normalSample, _NormalScale);
            normalWS = SafeNormalize(TransformTangentToWorld(normalTS, input.tangentToWorld));
        }

        return normalWS;
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex Vert
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_FORWARD_UNLIT
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            #define VARYINGS_NEED_TEXCOORD0

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

            PackedVaryingsType Vert(AttributesMesh inputMesh)
            {
                VaryingsType varyingsType;
                ZERO_INITIALIZE(VaryingsType, varyingsType);
                varyingsType.vmesh = VertMesh(inputMesh);
                return PackVaryingsType(varyingsType);
            }

            void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);

                FragInputs input = UnpackVaryingsToFragInputs(packedInput);
                float3 normalWS = VLiveKitGetNormalWS(input);
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

                float3 refractDir = refract(-V, normalWS, _RelativeRefractionIndex);
                float3 samplingPositionRWS = input.positionRWS + refractDir * _Distance;
                float2 samplingUV = ComputeNormalizedDeviceCoordinates(samplingPositionRWS, UNITY_MATRIX_VP);
                samplingUV = clamp(samplingUV, 0.0, 1.0 - _ScreenSize.zw);

                float3 sceneColor = SampleCameraColor(samplingUV, _SceneColorLod);
                outColor = float4(sceneColor * _Tint.rgb, saturate(_Opacity * _Tint.a));
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/HDRP/FallbackError"
    CustomEditor "toshi.VLiveKit.TestAssetsContainer.Editor.VLiveKitHDRPSceneColorRefractionShaderGUI"
}
