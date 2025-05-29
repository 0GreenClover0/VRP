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
    public class WaterSurfaceCameraSnapPass : ScriptableRenderPass
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Multiplier of cameras near plane distance, used to snap to water surface.")]
            [Range(0.25f, 3.0f)]
            public float nearClipFactor = 1.5f;
        }

        public Settings settings { get; private set; } = new();

#if UNITY_6000_0_OR_NEWER
        FieldInfo _frameDataField;
#endif
        FieldInfo _viewMatrixField;

        public WaterSurfaceCameraSnapPass()
        {
#if UNITY_6000_0_OR_NEWER
            _frameDataField = typeof(CameraData)
                .GetField("frameData", BindingFlags.Instance | BindingFlags.NonPublic);
            _viewMatrixField = typeof(UniversalCameraData)
                .GetField("m_ViewMatrix", BindingFlags.Instance | BindingFlags.NonPublic);
#else
            _viewMatrixField = typeof(CameraData)
                .GetField("m_ViewMatrix", BindingFlags.Instance | BindingFlags.NonPublic);
#endif
        }

        public void SetSettings(Settings settings)
        {
            this.settings = settings;
        }

#if UNITY_6000_0_OR_NEWER
        [System.Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if UNITY_6000_0_OR_NEWER
            ref var sCameraData = ref renderingData.cameraData;
            var cameraData = ((ContextContainer)_frameDataField.GetValue(sCameraData)).Get<UniversalCameraData>();
#else
            ref var cameraData = ref renderingData.cameraData;
#endif
            var camera = cameraData.camera;

            if (camera != DragonWaterManager.Instance.MainCamera)
                return;

            var hit = DragonWaterManager.Instance.CameraHitResult;
            if (!hit.HasHit)
                return;

            var clampDistance = camera.nearClipPlane * settings.nearClipFactor;
            var clampOffset = 0.0f;
            if (hit.IsUnderwater)
            {
                if (hit.Depth < clampDistance)
                    clampOffset = -(clampDistance - hit.Depth);
            }
            else
            {
                if (hit.Height < clampDistance)
                    clampOffset = clampDistance - hit.Height;
            }

            if (clampOffset == 0.0f)
                return;

            var viewMatrix = cameraData.GetViewMatrix();
            var cameraTranslation = viewMatrix.GetColumn(3);
            var cameraUp = viewMatrix.GetColumn(1);

            viewMatrix.SetColumn(3, cameraTranslation - cameraUp * clampOffset);
            cameraData.worldSpaceCameraPos += Vector3.up * clampOffset;

            var reference = __makeref(cameraData);
            _viewMatrixField.SetValueDirect(reference, viewMatrix);
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

            if (cameraData.camera != DragonWaterManager.Instance.MainCamera)
                return;

            using (var builder = renderGraph.AddUnsafePass<GraphPassData>(passName, out var passData))
            {
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                passData.cameraData = cameraData;
                passData.underwaterRenderer = DragonWaterManager.Instance.UnderwaterRenderer;

                builder.SetRenderFunc((GraphPassData data, UnsafeGraphContext context) =>
                {
                    var hit = DragonWaterManager.Instance.CameraHitResult;
                    if (!hit.HasHit)
                        return;

                    var cameraData = data.cameraData;
                    var camera = cameraData.camera;

                    var clampDistance = camera.nearClipPlane * settings.nearClipFactor;
                    var clampOffset = 0.0f;
                    if (hit.IsUnderwater)
                    {
                        if (hit.Depth < clampDistance)
                            clampOffset = -(clampDistance - hit.Depth);
                    }
                    else
                    {
                        if (hit.Height < clampDistance)
                            clampOffset = clampDistance - hit.Height;
                    }

                    if (clampOffset == 0.0f)
                        return;

                    var viewMatrix = cameraData.GetViewMatrix();
                    var cameraTranslation = viewMatrix.GetColumn(3);
                    var cameraUp = viewMatrix.GetColumn(1);

                    viewMatrix.SetColumn(3, cameraTranslation - cameraUp * clampOffset);
                    cameraData.worldSpaceCameraPos += Vector3.up * clampOffset;

                    var reference = __makeref(cameraData);
                    _viewMatrixField.SetValueDirect(reference, viewMatrix);
                });
            }
        }
#endif
    }
}
