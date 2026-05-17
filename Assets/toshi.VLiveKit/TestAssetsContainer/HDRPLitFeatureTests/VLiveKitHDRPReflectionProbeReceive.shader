Shader "toshi/VLiveKit/Test/HDRP Lit Feature/Reflection Probe Receive"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseColorMap("Base Color Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.9
        _MaskMap("Mask Map", 2D) = "white" {}
        _UseMaskMap("Use Mask Map", Range(0, 1)) = 0
        _NormalMap("Normal Map", 2D) = "bump" {}
        _UseNormalMap("Use Normal Map", Range(0, 1)) = 0
        _NormalScale("Normal Scale", Range(0, 4)) = 1
        _FeatureIntensity("Feature Intensity", Float) = 1
        _AlphaCutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [HDR] _EmissiveColor("Emissive Color", Color) = (0, 0, 0, 0)
    }

    HLSLINCLUDE

    #pragma target 4.5
    #define SUPPORT_GLOBAL_MIP_BIAS
    #define PREFER_HALF 0

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

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
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile_fragment SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
            #pragma multi_compile_fragment DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT
            #pragma multi_compile_fragment PUNCTUAL_SHADOW_LOW PUNCTUAL_SHADOW_MEDIUM PUNCTUAL_SHADOW_HIGH
            #pragma multi_compile_fragment DIRECTIONAL_SHADOW_LOW DIRECTIONAL_SHADOW_MEDIUM DIRECTIONAL_SHADOW_HIGH
            #pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH
            #pragma multi_compile_fragment USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #pragma vertex Vert
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_FORWARD
            #ifndef SHADER_STAGE_FRAGMENT
            #define SHADOW_LOW
            #ifndef USE_FPTL_LIGHTLIST
            #define USE_FPTL_LIGHTLIST
            #endif
            #endif

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
            #define HAS_LIGHTLOOP
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

            #define VLIVEKIT_LIGHT_FEATURE_FLAGS (LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_STANDARD)
            #define VLIVEKIT_ZERO_BUILTIN_DIFFUSE
            #define VLIVEKIT_OUTPUT_SPECULAR_ONLY
            #include "VLiveKitHDRPLitFeatureCommon.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/HDRP/FallbackError"
}
