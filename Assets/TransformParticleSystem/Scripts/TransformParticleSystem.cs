using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using TPS.Domain;

namespace TPS
{
    public class TransformParticleSystem : MonoBehaviour
    {
        private class PropertyDef
        {
            public int ParticleBufferID;
            public int InitDataListID;
            public int DeltaTimeID;
            public int TimeID;
            public int MatrixDataID;
            public int TexturesID;
            public int BaseScaleID;
            public int OffsetID;
            public int IndexBufferID;

            public PropertyDef()
            {

                ParticleBufferID = Shader.PropertyToID("_Particles");
                InitDataListID = Shader.PropertyToID("_InitDataList");
                DeltaTimeID = Shader.PropertyToID("_DeltaTime");
                TimeID = Shader.PropertyToID("_Time");
                MatrixDataID = Shader.PropertyToID("_MatrixData");
                TexturesID = Shader.PropertyToID("_Textures");
                BaseScaleID = Shader.PropertyToID("_BaseScale");
                OffsetID = Shader.PropertyToID("_Offset");
                IndexBufferID = Shader.PropertyToID("_IndexBuffer");
            }
        }

        [SerializeField] private int _count = 10000;
        [SerializeField] private int _capacityRatio = 2;
        [SerializeField] private ComputeShader _computeShader = null;
        [SerializeField] private Mesh _particleMesh = null;
        [SerializeField] private Material _particleMat = null;
        [SerializeField] private float _baseScale = 0.01f;
        [SerializeField] private Vector3 _offset = Vector3.zero;

        private readonly int THREAD_NUM = 64;

        public int ParticleCount => _count;

        public ComputeShader ComputeShader => _computeShader;

        private ComputeBuffer _particleBuffer = null;
        private ComputeBuffer _initDataListBuffer = null;
        private ComputeBuffer _matrixBuffer = null;
        private ComputeBuffer _argsBuffer = null;
        private ComputeBuffer _indexBuffer = null;

        private uint[] _argsData = new uint[] { 0, 0, 0, 0, 0, };
        private uint[] _indices = null;
        private uint[] _defaultIndices = null;
        private Matrix4x4[] _matrixData = new Matrix4x4[30];
        private TransformParticle[] _particleData = null;
        private InitData[] _initDataList = null;

        private PropertyDef _propertyDef = null;

        private int _maxCount = 0;

        private int _kernelInit = 0;
        private int _kernelUpdate = 0;
        private int _kernelSetup = 0;
        private int _kernelExplosion = 0;
        private int _currentKernel = 0;

        private bool _isRunning = false;

        #region ### MonoBehaviour ###
        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (_isRunning)
            {
                UpdateParticles();
                DrawParticles();
            }
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }
        #endregion ### MonoBehaviour ###

        #region ### Public methods
        /// <summary>
        /// Play this particle system.
        /// </summary>
        public void Play()
        {
            _isRunning = true;
        }

        /// <summary>
        /// Stop this particle system.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Set compute shader calculation type.
        /// </summary>
        /// <param name="type"></param>
        public void SetType(ComputeType type)
        {
            switch (type)
            {
                case ComputeType.Target:
                    _currentKernel = _kernelUpdate;
                    break;

                case ComputeType.Explode:
                    _currentKernel = _kernelExplosion;
                    break;

                default:
                    _isRunning = false;
                    break;
            }
        }

        public void SetInitData(InitData[] initData)
        {
            ClearMatrices();

            UpdateInitData(initData);
            UpdateIndices(_defaultIndices);
            UpdateBuffers(_kernelSetup);

            Dispatch(_kernelSetup);
        }

        /// <summary>
        /// Set a target group.
        /// </summary>
        public void SetGroup(ParticleTargetGroup group, int subGroupID = 0)
        {
            UpdateMatrices(group);

            UpdateInitData(group.AllInitData);
            UpdateIndices(group.GetIndicesAt(subGroupID));
            UpdateBuffers(_kernelSetup);

            Dispatch(_kernelSetup);

            _particleMat.SetTexture(_propertyDef.TexturesID, group.TextureArray);
        }

