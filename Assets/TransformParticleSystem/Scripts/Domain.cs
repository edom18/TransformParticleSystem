using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS
{
    public enum ComputeType
    {
        None,
        Setup,
        SetupImmediately,
        DisableAll,
    }

    public enum UpdateMethodType
    {
        None,
        Target,
        MoveTo,
        Explode,
        Orbit,
        Gravity,
    }

    public struct TransformParticle
    {
        public int isActive;
        public int targetId;
        public Vector2 uv;

        public Vector3 targetPosition;

        public float speed;
        public Vector3 position;

        public int useTexture;
        public float scale;

        public Vector4 velocity;

        public Vector3 horizontal;
    }

    public struct InitData
    {
        public int isActive;
        public Vector3 targetPosition;

        public int targetId;
        public float scale;

        public Vector4 velocity;

        public Vector2 uv;
        public Vector3 horizontal;
    }
}

