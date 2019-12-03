using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TPS.Domain;

namespace TPS
{
    public class ParticleTargetGroup : MonoBehaviour
    {
        private ParticleTarget[] _targets = null;

        private Vector3[] _allVertices = null;
        private InitData[] _allInitData = null;
        private Vector2[] _allUV = null;

        public ParticleTarget[] ParticleTargets => _targets;
        private Texture2DArray _textureArray = null;

        public Texture2DArray TextureArray => _textureArray;

        #region ### MonoBehaviour ###
        private void Awake()
        {
            _targets = GetComponentsInChildren<ParticleTarget>();

            Debug.Log($"Targets count is {_targets.Length}");

            _allInitData = new InitData[GetCount()];

            CreateTextureArray();

            CollectAllData();
        }

        private void OnDestroy()
        {
            if (_textureArray != null)
            {
                Destroy(_textureArray);
            }
        }
        #endregion ### MonoBehaviour ###

        /// <summary>
        /// Create a texture array for all of targers.
        /// </summary>
        private void CreateTextureArray()
        {
            int count = _targets.Length;
            int width = _targets[0].Texture.width;
            int height = _targets[0].Texture.height;
            _textureArray = new Texture2DArray(width, height, count, TextureFormat.RGBA32, false, true);
            _textureArray.filterMode = FilterMode.Bilinear;
            _textureArray.wrapMode = TextureWrapMode.Repeat;

            for (int i = 0; i < _targets.Length; i++)
            {
                _textureArray.SetPixels(_targets[i].Texture.GetPixels(0), i, 0);
            }

            _textureArray.Apply();
        }

        private void CollectAllData()
        {
            int count = GetCount();

            _allVertices = new Vector3[count];
            _allUV = new Vector2[count];

            int idx = 0;

            foreach (var t in _targets)
            {
                System.Array.Copy(t.Vertices, 0, _allVertices, idx, t.Vertices.Length);
                System.Array.Copy(t.UV, 0, _allUV, idx, t.UV.Length);
                idx += t.Vertices.Length;
            }
        }

        /// <summary>
        /// Get all of target vertices.
        /// </summary>
        /// <returns></returns>
        private int GetCount()
        {
            int total = 0;

            foreach (var t in _targets)
            {
                total += t.VertexCount;
            }

            return total;
        }

        /// <summary>
        /// Get a data from all targets.
        /// </summary>
        /// <returns></returns>
        public InitData[] GetAllInitData()
        {
            int idx = 0;
            int total = _targets[0].VertexCount;

            for (int i = 0; i < _allInitData.Length; i++)
            {
                if (i >= total)
                {
                    idx++;
                    total += _targets[idx].VertexCount;
                }

                _allInitData[i].isActive = 1;
                _allInitData[i].targetPosition = _allVertices[i];
                _allInitData[i].uv = _allUV[i];
                _allInitData[i].targetId = idx;
            }

            return _allInitData;
        }
    }
}
