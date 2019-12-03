using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TPS.Domain;

namespace TPS.Demo
{
    public class OpeningSequence : MonoBehaviour
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

            _particleSystem.Play();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _index = (_index + 1) % _groups.Length;
                _particleSystem.SetType(ComputeType.Target);
                _particleSystem.SetGroup(CurrentGroup);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Explosion();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                _particleSystem.ComputeShader.SetInt("_OnCircle", 1);
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
            _particleSystem.SetInitData(_initData);

            _particleSystem.ComputeShader.SetInt("_OnCircle", 0);

            _particleSystem.SetType(ComputeType.Explode);
        }
    }
}
