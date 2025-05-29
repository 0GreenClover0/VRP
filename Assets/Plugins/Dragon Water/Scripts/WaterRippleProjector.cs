using DragonWater.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater
{
    [AddComponentMenu("Dragon Water/Ripple Projector")]
    [ExecuteAlways]
    public class WaterRippleProjector : MonoBehaviour
    {
        [Serializable]
        public enum ProjectorType
        {
            Local,
            Infinite
        }

        [Serializable]
        public enum AttachTarget
        {
            MainCamera,
            CustomObject
        }

        [Serializable]
        public enum PrecisionType
        {
            High,
            Simple,
            Flat
        }

        public class WaveRippler
        {
            public WaveProfile Profile { get; internal set; }
            public RenderTexture SimulationTexture { get; internal set; }

            public bool IsEnqueued { get; internal set; }


            public void Enqueue()
            {
                IsEnqueued = true;
            }
            public void Dequeue()
            {
                IsEnqueued = false;
            }

            internal void CheckTextures(int width, int height)
            {
                if (SimulationTexture != null && SimulationTexture.IsCreated())
                {
                    if (SimulationTexture.width != width || SimulationTexture.height != height)
                    {
                        SimulationTexture.Release();
                        SimulationTexture = null;
                    }
                }

                if (SimulationTexture == null)
                {
                    SimulationTexture = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
                    SimulationTexture.enableRandomWrite = true;
                    SimulationTexture.Create();
                }
            }
            internal void Release()
            {
                if (SimulationTexture != null && SimulationTexture.IsCreated())
                {
                    SimulationTexture.Release();
                }
            }

            internal void BlitOffsetTexture(RenderTexture rt, Material material)
            {
                Graphics.Blit(SimulationTexture, rt, material);
                Graphics.Blit(rt, SimulationTexture);
            }

            internal void DispatchRippleCompute(ComputeShader shader, int kernel, PrecisionType precision)
            {
                Profile.ConfigureRippler(shader, kernel, precision == PrecisionType.Flat, precision == PrecisionType.High);

                shader.SetTexture(kernel, Constants.Shader.Property.ComputeResultSimulation, SimulationTexture);

                shader.Dispatch(kernel, SimulationTexture.width / 8, SimulationTexture.height / 8, 1);
            }
        }

        [SerializeField] internal int textureSize = 512;
        [SerializeField] internal ProjectorType type = ProjectorType.Local;
        [SerializeField] internal Vector2 size = new Vector2(128,128);
        [SerializeField] internal float distance = 128;
        [SerializeField] internal AttachTarget attachTo = AttachTarget.MainCamera;
        [SerializeField] internal Transform attachCustomTarget = null;
        [SerializeField] internal float upperClip = 10.0f;
        [SerializeField] internal float lowerClip = 10.0f;
        [SerializeField] internal PrecisionType precision = PrecisionType.High;
        [Tooltip("Time between scans for interactors inside projector.\nYou can also call ScanCasters() manually.")]
        [SerializeField] internal float scanInterval = 0.05f;

        RenderTexture _renderTexture;
        List<WaveRippler> _ripplers = new();
        float _lastScanTime = 0.0f;
        List<WaterRippleCaster> _activeCasters = new();
        Collider[] _collectionCache = new Collider[Constants.MaxRippleCasters];


        #region properties
        public int TextureSize
        {
            get { return textureSize; }
            set
            {
                textureSize = value;
                UpdateProjector();
            }
        }
        public ProjectorType Type
        {
            get { return type; }
            set
            {
                if (type == value) return;
#if UNITY_EDITOR
                //EditorSaveParams();
#endif
                type = value;
                UpdateProjector();
            }
        }
        public Vector3 Size
        {
            get { return size; }
            set
            {
                size = value;
                if (type == ProjectorType.Local) { UpdateProjector(); }
            }
        }
        public float Distance
        {
            get { return distance; }
            set
            {
                distance = value;
                if (type == ProjectorType.Infinite) { UpdateProjector(); }
            }
        }
        public AttachTarget AttachTo
        {
            get { return attachTo; }
            set
            {
                attachTo = value;
                if (type == ProjectorType.Infinite) { UpdateProjector(); }
            }
        }
        public Transform AttachCustomTarget
        {
            get { return attachCustomTarget; }
            set
            {
                attachCustomTarget = value;
                if (type == ProjectorType.Infinite) { UpdateProjector(); }
            }
        }
        public float LowerClip
        {
            get { return lowerClip; }
            set
            {
                lowerClip = value;
                UpdateProjector();
            }
        }
        public float UpperClip
        {
            get { return upperClip; }
            set
            {
                upperClip = value;
                UpdateProjector();
            }
        }
        public PrecisionType Precision
        {
            get { return precision; }
            set
            {
                precision = value;
            }
        }
        public float ScanInterval
        {
            get { return scanInterval; }
            set { scanInterval = value; }
        }
        #endregion

        public RenderTexture ProjectionTexture => GetRenderTexture();
        public IReadOnlyList<WaveRippler> Ripplers => _ripplers;
        public IReadOnlyList<WaterRippleCaster> ActiveCasters => _activeCasters;
        public Bounds Bounds => GetOperatingBounds();


        private void Awake()
        {
            UpdateProjector();
        }
        private void OnEnable()
        {
            UpdateProjector();
            DragonWaterManager.Instance.RegisterRippleProjector(this);
        }
        private void OnDisable()
        {
            DragonWaterManager.InstanceUnsafe?.UnregisterRippleProjector(this);

            DequeueAllRipplers();
        }
        private void OnDestroy()
        {
            if (_renderTexture != null && _renderTexture.IsCreated())
            {
                _renderTexture.Release();
            }

            _ripplers.ForEach(r => r.Release());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            //
        }
#endif

#if UNITY_EDITOR
        // in editor mode, force update activeness in manager in case of script reload
        private void Update()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (enabled && !DragonWaterManager.Instance.RippleProjectors.Contains(this))
                {
                    enabled = false;
                    enabled = true;
                }
            }
        }
