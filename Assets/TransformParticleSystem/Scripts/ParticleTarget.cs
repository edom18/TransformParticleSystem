using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS
{
    public class ParticleTarget : MonoBehaviour, IParticleTarget
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private float _minScale = 0.5f;
        [SerializeField] private float _maxScale = 1.0f;
        [SerializeField] protected Texture2D _texture = null;

        protected GameObject Target => _target == null ? gameObject : _target;
        private Mesh _mesh = null;
        public Mesh Mesh => _mesh ?? (_mesh = GetMesh());
        private Renderer _renderer = null;
        private Renderer Renderer => _renderer ?? (_renderer = Target.GetComponent<Renderer>());
        public virtual int VertexCount => Mesh.vertexCount;
        public virtual Vector3[] Vertices => Mesh.vertices;
        public virtual Vector2[] UV => Mesh.uv;
        public virtual Texture2D Texture => _texture != null ? _texture : Renderer.material.mainTexture as Texture2D;
        public Matrix4x4 WorldMatrix => Target.transform.localToWorldMatrix;
        public float MinScale => _minScale;
        public float MaxScale => _maxScale;

        private uint[] _indices = null;
        public uint[] SubGroupIndices => _indices;

        public virtual void Initialize() { }

        public void SetStartIndex(int startIdx)
        {
            _indices = new uint[VertexCount];

            for (int i = 0; i < _indices.Length; i++)
            {
                _indices[i] = (uint)(i + startIdx);
            }
        }

        private Mesh GetMesh()
        {
            MeshFilter filter = Target.GetComponent<MeshFilter>();
            if (filter != null)
            {
                return filter.mesh;
            }

            SkinnedMeshRenderer skin = Target.GetComponent<SkinnedMeshRenderer>();
            if (skin != null)
            {
                return skin.sharedMesh;
            }

            return null;
        }
    }
}
