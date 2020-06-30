using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

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
            public int OriginID;

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
                OriginID = Shader.PropertyToID("_Origin");
            }
        }

        [SerializeField] private int _count = 10000;
        [SerializeField] private int _initDataCount = 500000;
        [SerializeField] private ComputeShader _computeShader = null;
        [SerializeField] private Mesh _particleMesh = null;
        [SerializeField] private Material _particleMat = null;
        [SerializeField] private float _baseScale = 0.01f;
        [SerializeField] private Vector3 _offset = Vector3.zero;

        private readonly int THREAD_NUM = 64;

        public int ParticleCount => _count;

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

        private int _kernelSetupParticlesImmediately = 0;
        private int _kernelSetupParticles = 0;
        private int _kernelDisable = 0;

        private int _kernelUpdateAsTarget = 0;
        private int _kernelUpdateAsMoveTo = 0;
        private int _kernelUpdateAsExplosion = 0;
        private int _kernelUpdateAsOrbit = 0;
        private int _kernelUpdateAsGravity = 0;

        private int _currentUpdateKernel = 0;

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

        #region ### Set parameters ###
        public void SetInt(int propertyID, int value)
        {
            _computeShader.SetInt(propertyID, value);
        }

        public void SetFloat(int propertyID, float value)
        {
            _computeShader.SetFloat(propertyID, value);
        }

        public void SetVector(int propertyID, Vector4 vector)
        {
            _computeShader.SetVector(propertyID, vector);
        }

        public void SetOrigin(Vector3 origin)
        {
            _computeShader.SetVector(_propertyDef.OriginID, origin);
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

        public void SetTexture(Texture texture)
        {
            _particleMat.SetTexture(_propertyDef.TexturesID, texture);
        }

        /// <summary>
        /// Set a target group.
        /// </summary>
        public void SetGroup(IParticleTargetGroup group, ParticleTargetSubGroup subGroup = null)
        {
            DisableAllParticles();

            UpdateMatrices(group);

            UpdateInitData(group.AllInitData);

            if (subGroup == null)
            {
                UpdateIndices(group.Indices);
            }
            else
            {
                UpdateIndices(subGroup.GetIndices());
            }

            UpdateAllBuffers(_kernelSetupParticles);

            Dispatch(_kernelSetupParticles);

            _particleMat.SetTexture(_propertyDef.TexturesID, group.TextureArray);
        }
        #endregion ### Set parameters ###

        /// <summary>
        /// Set compute shader calculation type.
        /// </summary>
        /// <param name="type"></param>
        public void ChangeUpdateMethod(UpdateMethodType type)
        {
            switch (type)
            {
                case UpdateMethodType.Target:
                    _currentUpdateKernel = _kernelUpdateAsTarget;
                    break;

                case UpdateMethodType.MoveTo:
                    _currentUpdateKernel = _kernelUpdateAsMoveTo;
                    break;

                case UpdateMethodType.Explode:
                    _currentUpdateKernel = _kernelUpdateAsExplosion;
                    break;

                case UpdateMethodType.Orbit:
                    _currentUpdateKernel = _kernelUpdateAsOrbit;
                    break;

                case UpdateMethodType.Gravity:
                    _currentUpdateKernel = _kernelUpdateAsGravity;
                    break;
            }
        }

        public void ChangeUpdateMethodWithClear(UpdateMethodType type, bool needsImmediately = false)
        {
            ChangeUpdateMethod(type);
            ClearMatrices();
            DisableAllParticles();

            ComputeType computeType = needsImmediately ? ComputeType.SetupImmediately : ComputeType.Setup;

            UpdateAllBuffers(computeType);
            Dispatch(computeType);
        }

        public void ResetIndices()
        {
            UpdateIndices(_defaultIndices);
        }

        /// <summary>
        /// Clear all matrix data.
        /// </summary>
        public void ClearMatrices()
        {
            for (int i = 0; i < _matrixData.Length; i++)
            {
                _matrixData[i] = Matrix4x4.identity;
            }

            _matrixBuffer.SetData(_matrixData);

            SetBuffer(_kernelSetupParticles, _propertyDef.MatrixDataID, _matrixBuffer);
        }

        public void DisableAllParticles()
        {
            SetBuffer(_kernelDisable, _propertyDef.ParticleBufferID, _particleBuffer);
            Dispatch(ComputeType.DisableAll);
        }

        #region ### Update methods ###
        /// <summary>
        /// Set init data for all particles.
        /// </summary>
        /// <param name="updateData"></param>
        public void UpdateInitData(InitData[] updateData)
        {
            if (updateData.Length > _initDataList.Length)
            {
                Debug.LogError("Init data list size is not enough to use.");
            }

            int len = updateData.Length > _initDataList.Length ? _initDataList.Length : updateData.Length;

            System.Array.Copy(updateData, _initDataList, len);

            _initDataListBuffer.SetData(_initDataList);
        }

        public void UpdateIndices(uint[] indices)
        {
            if (indices.Length >= _indices.Length)
            {
                System.Array.Copy(indices, _indices, _indices.Length);
            }
            else
            {
                int idx = 0;

                while (true)
                {
                    int len = indices.Length;

                    if (_indices.Length < idx + len)
                    {
                        len = _indices.Length - idx;
                        System.Array.Copy(indices, 0, _indices, idx, len);
                        break;
                    }

                    System.Array.Copy(indices, 0, _indices, idx, len);

                    idx += len;
                }
            }

            _indexBuffer.SetData(_indices);
        }

        /// <summary>
        /// Update matrices from a group.
        /// </summary>
        public void UpdateMatrices(IParticleTargetGroup group)
        {
            group.UpdateMatrices();

            System.Array.Copy(group.MatrixData, 0, _matrixData, 0, group.MatrixData.Length);

            _matrixBuffer.SetData(_matrixData);
        }

        public void UpdateAllBuffers(ComputeType type)
        {
            int kernelId = GetKernelID(type);

            UpdateAllBuffers(kernelId);
        }
        #endregion ### Update methods ###

        public void Dispatch(ComputeType type)
        {
            int kernelId = GetKernelID(type);

            Dispatch(kernelId);
        }

        #endregion ### Public methods

        #region ### Private methods ###
        private int GetKernelID(ComputeType type)
        {
            switch (type)
            {
                case ComputeType.Setup:
                    return _kernelSetupParticles;

                case ComputeType.SetupImmediately:
                    return _kernelSetupParticlesImmediately;

                case ComputeType.DisableAll:
                    return _kernelDisable;
            }

            return -1;
        }

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
            _kernelSetupParticles = _computeShader.FindKernel("SetupParticles");
            _kernelSetupParticlesImmediately = _computeShader.FindKernel("SetupParticlesImmediately");
            _kernelDisable = _computeShader.FindKernel("Disable");

            _kernelUpdateAsTarget = _computeShader.FindKernel("UpdateAsTarget");
            _kernelUpdateAsMoveTo = _computeShader.FindKernel("UpdateAsMoveTo");
            _kernelUpdateAsExplosion = _computeShader.FindKernel("UpdateAsExplosion");
            _kernelUpdateAsOrbit = _computeShader.FindKernel("UpdateAsOrbit");
            _kernelUpdateAsGravity = _computeShader.FindKernel("UpdateAsGravity");

            _currentUpdateKernel = _kernelUpdateAsTarget;

            CreateBuffers();

            _computeShader.SetBuffer(_kernelUpdateAsTarget, _propertyDef.ParticleBufferID, _particleBuffer);

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
                    speed = Random.Range(2f, 5f),
                    position = Vector3.zero,
                    targetPosition = Vector3.zero,
                    scale = 1f,
                };
            }

            _initDataList = new InitData[_initDataCount];
            for (int i = 0; i < _initDataList.Length; i++)
            {
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

        #region ### Private update methods ###
        /// <summary>
        /// Update porticles position.
        /// </summary>
        private void UpdateParticles()
        {
            _computeShader.SetVector(_propertyDef.OffsetID, _offset);
            _computeShader.SetFloat(_propertyDef.DeltaTimeID, Time.deltaTime);
            _computeShader.SetFloat(_propertyDef.TimeID, Time.time);

            SetBuffer(_currentUpdateKernel, _propertyDef.ParticleBufferID, _particleBuffer);

            Dispatch(_currentUpdateKernel);

            _particleMat.SetFloat(_propertyDef.BaseScaleID, _baseScale);
        }

        private void UpdateAllBuffers(int kernelId)
        {
            SetBuffer(kernelId, _propertyDef.ParticleBufferID, _particleBuffer);
            SetBuffer(kernelId, _propertyDef.InitDataListID, _initDataListBuffer);
            SetBuffer(kernelId, _propertyDef.MatrixDataID, _matrixBuffer);
            SetBuffer(kernelId, _propertyDef.IndexBufferID, _indexBuffer);
        }
        #endregion ### Private update methods ###

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

        private void Dispatch(int kernelId)
        {
            _computeShader.Dispatch(kernelId, _maxCount / THREAD_NUM, 1, 1);
        }

        /// <summary>
        /// Draw all particles.
        /// </summary>
        private void DrawParticles()
        {
            UpdateParticles();

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
