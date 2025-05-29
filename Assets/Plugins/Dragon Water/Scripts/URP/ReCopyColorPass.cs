using System.Data;
using System.Net.Mail;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace DragonWater.URP
{
    public class ReCopyColorPass : ScriptableRenderPass
    {
        int _sampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
        ProfilingSampler _profilingSampler = new("Dragon Water - ReCopyColor");

        Material _blitMaterial;
        Material _samplingMaterial;
#if UNITY_2023_1_OR_NEWER
        RTHandle _opaqueColor;
        FieldInfo _opaqueColorField;
#else
        RenderTargetHandle _opaqueColor;
#endif

        public ReCopyColorPass()
        {
#if UNITY_2023_1_OR_NEWER
            _blitMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal/CoreBlit");
            _samplingMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Sampling");
#if UNITY_6000_0_OR_NEWER
            _samplingMaterial.SetFloat(_sampleOffsetShaderHandle, 2);
#endif
            _opaqueColorField = typeof(UniversalRenderer)
                .GetField("m_OpaqueColor", BindingFlags.Instance | BindingFlags.NonPublic);
#else
            _blitMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Blit");
            _samplingMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Sampling");
            _opaqueColor.Init("_CameraOpaqueTexture");
#endif
        }

#if UNITY_6000_0_OR_NEWER
        [System.Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                var downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;

#if UNITY_2023_1_OR_NEWER
                _opaqueColor = _opaqueColorField.GetValue(renderingData.cameraData.renderer) as RTHandle;
                var attachement = renderingData.cameraData.renderer.cameraColorTargetHandle;
                switch (downsamplingMethod)
                {
                    case Downsampling.None:
                        Blit(cmd, attachement, _opaqueColor, _blitMaterial, 0);
                        break;
                    case Downsampling._2xBilinear:
                        Blit(cmd, attachement, _opaqueColor, _blitMaterial, 1);
                        break;
                    case Downsampling._4xBox:
                        _samplingMaterial.SetFloat(_sampleOffsetShaderHandle, 2);
                        Blit(cmd, attachement, _opaqueColor, _samplingMaterial, 0);
                        break;
                    case Downsampling._4xBilinear:
                        Blit(cmd, attachement, _opaqueColor, _blitMaterial, 1);
                        break;
                }
#else
                switch (downsamplingMethod)
                {
                    case Downsampling.None:
                        cmd.Blit(colorAttachment, _opaqueColor.Identifier(), _blitMaterial);
                        break;
                    case Downsampling._2xBilinear:
                        cmd.Blit(colorAttachment, _opaqueColor.Identifier(), _blitMaterial);
                        break;
                    case Downsampling._4xBox:
                        _samplingMaterial.SetFloat(_sampleOffsetShaderHandle, 2);
                        cmd.Blit(colorAttachment, _opaqueColor.Identifier(), _samplingMaterial);
                        break;
                    case Downsampling._4xBilinear:
                        cmd.Blit(colorAttachment, _opaqueColor.Identifier(), _blitMaterial);
                        break;
                }
#endif
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

#if UNITY_6000_0_OR_NEWER
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;

            RenderGraphUtils.BlitMaterialParameters para = default;
            switch (downsamplingMethod)
            {
                case Downsampling.None:
                    para = new(resourceData.activeColorTexture, resourceData.cameraOpaqueTexture, _blitMaterial, 0);
                    break;
                case Downsampling._2xBilinear:
                    para = new(resourceData.activeColorTexture, resourceData.cameraOpaqueTexture, _blitMaterial, 1);
                    break;
                case Downsampling._4xBox:
                    para = new(resourceData.activeColorTexture, resourceData.cameraOpaqueTexture, _samplingMaterial, 0);
                    break;
                case Downsampling._4xBilinear:
                    para = new(resourceData.activeColorTexture, resourceData.cameraOpaqueTexture, _blitMaterial, 1);
                    break;
            }

            if (para.source.IsValid() && para.destination.IsValid())
                renderGraph.AddBlitPass(para, passName: _profilingSampler.name);
        }
#endif
    }
}
