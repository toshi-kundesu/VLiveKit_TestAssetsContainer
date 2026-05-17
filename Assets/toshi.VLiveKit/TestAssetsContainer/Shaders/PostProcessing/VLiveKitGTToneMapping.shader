Shader "Hidden/toshi/VLiveKit/PostProcessing/GT Tone Mapping"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    TEXTURE2D_X(_InputTexture);
    float4 _GTParams0; // x: pre exposure, y: blend intensity, z: max brightness, w: reference white
    float4 _GTParams1; // x: linear slope, y: linear start, z: linear length, w: toe contrast
    float4 _GTParams2; // x: toe offset, y: clamp output

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // Gran Turismo / Uchimura tone curve. Adapted for HDRP from yaoling1997/GT-ToneMapping (MIT License).
    float3 GTToneMap(float3 color)
    {
        float3 x = max(color, 0.0);
        float P = max(_GTParams0.z, 0.0001);
        float W = max(_GTParams0.w, 0.0001);
        float a = max(_GTParams1.x, 0.0001);
        float m = max(_GTParams1.y, 0.0001);
        float l = max(_GTParams1.z, 0.0);
        float c = max(_GTParams1.w, 0.0001);
        float b = _GTParams2.x;

        float l0 = ((P - m) * l) / a;
        float3 linearSegment = m + x / a;
        float3 toeSegment = m * pow(x / m, c) + b;

        float S0 = m + l0;
        float S1 = m + a * l0;
        float C2 = (a * P) / max(P - S1, 0.0001);
        float CP = -C2 / P;
        float3 shoulderSegment = P - (P - S1) * exp(CP * (x - S0));

        float3 toeWeight = 1.0 - smoothstep(0.0, m, x);
        float3 shoulderWeight = step(m + l0, x);
        float3 linearWeight = 1.0 - toeWeight - shoulderWeight;

        return (toeSegment * toeWeight + linearSegment * linearWeight + shoulderSegment * shoulderWeight) / W;
    }

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float3 sourceColor = LOAD_TEXTURE2D_X_LOD(_InputTexture, positionSS, 0).rgb;
        float3 mappedColor = GTToneMap(sourceColor * _GTParams0.x);
        float3 outputColor = lerp(sourceColor, mappedColor, saturate(_GTParams0.y));

        if (_GTParams2.y > 0.5)
            outputColor = saturate(outputColor);

        return float4(outputColor, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "GT Tone Mapping"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment CustomPostProcess
            ENDHLSL
        }
    }

    Fallback Off
}