#endif

        public void ScanCasters()
        {
            var bounds = GetOperatingBounds();
            var hits = Physics.OverlapBoxNonAlloc(
                bounds.center,
                bounds.extents,
                _collectionCache,
                Quaternion.identity,
                DragonWaterManager.Instance.Config.RippleMask,
                QueryTriggerInteraction.Collide
                );

            _activeCasters.Clear();
            for (int i = 0; i < hits; i++)
            {
                if (_collectionCache[i].transform.parent != null &&
                    _collectionCache[i].transform.parent.TryGetComponent<WaterRippleCaster>(out var caster))
                    if (caster.enabled)
                        _activeCasters.Add(caster);
            }

            _lastScanTime = Time.unscaledTime;
        }


        internal WaveRippler CreateRippler(WaveProfile profile)
        {
            var rt = ProjectionTexture;

            WaveRippler rippler = null;
            for (int i = 0; i < _ripplers.Count; i++)
            {
                if (_ripplers[i].Profile == profile)
                {
                    rippler = _ripplers[i];
                    break;
                }
            }

            if (rippler == null)
            {
                rippler = new()
                {
                    Profile = profile
                };
                _ripplers.Add(rippler);
            }

            rippler.CheckTextures(rt.width, rt.height);
            return rippler;
        }

        internal void ConfigureMaterial(Material material, WaveRippler rippler)
        {
            material.EnableKeyword(Constants.Shader.Keyword.UseRipple);
            material.SetTexture(Constants.Shader.Property.RippleTexture, rippler.SimulationTexture);

            var position = transform.position;
            if (type == ProjectorType.Infinite)
            {
                material.SetVector(Constants.Shader.Property.RippleProjection,
                    new Vector4(position.x - distance, position.z - distance, 1.0f / (distance * 2.0f), 1.0f / (distance * 2.0f))
                );
            }
            else if (type == ProjectorType.Local)
            {
                material.SetVector(Constants.Shader.Property.RippleProjection,
                    new Vector4(position.x - size.x * 0.5f, position.z - size.y * 0.5f, 1.0f / size.x, 1.0f / size.y)
                );
            }

        }
        internal static void CleanupMaterial(Material material)
        {
            material.DisableKeyword(Constants.Shader.Keyword.UseRipple);
        }

        internal void UpdateProjector()
        {
            CleanupDeprecated();
        }
        internal void DequeueAllRipplers()
        {
            _ripplers.ForEach(r => r.Dequeue());
        }

        internal void UpdateProjector(Camera mainCamera)
        {
            if (type == ProjectorType.Infinite)
            {
                var attachTransform = (attachTo == AttachTarget.CustomObject && attachCustomTarget != null)
                    ? attachCustomTarget : mainCamera.transform;

                var snap = (distance * 2.0f) / textureSize;

                var x = attachTransform.position.x;
                var y = attachTransform.position.z;

                if (attachTransform == mainCamera.transform)
                {
                    x += (attachTransform.transform.forward.x * distance * 0.25f);
                    y += (attachTransform.transform.forward.z * distance * 0.25f);
                }

                var ox = Mathf.Sign(x) * Mathf.Repeat(Mathf.Abs(x), snap);
                var oy = Mathf.Sign(y) * Mathf.Repeat(Mathf.Abs(y), snap);

                var snappedX = x - ox;
                var snappedY = y - oy;

                var oldX = transform.position.x;
                var height = transform.position.y;
                var oldY = transform.position.z;

                transform.position = new Vector3(snappedX, height, snappedY);

                var difference = new Vector2Int(
                    Mathf.RoundToInt((snappedX - oldX) / snap),
                    Mathf.RoundToInt((snappedY - oldY) / snap)
                    );

                if (difference.sqrMagnitude > 0)
                {
                    var size = GetTextureSize();
                    var blit = RenderTexture.GetTemporary(size.x, size.y, 0, RenderTextureFormat.RFloat);

                    var material = DragonWaterManager.Instance.BlitMaterial;
                    var uvOffset = new Vector2(
                        (float)difference.x / blit.width,
                        -(float)difference.y / blit.height
                    );
                    material.SetVector("_OffsetUV", uvOffset);

                    for (int i = 0; i < _ripplers.Count; i++)
                    {
                        var rippler = _ripplers[i];
                        if (!rippler.IsEnqueued) continue;

                        rippler.BlitOffsetTexture(blit, material);
                    }

                    RenderTexture.ReleaseTemporary(blit);
                }
            }


            if (Time.unscaledTime - _lastScanTime >= scanInterval || _lastScanTime == 0)
            {
                ScanCasters();
            }
        }

        internal void RenderTarget(CommandBuffer cmd)
        {
            if (!IsAnyQueued()) return;

            var bounds = GetOperatingBounds();

            var view = Matrix4x4.Inverse(Matrix4x4.TRS(
                new(bounds.center.x, bounds.min.y, bounds.center.z),
                Quaternion.Euler(-90, 0, 0),
                new(1, 1, -1)
            ));

            var projection = Matrix4x4.Ortho(
                -bounds.extents.x,
                bounds.extents.x,
                -bounds.extents.z,
                bounds.extents.z,
                0,
                bounds.size.y
                );

            cmd.SetViewProjectionMatrices(view, projection);


            cmd.SetRenderTarget(ProjectionTexture);
            cmd.ClearRenderTarget(false, true, new Color(float.MaxValue, 0, 0, 0));

            for (int j = 0; j < _activeCasters.Count; j++)
            {
                if (_activeCasters[j] == null) continue;
                _activeCasters[j].RenderCaster(cmd);
            }

            cmd.SetRenderTarget(BuiltinRenderTextureType.None);
        }

        internal void Simulate(ComputeShader shader, int kernelMain)
        {
            if (_renderTexture == null || !_renderTexture.IsCreated()) return;
            if (!IsAnyQueued()) return;

            shader.SetTexture(kernelMain, Constants.Shader.Property.ComputeRippleProjectionTex, _renderTexture);

            var position = transform.position;
            shader.SetVector(Constants.Shader.Property.ComputeProjectorOffset, new Vector4(
                position.x, position.z, 0, 0
                ));
            shader.SetFloat(Constants.Shader.Property.ComputeProjectorY, position.y);

            shader.SetVector(Constants.Shader.Property.ComputeRippleTextureSize, new Vector4(
                _renderTexture.width, _renderTexture.height, 1.0f / _renderTexture.width, 1.0f / _renderTexture.height
                ));
            if (type == ProjectorType.Infinite)
            {
                shader.SetVector(Constants.Shader.Property.ComputeRippleProjectionSize, new Vector4(distance * 2.0f, distance * 2.0f, 1.0f / (distance * 2.0f), 1.0f / (distance * 2.0f)));
            }
            else if (type == ProjectorType.Local)
            {
                shader.SetVector(Constants.Shader.Property.ComputeRippleProjectionSize, new Vector4(size.x, size.y, 1.0f / size.x, 1.0f / size.y));
            }

            for (int i = 0; i < _ripplers.Count; i++)
            {
                if (_ripplers[i].IsEnqueued)
                {
                    _ripplers[i].DispatchRippleCompute(shader, kernelMain, precision);
                }
            }
        }

        internal bool IsAnyQueued()
        {
            var anyQueued = false;
            for (int i = 0; i < _ripplers.Count; i++)
            {
                if (_ripplers[i].IsEnqueued)
                {
                    anyQueued = true;
                    break;
                }
            }
            return anyQueued;
        }

        private RenderTexture GetRenderTexture()
        {
            var size = GetTextureSize();

            if (_renderTexture != null &&
                _renderTexture.IsCreated())
            {
                if (_renderTexture.width != size.x || _renderTexture.height != size.y)
                {
                    _renderTexture.Release();
                    _renderTexture = null;
                }
                else
                {
                    return _renderTexture;
                }
            }

            var rtFormat = SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.RHalf)
                ? RenderTextureFormat.RHalf
                : RenderTextureFormat.RFloat;
            _renderTexture = new RenderTexture(size.x, size.y, 0, rtFormat);
            var msaaSupport = SystemInfo.GetRenderTextureSupportedMSAASampleCount(_renderTexture.descriptor);
            if (msaaSupport >= 4)
                _renderTexture.antiAliasing = 4;
            else if (msaaSupport >= 2)
                _renderTexture.antiAliasing = 2;
            _renderTexture.Create();

            return _renderTexture;
        }

        private Vector2Int GetTextureSize()
        {
            var width = 0;
            var height = 0;
            var ratio = 1.0f;

            if (type == ProjectorType.Infinite)
            {
                width = textureSize;
                height = textureSize;
            }
            else if (type == ProjectorType.Local)
            {
                if (size.magnitude >= 1.0f)
                    ratio = size.x / size.y;

                if (ratio > 1.0f)
                {
                    width = textureSize;
                    height = Mathf.RoundToInt((textureSize / ratio) * 0.125f) * 8;
                    height = Mathf.Max(height, 8);
                }
                else
                {
                    width = Mathf.RoundToInt((textureSize * ratio) * 0.125f) * 8;
                    height = textureSize;
                    width = Mathf.Max(width, 8);
                }
            }

            return new(width, height);
        }
        private Bounds GetOperatingBounds()
        {
            var cube = Vector3.zero;
            if (type == ProjectorType.Infinite)
            {
                cube = new Vector3(distance, 0.0f, distance) * 2.0f;
            }
            else if (type == ProjectorType.Local)
            {
                cube = new Vector3(size.x, 0.0f, size.y);
            }

            cube.y = (lowerClip + upperClip);

            return new(transform.position + Vector3.up * (upperClip - lowerClip) * 0.5f, cube);
        }

        private void CleanupDeprecated()
        {
            var oldCamera = transform.Find("#RippleCamera");
            if (oldCamera != null)
            {
                UnityEx.SafeDestroy(oldCamera.gameObject);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            CleanupDeprecated();

            // force zero rotation
            transform.rotation = Quaternion.identity;

            Gizmos.matrix = transform.localToWorldMatrix;

            var cube = Vector3.zero;
            if (type == ProjectorType.Infinite)
            {
                cube = new Vector3(distance, 0.1f, distance) * 2.0f;
            }
            else if (type == ProjectorType.Local)
            {
                cube = new Vector3(size.x, 0.2f, size.y);
            }

            Gizmos.color = new Color(0.5f, 0.0f, 1.0f, 0.4f);
            Gizmos.DrawCube(Vector3.zero, cube);

            cube.y = (lowerClip + upperClip);

            Gizmos.color = new Color(0.0f, 1.0f, 0.5f, 0.1f);
            Gizmos.DrawCube(
                Vector3.zero + Vector3.up * (upperClip - lowerClip) * 0.5f,
                cube
                );

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(
                Vector3.zero + Vector3.up * (upperClip - lowerClip) * 0.5f,
                cube
                );
        }
#endif
    }
}
