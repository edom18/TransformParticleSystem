using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS
{
    public class ParticleTarget : MonoBehaviour
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private Vector3 _scale = Vector3.one;

        protected GameObject Target => _target == null ? gameObject : _target;
        private Mesh _mesh = null;
        public Mesh Mesh
        {
            get
            {
                if (_mesh != null)
                {
                    return _mesh;
                }

                MeshFilter filter = GetComponent<MeshFilter>();
                if (filter != null)
                {
                    _mesh = filter.mesh;
                }
                else
                {
                    SkinnedMeshRenderer ren = GetComponent<SkinnedMeshRenderer>();
                    if (ren != null)
                    {
                        _mesh = ren.sharedMesh;
                    }
                    else
                    {
                        Debug.LogWarning($"This model ({name}) has no mesh.");
                    }
                }

                return _mesh;
            }
        }
        private Renderer _renderer = null;
        private Renderer Renderer => _renderer ?? (_renderer = Target.GetComponent<Renderer>());
        public virtual int VertexCount => Mesh.vertexCount;
        public virtual Vector3[] Vertices => Mesh.vertices;
        public virtual Vector2[] UV => Mesh.uv;
        public virtual Texture2D Texture => Renderer.material.mainTexture as Texture2D;
        public Matrix4x4 WorldMatrix => Target.transform.localToWorldMatrix;
        public Vector3 Scale => _scale;

        public virtual void Initialize() { }
    }
}
