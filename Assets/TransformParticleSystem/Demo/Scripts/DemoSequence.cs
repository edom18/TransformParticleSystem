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
            if (Input.GetKeyDown(KeyCode.N))
            {
                Next();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                Gravity();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Explosion();
            }
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 150, 30), "Next (N)"))
            {
                Next();
            }

            if (GUI.Button(new Rect(10, 50, 150, 30), "Explosion (E)"))
            {
                Explosion();
            }

            if (GUI.Button(new Rect(10, 90, 150, 30), "Gravity (G)"))
            {
                Gravity();
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

        private void Next()
        {
            _index = (_index + 1) % _groups.Length;
            _particleSystem.ChangeUpdateMethod(UpdateMethodType.Target);
            _particleSystem.SetGroup(CurrentGroup);
        }

        private void Gravity()
        {
            _particleSystem.SetOrigin(Vector3.one);
            _particleSystem.ChangeUpdateMethodWithClear(UpdateMethodType.Gravity);
        }

        private void Explosion()
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
            _particleSystem.UpdateInitData(_initData);
            _particleSystem.ChangeUpdateMethodWithClear(UpdateMethodType.Explode);
        }
    }
}

