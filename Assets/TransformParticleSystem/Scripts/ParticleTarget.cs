using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS
{
    public class ParticleTarget : MonoBehaviour
    {
        private Texture2D _texture = null;
        private Mesh _mesh = null;
        public Mesh Mesh
        {
            get
            {
                if (_mesh == null)
                {
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
                }

                return _mesh;
            }
        }
        private Renderer _renderer = null;
        private Renderer Renderer => _renderer ?? (_renderer = GetComponent<Renderer>());
        public int VertexCount => Mesh.vertexCount;
        public Vector3[] Vertices => Mesh.vertices;
        public Vector2[] UV => Mesh.uv;
        public Matrix4x4 WorldMatrix => transform.localToWorldMatrix;
        public Texture2D Texture
        {
            get
            {
                if (_texture == null)
                {
                    _texture = Renderer.material.mainTexture as Texture2D;

                    if (_texture == null)
                    {
                        _texture = new Texture2D(1, 1);
                    }
                }

                return _texture;
            }
        }
    }
}
