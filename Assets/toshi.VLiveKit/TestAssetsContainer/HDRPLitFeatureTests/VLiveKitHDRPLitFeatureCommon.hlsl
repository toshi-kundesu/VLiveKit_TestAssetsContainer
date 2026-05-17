#ifndef VLIVEKIT_HDRP_LIT_FEATURE_COMMON_INCLUDED
#define VLIVEKIT_HDRP_LIT_FEATURE_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);
TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
float4 _BaseColorMap_ST;
float _Metallic;
float _Smoothness;
float _NormalScale;
float _UseNormalMap;
float _UseMaskMap;
float _FeatureIntensity;
float _AlphaCutoff;
float4 _EmissiveColor;
CBUFFER_END

float2 VLiveKitTransformBaseUV(float2 uv)
{
    return uv * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;
}

float3 VLiveKitSampleNormalWS(FragInputs input, float2 uv)
{
    float3 normalWS = SafeNormalize(input.tangentToWorld[2]);

    if (_UseNormalMap > 0.0)
    {
        float4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv);
        float3 normalTS = UnpackNormalMapRGorAG(normalSample, _NormalScale);
        normalWS = SafeNormalize(TransformTangentToWorld(normalTS, input.tangentToWorld));
    }

    return normalWS;
}

void VLiveKitBuildLitSurface(FragInputs input, out SurfaceData surfaceData, out float alpha)
{
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    float2 uv = VLiveKitTransformBaseUV(input.texCoord0.xy);
    float4 color = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, uv) * _BaseColor;
    float4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
    float maskWeight = saturate(_UseMaskMap);

    alpha = color.a;
#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
    surfaceData.baseColor = color.rgb;
    surfaceData.specularOcclusion = lerp(1.0, mask.g, maskWeight);
    surfaceData.normalWS = VLiveKitSampleNormalWS(input, uv);
    surfaceData.geomNormalWS = SafeNormalize(input.tangentToWorld[2]);
    surfaceData.perceptualSmoothness = lerp(_Smoothness, mask.a, maskWeight);
    surfaceData.ambientOcclusion = lerp(1.0, mask.g, maskWeight);
    surfaceData.metallic = lerp(_Metallic, mask.r, maskWeight);
    surfaceData.coatMask = 0.0;
    surfaceData.specularColor = float3(1.0, 1.0, 1.0);
    surfaceData.diffusionProfileHash = 0;
    surfaceData.subsurfaceMask = 0.0;
    surfaceData.thickness = 1.0;
    surfaceData.transmissionMask = float3(0.0, 0.0, 0.0);
    surfaceData.tangentWS = SafeNormalize(input.tangentToWorld[0]);
    surfaceData.anisotropy = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1.0;
    surfaceData.transmittanceMask = 0.0;
}

void VLiveKitGetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    float alpha;
    VLiveKitBuildLitSurface(input, surfaceData, alpha);

    InitBuiltinData(posInput, alpha, surfaceData.normalWS, -input.tangentToWorld[2], input.texCoord1, input.texCoord2, builtinData);
    builtinData.emissiveColor = _EmissiveColor.rgb;
    builtinData.depthOffset = 0.0;
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);

#ifdef VLIVEKIT_ZERO_BUILTIN_DIFFUSE
    builtinData.bakeDiffuseLighting = 0.0;
    builtinData.emissiveColor = 0.0;
#endif
}

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
    uint2 tileIndex = uint2(input.positionSS.xy) / GetTileSize();
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, tileIndex);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    VLiveKitGetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, VLIVEKIT_LIGHT_FEATURE_FLAGS, lightLoopOutput);

#ifdef VLIVEKIT_OUTPUT_SPECULAR_ONLY
    float3 lighting = lightLoopOutput.specularLighting;
#elif defined(VLIVEKIT_OUTPUT_DIFFUSE_ONLY)
    float3 lighting = lightLoopOutput.diffuseLighting;
#else
    float3 lighting = lightLoopOutput.diffuseLighting + lightLoopOutput.specularLighting;
#endif

    float3 color = lighting * GetCurrentExposureMultiplier();
    outColor = float4(color * _FeatureIntensity, saturate(builtinData.opacity));
}

#endif
