using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS.Domain
{
    public enum ComputeType
    {
        None,
        Target,
        Explode,
    }

    public struct Particle
    {
        public int id;
        public int targetId;
        public Vector2 uv;

        public int isActive;
        public Vector3 targetPosition;

        public float speed;
        public Vector3 position;

        public Vector4 color;

        public int useTexture;
    }

    public struct InitData
    {
        public int isActive;
        public Vector3 targetPosition;
        public int targetId;
        public Vector2 uv;

        public void Copy(InitData src)
        {
            isActive = src.isActive;
            targetPosition = src.targetPosition;
            targetId = src.targetId;
            uv = src.uv;
        }
    }
}
