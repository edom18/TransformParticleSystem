using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS.Demo
{
    public class DemoSequence : MonoBehaviour
    {
        [SerializeField] private TransformParticleSystem _particleSystem = null;
        [SerializeField] private ParticleTargetGroup[] _groups = null;
        [SerializeField] private float _radius = 3f;

        private ParticleTargetGroup CurrentGroup => _groups[_index];

        private int _index = 0;

        private InitData[] _initData = null;

        #region ### MonoBehaviour ###
        private IEnumerator Start()
        {
            Initialize();

            yield return new WaitForSeconds(0.5f);

            foreach (var g in _groups)
            {
                g.Initialize(_particleSystem);
            }

            _particleSystem.SetGroup(_groups[0]);
            _particleSystem.Play();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _index = (_index + 1) % _groups.Length;
                _particleSystem.ChangeUpdateMethod(UpdateMethodType.Target);
                _particleSystem.SetGroup(CurrentGroup);
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                _particleSystem.SetOrigin(Vector3.one);
                _particleSystem.ClearMatrices();
                _particleSystem.ChangeUpdateMethod(UpdateMethodType.Orbit);
                _particleSystem.UpdateAllBuffers(ComputeType.Setup);
                _particleSystem.Dispatch(ComputeType.Setup);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                SetupExplosion();
                _particleSystem.ChangeUpdateMethod(UpdateMethodType.Explode);

                //Explosion();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                int id = Shader.PropertyToID("_OnCircle");
                _particleSystem.SetInt(id, 1);
            }
        }
        #endregion ### MonoBehaviour ###

        /// <summary>
        /// Initialize all init data.
        /// </summary>
        private void Initialize()
        {
            _initData = new InitData[_particleSystem.ParticleCount];

            for (int i = 0; i < _initData.Length; i++)
            {
                _initData[i] = new InitData();
            }
        }

        private void Explosion()
        {
            for (int i = 0; i < _initData.Length; i++)
            {
                Vector3 pos = Random.insideUnitSphere;
                pos.y = Mathf.Abs(pos.y);
                pos.Normalize();
                pos *= _radius;

                _initData[i].targetPosition = pos;
            }

            _particleSystem.ClearMatrices();
            _particleSystem.UpdateInitData(_initData);

            int id = Shader.PropertyToID("_OnCircle");
            _particleSystem.SetInt(id, 0);

            _particleSystem.ChangeUpdateMethod(UpdateMethodType.Explode);
        }

        private void SetupExplosion()
        {
            for (int i = 0; i < _initData.Length; i++)
            {
                _initData[i].isActive = 1;
                _initData[i].scale = 2.0f;
                _initData[i].horizontal = Random.onUnitSphere;
                Vector3 v = Vector3.forward;
                float w = Random.Range(1f, 3f);

                float d = Vector3.Dot(v, _initData[i].horizontal);

                if (d < 0)
                {
                    v = (v - _initData[i].horizontal);
                }
                else
                {
                    v = (v - _initData[i].horizontal);
                }

                _initData[i].velocity = new Vector4(v.x, v.y, v.z, w);
            }

            _particleSystem.SetOrigin(Vector3.one);

            _particleSystem.ClearMatrices();
            _particleSystem.DisableAllParticles();

            _particleSystem.UpdateInitData(_initData);
            _particleSystem.ResetIndices();
            _particleSystem.UpdateAllBuffers(ComputeType.Setup);
            _particleSystem.Dispatch(ComputeType.Setup);
        }

    }
}
