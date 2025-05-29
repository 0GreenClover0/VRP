using DragonWater.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace DragonWater.URP
{
    public class UnderwaterCausticsPass : ScriptableRenderPass
    {
        ShaderTagId _shaderTagID = new(Constants.Shader.Tag.UnderwaterCaustics);
        ProfilingSampler _profilingSampler = new("Dragon Underwater - Caustics");

#if UNITY_6000_0_OR_NEWER
        [System.Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var underwaterRenderer = DragonWaterManager.Instance.UnderwaterRenderer;

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                cmd.DrawMesh(
                    MeshUtility.GetBoxPrimitive(),
                    Matrix4x4.TRS(renderingData.cameraData.worldSpaceCameraPos, Quaternion.identity, Vector3.one * renderingData.cameraData.camera.farClipPlane * 0.25f),
                    underwaterRenderer.CausticsMaterial
                    );
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

#if UNITY_6000_0_OR_NEWER
        private class GraphPassData
        {
            public UniversalCameraData cameraData;
            public DragonUnderwaterRenderer underwaterRenderer;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            using (var builder = renderGraph.AddRasterRenderPass<GraphPassData>(passName, out var passData, _profilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                passData.cameraData = cameraData;
                passData.underwaterRenderer = DragonWaterManager.Instance.UnderwaterRenderer;

                builder.SetRenderFunc((GraphPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawMesh(
                        MeshUtility.GetBoxPrimitive(),
                        Matrix4x4.TRS(data.cameraData.worldSpaceCameraPos, Quaternion.identity, Vector3.one * data.cameraData.camera.farClipPlane * 0.25f),
                        data.underwaterRenderer.CausticsMaterial
                    );
                });
            }
        }
#endif
    }
}
