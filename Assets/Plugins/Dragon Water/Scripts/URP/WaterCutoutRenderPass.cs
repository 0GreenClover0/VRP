using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace DragonWater.URP
{
    public class WaterCutoutRenderPass : ScriptableRenderPass
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Scale of screen resolution used for drawing water cutout mask.\nIn most cases value in range 0.25-0.5 is enough.")]
            [Range(0.1f, 1.0f)]
            public float maskResolutionScale = 0.5f;
        }

        public Settings settings { get; private set; } = new();


        ShaderTagId _shaderTagID = new(Constants.Shader.Tag.WaterCutout);
        ProfilingSampler _profilingSampler = new("Dragon Water - Cutout");

        int _tmpId = Constants.Shader.Property.WaterCutoutMask;
        string _tmpName = Constants.Shader.Property.WaterCutoutMaskName;
        RenderTargetIdentifier _rt;
#if UNITY_2023_1_OR_NEWER
        RTHandle _rtHandle;
#endif


        public void SetSettings(Settings settings)
        {
            this.settings = settings;
        }

#if UNITY_6000_0_OR_NEWER
        [System.Obsolete]
#endif
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor ctr)
        {
            var width = ctr.width * settings.maskResolutionScale;
            var height = ctr.height * settings.maskResolutionScale;

            cmd.GetTemporaryRT(_tmpId, (int)width, (int)height, 0, FilterMode.Point, RenderTextureFormat.RGFloat);
            _rt = new RenderTargetIdentifier(_tmpId);
#if UNITY_2023_1_OR_NEWER
            _rtHandle = RTHandles.Alloc(_rt);
            ConfigureTarget(_rtHandle);
#else
            ConfigureTarget(_rt);
#endif
        }

#if UNITY_6000_0_OR_NEWER
        [System.Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var drawingSettings = CreateDrawingSettings(_shaderTagID, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            var filteringSettings = new FilteringSettings(null, DragonWaterManager.Instance.Config.CutoutMask);
            
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // clear texture
                cmd.SetRenderTarget(_rt);
                cmd.ClearRenderTarget(false, true, new Color(float.MaxValue, float.MaxValue, 0, 0));

                // Ensure we flush our command-buffer before we render...
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

                var width = cameraData.scaledWidth * settings.maskResolutionScale;
                var height = cameraData.scaledHeight * settings.maskResolutionScale;
                RenderTextureDescriptor textureProperties = new RenderTextureDescriptor((int)width, (int)height, RenderTextureFormat.RGFloat, 0);
                TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, _tmpName, false);
                builder.SetRenderAttachment(texture, 0, AccessFlags.Write);

                var drawingSettings = RenderingUtils.CreateDrawingSettings(_shaderTagID, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
                var filteringSettings = new FilteringSettings(null, DragonWaterManager.Instance.Config.CutoutMask);
                var listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.rendererListHdl = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.rendererListHdl);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((GraphPassData data, RasterGraphContext rgContext) =>
                {
                    rgContext.cmd.ClearRenderTarget(false, true, new Color(float.MaxValue, float.MaxValue, 0, 0));
                    rgContext.cmd.DrawRendererList(data.rendererListHdl);
                });

                builder.SetGlobalTextureAfterPass(texture, _tmpId);
            }
        }
#endif
    }
}
