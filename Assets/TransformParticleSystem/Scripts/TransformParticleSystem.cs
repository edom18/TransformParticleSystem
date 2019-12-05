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
            public int RadiusID;
            public int OffsetID;

            public PropertyDef()
            {

                ParticleBufferID = Shader.PropertyToID("_Particles");
                InitDataListID = Shader.PropertyToID("_InitDataList");
                DeltaTimeID = Shader.PropertyToID("_DeltaTime");
                TimeID = Shader.PropertyToID("_Time");
                MatrixDataID = Shader.PropertyToID("_MatrixData");
                TexturesID = Shader.PropertyToID("_Textures");
                BaseScaleID = Shader.PropertyToID("_BaseScale");
                RadiusID = Shader.PropertyToID("_Radius");
                OffsetID = Shader.PropertyToID("_Offset");
            }
        }

        [SerializeField] private int _count = 10000;
        [SerializeField] private ComputeShader _computeShader = null;
        [SerializeField] private Mesh _particleMesh = null;
        [SerializeField] private Material _particleMat = null;
        [SerializeField] private float _baseScale = 0.01f;
        [SerializeField] private float _radius = 1f;
        [SerializeField] private Vector3 _offset = Vector3.zero;

        private readonly int THREAD_NUM = 64;

        public int ParticleCount => _count;
        public ComputeShader ComputeShader => _computeShader;

        private ComputeBuffer _particleBuffer = null;
        private ComputeBuffer _initDataListBuffer = null;
        private ComputeBuffer _matrixBuffer = null;
        private ComputeBuffer _argsBuffer = null;
        private uint[] _argsData = new uint[] { 0, 0, 0, 0, 0, };
        private Matrix4x4[] _matrixData = new Matrix4x4[30];
        private Particle[] _particleData = null;
        private InitData[] _initDataList = null;

        private PropertyDef _propertyDef = default;

        private int _maxCount = 0;

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

        /// <summary>
        /// Release all buffers.
        /// </summary>
        private void ReleaseBuffers()
        {
            _particleBuffer?.Release();
            _argsBuffer?.Release();
            _matrixBuffer?.Release();
            _initDataListBuffer?.Release();
        }

        /// <summary>
        /// Initialize a particle system.
        /// </summary>
        private void Initialize()
        {
            _propertyDef = new PropertyDef();

            // Get kernel ids.
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

            _particleData = new Particle[_maxCount];
            for (int i = 0; i < _particleData.Length; i++)
            {
                _particleData[i] = new Particle
                {
                    id = i,
                    isActive = 0,
                    speed = Random.Range(2f, 5f),
                    position = Vector3.zero,
                    targetPosition = Vector3.zero,
                };
            }

            _initDataList = new InitData[_maxCount];
            for (int i = 0; i < _initDataList.Length; i++)
            {
                _initDataList[i].isActive = 0;
                _initDataList[i].targetPosition = Vector3.zero;
            }

            for (int i = 0; i < _matrixData.Length; i++)
            {
                _matrixData[i] = Matrix4x4.identity;
            }

            // Setup buffers.
            _argsBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)) * _argsData.Length, ComputeBufferType.IndirectArguments);
            _particleBuffer = new ComputeBuffer(_maxCount, Marshal.SizeOf(typeof(Particle)));
            _particleBuffer.SetData(_particleData);
            _initDataListBuffer = new ComputeBuffer(_maxCount, Marshal.SizeOf(typeof(InitData)));

            _matrixBuffer = new ComputeBuffer(_matrixData.Length, Marshal.SizeOf(typeof(Matrix4x4)));

            _argsData[0] = _particleMesh.GetIndexCount(0);
            _argsData[1] = (uint)_maxCount;
            _argsData[2] = _particleMesh.GetIndexStart(0);
            _argsData[3] = _particleMesh.GetBaseVertex(0);
            _argsBuffer.SetData(_argsData);
        }

        public void Play()
        {
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
        }

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

        /// <summary>
        /// Set a target group.
        /// </summary>
        public void SetGroup(ParticleTargetGroup group)
        {
            UpdateMatrices(group);
            UpdateData(group);

            _particleMat.SetTexture(_propertyDef.TexturesID, group.TextureArray);
        }

        public void ClearMatrices()
        {
            for (int i = 0; i < _matrixData.Length; i++)
            {
                _matrixData[i] = Matrix4x4.identity;
            }

            _matrixBuffer.SetData(_matrixData);
            _computeShader.SetBuffer(_kernelSetup, _propertyDef.MatrixDataID, _matrixBuffer);
        }

        /// <summary>
        /// Update matrices from a group.
        /// </summary>
        private void UpdateMatrices(ParticleTargetGroup group)
        {
            for (int i = 0; i < group.ParticleTargets.Length; i++)
            {
                _matrixData[i] = group.ParticleTargets[i].WorldMatrix;
            }

            _matrixBuffer.SetData(_matrixData);
            _computeShader.SetBuffer(_kernelSetup, _propertyDef.MatrixDataID, _matrixBuffer);
        }

        /// <summary>
        /// Update the data from a group.
        /// </summary>
        /// <param name="group"></param>
        private void UpdateData(ParticleTargetGroup group)
        {
            InitData[] updateData = group.GetAllInitData();
            SetInitData(updateData);
        }

        /// <summary>
        /// Set init data for all particles.
        /// </summary>
        /// <param name="updateData"></param>
        public void SetInitData(InitData[] updateData)
        {
            for (int i = 0; i < _initDataList.Length; i++)
            {
                if (i < updateData.Length)
                {
                    _initDataList[i].Copy(updateData[i]);
                }
                else
                {
                    _initDataList[i].isActive = 0;
                }
            }

            _initDataListBuffer.SetData(_initDataList);

            _computeShader.SetBuffer(_kernelSetup, _propertyDef.ParticleBufferID, _particleBuffer);
            _computeShader.SetBuffer(_kernelSetup, _propertyDef.InitDataListID, _initDataListBuffer);
            _computeShader.Dispatch(_kernelSetup, _maxCount / THREAD_NUM, 1, 1);
        }

        /// <summary>
        /// Update porticles position.
        /// </summary>
        private void UpdateParticles()
        {
            _computeShader.SetFloat(_propertyDef.DeltaTimeID, Time.deltaTime);
            _computeShader.SetFloat(_propertyDef.TimeID, Time.time);
            _computeShader.SetFloat(_propertyDef.RadiusID, _radius);
            _computeShader.SetVector(_propertyDef.OffsetID, _offset);

            _computeShader.SetBuffer(_currentKernel, _propertyDef.ParticleBufferID, _particleBuffer);
            _computeShader.Dispatch(_currentKernel, _maxCount / THREAD_NUM, 1, 1);
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
                new Bounds(Vector3.zero, Vector3.one * 32f),
                _argsBuffer,
                0,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false,
                gameObject.layer
            );
        }
    }
}