        /// <summary>
        /// Set a group as immediately.
        /// </summary>
        /// <param name="group">Target group</param>
        public void SetGroupImmediately(ParticleTargetGroup group, int subGroupID = 0)
        {
            UpdateMatrices(group);

            UpdateInitData(group.AllInitData);
            UpdateIndices(group.GetIndicesAt(subGroupID));
            UpdateBuffers(_kernelInit);

            Dispatch(_kernelInit);

            _particleMat.SetTexture(_propertyDef.TexturesID, group.TextureArray);
        }

        /// <summary>
        /// Set an offset to the particles.
        /// </summary>
        /// <param name="offset"></param>
        public void SetOffset(Vector3 offset)
        {
            _offset = offset;
            _computeShader.SetVector(_propertyDef.OffsetID, _offset);
        }
        #endregion ### Public methods

        #region ### Private methods ###
        /// <summary>
        /// Release all buffers.
        /// </summary>
        private void ReleaseBuffers()
        {
            _particleBuffer?.Release();
            _argsBuffer?.Release();
            _matrixBuffer?.Release();
            _initDataListBuffer?.Release();
            _indexBuffer?.Release();
        }

        /// <summary>
        /// Initialize a particle system.
        /// </summary>
        private void Initialize()
        {
            _propertyDef = new PropertyDef();

            // Get kernel ids.
            _kernelInit = _computeShader.FindKernel("Init");
            _kernelUpdate = _computeShader.FindKernel("Update");
            _kernelSetup = _computeShader.FindKernel("Setup");
            _kernelExplosion = _computeShader.FindKernel("Explosion");

            _currentKernel = _kernelUpdate;

            CreateBuffers();

            _computeShader.SetBuffer(_kernelUpdate, _propertyDef.ParticleBufferID, _particleBuffer);

            _particleMat.SetBuffer(_propertyDef.ParticleBufferID, _particleBuffer);
        }

