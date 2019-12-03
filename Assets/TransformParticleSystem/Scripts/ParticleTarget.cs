using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS
{
    public class ParticleTarget : MonoBehaviour
    {
        private Mesh _mesh = null;
        public Mesh Mesh => _mesh ?? (_mesh = GetComponent<MeshFilter>().mesh);
        private Renderer _renderer = null;
        private Renderer Renderer => _renderer ?? (_renderer = GetComponent<Renderer>());
        public int VertexCount => Mesh.vertexCount;
        public Vector3[] Vertices => Mesh.vertices;
        public Vector2[] UV => Mesh.uv;
        public Matrix4x4 WorldMatrix => transform.localToWorldMatrix;
        public Texture2D Texture => Renderer.material.mainTexture as Texture2D;
    }
}
