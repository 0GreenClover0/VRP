using System.Net.Mail;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace DragonWater.URP
{
    public class ReCopyDepthPass : ScriptableRenderPass
    {
        int _scaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");
        ProfilingSampler _profilingSampler = new("Dragon Water - ReCopyDepth");

        Material _copyDepthMaterial;
#if UNITY_2023_1_OR_NEWER
        RTHandle _depthTexture;
        FieldInfo _depthTextureField;
#else
        RenderTargetHandle _depthTexture;
#endif
#if UNITY_6000_0_OR_NEWER
        int _zwriteId;
        int _depthAttachmentId;
#endif

        public ReCopyDepthPass()
        {
            _copyDepthMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/CopyDepth");
#if UNITY_2023_1_OR_NEWER
            _depthTextureField = typeof(UniversalRenderer)
                .GetField("m_DepthTexture", BindingFlags.Instance | BindingFlags.NonPublic);
#else
            _depthTexture.Init("_CameraDepthTexture");
#endif
#if UNITY_6000_0_OR_NEWER
            _zwriteId = Shader.PropertyToID("_ZWrite");
            _depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
#endif
        }

#if !UNITY_2023_1_OR_NEWER
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cameraTextureDescriptor.colorFormat = RenderTextureFormat.Depth;
#if UNITY_SWITCH || UNITY_ANDROID
            cameraTextureDescriptor.depthBufferBits = 24;
#else
            cameraTextureDescriptor.depthBufferBits = 32;
#endif
            cameraTextureDescriptor.msaaSamples = 1;

            if (!_depthTexture.HasInternalRenderTargetId())
                cmd.GetTemporaryRT(_depthTexture.id, cameraTextureDescriptor, FilterMode.Point);

            ConfigureTarget(_depthTexture.Identifier());
            ConfigureClear(ClearFlag.None, Color.black);
        }
#endif

#if UNITY_6000_0_OR_NEWER
        [System.Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                var cameraSamples = descriptor.msaaSamples;

                // When auto resolve is supported or multisampled texture is not supported, set camera samples to 1
                if (SystemInfo.supportsMultisampledTextures == 0)
                    cameraSamples = 1;

                var cameraData = renderingData.cameraData;

                switch (cameraSamples)
                {
                    case 8:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 4:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 2:
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    // MSAA disabled, auto resolve supported or ms textures not supported
                    default:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;
                }


#if !UNITY_2023_1_OR_NEWER
                cmd.SetGlobalTexture("_CameraDepthAttachment", renderingData.cameraData.renderer.cameraDepthTarget);
#endif

                var yflip = cameraData.IsCameraProjectionMatrixFlipped();
                var flipSign = yflip ? -1.0f : 1.0f;
                Vector4 scaleBiasRt = (flipSign < 0.0f)
                    ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                    : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                cmd.SetGlobalVector(_scaleBiasRt, scaleBiasRt);

#if UNITY_2023_1_OR_NEWER
                _depthTexture = _depthTextureField.GetValue(renderingData.cameraData.renderer) as RTHandle;
                var attachement = renderingData.cameraData.renderer.cameraDepthTargetHandle;
#if UNITY_6000_0_OR_NEWER
                if (attachement.rt != null)
                {
                    if (_depthTexture.rt.depth > 0)
                        cmd.EnableShaderKeyword(ShaderKeywordStrings._OUTPUT_DEPTH);
                    else
                        cmd.DisableShaderKeyword(ShaderKeywordStrings._OUTPUT_DEPTH);
                    _copyDepthMaterial.SetTexture(_depthAttachmentId, attachement);
                    _copyDepthMaterial.SetFloat(_zwriteId, 1.0f);
                    Blit(cmd, attachement, _depthTexture, _copyDepthMaterial, 0);
                }
#else
                Blit(cmd, attachement, _depthTexture, _copyDepthMaterial, 0);
#endif
#else
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _copyDepthMaterial);
#endif

#if !UNITY_2023_1_OR_NEWER
                cmd.SetGlobalTexture("_CameraDepthTexture", _depthTexture.Identifier());
#endif
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }


#if UNITY_6000_0_OR_NEWER
        private class GraphPassData
        {
            public TextureHandle source;
            public UniversalCameraData cameraData;
            public Material copyDepthMaterial;
            public bool copyToDepth;
            public bool isDstBackbuffer;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            using (var builder = renderGraph.AddRasterRenderPass<GraphPassData>(passName, out var passData, profilingSampler))
            {
                passData.copyDepthMaterial = _copyDepthMaterial;
                passData.cameraData = cameraData;
                passData.isDstBackbuffer = resourceData.isActiveTargetBackBuffer;
                passData.copyToDepth = resourceData.cameraDepthTexture.GetDescriptor(renderGraph).depthBufferBits != DepthBits.None;

                if (passData.copyToDepth)
                {
                    builder.SetRenderAttachmentDepth(resourceData.cameraDepthTexture, AccessFlags.WriteAll);
                }
                else
                {
                    builder.SetRenderAttachment(resourceData.cameraDepthTexture, 0, AccessFlags.WriteAll);
                }

                passData.source = resourceData.activeDepthTexture;
                builder.UseTexture(passData.source, AccessFlags.Read);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((GraphPassData passData, RasterGraphContext context) =>
                {
                    var copyDepthMaterial = passData.copyDepthMaterial;
                    var copyToDepth = passData.copyToDepth;
                    var source = (RTHandle)passData.source;
                    var cmd = context.cmd;

                    using (new ProfilingScope(cmd, _profilingSampler))
                    {
                        int cameraSamples = ((RTHandle)source).rt.antiAliasing;
                        if (SystemInfo.supportsMultisampledTextures == 0)
                            cameraSamples = 1;

                        switch (cameraSamples)
                        {
                            case 8:
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                                cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                                break;

                            case 4:
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                                cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                                break;

                            case 2:
                                cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                                break;

                            // MSAA disabled, auto resolve supported or ms textures not supported
                            default:
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                                break;
                        }

                        if (passData.copyToDepth)
                            cmd.EnableShaderKeyword(ShaderKeywordStrings._OUTPUT_DEPTH);
                        else
                            cmd.DisableShaderKeyword(ShaderKeywordStrings._OUTPUT_DEPTH);

                        var yflip = passData.cameraData.IsHandleYFlipped(source) && passData.isDstBackbuffer;

                        var viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                        var scaleBias = yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);

                        copyDepthMaterial.SetTexture(_depthAttachmentId, source);
                        copyDepthMaterial.SetFloat(_zwriteId, 1.0f);
                        Blitter.BlitTexture(cmd, source, scaleBias, copyDepthMaterial, 0);
                    }
                });
            }
        }
#endif
    }
}
