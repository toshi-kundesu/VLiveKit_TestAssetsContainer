Shader "toshi/VLiveKit/Test/HDRP Lit Feature/Sobel Rim Light"
{
    Properties
    {
        [MainTexture] _BaseColorMap("Base Color Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor("Shadow Color", Color) = (0.35, 0.35, 0.35, 1)
        _ShadowThreshold("Shadow Threshold", Range(0.0, 1.0)) = 0.5

        [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimThickness("Rim Thickness", Range(0.0, 0.2)) = 0.03
        _RimThreshold("Rim Threshold", Range(0.0, 10.0)) = 1.0
        _RimIntensity("Rim Intensity", Range(0.0, 8.0)) = 1.0
        _DirectionalWidth("Directional Width", Range(0.0, 1.0)) = 1.0
        _RimLightDirectionWS("Rim Light Direction WS", Vector) = (0.35, 0.65, 0.65, 0)

        [Enum(Shaded Rim,0,Rim Only,1,Raw Sobel,2)] _DebugMode("Debug Mode", Float) = 0
        _RawSobelScale("Raw Sobel Scale", Range(0.0, 1.0)) = 0.1
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 2.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #define SUPPORT_GLOBAL_MIP_BIAS
    #define PREFER_HALF 0

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    TEXTURE2D(_BaseColorMap);
    SAMPLER(sampler_BaseColorMap);

    CBUFFER_START(UnityPerMaterial)
    float4 _BaseColorMap_ST;
    float4 _BaseColor;
    float4 _ShadowColor;
    float4 _RimColor;
    float4 _RimLightDirectionWS;
    float _ShadowThreshold;
    float _RimThickness;
    float _RimThreshold;
    float _RimIntensity;
    float _DirectionalWidth;
    float _DebugMode;
    float _RawSobelScale;
    CBUFFER_END

    float VLiveKitSampleOffsetLinearDepth(float3 positionVS, float2 offsetVS)
    {
        float3 samplePositionVS = float3(positionVS.xy + offsetVS, positionVS.z);
        float2 sampleUV = ComputeNormalizedDeviceCoordinates(samplePositionVS, UNITY_MATRIX_P);
        sampleUV = clamp(sampleUV, 0.0, 1.0 - _ScreenSize.zw);

        float depth = SampleCameraDepth(sampleUV);
        return LinearEyeDepth(depth, _ZBufferParams);
    }

    float VLiveKitSobelDepthEdge(float3 positionRWS, float thicknessVS)
    {
        float3 positionVS = TransformWorldToView(positionRWS);
        float2 stepVS = max(thicknessVS, 0.0001).xx;

        float d00 = VLiveKitSampleOffsetLinearDepth(positionVS, float2(-1.0, -1.0) * stepVS);
        float d01 = VLiveKitSampleOffsetLinearDepth(positionVS, float2( 0.0, -1.0) * stepVS);
        float d02 = VLiveKitSampleOffsetLinearDepth(positionVS, float2( 1.0, -1.0) * stepVS);
        float d10 = VLiveKitSampleOffsetLinearDepth(positionVS, float2(-1.0,  0.0) * stepVS);
        float d12 = VLiveKitSampleOffsetLinearDepth(positionVS, float2( 1.0,  0.0) * stepVS);
        float d20 = VLiveKitSampleOffsetLinearDepth(positionVS, float2(-1.0,  1.0) * stepVS);
        float d21 = VLiveKitSampleOffsetLinearDepth(positionVS, float2( 0.0,  1.0) * stepVS);
        float d22 = VLiveKitSampleOffsetLinearDepth(positionVS, float2( 1.0,  1.0) * stepVS);

        float edgeX = -d00 + d02 - 2.0 * d10 + 2.0 * d12 - d20 + d22;
        float edgeY = -d00 - 2.0 * d01 - d02 + d20 + 2.0 * d21 + d22;
        return length(float2(edgeX, edgeY));
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull [_CullMode]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma vertex Vert
            #pragma fragment FragDepth

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define ATTRIBUTES_NEED_NORMAL
            #define VARYINGS_NEED_POSITION_WS

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

            PackedVaryingsType Vert(AttributesMesh inputMesh)
            {
                VaryingsType varyingsType;
                ZERO_INITIALIZE(VaryingsType, varyingsType);
                varyingsType.vmesh = VertMesh(inputMesh);
                return PackVaryingsType(varyingsType);
            }

            void FragDepth(PackedVaryingsToPS packedInput)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Cull [_CullMode]
            ZWrite On
            ZTest LEqual

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
                float2 uv = input.texCoord0.xy * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;
                float4 albedo = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, uv) * _BaseColor;

                float3 normalWS = SafeNormalize(input.tangentToWorld[2]);
                float3 lightDirWS = SafeNormalize(_RimLightDirectionWS.xyz);
                float halfLambert = saturate(dot(normalWS, lightDirWS) * 0.5 + 0.5);
                float shade = step(_ShadowThreshold, halfLambert);

                float rimThickness = _RimThickness * lerp(1.0, halfLambert, _DirectionalWidth);
                float edgeStrength = VLiveKitSobelDepthEdge(input.positionRWS, rimThickness);
                float rim = step(_RimThreshold, edgeStrength) * _RimIntensity;

                float3 toonColor = albedo.rgb * lerp(_ShadowColor.rgb, _BaseColor.rgb, shade);
                float3 shadedRim = toonColor + rim * _RimColor.rgb;
                float rawEdge = saturate(edgeStrength * _RawSobelScale);

                if (_DebugMode < 0.5)
                    outColor = float4(shadedRim, albedo.a);
                else if (_DebugMode < 1.5)
                    outColor = float4((rim * _RimColor.rgb), albedo.a);
                else
                    outColor = float4(rawEdge.xxx, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/HDRP/FallbackError"
}
