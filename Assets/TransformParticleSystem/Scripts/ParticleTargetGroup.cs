using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TPS.Domain;

namespace TPS
{
    [System.Serializable]
    public class ParticleTargetSubGroup
    {
#if UNITY_EDITOR
        public string Label = "";
#endif
        public ParticleTarget[] Targets = null;

        private int _allCount = -1;

        public int GetCount()
        {
            if (_allCount != -1)
            {
                return _allCount;
            }

            int total = 0;

            foreach (var t in Targets)
            {
                total += t.VertexCount;
            }

            _allCount = total;

            return _allCount;
        }
    }

    [System.Serializable]
    public struct IndexData
    {
        public int ID;
        public uint[] Indices;
    }

    public class ParticleTargetGroup : MonoBehaviour
    {
        [SerializeField] private bool _autoDetection = true;
        [SerializeField] private ParticleTarget[] _targets = null;
        [SerializeField] private ParticleTargetSubGroup[] _subGroups = null;

        public ParticleTarget[] Targets => _targets;

        private Vector3[] _allVertices = null;
        private InitData[] _allInitData = null;
        private Vector2[] _allUV = null;
        private Matrix4x4[] _matrixData = null;
        private uint[] _indices = null;
        private IndexData[] _indexDataList = null;

        private Texture2DArray _textureArray = null;

        private bool HasSubGroup => _subGroups.Length != 0;

        private int _allCount = -1;

        public Texture2DArray TextureArray => _textureArray;
        public Matrix4x4[] MatrixData => _matrixData;
        public InitData[] AllInitData => _allInitData;

        #region ### MonoBehaviour ###
        private void Awake()
        {
            if (_autoDetection)
            {
                _targets = GetComponentsInChildren<ParticleTarget>();
            }

            foreach (var t in _targets)
            {
                t.Initialize();
            }

            Debug.Log($"Targets count is {_targets.Length}");
            Debug.Log($"All data count is {GetCount()}");

            _allInitData = new InitData[GetCount()];
            _indexDataList = new IndexData[_subGroups.Length];

            CreateTextureArray();

            CollectAllData();

            CreateAllInitData();
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
            _matrixData = new Matrix4x4[_targets.Length];

            int idx = 0;

            for (int i = 0; i < _targets.Length; i++)
            {
                ParticleTarget t = _targets[i];

                System.Array.Copy(t.Vertices, 0, _allVertices, idx, t.Vertices.Length);
                System.Array.Copy(t.UV, 0, _allUV, idx, t.UV.Length);

                _matrixData[i] = t.WorldMatrix;

                idx += t.Vertices.Length;
            }

            if (HasSubGroup)
            {
                for (int i = 0; i < _subGroups.Length; i++)
                {
                    _indexDataList[i].ID = i;
                    _indexDataList[i].Indices = new uint[_subGroups[i].GetCount()];

                    int subIndex = 0;

                    foreach (var st in _subGroups[i].Targets)
                    {
                        if (!TryGetSameTarget(st, out int startIndex))
                        {
                            Debug.LogWarning($"A target is not found. {st.name}");
                            continue;
                        }

                        for (int j = 0; j < st.VertexCount; j++)
                        {
                            _indexDataList[i].Indices[subIndex] = (uint)(startIndex + j);
                            subIndex++;
                        }
                    }
                }
            }
            else
            {
                _indices = new uint[GetCount()];

                for (int i = 0; i < _indices.Length; i++)
                {
                    _indices[i] = (uint)i;
                }
            }
        }

        private bool TryGetSameTarget(ParticleTarget target, out int startIndex)
        {
            startIndex = 0;

            foreach (var t in _targets)
            {
                if (t == target)
                {
                    return true;
                }

                startIndex += t.VertexCount;
            }

            return false;
        }

        /// <summary>
        /// Get all of target vertices.
        /// </summary>
        /// <returns></returns>
        private int GetCount()
        {
            if (_allCount != -1)
            {
                return _allCount;
            }

            int total = 0;

            foreach (var t in _targets)
            {
                total += t.VertexCount;
            }

            _allCount = total;

            return _allCount;
        }

        /// <summary>
        /// Get a data from all targets.
        /// </summary>
        /// <returns></returns>
        private void CreateAllInitData()
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
                _allInitData[i].scale = _targets[idx].Scale;
            }
        }

        public ParticleTarget[] GetTargetsAt(int idx)
        {
            return _subGroups[idx].Targets;
        }

        public uint[] GetIndicesAt(int idx)
        {
            if (!HasSubGroup)
            {
                return _indices;
            }

            if (idx >= _indexDataList.Length)
            {
                Debug.LogError("idx is out of range.");
                return null;
            }

            return _indexDataList[idx].Indices;
        }

        /// <summary>
        /// Update matrices from a group.
        /// </summary>
        public void UpdateMatrices()
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                _matrixData[i] = _targets[i].WorldMatrix;
            }
        }
    }
}
