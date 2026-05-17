Shader "_lil/[Optional] lilToonFakeShadow"
{
    Properties
    {
        //----------------------------------------------------------------------------------------------------------------------
        // Base
        [lilToggle]     _Invisible                  ("sInvisible", Int) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Main
        [lilHDR] [MainColor] _Color                 ("sColor", Color) = (0.925,0.7,0.74,1)
        [MainTexture]   _MainTex                    ("Texture", 2D) = "white" {}
        [lilVec3Float]  _FakeShadowVector           ("sFakeShadowVectors", Vector) = (0,0,0,0.05)

        //----------------------------------------------------------------------------------------------------------------------
        // Advanced
        [lilEnum]                                       _Cull               ("sCullModes", Int) = 2
        [Enum(UnityEngine.Rendering.BlendMode)]         _SrcBlend           ("sSrcBlendRGB", Int) = 2
        [Enum(UnityEngine.Rendering.BlendMode)]         _DstBlend           ("sDstBlendRGB", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]         _SrcBlendAlpha      ("sSrcBlendAlpha", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]         _DstBlendAlpha      ("sDstBlendAlpha", Int) = 1
        [Enum(UnityEngine.Rendering.BlendOp)]           _BlendOp            ("sBlendOpRGB", Int) = 0
        [Enum(UnityEngine.Rendering.BlendOp)]           _BlendOpAlpha       ("sBlendOpAlpha", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]         _SrcBlendFA         ("sSrcBlendRGB", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]         _DstBlendFA         ("sDstBlendRGB", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]         _SrcBlendAlphaFA    ("sSrcBlendAlpha", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]         _DstBlendAlphaFA    ("sDstBlendAlpha", Int) = 1
        [Enum(UnityEngine.Rendering.BlendOp)]           _BlendOpFA          ("sBlendOpRGB", Int) = 4
        [Enum(UnityEngine.Rendering.BlendOp)]           _BlendOpAlphaFA     ("sBlendOpAlpha", Int) = 4
        [lilToggle]                                     _ZClip              ("sZClip", Int) = 1
        [lilToggle]                                     _ZWrite             ("sZWrite", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)]   _ZTest              ("sZTest", Int) = 4
        [IntRange]                                      _StencilRef         ("Ref", Range(0, 255)) = 51
        [IntRange]                                      _StencilReadMask    ("ReadMask", Range(0, 255)) = 255
        [IntRange]                                      _StencilWriteMask   ("WriteMask", Range(0, 255)) = 255
        [Enum(UnityEngine.Rendering.CompareFunction)]   _StencilComp        ("Comp", Float) = 3
        [Enum(UnityEngine.Rendering.StencilOp)]         _StencilPass        ("Pass", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]         _StencilFail        ("Fail", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)]         _StencilZFail       ("ZFail", Float) = 0
                                                        _OffsetFactor       ("sOffsetFactor", Float) = 0
                                                        _OffsetUnits        ("sOffsetUnits", Float) = 0
        [lilColorMask]                                  _ColorMask          ("sColorMask", Int) = 15
        [lilToggle]                                     _AlphaToMask        ("sAlphaToMask", Int) = 0
                                                        _lilShadowCasterBias ("Shadow Caster Bias", Float) = 0

        //----------------------------------------------------------------------------------------------------------------------
        // Save (Unused)
        [HideInInspector]                               _BaseColor          ("sColor", Color) = (1,1,1,1)
        [HideInInspector]                               _BaseMap            ("Texture", 2D) = "white" {}
        [HideInInspector]                               _BaseColorMap       ("Texture", 2D) = "white" {}
        [HideInInspector]                               _lilToonVersion     ("Version", Int) = 45
    }

    SubShader
    {
        Tags {"RenderType" = "HDLitShader" "Queue" = "Transparent"}
        HLSLINCLUDE
            #define LIL_FEATURE_Main2ndDissolveNoiseMask
            #define LIL_FEATURE_Main3rdDissolveNoiseMask
            #define LIL_FEATURE_DissolveNoiseMask
            #define LIL_OPTIMIZE_APPLY_SHADOW_FA
            #define LIL_OPTIMIZE_USE_FORWARDADD
            #pragma skip_variants _REFLECTION_PROBE_BLENDING _REFLECTION_PROBE_BOX_PROJECTION
            #pragma skip_variants VERTEXLIGHT_ON LIGHTPROBE_SH
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON LIGHTMAP_SHADOW_MIXING SHADOWS_SHADOWMASK DIRLIGHTMAP_COMBINED _MIXED_LIGHTING_SUBTRACTIVE
            #define LIL_SRP_VERSION_MAJOR 17
            #define LIL_SRP_VERSION_MINOR 3
            #define LIL_SRP_VERSION_PATCH 0

            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma fragmentoption ARB_precision_hint_fastest
            #define LIL_FAKESHADOW

            #pragma skip_variants SHADOWS_SCREEN _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN _ADDITIONAL_LIGHT_SHADOWS SCREEN_SPACE_SHADOWS_ON SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON LIGHTMAP_SHADOW_MIXING SHADOWS_SHADOWMASK DIRLIGHTMAP_COMBINED _MIXED_LIGHTING_SUBTRACTIVE
            #pragma skip_variants DECALS_OFF DECALS_3RT DECALS_4RT DECAL_SURFACE_GRADIENT _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma skip_variants VERTEXLIGHT_ON LIGHTPROBE_SH
            #pragma skip_variants _ADDITIONAL_LIGHT_SHADOWS
            #pragma skip_variants _SCREEN_SPACE_OCCLUSION
            #pragma skip_variants USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST
            #pragma skip_variants _REFLECTION_PROBE_BLENDING _REFLECTION_PROBE_BOX_PROJECTION
        ENDHLSL


        Pass
        {
            Name "FORWARD"
            Tags {"LightMode" = "ForwardOnly"}

            Stencil
            {
                WriteMask 6
                Ref 0
                Comp Always
                Pass Replace
            }
            Cull [_Cull]
            ZClip [_ZClip]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            ColorMask [_ColorMask]
            Offset [_OffsetFactor], [_OffsetUnits]
            BlendOp [_BlendOp], [_BlendOpAlpha]
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM

            //----------------------------------------------------------------------------------------------------------------------
            // Build Option
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment PROBE_VOLUMES_OFF PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile_fragment SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
            #pragma multi_compile_fragment DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH
            #pragma multi_compile_fragment USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #define SHADERPASS SHADERPASS_FORWARD
            #define HAS_LIGHTLOOP
            #define LIL_PASS_FORWARD

            //----------------------------------------------------------------------------------------------------------------------
            // Pass
            #include "Includes/lil_pipeline_hdrp.hlsl"
            #include "Includes/lil_common.hlsl"
            // Insert functions and includes that depend on Unity here

            #include "Includes/lil_pass_forward_fakeshadow.hlsl"

            ENDHLSL
        }

    }
    Fallback "HDRP/Unlit"

    CustomEditor "lilToon.lilToonInspector"
}
