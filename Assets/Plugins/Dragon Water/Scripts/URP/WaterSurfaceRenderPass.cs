using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace DragonWater.URP
{
    public class WaterSurfaceRenderPass : ScriptableRenderPass
    {
        ShaderTagId _shaderTagID = new(Constants.Shader.Tag.WaterSurface);
        ProfilingSampler _profilingSampler = new("Dragon Water - Surface");

#if UNITY_6000_0_OR_NEWER
        [System.Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var drawingSettings = CreateDrawingSettings(_shaderTagID, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            var filteringSettings = new FilteringSettings(null, DragonWaterManager.Instance.Config.WaterRendererMask);

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Render the objects...
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

#if UNITY_6000_0_OR_NEWER
        private class GraphPassData
        {
            public RendererListHandle rendererListHdl;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<GraphPassData>(passName, out var passData, _profilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;
                TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);

                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                TextureHandle[] dBufferHandles = resourceData.dBuffer;
                for (int i = 0; i < dBufferHandles.Length; ++i)
                {
                    TextureHandle dBuffer = dBufferHandles[i];
                    if (dBuffer.IsValid())
                        builder.UseTexture(dBuffer, AccessFlags.Read);
                }

                TextureHandle ssaoTexture = resourceData.ssaoTexture;
                if (ssaoTexture.IsValid())
                    builder.UseTexture(ssaoTexture, AccessFlags.Read);

                var drawingSettings = RenderingUtils.CreateDrawingSettings(_shaderTagID, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
                var filteringSettings = new FilteringSettings(null, DragonWaterManager.Instance.Config.WaterRendererMask);
                var listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.rendererListHdl = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.rendererListHdl);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((GraphPassData data, RasterGraphContext rgContext) =>
                {
                    rgContext.cmd.DrawRendererList(data.rendererListHdl);
                });
            }
        }
#endif
    }
}
