Shader "toshi/VLiveKit/Test/HDRP Lit Feature/Mask Map Test"
{
    Properties
    {
        _MaskMap("Mask Map", 2D) = "white" {}
        [Enum(Packed RGB,0,Metallic R,1,Ambient Occlusion G,2,Detail Mask B,3,Smoothness A,4)] _MaskMapDebugChannel("Debug Channel", Float) = 0
        _Intensity("Intensity", Float) = 1
    }

    HLSLINCLUDE

    #pragma target 4.5

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    TEXTURE2D(_MaskMap);
    SAMPLER(sampler_MaskMap);

    CBUFFER_START(UnityPerMaterial)
    float4 _MaskMap_ST;
    float _MaskMapDebugChannel;
    float _Intensity;
    CBUFFER_END

    float3 VLiveKitMaskMapColor(float4 mask)
    {
        if (_MaskMapDebugChannel < 0.5)
            return float3(mask.r, mask.g, mask.b);
        if (_MaskMapDebugChannel < 1.5)
            return mask.rrr;
        if (_MaskMapDebugChannel < 2.5)
            return mask.ggg;
        if (_MaskMapDebugChannel < 3.5)
            return mask.bbb;

        return mask.aaa;
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex Vert
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_FORWARD
            #define ATTRIBUTES_NEED_TEXCOORD0
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
                float2 uv = input.texCoord0.xy * _MaskMap_ST.xy + _MaskMap_ST.zw;
                float4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);

                outColor = float4(VLiveKitMaskMapColor(mask) * _Intensity, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/HDRP/FallbackError"
}
