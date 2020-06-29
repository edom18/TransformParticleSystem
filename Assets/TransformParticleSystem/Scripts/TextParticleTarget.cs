using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS
{
    public class TextParticleTarget : ParticleTarget
    {
        [SerializeField] private int _assumedPixelArea = 10000;
        [SerializeField] private float _threshold = 0.3f;

        public override int VertexCount => _vertices.Length;
        public override Vector3[] Vertices => _vertices;
        public override Vector2[] UV => _uv;

        private Vector3[] _vertices = null;
        private Vector2[] _uv = null;

        private Texture2D _tex = null;

        public override void Initialize()
        {
            CalcTexelPos();
        }

        private int CalcTexelMapByAlpha(out int[,] mapData)
        {
            float area = _texture.width * _texture.height;
            float ratio = Mathf.Sqrt(area / _assumedPixelArea);

            int width = (int)(_texture.width / ratio);
            int height = (int)(_texture.height / ratio);

            RenderTexture back = RenderTexture.active;

            RenderTexture rt = RenderTexture.GetTemporary(width, height);

            RenderTexture.active = rt;
            Graphics.Blit(_texture, rt);

            _tex = new Texture2D(rt.width, rt.height);
            _tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            _tex.Apply();

            RenderTexture.active = back;

            RenderTexture.ReleaseTemporary(rt);

            mapData = new int[_tex.width, _tex.height];

            int effectiveness = 0;

            for (int y = 0; y < _tex.height; y++)
            {
                for (int x = 0; x < _tex.width; x++)
                {
                    Color col = _tex.GetPixel(x, y);
                    if (col.a >= _threshold)
                    {
                        mapData[x, y] = 1;
                        effectiveness++;
                    }
                    else
                    {
                        mapData[x, y] = 0;
                    }
                }
            }

            return effectiveness;
        }

        private void CalcTexelPos()
        {
            int count = CalcTexelMapByAlpha(out int[,] map);

            int width = map.GetLength(0);
            int height = map.GetLength(1);

            float xpix = 1f / width;
            float ypix = 1f / height;

            _vertices = new Vector3[count];
            _uv = new Vector2[count];

            int idx = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x, y] != 1)
                    {
                        continue;
                    }

                    float xp = ((x * xpix) - 0.5f);
                    float yp = (y * ypix) - 0.5f;

                    _vertices[idx] = new Vector3(xp, yp, 0);
                    _uv[idx] = new Vector2(x * xpix, y * ypix);

                    idx++;
                }
            }
        }
    }
}