        /// <summary>
        /// Create all buffers.
        /// </summary>
        private void CreateBuffers()
        {
            // Alignment count by THREAD_NUM
            _maxCount = (_count / THREAD_NUM) * THREAD_NUM;

            Debug.Log($"Will create {_maxCount} particles.");

            _particleData = new TransformParticle[_maxCount];
            for (int i = 0; i < _particleData.Length; i++)
            {
                _particleData[i] = new TransformParticle
                {
                    id = i,
                    isActive = 0,
                    speed = Random.Range(2f, 5f),
                    position = Vector3.zero,
                    targetPosition = Vector3.zero,
                    scale = Vector3.one,
                };
            }

            _initDataList = new InitData[_maxCount * _capacityRatio];
            for (int i = 0; i < _initDataList.Length; i++)
            {
                _initDataList[i].isActive = 0;
                _initDataList[i].targetPosition = Vector3.zero;
            }

            _indices = new uint[_maxCount];

            _defaultIndices = new uint[_maxCount];
            for (int i = 0; i < _defaultIndices.Length; i++)
            {
                _defaultIndices[i] = (uint)i;
            }

            for (int i = 0; i < _matrixData.Length; i++)
            {
                _matrixData[i] = Matrix4x4.identity;
            }

            // Setup buffers.
            _argsBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)) * _argsData.Length, ComputeBufferType.IndirectArguments);
            _particleBuffer = new ComputeBuffer(_maxCount, Marshal.SizeOf(typeof(TransformParticle)));
            _particleBuffer.SetData(_particleData);
            _initDataListBuffer = new ComputeBuffer(_initDataList.Length, Marshal.SizeOf(typeof(InitData)));

            _matrixBuffer = new ComputeBuffer(_matrixData.Length, Marshal.SizeOf(typeof(Matrix4x4)));

            _indexBuffer = new ComputeBuffer(_indices.Length, Marshal.SizeOf(typeof(uint)));
            _indexBuffer.SetData(_defaultIndices);

            _argsData[0] = _particleMesh.GetIndexCount(0);
            _argsData[1] = (uint)_maxCount;
            _argsData[2] = _particleMesh.GetIndexStart(0);
            _argsData[3] = _particleMesh.GetBaseVertex(0);
            _argsBuffer.SetData(_argsData);
        }

        /// <summary>
        /// Clear all matrix data.
        /// </summary>
        private void ClearMatrices()
        {
            for (int i = 0; i < _matrixData.Length; i++)
            {
                _matrixData[i] = Matrix4x4.identity;
            }

            _matrixBuffer.SetData(_matrixData);

            SetBuffer(_kernelSetup, _propertyDef.MatrixDataID, _matrixBuffer);
        }

        /// <summary>
        /// Set buffer to the compute shader.
        /// </summary>
        /// <param name="kernelId">Target kernel ID</param>
        /// <param name="propertyId">Target property ID</param>
        /// <param name="buffer">Target buffer</param>
        private void SetBuffer(int kernelId, int propertyId, ComputeBuffer buffer)
        {
            _computeShader.SetBuffer(kernelId, propertyId, buffer);
        }

        /// <summary>
        /// Dispatch a kernel by KernelID.
        /// </summary>
        /// <param name="kernelId">Target kernel ID</param>
        private void Dispatch(int kernelId)
        {
            _computeShader.Dispatch(kernelId, _maxCount / THREAD_NUM, 1, 1);
        }

        /// <summary>
        /// Set init data for all particles.
        /// </summary>
        /// <param name="updateData"></param>
        private void UpdateInitData(InitData[] updateData)
        {
            for (int i = 0; i < _initDataList.Length; i++)
            {
                int idx = i % updateData.Length;
                _initDataList[i].Copy(updateData[idx]);
            }

            _initDataListBuffer.SetData(_initDataList);
        }

        private void UpdateIndices(uint[] indices)
        {
            for (int i = 0; i < _indices.Length; i++)
            {
                int idx = i % indices.Length;
                _indices[i] = indices[idx];
            }

            _indexBuffer.SetData(_indices);
        }

        /// <summary>
        /// Update matrices from a group.
        /// </summary>
        private void UpdateMatrices(ParticleTargetGroup group)
        {
            group.UpdateMatrices();

            System.Array.Copy(group.MatrixData, 0, _matrixData, 0, group.MatrixData.Length);

            _matrixBuffer.SetData(_matrixData);
        }

        /// <summary>
        /// Update all buffers to the kernel.
        /// </summary>
        /// <param name="kernelId">Target kernel ID</param>
        private void UpdateBuffers(int kernelId)
        {
            SetBuffer(kernelId, _propertyDef.ParticleBufferID, _particleBuffer);
            SetBuffer(kernelId, _propertyDef.InitDataListID, _initDataListBuffer);
            SetBuffer(kernelId, _propertyDef.MatrixDataID, _matrixBuffer);
            SetBuffer(kernelId, _propertyDef.IndexBufferID, _indexBuffer);
        }

        /// <summary>
        /// Update porticles position.
        /// </summary>
        private void UpdateParticles()
        {
            _computeShader.SetVector(_propertyDef.OffsetID, _offset);
            _computeShader.SetFloat(_propertyDef.DeltaTimeID, Time.deltaTime);
            _computeShader.SetFloat(_propertyDef.TimeID, Time.time);

            SetBuffer(_currentKernel, _propertyDef.ParticleBufferID, _particleBuffer);

            Dispatch(_currentKernel);
        }

        /// <summary>
        /// Draw all particles.
        /// </summary>
        private void DrawParticles()
        {
            _particleMat.SetFloat(_propertyDef.BaseScaleID, _baseScale);

            Graphics.DrawMeshInstancedIndirect(
                _particleMesh,
                0, // submesh index
                _particleMat,
                new Bounds(_offset, Vector3.one * 32f),
                _argsBuffer,
                0,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false,
                gameObject.layer
            );
        }
        #endregion ### Private methods ###
    }
}
