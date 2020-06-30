using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS
{
    public class TextureParticleTarget : ParticleTarget
    {
        [SerializeField] private float _divideNum = 1;
        [SerializeField] private float _scalePerMeter = 1f;

        public override int VertexCount => _vertices.Length;
        public override Vector3[] Vertices => _vertices;
        public override Vector2[] UV => _uv;

        private Vector3[] _vertices = null;
        private Vector2[] _uv = null;

        public override void Initialize()
        {
            Texture2D tex = Texture;

            float fwidth = tex.width * _scalePerMeter;
            float fheight = tex.height * _scalePerMeter;
            fwidth /= _divideNum;
            fheight /= _divideNum;

            int width = (int)fwidth;
            int height = (int)fheight;
            int count = width * height;

            float xpix = 1f / width;
            float ypix = 1f / height;

            _vertices = new Vector3[count];
            _uv = new Vector2[count];

            float aspect = width / height;
            float invW = 1f / width;
            float invH = 1f / height;

            float halfW = width * 0.5f * xpix;
            float halfH = height * 0.5f * ypix;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = x + y * height;

                    float xp = x * xpix * aspect - halfW;
                    float yp = y * ypix - halfH;

                    _vertices[idx] = new Vector3(xp, yp, 0);
                    _uv[idx] = new Vector2(x * invW, y * invH);
                }
            }
        }
    }
}

