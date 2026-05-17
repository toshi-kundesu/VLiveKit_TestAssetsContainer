using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace toshi.VLiveKit.TestAssetsContainer
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [VolumeComponentMenu("Post-processing/VLiveKit/GT Tone Mapping")]
    public sealed class GTToneMapping : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        private const string ShaderName = "Hidden/toshi/VLiveKit/PostProcessing/GT Tone Mapping";

        private static readonly int InputTextureID = Shader.PropertyToID("_InputTexture");
        private static readonly int GTParams0ID = Shader.PropertyToID("_GTParams0");
        private static readonly int GTParams1ID = Shader.PropertyToID("_GTParams1");
        private static readonly int GTParams2ID = Shader.PropertyToID("_GTParams2");

        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter preExposure = new ClampedFloatParameter(1.0f, 0.0f, 16.0f);
        public ClampedFloatParameter maxBrightness = new ClampedFloatParameter(1.0f, 0.01f, 16.0f);
        public ClampedFloatParameter referenceWhite = new ClampedFloatParameter(1.2f, 0.01f, 16.0f);
        public ClampedFloatParameter linearSlope = new ClampedFloatParameter(1.0f, 0.01f, 8.0f);
        public ClampedFloatParameter linearStart = new ClampedFloatParameter(0.22f, 0.001f, 4.0f);
        public ClampedFloatParameter linearLength = new ClampedFloatParameter(0.4f, 0.0f, 4.0f);
        public ClampedFloatParameter toeContrast = new ClampedFloatParameter(1.33f, 0.01f, 8.0f);
        public FloatParameter toeOffset = new FloatParameter(0.0f);
        public BoolParameter clampOutput = new BoolParameter(true);

        private Material material;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcessBlurs;

        public bool IsActive()
        {
            return material != null && enable.value && intensity.value > 0.0f;
        }

        public override void Setup()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader != null)
                material = CoreUtils.CreateEngineMaterial(shader);
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            if (material == null)
                return;

            material.SetTexture(InputTextureID, source);
            material.SetVector(GTParams0ID, new Vector4(preExposure.value, intensity.value, maxBrightness.value, referenceWhite.value));
            material.SetVector(GTParams1ID, new Vector4(linearSlope.value, linearStart.value, linearLength.value, toeContrast.value));
            material.SetVector(GTParams2ID, new Vector4(toeOffset.value, clampOutput.value ? 1.0f : 0.0f, 0.0f, 0.0f));

            HDUtils.DrawFullScreen(cmd, material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(material);
            material = null;
        }
    }
}
