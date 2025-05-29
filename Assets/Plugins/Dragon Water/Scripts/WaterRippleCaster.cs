using DragonWater.Utils;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater
{
    [AddComponentMenu("Dragon Water/Ripple Caster")]
    [ExecuteAlways]
    public class WaterRippleCaster : MonoBehaviour
    {
        [SerializeField] internal Mesh mesh = null;

        MeshCollider _collider;

        #region properties
        public Mesh Mesh
        {
            get { return mesh; }
            set
            {
                mesh = value;
                UpdateCaster();
            }
        }
        #endregion


        private void Awake()
        {
            UpdateCaster();
            EnsureDynamicConfig();
            CleanupDeprecated();
        }

        private void OnEnable()
        {
            if (_collider) _collider.enabled = true;
        }
        private void OnDisable()
        {
            if (_collider) _collider.enabled = false;
        }

        private void Reset()
        {
            UpdateCaster();
            CleanupDeprecated();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (_collider != null)
            {
                _collider.convex = true;
                _collider.isTrigger = true;
            }
        }
#endif

        private void EnsureDynamicConfig()
        {
            _collider.convex = true;
            _collider.isTrigger = true;
            _collider.gameObject.layer = DragonWaterManager.Instance.Config.RippleLayer;
        }

        internal void UpdateCaster()
        {
            var collider = GetCollider();
            collider.sharedMesh = mesh;
            collider.transform.localPosition = Vector3.zero;
            collider.transform.localScale = Vector3.one;
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            UpdateCaster();
            EnsureDynamicConfig();
            CleanupDeprecated();

            var collider = GetCollider();
            var mesh = collider.sharedMesh;
            if (mesh != null)
            {
                Gizmos.color = new Color(1.0f, 0.0f, 0.5f, 0.3f);
                Gizmos.DrawMesh(
                    mesh,
                    collider.transform.position,
                    collider.transform.rotation,
                    collider.transform.lossyScale
                    );
            }
        }
#endif


        private MeshCollider GetCollider()
        {
            if (_collider != null)
            {
#if UNITY_EDITOR
                _collider.gameObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
                return _collider;
            }

            var child = transform.Find("Collider");
            if (child == null)
            {
                child = new GameObject("Collider").transform;
                child.parent = transform;
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
                child.gameObject.AddComponent<MeshCollider>();
            }

            child.gameObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            child.gameObject.hideFlags = HideFlags.None;

            _collider = child.GetComponent<MeshCollider>();
            _collider.convex = true;
            _collider.isTrigger = true;
            EnsureDynamicConfig();

            return _collider;
        }


        internal void RenderCaster(CommandBuffer cmd)
        {
            if (mesh != null)
            {
                var matrix = Matrix4x4.TRS(
                    transform.TransformPoint(Vector3.zero),
                    transform.rotation,
                    Vector3.Scale(transform.lossyScale, Vector3.one)
                );

                for (int i = 0; i<mesh.subMeshCount; i++)
                {
                    cmd.DrawMesh(mesh, matrix, DragonWaterManager.Instance.RippleCasterMaterial, i);
                }
            }
        }

        private void CleanupDeprecated()
        {
            var oldRenderer = transform.Find("Renderer");
            if (oldRenderer != null)
            {
                UnityEx.SafeDestroy(oldRenderer.gameObject);
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CONTEXT/MeshRenderer/Create Ripple Caster")]
        static void CreateFromContextMenu(UnityEditor.MenuCommand command)
        {
            var renderer = (MeshRenderer)command.context;
            var filter = renderer.GetComponent<MeshFilter>();
            var index = renderer.transform.GetSiblingIndex();

            var newObject = new GameObject(renderer.gameObject.name + " - Ripple Caster");
            newObject.transform.parent = renderer.transform.parent;
            newObject.transform.localPosition = renderer.transform.localPosition;
            newObject.transform.localRotation = renderer.transform.localRotation;
            newObject.transform.localScale = renderer.transform.localScale;
            newObject.transform.SetSiblingIndex(index + 1);

            var caster = newObject.AddComponent<WaterRippleCaster>();
            caster.Mesh = filter.sharedMesh;

            UnityEditor.Selection.activeGameObject = newObject;
        }
#endif
    }
}